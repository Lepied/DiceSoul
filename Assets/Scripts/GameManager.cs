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

    [Header("덱 정보")]
    public List<string> playerDiceDeck = new List<string>();
    public List<Relic> activeRelics = new List<Relic>();

    [Header("게임 밸런스")]
    public int bonusPerRollRemaining = 5;
    public int wavesPerZone = 5;
    public int maxDiceCount = 8;
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

    public List<MetaUpgradeData> allMetaUpgrades; //메타 업그레이드데이터 리스트

    public List<string> nextZoneBuffs = new List<string>();

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

        if (SaveManager.shouldLoadSave)
        {
            LoadRun();
            SaveManager.shouldLoadSave = false; // 플래그 리셋
        }
        else
        {
            StartNewRun();
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

        Debug.Log("저장된 게임을 불러옵니다...");

        // 1. 기본 스탯 복구
        PlayerHealth = data.currentHealth;
        MaxPlayerHealth = data.maxHealth;
        CurrentGold = data.currentGold;
        CurrentZone = data.currentZone;
        CurrentWave = data.currentWave;

        // 2. 덱 복구
        playerDiceDeck = new List<string>(data.myDeck);

        // 3. 유물 복구 (ID로 찾아서 다시 생성)
        activeRelics.Clear();
        if (RelicDB.Instance != null)
        {
            foreach (string id in data.myRelicIDs)
            {
                Relic relicData = RelicDB.Instance.GetRelicByID(id);
                if (relicData != null)
                {
                    // AddRelic()을 그냥 부르면 얻을때 되는 효과가
                    // 중복 적용될 수 있으므로, 리스트에만 담고 패시브 효과만 갱신해야 합니다.
                    // 여기서는 간단히 activeRelics에만 넣고 효과 갱신 함수를 호출합니다.
                    activeRelics.Add(relicData);
                }
            }
        }
        if (RelicEffectHandler.Instance != null)
        {
            RelicEffectHandler.Instance.ResetForNewRun();
            Debug.Log($"[RelicEffectHandler] 유물 효과 초기화 완료 (유물 {activeRelics.Count}개)");
        }

        // 4. 웨이브 생성 및 UI 갱신
        if (WaveGenerator.Instance != null)
        {
            WaveGenerator.Instance.BuildRunZoneOrder(); //존순서같은것도 저장해야하는데 일단 랜덤으로두기
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            UIManager.Instance.UpdateGold(CurrentGold);
            UIManager.Instance.UpdateRelicPanel(activeRelics);
            UIManager.Instance.UpdateWaveText(CurrentZone, CurrentWave);
        }

        // 5. 스테이지 준비
        if (StageManager.Instance != null)
        {
            StageManager.Instance.PrepareNextWave();
        }
    }

    public void StartNewRun()
    {
        CurrentGold = 0;
        CurrentZone = 1;
        CurrentWave = 1;
        MaxPlayerHealth = 100;
        PlayerHealth = MaxPlayerHealth; 
        CurrentShield = 0; 

        playerDiceDeck.Clear();
        activeRelics.Clear();
        nextZoneBuffs.Clear();

        buffDuration = 0;
        buffDamageValue = 0;
        buffShieldValue = 0;
        buffRerollValue = 0;
        hasInsurance = false;

        ApplyMetaUpgrades();
        ApplyMarketItems();

        if (WaveGenerator.Instance != null)
        {
            WaveGenerator.Instance.BuildRunZoneOrder();
        }

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

        if (WaveGenerator.Instance != null)
        {
            WaveGenerator.Instance.BuildRunZoneOrder();
        }
        ZoneData startingZone = WaveGenerator.Instance.GetCurrentZoneData(1);
        string zoneName = startingZone != null ? startingZone.zoneName : "평원";

        //이벤트 시스템: 새 런 시작 시 존 이벤트 + 핸들러 초기화
        ZoneContext zoneCtx = new ZoneContext
        {
            ZoneNumber = CurrentZone,
            ZoneName = zoneName
        };
        GameEvents.RaiseZoneStart(zoneCtx);
        
        if (RelicEffectHandler.Instance != null)
        {
            RelicEffectHandler.Instance.ResetForNewRun();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.FadeIn();
            UIManager.Instance.ShowZoneTitle("Zone 1: " + zoneName);
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            //TODO
            //실드있으면 별도로 텍스트 색이라도 변경?해야할거같은데  일단 보류
            UIManager.Instance.UpdateGold(CurrentGold);
            UIManager.Instance.UpdateRelicPanel(activeRelics);
        }
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
        // ScriptableObject 리스트를 순회하며 자동으로 합산
        
        // 최대 체력 
        int bonusHealth = (int)GetTotalMetaBonus(MetaEffectType.MaxHealth);
        ModifyMaxHealth(bonusHealth); 
        PlayerHealth = MaxPlayerHealth; // 체력 100%로 시작

        // 시작 자금
        int bonusGold = (int)GetTotalMetaBonus(MetaEffectType.StartGold);
        AddGold(bonusGold);

        //임시 체력 
        CurrentShield = (int)GetTotalMetaBonus(MetaEffectType.Shield);

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

            //전투 보조 (3웨이브짜리 버프 설정)
            else if (key == "Buff_Damage_3wave_2") { buffDuration = 3; buffDamageValue = 2; }
            else if (key == "Buff_Shield_3wave_3") { buffDuration = 3; buffShieldValue = 3; }
            else if (key == "Buff_Reroll_3wave_2") { buffDuration = 3; buffRerollValue = 2; }
        }

        // 사용했으니 초기화
        PlayerPrefs.SetString("NextRunBuffs", ""); 
    }

    //UIManager업데이트 메서드
    public void StartNewWave()
    {
        Debug.Log($"[Zone {CurrentZone} - Wave {CurrentWave}] 웨이브 시작.");
        
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

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateRollCount(0, diceController.maxRolls);
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
                //이벤트 시스템: 존 종료 이벤트
                ZoneContext zoneEndCtx = new ZoneContext
                {
                    ZoneNumber = CurrentZone,
                    ZoneName = WaveGenerator.Instance?.GetCurrentZoneData(CurrentZone)?.zoneName ?? "Unknown"
                };
                GameEvents.RaiseZoneEnd(zoneEndCtx);
                
                UIManager.Instance.FadeOut(1.0f, () =>
                {
                    CurrentZone++;
                    CurrentWave = 1;
                    
                    //이벤트 시스템: 존 시작 이벤트 (작은 방패 등 초기화)
                    ZoneContext zoneCtx = new ZoneContext
                    {
                        ZoneNumber = CurrentZone,
                        ZoneName = WaveGenerator.Instance?.GetCurrentZoneData(CurrentZone)?.zoneName ?? "Unknown"
                    };
                    GameEvents.RaiseZoneStart(zoneCtx);
                    
                    UIManager.Instance.StartMaintenancePhase();
                    UIManager.Instance.FadeIn();

                });
            }
            else
            {
                Debug.Log("웨이브 클리어! 유물 보상.");
                ShowRewardScreen();
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
                Debug.Log($"쉴드 방어! 남은 쉴드: {CurrentShield}");
            }
            else
            {
                PlayerHealth -= damageCtx.FinalDamage;
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
                    //ui들 없애기(연출해야되니까)
                    if (UIManager.Instance != null) UIManager.Instance.gameObject.SetActive(false);
                    GameOverDirector.Instance.PlayGameOverSequence(earnedCurrency);
                }
                else
                {
                    // 개발중 그냥게임씬에서 할떄
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowGameOverScreen(earnedCurrency);
                    }
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
        List<Relic> rewardOptions = RelicDB.Instance.GetRandomRelics(3);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowRewardScreen(rewardOptions);
        }
    }

    public void AddRelic(Relic chosenRelic)
    {
        if (chosenRelic == null) return;

        activeRelics.Add(chosenRelic);
        Debug.Log($"유물 획득: {chosenRelic.Name}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRelicPanel(activeRelics);
        }

        //이벤트 시스템: 유물 획득 이벤트 발생
        // 모든 '획득 즉시' 효과는 RelicEffectHandler.HandleRelicAcquire()에서 처리
        RelicContext relicCtx = new RelicContext
        {
            RelicID = chosenRelic.RelicID,
            RelicName = chosenRelic.Name
        };
        GameEvents.RaiseRelicAcquire(relicCtx);

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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRollCount(0, diceController.maxRolls);
        }
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
        
        // 이벤트 시스템: 피해 전 이벤트
        GameEvents.RaiseBeforePlayerDamaged(damageCtx);
        
        // 유물이 피해를 취소했는지 확인
        if (damageCtx.Cancelled)
        {
            Debug.Log($"[이벤트] {damageCtx.Source}의 피해가 무효화되었습니다!");
            return;
        }
        
        // 쉴드 먼저 소모
        if (CurrentShield > 0)
        {
            int shieldAbsorb = Mathf.Min(CurrentShield, damageCtx.FinalDamage);
            CurrentShield -= shieldAbsorb;
            damageCtx.FinalDamage -= shieldAbsorb;
            Debug.Log($"쉴드 방어! -{shieldAbsorb} (남은 쉴드: {CurrentShield})");
        }
        
        if (damageCtx.FinalDamage > 0)
        {
            PlayerHealth -= damageCtx.FinalDamage;
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
        // 이벤트 시스템: 사망 이벤트 (유물이 부활 가능)
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
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOverScreen(earnedCurrency);
            }
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

}