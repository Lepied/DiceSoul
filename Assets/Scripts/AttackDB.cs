using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 모든 '공격 족보'를 정의하고,
/// 현재 주사위로 달성 가능한 족보 리스트를 반환하는 데이터베이스.
/// (MonoBehaviour 싱글톤 방식)
/// [수정] 
/// 1. InitializeJokbos가 새 생성자(4-arg)를 사용하도록 변경
/// 2. GetAchievableJokbos가 CheckAndCalculate()를 사용하고,
///    족보 '복사본'을 반환하도록 수정 (버그 수정)
/// </summary>
public class AttackDB : MonoBehaviour
{
    public static AttackDB Instance { get; private set; }

    // 게임에 존재하는 모든 공격 족보 리스트 (프로토타입)
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
        allJokbos.Add(new AttackJokbo(
            "스트레이트 (5연속)",
            60, // BaseDamage
            35, // BaseScore
            (diceValues) => {
                var sorted = diceValues.Distinct().OrderBy(v => v).ToList();
                if (sorted.Count < 5) return false;
                // (1,2,3,4,5) 또는 (2,3,4,5,6)
                // (참고: D4 주사위로는 절대 달성 불가)
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
        // [!!! 오류 수정 !!!]
        // 7개 인자 생성자 대신, 4개 인자를 받는 '가변 족보' 생성자를 사용합니다.
        allJokbos.Add(new AttackJokbo(
            "총합", // Description
            (diceValues) => diceValues.Sum(), // 데미지 계산 로직
            (diceValues) => diceValues.Sum(), // 점수 계산 로직
            (diceValues) => true              // 달성 조건 (항상 true)
        ));
        
        // (추가) 투 페어, 더블 등...
        // ...
    }

    /// <summary>
    /// [!!! 오류 수정 !!!]
    /// CheckCondition/CalculateValues 대신 CheckAndCalculate를 사용합니다.
    /// '복사본'을 반환하여 원본 족보가 오염되는 버그를 수정합니다.
    /// </summary>
    public List<AttackJokbo> GetAchievableJokbos(List<int> diceValues)
    {
        List<AttackJokbo> achievableJokbos = new List<AttackJokbo>();

        // allJokbos 리스트의 '원본' 족보를 순회 (프로토타입)
        foreach (var jokboPrototype in allJokbos)
        {
            // 1. 프로토타입의 CheckAndCalculate를 호출
            // (이 함수는 jokboPrototype 내부의 BaseDamage/Score를 '임시'로 변경시킴)
            if (jokboPrototype.CheckAndCalculate(diceValues))
            {
                // 2. [!!! 중요 !!!]
                // 달성된 족보의 '복사본'을 새로 생성하여 리스트에 추가
                // (원본 프로토타입(jokboPrototype)을 리스트에 넣으면, 
                // "총합" 족보의 값이 영구적으로 변경되는 버그 발생)
                achievableJokbos.Add(new AttackJokbo(jokboPrototype));
            }
        }
        
        // 3. '복사본'들의 데미지를 기준으로 정렬
        return achievableJokbos.OrderByDescending(j => j.BaseDamage).ToList();
    }
}
