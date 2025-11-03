using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용

/// <summary>
/// [수정] 
/// 1. ApplyDiceModificationRelics 함수가 '자철석(RLC_LODESTONE)' 유물 효과(홀수 다시 굴리기)를 구현
/// 2. 이 때, 자신의 playerDiceDeck을 참조하여 올바른 타입(D4/D6/D8)으로 다시 굴림
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
    public int wavesPerZone = 5; // 5 웨이브마다 1 존
    public int maxDiceCount = 8; 

    [Header("런(Run) 진행 상태")]
    public int CurrentZone = 1; 
    public int CurrentWave = 1;

    
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
        
        // [수정] playerDiceDeck 초기화
        playerDiceDeck.Clear();
        for(int i=0; i<5; i++)
        {
            playerDiceDeck.Add("D6"); // 기본 덱 5x D6
        }
        activeRelics.Clear(); 
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            UIManager.Instance.UpdateScore(CurrentScore); 
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

    public void AddScore(int scoreToAdd)
    {
        float scoreMultiplier = 1.0f;
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddScoreMultiplier))
        {
            scoreMultiplier *= relic.FloatValue; 
        }
        
        int finalScore = (int)(scoreToAdd * scoreMultiplier);
        CurrentScore += finalScore;

        Debug.Log($"점수 획득: +{finalScore} (기본: {scoreToAdd}, 배율: {scoreMultiplier}) (총: {CurrentScore})");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(CurrentScore);
        }
    }


    public void ProcessWaveClear(bool isSuccess)
    {
        if (isSuccess)
        {
            CurrentWave++; 
            if(CurrentWave > wavesPerZone)
            {
                Debug.Log("존 클리어! 정비(상점) 단계 시작.");
                CurrentZone++;
                CurrentWave = 1;
                
                if(UIManager.Instance != null && UIManager.Instance.gameObject.activeInHierarchy) // UIManager 활성화 체크
                {
                    UIManager.Instance.StartMaintenancePhase(); // 상점 열기
                }
            }
            else
            {
                Debug.Log("웨이브 클리어! 유물 보상.");
                ShowRewardScreen(); // 유물 보상
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
                Debug.Log("게임 오버");
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
            Debug.LogError("RelicDatabase가 씬에 없습니다!");
            return;
        }
        List<Relic> rewardOptions = RelicDB.Instance.GetRandomRelics(3);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowRewardScreen(rewardOptions);
        }
    }

    /// <summary>
    /// UIManager(보상) 또는 ShopManager(구매)가 호출.
    /// 획득 즉시 발동하는 유물 효과(예: AddDice)를 여기서 처리합니다.
    /// </summary>
    public void AddRelic(Relic chosenRelic)
    {
        if (chosenRelic == null) return;
        
        activeRelics.Add(chosenRelic);
        Debug.Log($"유물 획득: {chosenRelic.Name}");

        // [신규] '획득 즉시' 발동 효과 처리
        switch (chosenRelic.EffectType)
        {
            case RelicEffectType.AddDice:
                if (playerDiceDeck.Count < maxDiceCount)
                {
                    AddDiceToDeck(chosenRelic.StringValue); 
                }
                break;
        }

        // (UI 흐름) 보상 화면에서 획득한 거라면 다음 웨이브로 진행
        if (UIManager.Instance != null && !UIManager.Instance.IsShopOpen())
        {
             if (StageManager.Instance != null)
            {
                StageManager.Instance.PrepareNextWave();
            }
        }
    }

    /// <summary>
    /// (웨이브 시작 시) '지속' 스탯형 유물 효과 적용
    /// </summary>
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
        return false; // 점수 부족
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

    /// <summary>
    /// 현재 유물에 따른 '기본 데미지' 보너스 값을 계산하여 반환합니다.
    /// </summary>
    public int GetAttackDamageBonus()
    {
        int bonusDamage = 0;
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddBaseDamage))
        {
            bonusDamage += relic.IntValue;
        }
        return bonusDamage;
    }
    
    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// 굴림 직후 호출됨. '주사위 값'을 변경하는 유물 효과를 적용합니다.
    /// (자신의 playerDiceDeck 리스트를 참조)
    /// </summary>
    public List<int> ApplyDiceModificationRelics(List<int> originalValues)
    {
        // 리스트를 복사하여 원본을 수정하지 않음
        List<int> modifiedValues = new List<int>(originalValues);
        bool wasModified = false;

        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.ModifyDiceValue))
        {
            wasModified = true;

            // --- 1. 연금술사의 돌 ("1" -> "7") ---
            if (relic.ID == "RLC_ALCHEMY")
            {
                for (int i = 0; i < modifiedValues.Count; i++)
                {
                    if (modifiedValues[i] == 1)
                    {
                        modifiedValues[i] = 7;
                    }
                }
            }

            // --- 2. 자철석 ("홀수" -> "다시 굴리기") ---
            if (relic.ID == "RLC_LODESTONE")
            {
                for (int i = 0; i < modifiedValues.Count; i++)
                {
                    // 홀수이고, 덱 정보가 존재할 때
                    if (modifiedValues[i] % 2 != 0 && i < playerDiceDeck.Count)
                    {
                        // 덱 타입에 맞춰서 '다시' 굴립니다.
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
            
            // --- 3. (다른 주사위 값 변경 유물들...) ---
        }

        if(wasModified)
        {
            Debug.Log($"유물 효과 적용! 굴림 값 변경: {string.Join(",", originalValues)} -> {string.Join(",", modifiedValues)}");
        }

        return modifiedValues;
    }
}

