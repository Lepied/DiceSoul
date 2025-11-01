using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [수정] '정비 단계'를 ShopManager와 연동
/// [수정] ShopManager가 요구하는 Helper 함수들 추가 (SpendScore, HealPlayer 등)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 상태")]
    public int PlayerHealth;
    public int MaxPlayerHealth = 10;
    public int CurrentScore { get; private set; }

    [Header("게임 밸런스")]
    public int bonusPerRollRemaining = 5;
    public int wavesPerZone = 5; 

    [Header("런(Run) 진행 상태")]
    public int CurrentZone = 1; 
    public int CurrentWave = 1;
    
    public List<Relic> activeRelics = new List<Relic>();
    // [추가] 플레이어의 주사위 덱 상태
    // (TODO: 이 데이터를 기반으로 DiceController가 주사위 굴림)
    public List<string> playerDiceDeck = new List<string> { "D6", "D6", "D6", "D6", "D6" };

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
        StartNewRun();
    }

    public void StartNewRun()
    {
        PlayerHealth = MaxPlayerHealth;
        CurrentScore = 0; 
        CurrentZone = 1;
        CurrentWave = 1; 
        
        // (TODO: playerDiceDeck 초기화)
        
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
        
        int finalScore = (int)(scoreToAdd * scoreMultiplier);
        CurrentScore += finalScore;

        Debug.Log($"점수 획득: +{finalScore} (총: {CurrentScore})");
        
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

            if (CurrentWave > wavesPerZone)
            {
                Debug.Log($"[Zone {CurrentZone}] 클리어! 정비 단계로 진입합니다.");
                StartMaintenancePhase();
            }
            else
            {
                Debug.Log("웨이브 클리어! 보상 화면으로 이동합니다.");
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
    
    public void OnRelicSelected(Relic chosenRelic)
    {
        AddRelic(chosenRelic); // [수정] AddRelic 헬퍼 함수 사용
        
        if (StageManager.Instance != null)
        {
            StageManager.Instance.PrepareNextWave(); 
        }
    }


    // --- [신규/수정된 함수 (상점 연동)] ---

    /// <summary>
    /// [수정] ShopManager를 호출하도록 변경
    /// </summary>
    private void StartMaintenancePhase()
    {
        if (ShopManager.Instance == null)
        {
            Debug.LogError("ShopManager가 씬에 없습니다!");
            return;
        }

        // 1. ShopManager에게 아이템 리스트 생성 요청
        List<ShopItem> shopItems = ShopManager.Instance.GenerateShopItems(CurrentZone);
        
        // 2. UIManager에게 '진짜' 아이템 리스트를 전달하며 상점 열기 요청
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMaintenanceScreen(shopItems);
        }
    }

    /// <summary>
    /// UIManager가 호출. 상점을 닫고 다음 존으로 이동합니다. (변경 없음)
    /// </summary>
    public void EndMaintenancePhase()
    {
        Debug.Log("정비 단계 종료. 다음 존으로 이동합니다.");

        CurrentZone++;
        CurrentWave = 1; 

        if (StageManager.Instance != null)
        {
            StageManager.Instance.PrepareNextWave();
        }
    }

    /// <summary>
    /// [신규] ShopManager가 호출. 점수를 사용합니다.
    /// </summary>
    public void SpendScore(int amount)
    {
        CurrentScore -= amount;
        
        // 점수 UI 즉시 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(CurrentScore);
            // (TODO: 상점의 다른 버튼들 구매 가능 여부 갱신)
        }
    }

    /// <summary>
    /// [신규] ShopManager가 호출. 체력을 회복합니다.
    /// </summary>
    public void HealPlayer(int amount)
    {
        PlayerHealth = Mathf.Min(PlayerHealth + amount, MaxPlayerHealth);
        Debug.Log($"체력 회복! 현재: {PlayerHealth}/{MaxPlayerHealth}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
        }
    }

    /// <summary>
    /// [신규] ShopManager 또는 유물 획득 시 호출.
    /// </summary>
    public void AddRelic(Relic relic)
    {
        if (relic == null) return;
        activeRelics.Add(relic);
        Debug.Log($"유물 획득: {relic.Name}");

        // (TODO: 유물 획득 즉시 효과 적용 로직? 예: AddMaxRolls)
        // ApplyImmediateRelicEffect(relic);
    }
    
    /// <summary>
    /// [신규] ShopManager가 호출. 주사위 덱을 업그레이드합니다.
    /// </summary>
    public void UpgradeDice(string diceType) // (예: "D8")
    {
        // "D6"를 찾아 "D8"로 바꿈 (하나만)
        int index = playerDiceDeck.IndexOf("D6");
        if (index != -1)
        {
            playerDiceDeck[index] = diceType;
            Debug.Log($"주사위 업그레이드! 덱 상태: {string.Join(", ", playerDiceDeck)}");
        }
        else
        {
            Debug.Log("업그레이드할 D6 주사위가 없습니다.");
            // (TODO: D6가 없으면 D8을 D10으로 바꿔주는 등 예외 처리)
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
                    diceController.ApplyRollBonus(relic.EffectIntValue);
                    break;
                
                case RelicEffectType.AddDice:
                    break;
                case RelicEffectType.ModifyDiceValue:
                    break;
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRollCount(0, diceController.maxRolls);
        }
    }

}

