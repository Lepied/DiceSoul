using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 모든 '공격 족보'를 정의하고,
/// 현재 주사위로 달성 가능한 족보 리스트를 반환하는 데이터베이스.
/// (MonoBehaviour 싱글톤 방식)
    /// </summary>
public class AttackDB : MonoBehaviour
{
    public static AttackDB Instance { get; private set; }

    // 게임에 존재하는 모든 공격 족보 리스트
    private List<AttackJokbo> allJokbos = new List<AttackJokbo>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeJokbos(); // 게임 시작 시 모든 족보를 미리 정의
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// 여기에 모든 족보의 데미지, 점수, 달성 조건을 코드로 정의합니다.
    /// (데미지가 높은 순서대로 정렬)
    /// </summary>
    private void InitializeJokbos()
    {
        // --- 7. 야찌 (5개) ---
        allJokbos.Add(new AttackJokbo(
            "야찌 (5개)",
            150, // BaseDamage
            100, // BaseScore
            (diceValues) => {
                var groups = diceValues.GroupBy(v => v);
                return groups.Any(g => g.Count() >= 5);
            }
        ));

        // --- 6. 포카드 (4개) ---
        allJokbos.Add(new AttackJokbo(
            "포카드 (4개)",
            80, // BaseDamage
            50, // BaseScore
            (diceValues) => {
                var groups = diceValues.GroupBy(v => v);
                return groups.Any(g => g.Count() >= 4);
            }
        ));
        
        // --- 5. 풀 하우스 (3+2) ---
        allJokbos.Add(new AttackJokbo(
            "풀 하우스 (3+2)",
            70, // BaseDamage
            40, // BaseScore
            (diceValues) => {
                var groups = diceValues.GroupBy(v => v);
                // 3개짜리 그룹이 있고, 2개짜리 그룹이 있는지
                return groups.Any(g => g.Count() == 3) && groups.Any(g => g.Count() == 2);
            }
        ));

        // --- 4. 스트레이트 (5연속) ---
        // (참고: 5개 주사위로 4연속(Small Straight)과 5연속(Large Straight)을 구분할 수 있음)
        allJokbos.Add(new AttackJokbo(
            "스트레이트 (5연속)",
            60, // BaseDamage
            35, // BaseScore
            (diceValues) => {
                var sorted = diceValues.Distinct().OrderBy(v => v).ToList();
                if (sorted.Count < 5) return false;
                // (1,2,3,4,5) 또는 (2,3,4,5,6)
                bool straight1 = sorted.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 });
                bool straight2 = sorted.SequenceEqual(new List<int> { 2, 3, 4, 5, 6 });
                return straight1 || straight2;
            }
        ));
        
        // --- 3. 트리플 (3개) ---
        allJokbos.Add(new AttackJokbo(
            "트리플 (3개)",
            40, // BaseDamage
            20, // BaseScore
            (diceValues) => {
                var groups = diceValues.GroupBy(v => v);
                return groups.Any(g => g.Count() >= 3);
            }
        ));

        // --- 2. 모두 짝수 / 모두 홀수 (마법 족보) ---
        allJokbos.Add(new AttackJokbo(
            "모두 짝수",
            30, // BaseDamage
            15, // BaseScore
            (diceValues) => diceValues.All(v => v % 2 == 0)
        ));
        allJokbos.Add(new AttackJokbo(
            "모두 홀수",
            30, // BaseDamage
            15, // BaseScore
            (diceValues) => diceValues.All(v => v % 2 != 0)
        ));

        // --- 1. 총합 (기본 족보) ---
        // '총합'은 다른 족보와 달리, 항상 달성 가능하며 점수가 값에 비례하도록 특수 처리
        allJokbos.Add(new AttackJokbo(
            "총합",
            0, // (특수) 데미지를 값의 합계로 설정
            0, // (특수) 점수를 값의 합계로 설정
            (diceValues) => true, // 항상 true
            (diceValues) => diceValues.Sum(), // 데미지 계산 로직
            (diceValues) => diceValues.Sum()  // 점수 계산 로직
        ));
        
        // (추가) 투 페어, 더블 등...
        // ...
    }

    /// <summary>
    /// 현재 주사위 리스트로 달성 가능한 '모든' 족보 리스트를 반환합니다.
    /// (데미지가 높은 순서대로 정렬)
    /// </summary>
    public List<AttackJokbo> GetAchievableJokbos(List<int> diceValues)
    {
        List<AttackJokbo> achievableJokbos = new List<AttackJokbo>();

        foreach (var jokbo in allJokbos)
        {
            if (jokbo.CheckCondition(diceValues))
            {
                // [수정] 족보 정의에 있는 계산 로직을 사용하여 데미지와 점수를 설정
                jokbo.CalculateValues(diceValues); 
                achievableJokbos.Add(jokbo);
            }
        }
        
        // 데미지가 높은 순서대로 정렬하여 반환
        return achievableJokbos.OrderByDescending(j => j.BaseDamage).ToList();
    }
}

