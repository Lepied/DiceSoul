using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. AddRelic() 함수: '유리 대포'(체력-), '집중'(덱제거), '무거운 주사위'(굴림-) 효과 구현
/// 2. GetAttackDamageModifiers() 함수: '금권 정치'(점수비례), '피의 갈증'(잃은체력비례) 효과 구현
/// 3. GetAttackScoreBonus() 함수: '녹슨 톱니'(점수+) 효과 구현
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 상태")]
    public int PlayerHealth;
    public int MaxPlayerHealth = 10;
    public int CurrentScore { get; private set; }

    [Header("덱 정보")]
    public List<string> playerDiceDeck = new List<string>();
    public List<Relic> activeRelics = new List<Relic>();

    [Header("게임 밸런스")]
    public int bonusPerRollRemaining = 5;
    public int wavesPerZone = 5;
    public int maxDiceCount = 8;
    public int minDiceCount = 3; // [신규] '집중' 유물을 위한 최소 주사위 수

    [Header("런(Run) 진행 상태")]
    public int CurrentZone = 1;
    public int CurrentWave = 1;

    [Header("영구 재화")]
    public string metaCurrencySaveKey = "MetaCurrency";

    private string selectedDeckKey = "SelectedDeck";


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartNewRun();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartNewRun()
    {
        PlayerHealth = MaxPlayerHealth;
        CurrentScore = 0;
        CurrentZone = 1;
        CurrentWave = 1;

        playerDiceDeck.Clear();
        activeRelics.Clear();

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

            case "Default":
            default:
                for (int i = 0; i < 5; i++)
                {
                    playerDiceDeck.Add("D6");
                }
                break;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            UIManager.Instance.UpdateScore(CurrentScore);
            UIManager.Instance.UpdateRelicPanel(activeRelics);
        }
    }

    public void StartNewWave()
    {
        Debug.Log($"[Zone {CurrentZone} - Wave {CurrentWave}] 웨이브 시작.");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateWaveText(CurrentZone, CurrentWave);
        }
    }

    public void AddScore(int scoreToAdd, AttackJokbo jokbo)
    {
        float globalMultiplier = 1.0f;
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddScoreMultiplier))
        {
            globalMultiplier *= relic.FloatValue;
        }

        float jokboMultiplier = GetJokboScoreMultiplier(jokbo);
        // [수정] 족보별 '보너스 점수' 추가 (예: 녹슨 톱니)
        int bonusScore = GetAttackScoreBonus(jokbo);

        int finalScore = (int)((scoreToAdd + bonusScore) * globalMultiplier * jokboMultiplier);
        CurrentScore += finalScore;

        Debug.Log($"점수 획득(족보): +{finalScore} (기본: {scoreToAdd}, 보너스: {bonusScore}, 전역배율: {globalMultiplier}x, 족보배율: {jokboMultiplier}x) (총: {CurrentScore})");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(CurrentScore);
        }
    }

    public void AddScore(int scoreToAdd)
    {
        float globalMultiplier = 1.0f;
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddScoreMultiplier))
        {
            globalMultiplier *= relic.FloatValue;
        }

        int finalScore = (int)(scoreToAdd * globalMultiplier);
        CurrentScore += finalScore;

        Debug.Log($"점수 획득(보너스): +{finalScore} (기본: {scoreToAdd}, 전역배율: {globalMultiplier}x) (총: {CurrentScore})");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(CurrentScore);
        }
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
                AddScore(bonus);
            }

            if (CurrentWave > wavesPerZone)
            {
                Debug.Log("존 클리어! 정비(상점) 단계 시작.");
                CurrentZone++;
                CurrentWave = 1;

                if (UIManager.Instance != null && UIManager.Instance.gameObject.activeInHierarchy)
                {
                    UIManager.Instance.StartMaintenancePhase();
                }
            }
            else
            {
                Debug.Log("웨이브 클리어! 유물 보상.");
                ShowRewardScreen();
            }
        }
        else
        {
            Debug.Log("웨이브 실패. 체력이 1 감소합니다.");
            PlayerHealth--;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            }

            if (PlayerHealth <= 0)
            {
                Debug.Log("게임 오버. 영구 재화를 저장합니다.");

                int earnedCurrency = CalculateAndSaveMetaCurrency();

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowGameOverScreen(earnedCurrency);
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

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// '획득 즉시' 발동하는 유물 효과(유리 대포, 집중, 무거운 주사위 등)를 여기서 처리합니다.
    /// </summary>
    public void AddRelic(Relic chosenRelic)
    {
        if (chosenRelic == null) return;

        activeRelics.Add(chosenRelic);
        Debug.Log($"유물 획득: {chosenRelic.Name}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRelicPanel(activeRelics);
        }

        // [수정] '획득 즉시' 발동 효과 처리
        switch (chosenRelic.EffectType)
        {
            case RelicEffectType.AddDice:
                if (playerDiceDeck.Count < maxDiceCount)
                {
                    AddDiceToDeck(chosenRelic.StringValue);
                }
                break;

            // [!!! 신규 추가 !!!]
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

        // [신규] '무거운 주사위', '유리 대포', '집중'은 여러 효과를 가질 수 있음
        // (ID로 하드코딩하여 2차 효과 처리)
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

    /// <summary>
    /// [수정] '무거운 주사위' (ModifyMaxRolls) 효과 적용
    /// </summary>
    public void ApplyAllRelicEffects(DiceController diceController)
    {
        Debug.Log("보유한 유물 효과를 모두 적용합니다...");

        // (DiceController가 maxRolls를 baseMaxRolls로 리셋한 후)
        foreach (Relic relic in activeRelics)
        {
            switch (relic.EffectType)
            {
                case RelicEffectType.AddMaxRolls:
                    diceController.ApplyRollBonus(relic.IntValue);
                    break;

                // [!!! 신규 추가 !!!]
                case RelicEffectType.ModifyMaxRolls: // (예: 무거운 주사위)
                    diceController.ApplyRollBonus(relic.IntValue); // (IntValue: -1)
                    break;
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRollCount(0, diceController.maxRolls);
        }
    }

    // --- 상점 구매용 함수들 ---
    public bool SubtractScore(int amount)
    {
        if (CurrentScore >= amount)
        {
            CurrentScore -= amount;
            UIManager.Instance.UpdateScore(CurrentScore);
            return true;
        }
        return false;
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

    // --- 유물 효과 계산 함수들 ---

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// '금권 정치'(점수 비례), '피의 갈증'(잃은 체력 비례) 효과 구현
    /// </summary>
    public int GetAttackDamageModifiers(AttackJokbo jokbo)
    {
        int bonusDamage = 0;

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

        // 3. [!!! 신규 추가 !!!] '금권 정치' (점수 비례)
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.DynamicDamage_Score))
        {
            // (CurrentScore 100점당 +1, 최대 +50)
            int dynamicBonus = Mathf.Min(50, CurrentScore / 100);
            bonusDamage += dynamicBonus;
        }

        // 4. [!!! 신규 추가 !!!] '피의 갈증' (잃은 체력 비례)
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.DynamicDamage_LostHealth))
        {
            int lostHealth = MaxPlayerHealth - PlayerHealth;
            bonusDamage += lostHealth;
        }

        return bonusDamage;
    }
    public float GetJokboScoreMultiplier(AttackJokbo jokbo)
    {
        float multiplier = 1.0f;

        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.JokboScoreMultiplier))
        {
            if (jokbo.Description.Contains(relic.StringValue))
            {
                multiplier *= relic.FloatValue;
            }
        }
        return multiplier;
    }

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// '녹슨 톱니' (모든 족보 점수 +) 효과 구현
    /// </summary>
    public int GetAttackScoreBonus(AttackJokbo jokbo)
    {
        int bonusScore = 0;

        // [!!! 신규 추가 !!!]
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.JokboScoreAdd))
        {
            // "All"이거나, 족보 설명에 포함되면
            if (relic.StringValue == "All" || jokbo.Description.Contains(relic.StringValue))
            {
                bonusScore += relic.IntValue;
            }
        }

        return bonusScore;
    }
    public (float damageMultiplier, float scoreMultiplier) GetRollCountBonuses(int currentRoll)
    {
        float damageMult = 1.0f;
        float scoreMult = 1.0f;

        // 굴림 횟수 기반 보너스 유물을 모두 확인
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.RollCountBonus))
        {
            // ["명함" 유물 효과 ]
            if (relic.RelicID == "RLC_FIRST_IMPRESSION" && currentRoll == 1)
            {
                damageMult *= relic.FloatValue;
                scoreMult *= relic.FloatValue;
            }

            // 나중에 이렇게 확장
            // if (relic.RelicID == "RLC_NTH_ROLL" && currentRoll == relic.IntValue)
            // {
            //     damageMult *= relic.FloatValue;
            // }
        }

        return (damageMult, scoreMult);
    }
    /// <summary>
    /// [수정] 굴림 직후 호출됨.
    /// </summary>
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
        int earnedCurrency = (int)(CurrentScore * 0.1f) + (CurrentZone * 50);
        int totalMetaCurrency = PlayerPrefs.GetInt(metaCurrencySaveKey, 0);

        totalMetaCurrency += earnedCurrency;
        PlayerPrefs.SetInt(metaCurrencySaveKey, totalMetaCurrency);
        PlayerPrefs.Save();

        Debug.Log($"이번 런 획득 재화: {earnedCurrency}. 저장된 총 재화: {totalMetaCurrency}");
        return earnedCurrency;
    }
}