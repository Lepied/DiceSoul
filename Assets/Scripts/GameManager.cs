using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
                Debug.Log("[LoadRun] 상점 페이즈 복원 완료");
            }
        }
        else
        {
            if (data.currentWave > wavesPerZone)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.StartMaintenancePhase();
                    Debug.Log("[LoadRun] 이전 버전 세이브 - 상점 페이즈 복원");
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
        Debug.Log($"[GameManager] '{selectedDeck}' 덱으로 새 런을 시작합니다.");

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
        Debug.Log($"[Zone {CurrentZone} - Wave {CurrentWave}] 웨이브 시작.");


        // 메타업그레이드중에 이전 웨이브 Shield 저장
        float carryPercent = GetTotalMetaBonus(MetaEffectType.ShieldCarryOver);
        if (carryPercent > 0 && CurrentShield > 0)
        {
            int carriedShield = Mathf.RoundToInt(CurrentShield * carryPercent / 100f);
            PlayerPrefs.SetInt("CarriedShield", carriedShield);
            Debug.Log($"[보존의 장막] Shield {CurrentShield}의 {carryPercent}% ({carriedShield}) 저장");
        }

        // 웨이브 시작 시 실드 초기화
        ClearShield();

        //이벤트 시스템: 웨이브 시작 이벤트
        WaveContext waveCtx = new WaveContext
        {
            ZoneNumber = CurrentZone,
            WaveNumber = CurrentWave
        };
        GameEvents.RaiseWaveStart(waveCtx);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateWaveText(CurrentZone, CurrentWave);
        }
    }


    // 이벤트 시스템에서 이미 계산된 골드를 직접 추가 (중복 계산 방지)
    public void AddGoldDirect(int finalGold)
    {
        CurrentGold += finalGold;
        Debug.Log($"금화 획득(직접): +{finalGold} (총: {CurrentGold})");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateGold(CurrentGold);
        }
    }

    public void AddGold(int goldToAdd)
    {
        float globalMultiplier = 1.0f;
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddGoldMultiplier))
        {
            globalMultiplier *= relic.FloatValue;
        }

        int finalGold = (int)(goldToAdd * globalMultiplier);

        //이벤트 시스템: 골드 획득 이벤트
        GoldContext goldCtx = new GoldContext
        {
            OriginalAmount = goldToAdd,
            FinalAmount = finalGold,
            Source = "Bonus"
        };
        GameEvents.RaiseGoldGain(goldCtx);
        finalGold = goldCtx.FinalAmount;

        CurrentGold += finalGold;
        totalGoldEarned += finalGold;

        Debug.Log($"금화 획득(보너스): +{finalGold} (기본: {goldToAdd}, 전역배율: {globalMultiplier}x) (총: {CurrentGold})");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateGold(CurrentGold);
        }
    }
    public void AddNextZoneBuff(string buffKey)
    {
        nextZoneBuffs.Add(buffKey);
        Debug.Log($"[GameManager] 다음 존 버프 획득: {buffKey}");
    }

    public void ApplyNextZoneBuffs(DiceController diceController)
    {
        if (nextZoneBuffs.Count == 0) return;

        Debug.Log("다음 존 버프를 적용합니다...");
        foreach (string buff in nextZoneBuffs)
        {
            switch (buff)
            {
                case "ExtraRoll":
                    diceController.ApplyRollBonus(1); // 굴림 +1
                    break;
                // DamageBoost와 GoldBoost는 GetAttackDamageModifiers에서 계산
                case "DamageBoost":
                    Debug.Log(">> 공격력 증가 적용됨!");
                    break;
                case "GoldBoost":
                    Debug.Log(">> 금화 획득량 증가 적용됨!");
                    break;
            }
        }
    }

    public void ProcessWaveClear(bool isSuccess, int rollsRemaining)
    {
        if (isSuccess)
        {
            //이벤트 시스템: 웨이브 종료 이벤트
            WaveContext waveEndCtx = new WaveContext
            {
                ZoneNumber = CurrentZone,
                WaveNumber = CurrentWave
            };
            GameEvents.RaiseWaveEnd(waveEndCtx);

            CurrentWave++;

            if (rollsRemaining > 0)
            {
                int bonus = rollsRemaining * bonusPerRollRemaining;
                Debug.Log($"남은 굴림 횟수 보너스: +{bonus}점 (남은 횟수: {rollsRemaining})");
                AddGold(bonus);
            }
            if (buffDuration > 0)
            {
                buffDuration--;
                Debug.Log($"남은 웨이브: {buffDuration}");
                if (buffDuration == 0)
                {
                    // 버프 종료 시 스탯 초기화
                    buffDamageValue = 0;
                    buffShieldValue = 0;
                    buffRerollValue = 0;
                    Debug.Log("버프 효과 종료.");
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
                        Debug.Log($"[이자 수익] 골드 {currentGoldBeforeInterest}의 {interestRate}% = {interestGold} 골드 추가 획득!");

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
                Debug.Log("웨이브 클리어! 유물 보상.");

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
            Debug.Log($"웨이브 실패. 살아있는 적의 총 공격력 {totalEnemyDamage} 피해를 받습니다.");

            //피해 전 이벤트 발생 (유물이 피해 무효화/감소 가능)
            DamageContext damageCtx = new DamageContext
            {
                OriginalDamage = totalEnemyDamage,
                FinalDamage = totalEnemyDamage,
                Source = "WaveFail",
                Cancelled = false
            };
            GameEvents.RaiseBeforePlayerDamaged(damageCtx);

            // 유물이 피해를 취소했는지 확인
            if (damageCtx.Cancelled)
            {
                Debug.Log("[이벤트] 유물 효과로 피해가 무효화되었습니다!");
            }
            else if (CurrentShield > 0)
            {
                CurrentShield--;

                // 실드 피격 사운드
                if (SoundManager.Instance != null && playerShieldHitSound != null)
                {
                    SoundManager.Instance.PlaySoundConfig(playerShieldHitSound);
                }

                Debug.Log($"쉴드 방어! 남은 쉴드: {CurrentShield}");
            }
            else
            {
                PlayerHealth -= damageCtx.FinalDamage;

                // 피해량 체크 (40 기준)
                bool isHeavyHit = damageCtx.FinalDamage >= 40;

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
                GameEvents.RaiseAfterPlayerDamaged(damageCtx);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            }

            if (PlayerHealth <= 0)
            {
                //이벤트 시스템: 사망 이벤트 발생 (유물이 부활 가능)
                DeathContext deathCtx = new DeathContext
                {
                    Revived = false,
                    ReviveHP = 0,
                    ReviveSource = null
                };
                GameEvents.RaisePlayerDeath(deathCtx);

                // 유물이 부활시켰는지 확인
                if (deathCtx.Revived)
                {
                    PlayerHealth = deathCtx.ReviveHP;
                    Debug.Log($"[이벤트] {deathCtx.ReviveSource}에 의해 체력 {deathCtx.ReviveHP}로 부활!");
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

                Debug.Log("게임 오버. 영구 재화를 저장하고 연출을 재생합니다.");

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
        if (RelicDB.Instance == null)
        {
            Debug.LogError("RelicDB가 씬에 없습니다!");
            return;
        }

        List<Relic> rewardOptions = RelicDB.Instance.GetAcquirableRelics(3);

        if (rewardOptions.Count == 0)
        {
            Debug.Log("모든 유물이 최대치 도달! 골드 보상 지급");
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
            Debug.LogWarning($"유물 획득 실패: {chosenRelic.Name} (최대 개수 도달)");
            return;
        }

        activeRelics.Add(chosenRelic);

        int currentCount = activeRelics.Count(r => r.RelicID == chosenRelic.RelicID);
        int effectiveMax = GetEffectiveMaxCount(chosenRelic.RelicID, chosenRelic.MaxCount);
        string maxInfo = effectiveMax > 0 ? $" ({currentCount}/{effectiveMax})" : "";
        Debug.Log($"유물 획득: {chosenRelic.Name}{maxInfo}");

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
        Debug.Log("보유한 유물 효과를 모두 적용합니다...");

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
        DamageContext ctx = new DamageContext
        {
            OriginalDamage = damage,
            FinalDamage = damage,
            Source = source,
            Cancelled = false
        };
        DamagePlayer(ctx);
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
            Debug.Log("첫 피격 무효화!");
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
            Debug.Log($"[이벤트] {damageCtx.Source}의 피해가 무효화되었습니다!");
            return;
        }

        // 메타강화로 받는 피해 감소
        int reduction = (int)GetTotalMetaBonus(MetaEffectType.DamageReduction);
        if (reduction > 0)
        {
            int beforeReduce = damageCtx.FinalDamage;
            damageCtx.FinalDamage = Mathf.Max(1, damageCtx.FinalDamage - reduction);
            Debug.Log($"[강철 피부] 피해 감소: {beforeReduce} → {damageCtx.FinalDamage} (-{reduction})");
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

            Debug.Log($"쉴드 방어! -{shieldAbsorb} (남은 쉴드: {CurrentShield})");
        }

        if (damageCtx.FinalDamage > 0)
        {
            PlayerHealth -= damageCtx.FinalDamage;

            // 체력 피해 누적
            accumulatedMechanicDamage += damageCtx.FinalDamage;
            hasPendingFeedback = true;

            Debug.Log($"[{damageCtx.Source}] 플레이어 피해: -{damageCtx.FinalDamage} (남은 체력: {PlayerHealth})");

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
                Debug.Log($"[수호천사] 부활! 체력 {PlayerHealth}로 회복");
                EffectManager.Instance?.ShowText(UIManager.Instance.transform, "부활!", Color.yellow);

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
                }
                return;
            }
        }

        // 이벤트 시스템: 사망 이벤트 (유물로부활)
        DeathContext deathCtx = new DeathContext
        {
            Revived = false,
            ReviveHP = 0,
            ReviveSource = null
        };
        GameEvents.RaisePlayerDeath(deathCtx);

        // 유물이 부활시켰는지 확인
        if (deathCtx.Revived)
        {
            PlayerHealth = deathCtx.ReviveHP;
            Debug.Log($"[이벤트] {deathCtx.ReviveSource}에 의해 체력 {deathCtx.ReviveHP}로 부활!");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            }
            return;
        }

        // 진짜 사망 처리
        Debug.Log("게임 오버. 영구 재화를 저장하고 연출을 재생합니다.");
        int earnedCurrency = CalculateAndSaveMetaCurrency();
        SaveManager.Instance.DeleteSaveFile();

        if (GameOverDirector.Instance != null)
        {
            if (UIManager.Instance != null) UIManager.Instance.gameObject.SetActive(false);
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

    public void HealPlayer(int amount)
    {
        //이벤트 시스템: 회복 이벤트
        HealContext healCtx = new HealContext
        {
            OriginalAmount = amount,
            FinalAmount = amount,
            Cancelled = false
        };
        GameEvents.RaisePlayerHeal(healCtx);

        if (healCtx.Cancelled)
        {
            Debug.Log("[이벤트] 회복이 차단되었습니다!");
            return;
        }

        PlayerHealth = Mathf.Min(PlayerHealth + healCtx.FinalAmount, MaxPlayerHealth);
        UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
    }

    public void AddShield(int amount)
    {
        CurrentShield += amount;
        Debug.Log($"실드 획듍: +{amount} (총: {CurrentShield})");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
        }
    }

    public void ClearShield()
    {
        if (CurrentShield > 0)
        {
            Debug.Log($"실드 제거: {CurrentShield} -> 0");
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
            Debug.Log($"덱 업그레이드: {diceType} 1개 추가. (현재 {playerDiceDeck.Count}개)");
        }
        else
        {
            Debug.Log($"덱 업그레이드 실패: 최대 주사위 개수({maxDiceCount}) 도달");
        }
    }

    public void RemoveDiceFromDeck(string diceTypeToRemove)
    {
        if (playerDiceDeck.Count > minDiceCount)
        {
            if (playerDiceDeck.Remove(diceTypeToRemove)) // (리스트에서 첫 번째 "D6"를 찾아 제거)
            {
                Debug.Log($"덱 수정: {diceTypeToRemove} 1개 제거. (현재 {playerDiceDeck.Count}개)");
            }
            else
            {
                Debug.LogWarning($"덱 수정 실패: 덱에서 {diceTypeToRemove}를 찾지 못했습니다.");
            }
        }
        else
        {
            Debug.Log($"덱 수정 실패: 최소 주사위 개수({minDiceCount}) 도달");
        }
    }

    public void UpgradeSingleDice(int diceIndex, string newDiceType)
    {
        if (diceIndex >= 0 && diceIndex < playerDiceDeck.Count)
        {
            playerDiceDeck[diceIndex] = newDiceType;
            Debug.Log($"덱 업그레이드: {diceIndex}번 주사위가 {newDiceType}이 되었습니다.");
        }
    }

    private int CalculateAndSaveMetaCurrency()
    {
        int earnedCurrency = (int)(CurrentGold * 0.1f) + (CurrentZone * 50);
        int totalMetaCurrency = PlayerPrefs.GetInt(metaCurrencySaveKey, 0);

        if (hasInsurance)
        {
            int insuranceMoney = (int)(CurrentGold * 0.3f); // 30% 환급
            earnedCurrency += insuranceMoney;
            Debug.Log($"보험 아이템 추가 환급금: {insuranceMoney}");
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

        Debug.Log($"이번 런 획득 재화: {earnedCurrency}. 저장된 총 재화: {totalMetaCurrency}");
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
            Debug.Log($"[보존의 장막] 이월된 Shield +{carriedShield}");
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
            Debug.Log($"[재생성 장벽] 웨이브 종료 - 체력 +{waveHeal} 회복");
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
