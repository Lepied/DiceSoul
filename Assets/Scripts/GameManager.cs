using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[DefaultExecutionOrder(-50)] //현재 WaveGenerator(-100) 다음, StageManager(0) 이전
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 상태")]
    public int PlayerHealth;
    public int MaxPlayerHealth = 10;
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
        MaxPlayerHealth = 10;
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
            if (key == "MaxHealth_3") ModifyMaxHealth(3);
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
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateWaveText(CurrentZone, CurrentWave);
        }
    }

    public void AddGold(int goldToAdd, AttackJokbo jokbo)
    {
        float globalMultiplier = 1.0f;
        //포션으로 인한 버프 먼저(합연산)
        if (nextZoneBuffs.Contains("GoldBoost"))
        {
            globalMultiplier += 0.5f;
        }
        //유물 버프 (곱연산)
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddGoldMultiplier))
        {
            globalMultiplier *= relic.FloatValue;
        }

        float jokboMultiplier = GetJokboGoldMultiplier(jokbo);
        // 족보별 '보너스 금화' 추가 (예: 녹슨 톱니)
        int bonusGold = GetAttackGoldBonus(jokbo);

        int finalGold = (int)((goldToAdd + bonusGold) * globalMultiplier * jokboMultiplier);
        CurrentGold += finalGold;

        Debug.Log($"금화 획득(족보): +{finalGold} (기본: {goldToAdd}, 보너스: {bonusGold}, 전역배율: {globalMultiplier}x, 족보배율: {jokboMultiplier}x) (총: {CurrentGold})");

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
                UIManager.Instance.FadeOut(1.0f, () =>
                {
                    CurrentZone++;
                    CurrentWave = 1;
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
            Debug.Log("웨이브 실패. 체력이 1 감소합니다.");
            if (CurrentShield > 0)
            {
                CurrentShield--;
                Debug.Log($"쉴드 방어! 남은 쉴드: {CurrentShield}");
                //TODO 
                // UI에 쉴드 감소 연출 추가하기?
            }
            else
            {
                PlayerHealth--;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            }

            if (PlayerHealth <= 0)
            {
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

        //  '획득 즉시' 발동 효과 처리
        switch (chosenRelic.EffectType)
        {
            case RelicEffectType.AddDice:
                if (playerDiceDeck.Count < maxDiceCount)
                {
                    AddDiceToDeck(chosenRelic.StringValue);
                }
                break;

            case RelicEffectType.ModifyHealth: // (예: 유리 대포)
                ModifyMaxHealth(chosenRelic.IntValue); // (음수 값 전달)
                break;

            case RelicEffectType.RemoveDice: // (예: 집중)
                if (playerDiceDeck.Count > minDiceCount)
                {
                    RemoveDiceFromDeck(chosenRelic.StringValue); // (예: "D6" 제거)
                }
                break;

            case RelicEffectType.ModifyMaxRolls: // (예: 무거운 주사위)
                // (이 효과는 'AddMaxRolls'와 다름. ApplyAllRelicEffects에 추가)
                break;
        }

        switch (chosenRelic.RelicID)
        {
            case "RLC_HEAVY_DICE": // 무거운 주사위 (JokboDamageAdd + ModifyMaxRolls)
                ApplyAllRelicEffects(StageManager.Instance.diceController); // (최대 굴림 횟수 즉시 갱신)
                break;
            case "RLC_GLASS_CANNON": // 유리 대포 (AddBaseDamage + ModifyHealth)
                ModifyMaxHealth(-2); // (RelicDB 정의와 별개로 하드코딩)
                break;
            case "RLC_FOCUS": // 집중 (AddBaseDamage + RemoveDice)
                if (playerDiceDeck.Count > minDiceCount)
                {
                    RemoveDiceFromDeck("D6"); // (RelicDB 정의와 별개로 하드코딩)
                }
                break;
        }


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
        foreach (Relic relic in activeRelics)
        {
            switch (relic.EffectType)
            {
                case RelicEffectType.AddMaxRolls:
                    diceController.ApplyRollBonus(relic.IntValue);
                    break;


                case RelicEffectType.ModifyMaxRolls: // (예: 무거운 주사위)
                    diceController.ApplyRollBonus(relic.IntValue); // (IntValue: -1)
                    break;
            }
        }
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

    public void HealPlayer(int amount)
    {
        PlayerHealth = Mathf.Min(PlayerHealth + amount, MaxPlayerHealth);
        UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
    }


    public void ModifyMaxHealth(int amount)
    {
        MaxPlayerHealth += amount;
        if (PlayerHealth > MaxPlayerHealth) // (체력이 최대 체력보다 높으면 깎음)
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

    // 데미지 계산식 / 유물 효과 계산 
    public int GetAttackDamageModifiers(AttackJokbo jokbo)
    {
        int bonusDamage = 0;

        //영구강화로 얻은 데미지 보너스부터 계산
        bonusDamage += (int)GetTotalMetaBonus(MetaEffectType.BaseDamage);

        //그 다음 포션버프
        if (buffDuration > 0)
        {
            bonusDamage += buffDamageValue;
        }

        // 1. '모든' 족보 데미지 증가 (예: 숫돌, 유리 대포, 집중)
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddBaseDamage))
        {
            bonusDamage += relic.IntValue;
        }

        // 2. '특정' 족보 데미지 증가 (예: 검술 교본, 무거운 주사위)
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.JokboDamageAdd))
        {
            if (jokbo.Description.Contains(relic.StringValue))
            {
                bonusDamage += relic.IntValue;
            }
        }

        // 3. '금권 정치' (금화 비례)
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.DynamicDamage_Gold))
        {
            // (CurrentGold 100점당 +1, 최대 +50)
            int dynamicBonus = Mathf.Min(50, CurrentGold / 100);
            bonusDamage += dynamicBonus;
        }

        // 4.  '피의 갈증' (잃은 체력 비례)
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.DynamicDamage_LostHealth))
        {
            int lostHealth = MaxPlayerHealth - PlayerHealth;
            bonusDamage += lostHealth;
        }

        //ex . 포션 버프 
        if (nextZoneBuffs.Contains("DamageBoost"))
        {
            bonusDamage += 15; // 존 내내 데미지 +10
        }

        return bonusDamage;
    }
    public float GetJokboGoldMultiplier(AttackJokbo jokbo)
    {
        float multiplier = 1.0f;

        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.JokboGoldMultiplier))
        {
            if (jokbo.Description.Contains(relic.StringValue))
            {
                multiplier *= relic.FloatValue;
            }
        }
        return multiplier;
    }

    public int GetAttackGoldBonus(AttackJokbo jokbo)
    {
        int bonusGold = 0;
        //영구강화로 금화 보너스 계산
        bonusGold += (int)GetTotalMetaBonus(MetaEffectType.GoldBonus);
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.JokboGoldAdd))
        {
            // "All"이거나, 족보 설명에 포함되면
            if (relic.StringValue == "All" || jokbo.Description.Contains(relic.StringValue))
            {
                bonusGold += relic.IntValue;
            }
        }

        return bonusGold;
    }
    public (float damageMultiplier, float goldMultiplier) GetRollCountBonuses(int currentRoll)
    {
        float damageMult = 1.0f;
        float goldMult = 1.0f;

        // 굴림 횟수 기반 보너스 유물을 모두 확인
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.RollCountBonus))
        {
            // ["명함" 유물 효과 ]
            if (relic.RelicID == "RLC_BUSINESS_CARD" && currentRoll == 1)
            {
                damageMult *= relic.FloatValue;
                goldMult *= relic.FloatValue;
            }

            // 나중에 이렇게 확장
            // if (relic.RelicID == "RLC_NTH_ROLL" && currentRoll == relic.IntValue)
            // {
            //     damageMult *= relic.FloatValue;
            // }
        }

        return (damageMult, goldMult);
    }

    public List<int> ApplyDiceModificationRelics(List<int> originalValues)
    {
        List<int> modifiedValues = new List<int>(originalValues);
        bool wasModified = false;

        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.ModifyDiceValue))
        {
            wasModified = true;

            if (relic.RelicID == "RLC_ALCHEMY")
            {
                for (int i = 0; i < modifiedValues.Count; i++)
                {
                    if (modifiedValues[i] == 1)
                    {
                        modifiedValues[i] = 7;
                    }
                }
            }

            if (relic.RelicID == "RLC_LODESTONE")
            {
                for (int i = 0; i < modifiedValues.Count; i++)
                {
                    if (modifiedValues[i] % 2 != 0 && i < playerDiceDeck.Count)
                    {
                        modifiedValues[i] = RerollDice(playerDiceDeck[i]);
                    }
                }
            }

            if (relic.RelicID == "RLC_TANZANITE")
            {
                for (int i = 0; i < modifiedValues.Count; i++)
                {
                    if (modifiedValues[i] % 2 == 0 && i < playerDiceDeck.Count)
                    {
                        modifiedValues[i] = RerollDice(playerDiceDeck[i]);
                    }
                }
            }

            if (relic.RelicID == "RLC_FEATHER")
            {
                for (int i = 0; i < modifiedValues.Count; i++)
                {
                    if (modifiedValues[i] == 6 && i < playerDiceDeck.Count)
                    {
                        modifiedValues[i] = RerollDice(playerDiceDeck[i]);
                    }
                }
            }

            if (relic.RelicID == "RLC_COUNTERWEIGHT")
            {
                for (int i = 0; i < modifiedValues.Count; i++)
                {
                    if (playerDiceDeck[i] == "D20" && modifiedValues[i] <= 10)
                    {
                        modifiedValues[i] += 10;
                    }
                }
            }
        }

        if (wasModified)
        {
            Debug.Log($"유물 효과 적용! 굴림 값 변경: {string.Join(",", originalValues)} -> {string.Join(",", modifiedValues)}");
        }

        return modifiedValues;
    }

    private int RerollDice(string diceType)
    {
        switch (diceType)
        {
            case "D4":
                return Random.Range(1, 5); // 1~4
            case "D8":
                return Random.Range(1, 9); // 1~8
            case "D20":
                return Random.Range(1, 21); // 1~20
            case "D6":
            default:
                return Random.Range(1, 7); // 1~6
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