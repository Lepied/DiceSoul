using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. ProcessWaveClear(bool isSuccess, int rollsRemaining)로 변경 (인자 추가)
/// 2. isSuccess == true일 때, 'rollsRemaining * bonusPerRollRemaining' 만큼
///    보너스 점수를 AddScore() 하도록 수정
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 상태")]
    public int PlayerHealth;
    public int MaxPlayerHealth = 10;
    public int CurrentScore { get; private set; }

    [Header("덱 정보")]
    private string selectedDeckKey = "SelectedDeck";
    public List<string> playerDiceDeck = new List<string>(); 
    public List<Relic> activeRelics = new List<Relic>();

    [Header("게임 밸런스")]
    public int bonusPerRollRemaining = 5;
    public int wavesPerZone = 5; 
    public int maxDiceCount = 8; 

    [Header("런(Run) 진행 상태")]
    public int CurrentZone = 1;
    public int CurrentWave = 1;

    [Header("영구 재화")]
    [Tooltip("PlayerPrefs에 저장될 영구 재화의 키(Key)")]
    public string metaCurrencySaveKey = "MetaCurrency";
    

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
        
        // [!!!] PlayerPrefs에서 '선택된 덱' 정보를 읽어옵니다.
        string selectedDeck = PlayerPrefs.GetString(selectedDeckKey, "Default");
        Debug.Log($"[GameManager] '{selectedDeck}' 덱으로 새 런을 시작합니다.");

        // [!!!] 선택된 덱에 따라 주사위 구성
        switch (selectedDeck)
        {
            case "Regular": // (예시) 단골 덱 (해금 필요)
                playerDiceDeck.Add("D8");
                playerDiceDeck.Add("D6");
                playerDiceDeck.Add("D6");
                playerDiceDeck.Add("D6");
                playerDiceDeck.Add("D6");
                break;
                
            // (예시) case "Gambler":
            //     playerDiceDeck.Add("D20");
            //     playerDiceDeck.Add("D4");
            //     ...
            //     break;
                
            case "Default":
            default:
                // 기본 덱: D6 5개
                for(int i=0; i<5; i++)
                {
                    playerDiceDeck.Add("D6"); 
                }
                break;
        }

        // [!!!] UI 업데이트 (activeRelics가 비었으므로 유물 UI도 리셋됨)
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


    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// (bool isSuccess, int rollsRemaining) 2개의 인자를 받습니다.
    /// </summary>
    public void ProcessWaveClear(bool isSuccess, int rollsRemaining)
    {
        if (isSuccess)
        {
            CurrentWave++; 

         
            // 남은 굴림 횟수만큼 보너스 점수 추가
            if (rollsRemaining > 0)
            {
                int bonus = rollsRemaining * bonusPerRollRemaining;
                Debug.Log($"남은 굴림 횟수 보너스: +{bonus}점 (남은 횟수: {rollsRemaining})");
                AddScore(bonus); // (보너스 점수이므로 족보 배율은 안 받음)
            }
            
            // (이하 동일) 존 클리어 또는 웨이브 클리어 처리
            if(CurrentWave > wavesPerZone)
            {
                Debug.Log("존 클리어! 정비(상점) 단계 시작.");
                CurrentZone++;
                CurrentWave = 1;
                
                if(UIManager.Instance != null && UIManager.Instance.gameObject.activeInHierarchy) 
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


            // [!!! 핵심 수정 !!!]
            if (PlayerHealth <= 0)
            {
                Debug.Log("게임 오버. 영구 재화를 저장합니다.");
                
                // 1. 재화 계산 및 저장
                int earnedCurrency = CalculateAndSaveMetaCurrency();
                
                // 2. UI 호출
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowGameOverScreen(earnedCurrency);
                }
                
                return; // (PrepareNextWave()를 호출하지 않고 런 종료)
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

        switch (chosenRelic.EffectType)
        {
            case RelicEffectType.AddDice:
                if (playerDiceDeck.Count < maxDiceCount)
                {
                    AddDiceToDeck(chosenRelic.StringValue); 
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

    public void UpgradeSingleDice(int diceIndex, string newDiceType)
    {
        if (diceIndex >= 0 && diceIndex < playerDiceDeck.Count)
        {
            playerDiceDeck[diceIndex] = newDiceType;
            Debug.Log($"덱 업그레이드: {diceIndex}번 주사위가 {newDiceType}이 되었습니다.");
        }
    }

    public int GetAttackDamageModifiers(AttackJokbo jokbo)
    {
        int bonusDamage = 0;
        
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddBaseDamage))
        {
            bonusDamage += relic.IntValue;
        }
        
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.JokboDamageAdd))
        {
            if (jokbo.Description.Contains(relic.StringValue))
            {
                bonusDamage += relic.IntValue;
            }
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

    public int GetAttackScoreBonus(AttackJokbo jokbo)
    {
        int bonusScore = 0;
        return bonusScore;
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
                        switch (playerDiceDeck[i])
                        {
                            case "D4":
                                modifiedValues[i] = Random.Range(1, 5); // 1~4
                                break;
                            case "D8":
                                modifiedValues[i] = Random.Range(1, 9); // 1~8
                                break;
                            case "D20":
                                modifiedValues[i] = Random.Range(1, 21); // 1~20
                                break;
                            case "D6":
                            default:
                                modifiedValues[i] = Random.Range(1, 7); // 1~6
                                break;
                        }
                    }
                }
            }

            if (relic.RelicID == "RLC_TANZANITE")
            {
                for (int i = 0; i < modifiedValues.Count; i++)
                {
                    if (modifiedValues[i] % 2 == 0 && i < playerDiceDeck.Count)
                    {
                        switch (playerDiceDeck[i])
                        {
                            case "D4": modifiedValues[i] = Random.Range(1, 5); break;
                            case "D8": modifiedValues[i] = Random.Range(1, 9); break;
                            case "D20": modifiedValues[i] = Random.Range(1, 21); break;
                            case "D6":
                            default: modifiedValues[i] = Random.Range(1, 7); break;
                        }
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
    
    /// <summary>
    /// 이번 런에서 획득한 재화를 계산하고, PlayerPrefs에 영구 저장합니다.
    /// </summary>
    /// <returns>이번 런에서 획득한 재화</returns>
    private int CalculateAndSaveMetaCurrency()
    {
        // 1. 이번 런에서 번 재화 계산 (예: 점수의 10% + 존*50)
        int earnedCurrency = (int)(CurrentScore * 0.1f) + (CurrentZone * 50);

        // 2. 기존에 저장된 재화 불러오기
        int totalMetaCurrency = PlayerPrefs.GetInt(metaCurrencySaveKey, 0);

        // 3. 합산하여 저장
        totalMetaCurrency += earnedCurrency;
        PlayerPrefs.SetInt(metaCurrencySaveKey, totalMetaCurrency);
        PlayerPrefs.Save(); // (필수)

        Debug.Log($"이번 런 획득 재화: {earnedCurrency}. 저장된 총 재화: {totalMetaCurrency}");
        return earnedCurrency;
    }
}