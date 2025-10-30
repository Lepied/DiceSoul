using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StageGoalDB : MonoBehaviour
{
    public static StageGoalDB Instance { get; private set; }

    // 모든 목표를 저장할 리스트
    private List<Goal> allGoals = new List<Goal>();
    
    // (선택) 성능을 위해 미리 분류해둘 수 있음
    private List<Goal> normalGoals = new List<Goal>();
    private List<Goal> bossGoals = new List<Goal>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGoals(); // 게임 시작 시 모든 목표를 생성
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 여기에 모든 목표를 코드로 정의합니다.
    /// </summary>
    private void InitializeGoals()
    {
        // --- 일반 목표 (Normal) ---
        allGoals.Add(new Goal(
            "총합 15 이상", 
            GoalType.Normal,
            (diceValues) => diceValues.Sum() >= 15
        ));

        allGoals.Add(new Goal(
            "총합 20 이상", 
            GoalType.Normal,
            (diceValues) => diceValues.Sum() >= 20
        ));
        
        allGoals.Add(new Goal(
            "같은 숫자 3개 (Triples)", 
            GoalType.Normal,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 3)
        ));

        allGoals.Add(new Goal(
            "모두 짝수", 
            GoalType.Normal,
            (diceValues) => diceValues.All(v => v % 2 == 0)
        ));

        // --- 보스 목표 (Boss) ---
        allGoals.Add(new Goal(
            "총합 30 이상 (보스)", 
            GoalType.Boss,
            (diceValues) => diceValues.Sum() >= 30
        ));

        allGoals.Add(new Goal(
            "같은 숫자 5개 (Yatzy!) (보스)", 
            GoalType.Boss,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 5)
        ));
        
        // (선택) 미리 분류
        normalGoals = allGoals.Where(g => g.Type == GoalType.Normal).ToList();
        bossGoals = allGoals.Where(g => g.Type == GoalType.Boss).ToList();
    }

    /// <summary>
    /// 랜덤한 일반 목표를 하나 뽑아서 반환합니다.
    /// </summary>
    public Goal GetRandomNormalGoal()
    {
        if (normalGoals.Count == 0) return null;
        
        int randomIndex = Random.Range(0, normalGoals.Count);
        return normalGoals[randomIndex];
    }

    /// <summary>
    /// 랜덤한 보스 목표를 하나 뽑아서 반환합니다.
    /// </summary>
    public Goal GetRandomBossGoal()
    {
        if (bossGoals.Count == 0) return null;

        int randomIndex = Random.Range(0, bossGoals.Count);
        return bossGoals[randomIndex];
    }
}