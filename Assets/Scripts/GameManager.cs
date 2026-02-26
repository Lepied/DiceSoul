using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 골드 출처
public enum GoldSource
{
    Combat,   // 전투 중
    Bonus,    // 보스/이벤트 보너스
    Event     // 유물 효과 등 이벤트
}

[DefaultExecutionOrder(-50)] //현재 WaveGenerator(-100) 다음, StageManager(0) 이전
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 상태")]
    public int PlayerHealth;
    public int MaxPlayerHealth = 100;
    public int CurrentGold { get; private set; }
    public int CurrentShield { get; private set; } //임시 체력

    //메타강화
    private bool firstHitThisWave = false; //한대 막기
    private bool hasRevived = false; //수호천사

    [Header("사운드")]
    [Tooltip("40 미만으로 맞았을 때")]
    public SoundConfig playerHitLightSound;

    [Tooltip("40 이상으로 맞았을 때")]
    public SoundConfig playerHitHeavySound;

    [Tooltip("실드로 피해를 막았을 때")]
    public SoundConfig playerShieldHitSound;

    [Header("덱 정보")]
    public List<string> playerDiceDeck = new List<string>();
    public List<Relic> activeRelics = new List<Relic>();

    [Header("게임 밸런스")]
    public int bonusPerRollRemaining = 5;
    public int wavesPerZone = 5;
    public int maxDiceCount = 15;
    public int minDiceCount = 3;

    [Header("런(Run) 진행 상태")]
    public int CurrentZone = 1;
    public int CurrentWave = 1;

    [Header("잡화점 버프 상태")]
    public int buffDuration = 0; // 남은 지속 웨이브 수
    public int buffDamageValue = 0;
    public int buffShieldValue = 0;
    public int buffRerollValue = 0;
    public bool hasInsurance = false; // 보험 가입 여부

    [Header("영구 재화")]
    public string metaCurrencySaveKey = "MetaCurrency";

    private string selectedDeckKey = "SelectedDeck";

    [Header("튜토리얼")]
    public bool isTutorialMode = false;
    private bool tutorialCompleted = false;
    public bool hasInitializedRun = false;
    public List<MetaUpgradeData> allMetaUpgrades; //메타 업그레이드데이터 리스트

    public List<string> nextZoneBuffs = new List<string>();

    [Header("런 통계 (Run Statistics)")]
    public float playTime = 0f; // 플레이 시간 (초)
    public int totalKills = 0; // 처치한 적 총 수
    public int totalGoldEarned = 0; // 획득한 총 골드
    public int maxDamageDealt = 0; // 최고 데미지
    public int maxChainCount = 0; // 최장 연쇄
    public Dictionary<string, int> handUsageCount = new Dictionary<string, int>(); // 족보 사용 횟수
    public int bossesDefeated = 0; // 처치한 보스 수
    public int perfectWaves = 0; // 무피해 웨이브 수
    private bool wasDamagedThisWave = false; // 이번 웨이브에 피해 받았는지

    // 적 기믹 피해 누적
    private int accumulatedMechanicDamage = 0;
    private int accumulatedShieldDamage = 0;
    private bool hasPendingFeedback = false;
    
    private WaveContext _cachedWaveContext = new WaveContext();
    private GoldContext _cachedGoldContext = new GoldContext();
    private DamageContext _cachedDamageContext = new DamageContext();
    private DeathContext _cachedDeathContext = new DeathContext();
    private HealContext _cachedHealContext = new HealContext();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 튜토리얼 완료 여부 확인
        tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        if (SaveManager.shouldLoadSave)
        {
            LoadRun();
            SaveManager.shouldLoadSave = false; // 플래그 리셋
        }
        else
        {
            // 튜토리얼 미완료 시 튜토리얼 모드로 시작
            if (!tutorialCompleted)
            {
                StartTutorialMode();
            }
            else
            {
                // 메타강화 - 시작 유물 선택 후 게임 시작
                float startRelicLevel = GetTotalMetaBonus(MetaEffectType.StartingRelicChoice);
                if (startRelicLevel > 0 && RelicDB.Instance != null)
                {
                    ShowStartingRelicChoice();
                }
                else
                {
                    StartNewRun();
                }
            }
        }
    }

    void Update()
    {
        // 플레이 시간 추적 (게임이 진행 중일 때만)
        if (PlayerHealth > 0)
        {
            playTime += Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        if (hasPendingFeedback)
        {
            // 쉴드 피해가 있었으면 쉴드 사운드
            if (accumulatedShieldDamage > 0)
            {
                SoundManager.Instance.PlaySoundConfig(playerShieldHitSound);
            }

            // 체력 피해가 있었으면
            if (accumulatedMechanicDamage > 0)
            {
                bool isHeavyHit = accumulatedMechanicDamage >= 40;

                // 사운드
                SoundConfig soundToPlay = isHeavyHit ? playerHitHeavySound : playerHitLightSound;
                if (soundToPlay != null)
                {
                    SoundManager.Instance.PlaySoundConfig(soundToPlay);
                }

                // 카메라 흔들림
                float shakeIntensity = isHeavyHit ? 0.4f : 0.2f;
                float shakeDuration = isHeavyHit ? 0.3f : 0.15f;
                CameraShake.Instance.Shake(shakeIntensity, shakeDuration);

                // 비네팅 효과
                PlayerHitVignette.Instance.PlayHitEffect(isHeavyHit);

            }

            // 누적 변수 리셋
            accumulatedMechanicDamage = 0;
            accumulatedShieldDamage = 0;
            hasPendingFeedback = false;
        }
    }

    public void SaveCurrentRun()
    {
        GameData data = new GameData();
        data.currentHealth = PlayerHealth;
        data.maxHealth = MaxPlayerHealth;
        data.currentGold = CurrentGold;
        data.currentZone = CurrentZone;
        data.currentWave = CurrentWave;

        data.myDeck = new List<string>(playerDiceDeck);

        // 유물은 ID만 추출해서 저장
        data.myRelicIDs = new List<string>();
        foreach (var relic in activeRelics)
        {
            data.myRelicIDs.Add(relic.RelicID);
        }

        // 런 존 순서 저장
        if (WaveGenerator.Instance != null)
        {
            data.runZoneOrder = WaveGenerator.Instance.GetRunZoneOrderAsStrings();
        }

        // 런 통계 저장
        data.playTime = playTime;
        data.totalKills = totalKills;
        data.totalGoldEarned = totalGoldEarned;
        data.maxDamageDealt = maxDamageDealt;
        data.maxChainCount = maxChainCount;
        data.bossesDefeated = bossesDefeated;
        data.perfectWaves = perfectWaves;
        data.handUsageKeys.Clear();
        data.handUsageValues.Clear();
        foreach (var kvp in handUsageCount)
        {
            data.handUsageKeys.Add(kvp.Key);
            data.handUsageValues.Add(kvp.Value);
        }

        // 상점 페이즈 여부
        bool shopOpen = (UIManager.Instance != null && UIManager.Instance.IsShopOpen());
        data.isInMaintenancePhase = shopOpen;

        SaveManager.Instance.SaveGame(data);
    }

    //불러오기 기능 
    private void LoadRun()
    {
        GameData data = SaveManager.Instance.LoadGame();
        if (data == null)
        {
            StartNewRun();
            return;
        }

        hasInitializedRun = true;

        // 기본 스탯 복구
        PlayerHealth = data.currentHealth;
        MaxPlayerHealth = data.maxHealth;
        CurrentGold = data.currentGold;
        CurrentZone = data.currentZone;
        CurrentWave = data.currentWave;

        //덱 복구
        playerDiceDeck = new List<string>(data.myDeck);

        // 유물 복구 (ID로 찾아서 다시 생성)
        activeRelics.Clear();
        if (RelicDB.Instance != null)
        {
            foreach (string id in data.myRelicIDs)
            {
                Relic relicData = RelicDB.Instance.GetRelicByID(id);
                if (relicData != null)
                {
                    activeRelics.Add(relicData);
                }
            }
        }
        if (RelicEffectHandler.Instance != null)
        {
            RelicEffectHandler.Instance.ResetForNewRun();
        }

        if (WaveGenerator.Instance != null)
        {
            if (data.runZoneOrder != null && data.runZoneOrder.Count > 0)
            {
                // 저장된 존 순서 복원
                WaveGenerator.Instance.RestoreRunZoneOrder(data.runZoneOrder);
            }
            else
            {
                // 이전 버전 저장 파일 호환성
                WaveGenerator.Instance.BuildRunZoneOrder();
            }
        }

        // 런 통계 복원
        playTime = data.playTime;
        totalKills = data.totalKills;
        totalGoldEarned = data.totalGoldEarned;
        maxDamageDealt = data.maxDamageDealt;
        maxChainCount = data.maxChainCount;
        bossesDefeated = data.bossesDefeated;
        perfectWaves = data.perfectWaves;

        // List → Dictionary 변환
        handUsageCount.Clear();
        if (data.handUsageKeys != null && data.handUsageValues != null)
        {
            for (int i = 0; i < data.handUsageKeys.Count && i < data.handUsageValues.Count; i++)
            {
                handUsageCount[data.handUsageKeys[i]] = data.handUsageValues[i];
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            UIManager.Instance.UpdateGold(CurrentGold);
            UIManager.Instance.UpdateRelicPanel(activeRelics);
            UIManager.Instance.UpdateWaveText(CurrentZone, CurrentWave);
        }

        // 상점 페이즈 복원 여부 확인
        if (data.isInMaintenancePhase)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.StartMaintenancePhase();
            }
        }
        else
        {
            if (data.currentWave > wavesPerZone)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.StartMaintenancePhase();
                }
            }
            else
            {
                if (StageManager.Instance != null)
                {
                    StageManager.Instance.PrepareNextWave();
                }
            }
        }

    }

    public void StartNewRun()
    {
        hasInitializedRun = true;

        CurrentGold = 0;
        CurrentZone = 1;
        CurrentWave = 1;
        MaxPlayerHealth = 100;
        PlayerHealth = MaxPlayerHealth;
        CurrentShield = 0;
        isTutorialMode = false;

        playerDiceDeck.Clear();
        activeRelics.Clear();
        nextZoneBuffs.Clear();

        buffDuration = 0;
        buffDamageValue = 0;
        buffShieldValue = 0;
        buffRerollValue = 0;
        hasInsurance = false;

        // 런 통계 초기화
        ResetRunStatistics();

        ApplyMetaUpgrades();
        ApplyMarketItems();


        string selectedDeck = PlayerPrefs.GetString(selectedDeckKey, "Default");

        switch (selectedDeck)
        {
            case "단골":
                playerDiceDeck.Add("D8");
                playerDiceDeck.Add("D6");
                playerDiceDeck.Add("D6");
                playerDiceDeck.Add("D6");
                playerDiceDeck.Add("D6");
                break;

            case "도박사":
                playerDiceDeck.Add("D20");
                playerDiceDeck.Add("D4");
                playerDiceDeck.Add("D4");
                playerDiceDeck.Add("D4");
                playerDiceDeck.Add("D4");
                break;

            case "마법사":
                for (int i = 0; i < 4; i++)
                {
                    playerDiceDeck.Add("D6");
                }
                if (RelicDB.Instance != null)
                {
                    Relic lodestone = RelicDB.Instance.GetRelicByID("RLC_LODESTONE");
                    if (lodestone != null) AddRelic(lodestone);
                }
                break;
            case "Default":
            default:
                for (int i = 0; i < 5; i++)
                {
                    playerDiceDeck.Add("D6");
                }
                break;
        }


        if (PlayerPrefs.GetInt("Consumable_Revive", 0) == 1)
        {
            PlayerPrefs.SetInt("Consumable_Revive", 0);
        }



        WaveGenerator.Instance.BuildRunZoneOrder();
        ZoneData startingZone = WaveGenerator.Instance.GetCurrentZoneData(1);
        string zoneName = startingZone != null ? startingZone.zoneName : "평원";

        //이벤트 시스템: 새 런 시작 시 존 이벤트 + 핸들러 초기화
        ZoneContext zoneCtx = new ZoneContext
        {
            ZoneNumber = CurrentZone,
            ZoneName = zoneName
        };
        GameEvents.RaiseZoneStart(zoneCtx);


        RelicEffectHandler.Instance.ResetForNewRun();

        UIManager.Instance.FadeIn();
        UIManager.Instance.ShowZoneTitle("Zone 1: " + zoneName);
        UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
        //TODO
        //실드있으면 별도로 텍스트 색이라도 변경?해야할거같은데  일단 보류
        UIManager.Instance.UpdateGold(CurrentGold);
        UIManager.Instance.UpdateRelicPanel(activeRelics);

    }

    //메타 업그레이드 효과 총합 계산
    public float GetTotalMetaBonus(MetaEffectType type)
    {
        float totalBonus = 0;
        foreach (var data in allMetaUpgrades)
        {
            if (data.effectType == type)
            {
                int level = PlayerPrefs.GetInt(data.id, 0);
                totalBonus += data.GetTotalEffect(level);
            }
        }
        return totalBonus;
    }

    //영구강화 적용
    private void ApplyMetaUpgrades()
    {
        // 최대 체력 
        int bonusHealth = (int)GetTotalMetaBonus(MetaEffectType.MaxHealth);
        ModifyMaxHealth(bonusHealth);
        PlayerHealth = MaxPlayerHealth;

        // 시작 자금
        int bonusGold = (int)GetTotalMetaBonus(MetaEffectType.StartGold);
        AddGold(bonusGold);


        // 시작 주사위 추가
        int bonusDice = (int)GetTotalMetaBonus(MetaEffectType.StartDiceBonus);
        for (int i = 0; i < bonusDice; i++)
        {
            AddDiceToDeck("D4");
        }

        //TODO
        //리롤 횟수 등은 DiceController나 필요한 시점에 GetTotalMetaBonus로 가져다 씀
    }

    // 잡화점 아이템 적용
    private void ApplyMarketItems()
    {
        string buffs = PlayerPrefs.GetString("NextRunBuffs", "");
        if (string.IsNullOrEmpty(buffs)) return;

        string[] keys = buffs.Split(',');
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key)) continue;

            //기초 보급품
            if (key == "MaxHealth_30") ModifyMaxHealth(30);
            else if (key == "MaxHealth_3") ModifyMaxHealth(30); //이전 데이터인데 날려야됨
            else if (key == "StartGold_150") AddGold(150);
            else if (key == "AddDice_D6") AddDiceToDeck("D6");
            else if (key == "Insurance_30") hasInsurance = true;

            //전투 보조
            else if (key == "Buff_Damage_5wave_5") { buffDuration = 5; buffDamageValue = 5; }
            else if (key == "Buff_Shield_5wave_10") { buffDuration = 5; buffShieldValue = 10; }
            else if (key == "Buff_Reroll_5wave_2") { buffDuration = 5; buffRerollValue = 2; }
            // 유물 꾸러미 (랜덤 일반 유물)
            else if (key == "RandomRelic_Common_1")
            {
                if (RelicDB.Instance != null)
                {
                    var relics = RelicDB.Instance.GetWeightedRandomRelics(1, RelicDropPool.WaveReward);
                    if (relics.Count > 0)
                    {
                        AddRelic(relics[0]);
                    }
                }
            }
        }

        // 사용했으니 초기화
        PlayerPrefs.SetString("NextRunBuffs", "");
    }

    //UIManager업데이트 메서드
    public void StartNewWave()
    {


        // 메타업그레이드중에 이전 웨이브 Shield 저장
        float carryPercent = GetTotalMetaBonus(MetaEffectType.ShieldCarryOver);
        if (carryPercent > 0 && CurrentShield > 0)
        {
            int carriedShield = Mathf.RoundToInt(CurrentShield * carryPercent / 100f);
            PlayerPrefs.SetInt("CarriedShield", carriedShield);
        }

        // 웨이브 시작 시 실드 초기화
        ClearShield();

        //이벤트 시스템: 웨이브 시작 이벤트 재사용
        _cachedWaveContext.Reset();
        _cachedWaveContext.ZoneNumber = CurrentZone;
        _cachedWaveContext.WaveNumber = CurrentWave;
        _cachedWaveContext.IsBossWave = false;
        
        GameEvents.RaiseWaveStart(_cachedWaveContext);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateWaveText(CurrentZone, CurrentWave);
        }
    }


    // 골드 추가
    public void AddGold(int goldAmount, GoldSource source = GoldSource.Bonus)
    {
        int finalGold = goldAmount;
        
        if (source != GoldSource.Combat) 
        {
            float globalMultiplier = 1.0f;
            for (int i = 0; i < activeRelics.Count; i++)
            {
                if (activeRelics[i].EffectType == RelicEffectType.AddGoldMultiplier)
                {
                    globalMultiplier *= activeRelics[i].FloatValue;
                }
            }
            
            //골드 획득 이벤트 재사용
            _cachedGoldContext.Reset();
            _cachedGoldContext.OriginalAmount = goldAmount;
            _cachedGoldContext.BaseAmount = goldAmount;
            _cachedGoldContext.Multiplier = globalMultiplier;
            _cachedGoldContext.FinalAmount = (int)(goldAmount * globalMultiplier);
            _cachedGoldContext.Source = source.ToString();
            
            GameEvents.RaiseGoldGain(_cachedGoldContext);
            finalGold = _cachedGoldContext.FinalAmount;
        }
        
        CurrentGold += finalGold;
        totalGoldEarned += finalGold;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateGold(CurrentGold);
        }
    }
    public void AddNextZoneBuff(string buffKey)
    {
        nextZoneBuffs.Add(buffKey);
    }

    public void ApplyNextZoneBuffs(DiceController diceController)
    {
        if (nextZoneBuffs.Count == 0) return;
        foreach (string buff in nextZoneBuffs)
        {
            switch (buff)
            {
                case "ExtraRoll":
                    diceController.ApplyRollBonus(1); // 굴림 +1
                    break;
                // DamageBoost와 GoldBoost는 GetAttackDamageModifiers에서 계산
                case "DamageBoost":
                    break;
                case "GoldBoost":
                    break;
            }
        }
    }

    public void ProcessWaveClear(bool isSuccess, int rollsRemaining)
    {
        if (isSuccess)
        {
            //이벤트 시스템: 웨이브 종료 이벤트 재사용
            _cachedWaveContext.Reset();
            _cachedWaveContext.ZoneNumber = CurrentZone;
            _cachedWaveContext.WaveNumber = CurrentWave;
            _cachedWaveContext.IsBossWave = false;
            
            GameEvents.RaiseWaveEnd(_cachedWaveContext);

            CurrentWave++;

            if (rollsRemaining > 0)
            {
                int bonus = rollsRemaining * bonusPerRollRemaining;
                AddGold(bonus);
            }
            if (buffDuration > 0)
            {
                buffDuration--;
                if (buffDuration == 0)
                {
                    // 버프 종료 시 스탯 초기화
                    buffDamageValue = 0;
                    buffShieldValue = 0;
                    buffRerollValue = 0;
                }
            }

            if (CurrentWave > wavesPerZone)
            {
                // 튜토리얼 모드일 때는 상점 튜토리얼로 이동
                if (isTutorialMode)
                {
                    PlayerPrefs.SetInt("TutorialCompleted", 1);
                    PlayerPrefs.Save();

                    UIManager.Instance.FadeOut(1.0f, () =>
                    {
                        CurrentZone++;
                        CurrentWave = 1;
                        UIManager.Instance.StartMaintenancePhase();
                        UIManager.Instance.FadeIn();
                    });
                    return;
                }

                // 메타강화 - 존 클리어 시 골드 추가
                float interestRate = GetTotalMetaBonus(MetaEffectType.InterestRate);
                if (interestRate > 0)
                {
                    int currentGoldBeforeInterest = CurrentGold;
                    int interestGold = Mathf.RoundToInt(currentGoldBeforeInterest * interestRate / 100f);
                    if (interestGold > 0)
                    {
                        AddGold(interestGold);
                        if (EffectManager.Instance != null && UIManager.Instance != null)
                        {
                            EffectManager.Instance.ShowText(
                                UIManager.Instance.transform,
                                $"+{interestGold} (이자)",
                                Color.yellow
                            );
                        }
                    }
                }


                UIManager.Instance.FadeOut(1.0f, () =>
                {
                    UIManager.Instance.StartMaintenancePhase();
                    UIManager.Instance.FadeIn();

                    // 상점 열린 상태로 저장
                    SaveCurrentRun();
                });

                return;
            }
            else
            {
                // 튜토리얼 모드이고 Wave 1이면 튜토리얼 완료 체크
                if (isTutorialMode && CurrentWave == 2)
                {
                    // Wave1 튜토리얼이 이미 완료되었으면 바로 보상 화면
                    if (wave1TutorialCompleted)
                    {
                        ShowRewardScreen();
                    }
                    else
                    {
                        isWaitingForWave1Tutorial = true;
                    }
                }
                else
                {
                    ShowRewardScreen();
                }
            }

            SaveCurrentRun();
        }
        else
        {
            // 웨이브 실패하면 살아있는 적의 총 공격력만큼 피해입기
            int totalEnemyDamage = GetAllEnemyDamage();

            //피해 전 이벤트 발생 재사용
            _cachedDamageContext.Reset();
            _cachedDamageContext.OriginalDamage = totalEnemyDamage;
            _cachedDamageContext.FinalDamage = totalEnemyDamage;
            _cachedDamageContext.Source = "WaveFail";
            _cachedDamageContext.Cancelled = false;
            
            GameEvents.RaiseBeforePlayerDamaged(_cachedDamageContext);

            // 유물이 피해를 취소했는지 확인
            if (_cachedDamageContext.Cancelled)
            {
                Debug.Log("[이벤트] 유물 효과 피해 무효화");
            }
            else if (CurrentShield > 0)
            {
                CurrentShield--;

                // 실드 피격 사운드
                if (SoundManager.Instance != null && playerShieldHitSound != null)
                {
                    SoundManager.Instance.PlaySoundConfig(playerShieldHitSound);
                }

            }
            else
            {
                PlayerHealth -= _cachedDamageContext.FinalDamage;

                // 피해량 체크 (40 기준)
                bool isHeavyHit = _cachedDamageContext.FinalDamage >= 40;

                // 플레이어 피격 사운드 (피해량에 따라 다름)
                if (SoundManager.Instance != null)
                {
                    SoundConfig soundToPlay = isHeavyHit ? playerHitHeavySound : playerHitLightSound;
                    if (soundToPlay != null)
                    {
                        SoundManager.Instance.PlaySoundConfig(soundToPlay);
                    }
                }

                // 카메라 흔들림 (피해량에 비례해서?)
                if (CameraShake.Instance != null)
                {
                    float shakeIntensity = isHeavyHit ? 0.4f : 0.2f;
                    float shakeDuration = isHeavyHit ? 0.3f : 0.15f;
                    CameraShake.Instance.Shake(shakeIntensity, shakeDuration);
                }

                // 비네팅

                PlayerHitVignette.Instance.PlayHitEffect(isHeavyHit);

                // 런 통계: 피격 기록
                OnPlayerDamaged();

                //이벤트 시스템: 피격 후 이벤트
                GameEvents.RaiseAfterPlayerDamaged(_cachedDamageContext);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            }

            if (PlayerHealth <= 0)
            {
                //이벤트 시스템: 사망 이벤트 발생 재사용
                _cachedDeathContext.Reset();
                _cachedDeathContext.Revived = false;
                _cachedDeathContext.ReviveHP = 0;
                _cachedDeathContext.ReviveSource = null;
                
                GameEvents.RaisePlayerDeath(_cachedDeathContext);

                // 유물이 부활시켰는지 확인
                if (_cachedDeathContext.Revived)
                {
                    PlayerHealth = _cachedDeathContext.ReviveHP;
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
                    }
                    // 부활 후 다음 웨이브 진행
                    if (StageManager.Instance != null)
                    {
                        StageManager.Instance.PrepareNextWave();
                    }
                    return;
                }

                Debug.Log("게임 오버");

                int earnedCurrency = CalculateAndSaveMetaCurrency();

                SaveManager.Instance.DeleteSaveFile();

                if (GameOverDirector.Instance != null)
                {
                    //ui들 없애기
                    if (UIManager.Instance != null) UIManager.Instance.gameObject.SetActive(false);
                    GameOverDirector.Instance.PlayGameOverSequence(earnedCurrency);
                }
                return;
            }

            if (StageManager.Instance != null)
            {
                StageManager.Instance.PrepareNextWave();
            }
        }
    }

    private void ShowRewardScreen()
    {
        List<Relic> rewardOptions = RelicDB.Instance.GetAcquirableRelics(3);

        if (rewardOptions.Count == 0)
        {
            AddGold(150);
            if (StageManager.Instance != null)
            {
                StageManager.Instance.PrepareNextWave();
            }
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowRewardScreen(rewardOptions);
        }

        // 튜토리얼 모드이고 Wave 1 클리어 후라면 유물 튜토리얼 시작
        if (isTutorialMode && CurrentZone == 1 && CurrentWave == 2)
        {
            TutorialRelicController relicTutorial = FindFirstObjectByType<TutorialRelicController>();
            if (relicTutorial != null)
            {
                Invoke(nameof(StartRelicTutorial), 0.5f);
            }
        }
    }

    private void StartRelicTutorial()
    {
        TutorialRelicController relicTutorial = FindFirstObjectByType<TutorialRelicController>();
        if (relicTutorial != null)
        {
            relicTutorial.StartRelicTutorial();
        }
    }

    // 메타강화 게임 시작 시 유물 선택
    private void ShowStartingRelicChoice()
    {
        if (RelicDB.Instance == null || UIManager.Instance == null)
        {
            StartNewRun();
            return;
        }

        // 높은 등급 유물 위주로
        List<Relic> startingOptions = RelicDB.Instance.GetWeightedRandomRelics(3, RelicDropPool.MaintenanceReward);

        if (startingOptions.Count == 0)
        {
            StartNewRun();
            return;
        }

        // UI에 표시
        UIManager.Instance.ShowRewardScreen(startingOptions, () =>
        {
            // 유물 선택 후 게임 시작
            StartNewRun();
        });
    }

    // 유물 획득 가능 여부
    public bool CanAcquireRelic(Relic relic)
    {
        if (relic == null) return false;

        int currentCount = activeRelics.Count(r => r.RelicID == relic.RelicID);
        int effectiveMax = GetEffectiveMaxCount(relic.RelicID, relic.MaxCount);

        // 무제한이면 항상 가능
        if (effectiveMax <= 0) return true;

        // 현재 개수가 유효 최대치 미만이면 가능
        return currentCount < effectiveMax;
    }

    //유물 최대 개수 계산
    public int GetEffectiveMaxCount(string relicID, int baseMaxCount)
    {
        // 무제한 유물 (0 또는 음수)
        if (baseMaxCount <= 0)
            return baseMaxCount;

        // 최대 1개 유물은 영향 없음 고유 유물이니까
        if (baseMaxCount == 1)
            return 1;

        // 가벼운 가방 자신은 영향 없음
        if (relicID == "RLC_LIGHTWEIGHT_BAG")
            return baseMaxCount;

        // 가벼운 가방 개수만큼 한도 증가
        int bagCount = activeRelics.Count(r => r.RelicID == "RLC_LIGHTWEIGHT_BAG");
        return baseMaxCount + bagCount;
    }

    public void AddRelic(Relic chosenRelic)
    {
        if (chosenRelic == null) return;

        // 획득 가능 여부 체크
        if (!CanAcquireRelic(chosenRelic))
        {
            return;
        }

        activeRelics.Add(chosenRelic);

        int currentCount = activeRelics.Count(r => r.RelicID == chosenRelic.RelicID);
        int effectiveMax = GetEffectiveMaxCount(chosenRelic.RelicID, chosenRelic.MaxCount);
        string maxInfo = effectiveMax > 0 ? $" ({currentCount}/{effectiveMax})" : "";


        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRelicPanel(activeRelics);
        }

        //이벤트 시스템: 유물 획득 이벤트 발생
        RelicContext relicCtx = new RelicContext
        {
            RelicID = chosenRelic.RelicID,
            RelicName = chosenRelic.Name
        };
        GameEvents.RaiseRelicAcquire(relicCtx);

        SaveCurrentRun();

        if (UIManager.Instance != null && !UIManager.Instance.IsShopOpen())
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.PrepareNextWave();
            }
        }
    }

    public void ApplyAllRelicEffects(DiceController diceController)
    {
        ApplyZoneBuffs(diceController);
    }

    // --- 상점 구매용 함수들 ---
    public bool SubtractGold(int amount)
    {
        if (CurrentGold >= amount)
        {
            CurrentGold -= amount;
            UIManager.Instance.UpdateGold(CurrentGold);
            return true;
        }
        return false;
    }
    // 현재 활성화된 존 버프 적용 (매 웨이브 호출)
    private void ApplyZoneBuffs(DiceController diceController)
    {
        foreach (string buff in nextZoneBuffs)
        {
            if (buff == "ExtraRoll") diceController.ApplyRollBonus(1);
        }
    }

    // 존이 끝날 때 버프 제거 (ProcessWaveClear에서 존 넘어갈 때 호출)
    public void ClearZoneBuffs()
    {
        nextZoneBuffs.Clear();
    }

    // 플레이어에게 데미지 (간단한 호출용)
    public void DamagePlayer(int damage, string source = "Unknown")
    {
        // 캐시 재사용
        _cachedDamageContext.Reset();
        _cachedDamageContext.OriginalDamage = damage;
        _cachedDamageContext.FinalDamage = damage;
        _cachedDamageContext.Source = source;
        _cachedDamageContext.Cancelled = false;
        
        DamagePlayer(_cachedDamageContext);
    }

    // 살아있는 적의 총 공격력 합산
    private int GetAllEnemyDamage()
    {
        if (StageManager.Instance == null) return 5;

        int totalDamage = 0;
        foreach (Enemy enemy in StageManager.Instance.activeEnemies)
        {
            if (enemy != null && !enemy.isDead)
            {
                totalDamage += enemy.attackDamage;
            }
        }

        return totalDamage > 0 ? totalDamage : 5;
    }

    // 플레이어에게 데미지 (세부 제어용 - 피해 타입, 무시 옵션 등 확장 가능)
    public void DamagePlayer(DamageContext damageCtx)
    {
        if (damageCtx.FinalDamage <= 0) return;

        //  첫 피격 무효화
        if (firstHitThisWave)
        {
            firstHitThisWave = false;
            if (EffectManager.Instance != null && UIManager.Instance != null)
            {
                EffectManager.Instance.ShowText(UIManager.Instance.transform, "무효", Color.cyan);
            }
            return;
        }

        // 이벤트 시스템: 피해 전 이벤트
        GameEvents.RaiseBeforePlayerDamaged(damageCtx);

        // 유물이 피해를 취소했는지 확인
        if (damageCtx.Cancelled)
        {
            Debug.Log($"{damageCtx.Source}의 피해 무효화");
            return;
        }

        // 메타강화로 받는 피해 감소
        int reduction = (int)GetTotalMetaBonus(MetaEffectType.DamageReduction);
        if (reduction > 0)
        {
            int beforeReduce = damageCtx.FinalDamage;
            damageCtx.FinalDamage = Mathf.Max(1, damageCtx.FinalDamage - reduction);
        }

        // 쉴드 먼저 소모
        if (CurrentShield > 0)
        {
            int shieldAbsorb = Mathf.Min(CurrentShield, damageCtx.FinalDamage);
            CurrentShield -= shieldAbsorb;
            damageCtx.FinalDamage -= shieldAbsorb;

            // 쉴드 피해 누적
            accumulatedShieldDamage += shieldAbsorb;
            hasPendingFeedback = true;
        }

        if (damageCtx.FinalDamage > 0)
        {
            PlayerHealth -= damageCtx.FinalDamage;

            // 체력 피해 누적
            accumulatedMechanicDamage += damageCtx.FinalDamage;
            hasPendingFeedback = true;

            // 이벤트 시스템: 피격 후 이벤트
            GameEvents.RaiseAfterPlayerDamaged(damageCtx);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
        }

        // 사망 체크
        if (PlayerHealth <= 0)
        {
            HandlePlayerDeath();
        }
    }

    // 플레이어 사망 처리
    private void HandlePlayerDeath()
    {
        // 메타강화 수호천사로 사망 시 1회 부활
        if (!hasRevived)
        {
            float reviveLevel = GetTotalMetaBonus(MetaEffectType.Revive);
            if (reviveLevel > 0)
            {
                hasRevived = true;
                PlayerHealth = Mathf.RoundToInt(MaxPlayerHealth * 0.3f);
                EffectManager.Instance?.ShowText(UIManager.Instance.transform, "부활!", Color.yellow);

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
                }
                return;
            }
        }

        // 이벤트 시스템: 사망 이벤트 재사용
        _cachedDeathContext.Reset();
        _cachedDeathContext.Revived = false;
        _cachedDeathContext.ReviveHP = 0;
        _cachedDeathContext.ReviveSource = null;
        
        GameEvents.RaisePlayerDeath(_cachedDeathContext);

        // 유물이 부활시켰는지 확인
        if (_cachedDeathContext.Revived)
        {
            PlayerHealth = _cachedDeathContext.ReviveHP;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            }
            return;
        }

        // 진짜 사망 처리
        Debug.Log("게임 오버");
        int earnedCurrency = CalculateAndSaveMetaCurrency();
        SaveManager.Instance.DeleteSaveFile();

        if (GameOverDirector.Instance != null)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseAllUIPanels();
                UIManager.Instance.gameObject.SetActive(false);
            }
            GameOverDirector.Instance.PlayGameOverSequence(earnedCurrency);
        }
    }

    // 런 포기
    public void ProcessAbandonRun()
    {
        int earnedCurrency = 0;

        // 그래도 게임은 해야지 90초 이상 해야 마석얻음
        if (playTime >= 90f)
        {
            earnedCurrency = CalculateAndSaveMetaCurrency();
        }
        else
        {
            // 런 카운트는 증가
            int runCount = PlayerPrefs.GetInt("TotalRunCount", 0);
            PlayerPrefs.SetInt("TotalRunCount", runCount + 1);
            PlayerPrefs.Save();
        }

        SaveManager.Instance.DeleteSaveFile();

        if (GameOverDirector.Instance != null)
        {
            if (UIManager.Instance != null) UIManager.Instance.gameObject.SetActive(false);
            GameOverDirector.Instance.PlayGameOverSequence(earnedCurrency);
        }
    }

    ///모든 존 클리어 시 승리
    public void ProcessVictory()
    {

        SaveManager.Instance.DeleteSaveFile();
        int earnedCurrency = CalculateAndSaveMetaCurrency();
        RecordVictoryStats(); // 승리한 판 기록

        UIManager.Instance.CloseAllUIPanels();
        GameOverScreen.Instance.ShowVictory(earnedCurrency);

    }
    //승리기록
    private void RecordVictoryStats()
    {
        // 총 승리 횟수
        int totalVictories = PlayerPrefs.GetInt("TotalVictories", 0);
        PlayerPrefs.SetInt("TotalVictories", totalVictories + 1);

        // 가장 빠른 클리어 타임 (playTime 사용)
        float bestTime = PlayerPrefs.GetFloat("BestClearTime", float.MaxValue);
        if (playTime < bestTime)
        {
            PlayerPrefs.SetFloat("BestClearTime", playTime);
        }

        // 최고 점수 = 이번판 골드 총 획득한거
        int bestGold = PlayerPrefs.GetInt("BestGoldEarned", 0);
        if (totalGoldEarned > bestGold)
        {
            PlayerPrefs.SetInt("BestGoldEarned", totalGoldEarned);
        }

        PlayerPrefs.Save();
    }

    public void HealPlayer(int amount)
    {
        //회복 이벤트 재사용
        _cachedHealContext.Reset();
        _cachedHealContext.OriginalAmount = amount;
        _cachedHealContext.FinalAmount = amount;
        _cachedHealContext.Cancelled = false;
        
        GameEvents.RaisePlayerHeal(_cachedHealContext);

        if (_cachedHealContext.Cancelled)
        {
            return;
        }

        PlayerHealth = Mathf.Min(PlayerHealth + _cachedHealContext.FinalAmount, MaxPlayerHealth);
        UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
    }

    public void AddShield(int amount)
    {
        CurrentShield += amount;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
        }
    }

    public void ClearShield()
    {
        if (CurrentShield > 0)
        {
            CurrentShield = 0;
        }
    }


    public void ModifyMaxHealth(int amount)
    {
        MaxPlayerHealth += amount;

        // 최대 체력 증가 시 현재 체력도 함께 증가 (잡화점 아이템용)
        if (amount > 0)
        {
            PlayerHealth += amount;
        }

        // 최대 체력을 초과하면 깎음
        if (PlayerHealth > MaxPlayerHealth)
        {
            PlayerHealth = MaxPlayerHealth;
        }
        if (PlayerHealth <= 0) PlayerHealth = 1; // (죽지는 않게 1로 보정)

        UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
    }

    public void AddDiceToDeck(string diceType)
    {
        if (playerDiceDeck.Count < maxDiceCount)
        {
            playerDiceDeck.Add(diceType);
        }
        else
        {
        }
    }

    public void RemoveDiceFromDeck(string diceTypeToRemove)
    {
        if (playerDiceDeck.Count > minDiceCount)
        {
            if (playerDiceDeck.Remove(diceTypeToRemove))
            {
            }
        }
    }

    public void UpgradeSingleDice(int diceIndex, string newDiceType)
    {
        if (diceIndex >= 0 && diceIndex < playerDiceDeck.Count)
        {
            playerDiceDeck[diceIndex] = newDiceType;
        }
    }

    private int CalculateAndSaveMetaCurrency()
    {
        int earnedCurrency = (int)(CurrentGold * 0.1f) + (CurrentZone * 50);
        int totalMetaCurrency = PlayerPrefs.GetInt(metaCurrencySaveKey, 0);

        if (hasInsurance)
        {
            int insuranceMoney = (int)(CurrentGold * 0.3f);
            earnedCurrency += insuranceMoney;
        }

        totalMetaCurrency += earnedCurrency;
        PlayerPrefs.SetInt(metaCurrencySaveKey, totalMetaCurrency);

        // 게임 종료 횟수 증가
        int runCount = PlayerPrefs.GetInt("TotalRunCount", 0);
        PlayerPrefs.SetInt("TotalRunCount", runCount + 1);
        PlayerPrefs.Save();

        // 세이브 파일 삭제 (런 종료)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSaveFile();
        }
        return earnedCurrency;
    }

    // 웨이브 시작 시 쉴드/리롤 버프 적용
    public void ApplyWaveStartBuffs(DiceController diceController)
    {
        if (buffDuration > 0)
        {
            if (buffShieldValue > 0)
            {
                CurrentShield += buffShieldValue;
                // UI 갱신 필요
            }
            if (buffRerollValue > 0)
            {
                diceController.ApplyRollBonus(buffRerollValue);
            }
        }
    }
    // 통계 초기화
    private void ResetRunStatistics()
    {
        playTime = 0f;
        totalKills = 0;
        totalGoldEarned = 0;
        maxDamageDealt = 0;
        maxChainCount = 0;
        handUsageCount.Clear();
        bossesDefeated = 0;
        perfectWaves = 0;
        wasDamagedThisWave = false;
    }

    // 씬 전환 시 게임 상태 리셋
    public void ResetGameState()
    {
        CurrentGold = 0;
        CurrentZone = 1;
        CurrentWave = 1;
        MaxPlayerHealth = 100;
        PlayerHealth = MaxPlayerHealth;
        CurrentShield = 0;
        isTutorialMode = false;

        playerDiceDeck.Clear();
        activeRelics.Clear();
        nextZoneBuffs.Clear();

        buffDuration = 0;
        buffDamageValue = 0;
        buffShieldValue = 0;
        buffRerollValue = 0;
        hasInsurance = false;
        hasRevived = false;
        firstHitThisWave = false;

        ResetRunStatistics();

        hasInitializedRun = false;
        
        // 이벤트 시스템 정리
        GameEvents.ClearAllEvents();
    }

    // 적 처치 기록
    public void RecordKill(bool isBoss = false)
    {
        totalKills++;
        if (isBoss) bossesDefeated++;
    }

    // 데미지 기록
    public void RecordDamage(int damage)
    {
        if (damage > maxDamageDealt)
        {
            maxDamageDealt = damage;
        }
    }

    // 연쇄 공격 기록
    public void RecordChainCount(int chainCount)
    {
        if (chainCount > maxChainCount)
        {
            maxChainCount = chainCount;
        }
    }

    // 족보 사용 기록
    public void RecordHandUsage(string handName)
    {
        if (!handUsageCount.ContainsKey(handName))
        {
            handUsageCount[handName] = 0;
        }
        handUsageCount[handName]++;
    }

    // 웨이브 시작 시
    public void OnWaveStart()
    {
        wasDamagedThisWave = false;

        // 메타강화 재생성 장벽 - 웨이브 시작 시 실드
        int waveShield = (int)GetTotalMetaBonus(MetaEffectType.WaveStartShield);
        if (waveShield > 0)
        {
            AddShield(waveShield);
        }

        //  메타강화 보존의 장막 - 이전 웨이브에서 이월된 실드얻기
        int carriedShield = PlayerPrefs.GetInt("CarriedShield", 0);
        if (carriedShield > 0)
        {
            AddShield(carriedShield);
            PlayerPrefs.DeleteKey("CarriedShield");
        }

        //메타강화 절대 방어 - 첫 피격 무효화
        float immuneLevel = GetTotalMetaBonus(MetaEffectType.FirstHitImmune);
        firstHitThisWave = (immuneLevel > 0);
    }

    // 플레이어 피격 시
    public void OnPlayerDamaged()
    {
        wasDamagedThisWave = true;
    }

    // 웨이브 종료 시
    public void OnWaveComplete()
    {
        if (!wasDamagedThisWave)
        {
            perfectWaves++;
        }

        //메타강화 재생성 장벽 → 웨이브 종료 시 회복
        int waveHeal = (int)GetTotalMetaBonus(MetaEffectType.WaveEndHeal);
        if (waveHeal > 0)
        {
            HealPlayer(waveHeal);
        }
    }

    //튜토리얼 관련
    private bool isWaitingForWave1Tutorial = false;
    private bool wave1TutorialCompleted = false;

    public void OnWave1TutorialComplete()
    {
        wave1TutorialCompleted = true;

        // 튜토리얼 완료 시점에 적이 남아잇나?
        bool allEnemiesDead = StageManager.Instance != null &&
                              StageManager.Instance.activeEnemies.All(e => e == null || e.isDead);

        if (allEnemiesDead)
        {
            // 이미 적을 다 잡은 상태면 바로 보상 화면 표시
            ShowRewardScreen();
        }
        else if (isWaitingForWave1Tutorial)
        {
            // 웨이브 클리어 후 튜토리얼 완료 대기 중이면
            isWaitingForWave1Tutorial = false;
            ShowRewardScreen();
        }
    }

    public void StartTutorialMode()
    {
        hasInitializedRun = true; 
        isTutorialMode = true;
        wave1TutorialCompleted = false;
        isWaitingForWave1Tutorial = false;

        // 기본 게임 시작
        CurrentGold = 0;
        CurrentZone = 1;
        CurrentWave = 1;
        MaxPlayerHealth = 100;
        PlayerHealth = MaxPlayerHealth;
        CurrentShield = 0;

        playerDiceDeck.Clear();
        activeRelics.Clear();

        for (int i = 0; i < 6; i++)
        {
            playerDiceDeck.Add("D6");
        }

        // 통계 초기화
        playTime = 0f;
        totalKills = 0;
        totalGoldEarned = 0;
        maxDamageDealt = 0;
        maxChainCount = 0;
        handUsageCount.Clear();
        bossesDefeated = 0;
        perfectWaves = 0;
        wasDamagedThisWave = false;

        // 웨이브 생성
        if (WaveGenerator.Instance != null)
        {
            WaveGenerator.Instance.BuildRunZoneOrder();
        }

        // UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            UIManager.Instance.UpdateGold(CurrentGold);
            UIManager.Instance.UpdateRelicPanel(activeRelics);
            UIManager.Instance.UpdateWaveText(CurrentZone, CurrentWave);
        }

        // 스테이지 준비
        if (StageManager.Instance != null)
        {
            StageManager.Instance.PrepareNextWave();
        }

        // 튜토리얼 시작 (Wave1Controller)
        TutorialWave1Controller wave1Tutorial = FindFirstObjectByType<TutorialWave1Controller>();
        if (wave1Tutorial != null)
        {
            Invoke(nameof(StartWave1Tutorial), 0.2f);
        }
    }

    private void StartWave1Tutorial()
    {
        TutorialWave1Controller wave1Tutorial = FindFirstObjectByType<TutorialWave1Controller>();
        if (wave1Tutorial != null)
        {
            wave1Tutorial.StartWave1Tutorial();
        }
    }

    // 튜토리얼 완료 콜백
    public void OnTutorialCompleted()
    {
        isTutorialMode = false;
        tutorialCompleted = true;

        // 메인 메뉴로 이동
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("MainMenu");
        }
        else
        {
            // SceneController가 없으면 직접 로드
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    // 플레이 시간 포맷
    public string GetFormattedPlayTime()
    {
        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    // 가장 많이 사용한 족보
    public string GetMostUsedHand()
    {
        if (handUsageCount.Count == 0) return "없음";
        return handUsageCount.OrderByDescending(x => x.Value).First().Key;
    }

    // 유물 ID로 슬롯 인덱스 찾기
    public int FindRelicSlotIndex(string relicId)
    {
        for (int i = 0; i < activeRelics.Count; i++)
        {
            if (activeRelics[i] != null && activeRelics[i].RelicID == relicId)
            {
                return i;
            }
        }
        return -1;
    }

}
