using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용

/// <summary>
/// [수정] 
/// 1. activeRelics 리스트를 Relic 클래스 타입으로 변경
/// 2. AddRelic(Relic relic) 함수 추가
/// 3. ApplyAllRelicEffects가 Relic 리스트를 읽도록 수정
/// 4. GetAttackDamageBonus() 함수 추가 (데미지 유물 계산용)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 상태")]
    public int PlayerHealth;
    public int MaxPlayerHealth = 10;
    public int CurrentScore { get; private set; }

    [Header("덱 정보")]
    // (시작 시 StartNewRun에서 "D6" 5개로 초기화됨)
    public List<string> playerDiceDeck = new List<string>(); 
    
    // 플레이어가 보유한 유물 리스트
    public List<Relic> activeRelics = new List<Relic>();

    [Header("게임 밸런스")]
    public int bonusPerRollRemaining = 5;
    public int wavesPerZone = 5; // 5 웨이브마다 1 존

    [Header("런(Run) 진행 상태")]
    public int CurrentZone = 1; 
    public int CurrentWave = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartNewRun(); // (씬 로드 시 첫 실행)
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

        // 기본 주사위 덱 설정
        playerDiceDeck.Clear();
        for(int i=0; i<5; i++)
        {
            playerDiceDeck.Add("D6");
        }
        activeRelics.Clear(); // 유물 초기화
        
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
        // 점수 배율 유물이 있는지 확인
        float scoreMultiplier = 1.0f;
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddScoreMultiplier))
        {
            scoreMultiplier *= relic.FloatValue; // (예: 1.5f)
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
            
            // 존(Zone) 클리어 확인
            if(CurrentWave > wavesPerZone)
            {
                // [정비 단계]
                Debug.Log("존 클리어! 정비(상점) 단계 시작.");
                CurrentZone++;
                CurrentWave = 1;
                
                if(UIManager.Instance != null)
                {
                    UIManager.Instance.StartMaintenancePhase();
                }
            }
            else
            {
                // [일반 보상]
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
                Debug.Log("게임 오버");
                return; 
            }

            // 실패 시에도 다음 웨이브로 (보상/상점 없이)
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

    // UIManager(보상) 또는 ShopManager(구매)가 호출
    public void AddRelic(Relic chosenRelic)
    {
        if (chosenRelic == null) return;
        
        activeRelics.Add(chosenRelic);
        Debug.Log($"유물 획득: {chosenRelic.Name}");

        // 유물 획득이 상점(Maintenance) 중에 일어난 게 *아니라면*,
        // (즉, 웨이브 보상으로 얻었다면) 다음 웨이브로 진행
        if (UIManager.Instance != null && !UIManager.Instance.IsShopOpen())
        {
             if (StageManager.Instance != null)
            {
                StageManager.Instance.PrepareNextWave();
            }
        }
    }

    /// <summary>
    /// Relic 클래스의 EffectType과 값을 읽도록 변경
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
                
                // (TODO: AddDice 유물 효과 적용)
                case RelicEffectType.AddDice:
                    // AddDiceToDeck(relic.StringValue); // (이 방식은 영구적이라 매번 호출하면 안 됨)
                    break;
                
                // (TODO: ModifyDiceValue 등 다른 유물 효과 적용)
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
        playerDiceDeck.Add(diceType);
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
    /// [신규] 현재 유물에 따른 '기본 데미지' 보너스 값을 계산하여 반환합니다.
    /// (StageManager가 호출)
    /// </summary>
    public int GetAttackDamageBonus()
    {
        int bonusDamage = 0;
        foreach (Relic relic in activeRelics.Where(r => r.EffectType == RelicEffectType.AddBaseDamage))
        {
            bonusDamage += relic.IntValue; // 예: "숫돌" (+5 데미지)
        }
        return bonusDamage;
    }
}

