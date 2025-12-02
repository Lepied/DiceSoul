using UnityEngine;
using System.Collections.Generic;
using System.Linq;


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
    /// [수정] 족보 추가 (투 페어) 및 수정 (스트레이트)
    /// </summary>
    private void InitializeJokbos()
    {
        // --- 7. 야찌 (5개) ---
        allJokbos.Add(new AttackJokbo(
            "야찌 (5개)", 150, 100,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 5)
        ));

        // --- 6. 포카드 (4개) ---
        allJokbos.Add(new AttackJokbo(
            "포카드 (4개)", 80, 50,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 4)
        ));
        
        // --- 5. 풀 하우스 (3+2) ---
        allJokbos.Add(new AttackJokbo(
            "풀 하우스 (3+2)", 70, 40,
            (diceValues) => {
                var groups = diceValues.GroupBy(v => v);
                return groups.Any(g => g.Count() == 3) && groups.Any(g => g.Count() == 2);
            }
        ));

        // --- 4. [!!! 신규 추가 !!!] 스트레이트 (5연속) (상위 족보) ---
        allJokbos.Add(new AttackJokbo(
            "스트레이트 (5연속)",
            70, // (4연속보다 데미지/점수 높음)
            40, 
            (diceValues) => {
                var sorted = diceValues.Distinct().OrderBy(v => v).ToList();
                if (sorted.Count < 5) return false;
                bool straight1 = sorted.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 });
                bool straight2 = sorted.SequenceEqual(new List<int> { 2, 3, 4, 5, 6 });
                return straight1 || straight2;
            }
        ));
        
        // --- 3. [!!! 수정 !!!] 스트레이트 (4연속) (기본 족보) ---
        allJokbos.Add(new AttackJokbo(
            "스트레이트 (4연속)",
            50, // (기획서 V1.0 기준)
            25, 
            (diceValues) => {
                var s = diceValues.Distinct().OrderBy(v => v).ToList();
                if (s.Count < 4) return false;
                // (1,2,3,4) (2,3,4,5) (3,4,5,6) 또는 (1,2,3,4,6) 같은 5개 중 4연속
                bool c1 = (s.Count >= 4 && (s[0]+1 == s[1] && s[1]+1 == s[2] && s[2]+1 == s[3]));
                bool c2 = (s.Count >= 5 && (s[1]+1 == s[2] && s[2]+1 == s[3] && s[3]+1 == s[4]));
                return c1 || c2;
            }
        ));
        
        // --- 3. 트리플 (3개) ---
        allJokbos.Add(new AttackJokbo(
            "트리플 (3개)", 40, 20,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 3)
        ));

        // --- 2. [!!! 신규 추가 !!!] 투 페어 (2+2) ---
        allJokbos.Add(new AttackJokbo(
            "투 페어 (2+2)", 25, 10,
            (diceValues) => diceValues.GroupBy(v => v).Count(g => g.Count() >= 2) >= 2
        ));

        // --- 2. 모두 짝수 / 모두 홀수 ---
        allJokbos.Add(new AttackJokbo(
            "모두 짝수", 30, 15,
            (diceValues) => diceValues.All(v => v % 2 == 0)
        ));
        allJokbos.Add(new AttackJokbo(
            "모두 홀수", 30, 15,
            (diceValues) => diceValues.All(v => v % 2 != 0)
        ));

        // --- 1. 총합 (가변 족보) ---
        allJokbos.Add(new AttackJokbo(
            "총합", 
            (diceValues) => diceValues.Sum(), 
            (diceValues) => diceValues.Sum(), 
            (diceValues) => true              
        ));
    }

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// 'RLC_PERFECTIONIST' 유물 여부에 따라 4연속/5연속 스트레이트를 필터링합니다.
    /// (컴파일 오류 수정: CheckAndCalculate 사용, 복사 생성자 사용)
    /// </summary>
    public List<AttackJokbo> GetAchievableJokbos(List<int> diceValues)
    {
        List<AttackJokbo> achievableJokbos = new List<AttackJokbo>();

        // [신규] '완벽주의자' 유물 보유 여부 확인
        bool hasPerfectionist = GameManager.Instance.activeRelics.Any(r => r.RelicID == "RLC_PERFECTIONIST");

        foreach (var jokboPrototype in allJokbos)
        {
            // [신규] 유물 필터링 로직
            string desc = jokboPrototype.Description;
            if (hasPerfectionist && desc.Contains("4연속")) continue; // 4연속 비활성화
            if (!hasPerfectionist && desc.Contains("5연속")) continue; // 5연속 비활성화 (유물 없으면)

            // [수정] CheckAndCalculate 및 복사 생성자 사용
            if (jokboPrototype.CheckAndCalculate(diceValues))
            {
                achievableJokbos.Add(new AttackJokbo(jokboPrototype));
            }
        }
        
        return achievableJokbos.OrderByDescending(j => j.BaseDamage).ToList();
    }
}