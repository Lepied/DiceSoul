using UnityEngine;
using System.Collections.Generic;
using System.Linq;


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

    private void InitializeJokbos()
    {
        VFXConfig missileVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_Missile");
        VFXConfig straightVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_Straight");
        VFXConfig oddVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_Odd");
        VFXConfig evenVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_Even");
        VFXConfig pairVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_Pairs");
        VFXConfig twoPairVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_TwoPairs");
        VFXConfig tripleVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_Triple");
        VFXConfig fourCardVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_FourCard");
        VFXConfig fullHouseMainVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_FullHouse_Main");
        VFXConfig fullHouseSubVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_FullHouse_Sub");
        VFXConfig YachtVFX = Resources.Load<VFXConfig>("VFXConfigs/VFXConfig_Yacht");
        
        // 야찌 (5개)
        allJokbos.Add(new AttackJokbo(
            "야찌 (5개)", 150, 100,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 5),
            (diceValues) => GetSameValueIndices(diceValues, 5),
            AttackTargetType.AoE,
            vfxConfig : YachtVFX
        ));

        // 포카드 (4개) - 1명 선택 + 전체 공격
        allJokbos.Add(new AttackJokbo(
            "포카드 (4개)", 80, 50,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 4),
            (diceValues) => GetSameValueIndices(diceValues, 4),
            AttackTargetType.Hybrid,
            1,
            1, 
            AttackTargetType.AoE,  // SubTargetType
            40, 
            1,
            vfxConfig : fourCardVFX
        ));

        // 풀 하우스 (3+2) - 2명 선택 + 랜덤 공격
        allJokbos.Add(new AttackJokbo(
            "풀 하우스 (3+2)", 70, 40,
            (diceValues) => {
                var groups = diceValues.GroupBy(v => v);
                return groups.Any(g => g.Count() == 3) && groups.Any(g => g.Count() == 2);
            },
            (diceValues) => GetFullHouseIndices(diceValues),
            AttackTargetType.Hybrid,
            2,
            1,
            AttackTargetType.Random,
            35,
            3,
            vfxConfig : fullHouseMainVFX,
            subVfxConfig : fullHouseSubVFX
        ));

        // 스트레이트 (5연속)
        allJokbos.Add(new AttackJokbo(
            "스트레이트 (5연속)",
            70,
            40, 
            (diceValues) => {
                var sorted = diceValues.Distinct().OrderBy(v => v).ToList();
                if (sorted.Count < 5) return false;
                bool straight1 = sorted.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 });
                bool straight2 = sorted.SequenceEqual(new List<int> { 2, 3, 4, 5, 6 });
                return straight1 || straight2;
            },
            (diceValues) => GetStraightIndices(diceValues, 5),
            AttackTargetType.AoE,
            vfxConfig : straightVFX
        ));
        
        // 스트레이트 (4연속)
        allJokbos.Add(new AttackJokbo(
            "스트레이트 (4연속)",
            50,
            25, 
            (diceValues) => {
                var s = diceValues.Distinct().OrderBy(v => v).ToList();
                if (s.Count < 4) return false;
                bool c1 = (s.Count >= 4 && (s[0]+1 == s[1] && s[1]+1 == s[2] && s[2]+1 == s[3]));
                bool c2 = (s.Count >= 5 && (s[1]+1 == s[2] && s[2]+1 == s[3] && s[3]+1 == s[4]));
                return c1 || c2;
            },
            (diceValues) => GetStraightIndices(diceValues, 4),
            AttackTargetType.AoE,
            vfxConfig : straightVFX
        ));
        
        // 트리플 (3개) - 3명 선택
        allJokbos.Add(new AttackJokbo(
            "트리플 (3개)", 40, 20,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 3),
            (diceValues) => GetSameValueIndices(diceValues, 3),
            AttackTargetType.Single,
            3,  // 3명 선택
            vfxConfig : tripleVFX
        ));

        // 투 페어 (2+2) - 2명 선택
        allJokbos.Add(new AttackJokbo(
            "투 페어 (2+2)", 25, 10,
            (diceValues) => diceValues.GroupBy(v => v).Count(g => g.Count() >= 2) >= 2,
            (diceValues) => GetTwoPairIndices(diceValues),
            AttackTargetType.Single,
            2,
            vfxConfig : twoPairVFX
        ));

        // 원 페어 (2)
        allJokbos.Add(new AttackJokbo(
            "원 페어 (2)", 15, 5,
            (diceValues) => diceValues.GroupBy(v => v).Any(g => g.Count() >= 2),
            (diceValues) => GetSameValueIndices(diceValues, 2),
            AttackTargetType.Random,
            0,
            1,
            vfxConfig : pairVFX
        ));

        // 모두 짝수
        allJokbos.Add(new AttackJokbo(
            "모두 짝수", 30, 15,
            (diceValues) => diceValues.All(v => v % 2 == 0),
            (diceValues) => GetAllIndices(diceValues),
            AttackTargetType.AoE,
            vfxConfig : evenVFX
        ));
        
        // 모두 홀수
        allJokbos.Add(new AttackJokbo(
            "모두 홀수", 30, 15,
            (diceValues) => diceValues.All(v => v % 2 != 0),
            (diceValues) => GetAllIndices(diceValues),
            AttackTargetType.AoE,
            vfxConfig : oddVFX
        ));

        // 총합 (랜덤 타겟은 주사위 수만큼)
        allJokbos.Add(new AttackJokbo(
            "총합", 
            (diceValues) => diceValues.Sum(), 
            (diceValues) => diceValues.Sum(), 
            (diceValues) => true,
            (diceValues) => GetAllIndices(diceValues),
            AttackTargetType.Random,
            0,
            0,
            missileVFX
        ));
        
        // 수비 (주사위 합만큼 실드 획득)
        allJokbos.Add(new AttackJokbo(
            "수비", 
            (diceValues) => diceValues.Sum(), //주사위값 다합친거 를 실드량으로하기
            (diceValues) => 0,
            (diceValues) => true,
            (diceValues) => GetAllIndices(diceValues),
            AttackTargetType.Defense,
            0,
            0
        ));
    }

    // 같은 값을 가진 주사위 N개의 인덱스 반환
    private List<int> GetSameValueIndices(List<int> diceValues, int count)
    {
        var indices = new List<int>();
        var group = diceValues.GroupBy(v => v).FirstOrDefault(g => g.Count() >= count);
        if (group != null)
        {
            int targetValue = group.Key;
            for (int i = 0; i < diceValues.Count && indices.Count < count; i++)
            {
                if (diceValues[i] == targetValue) indices.Add(i);
            }
        }
        return indices;
    }

    // 풀하우스 인덱스 반환 (3개 + 2개)
    private List<int> GetFullHouseIndices(List<int> diceValues)
    {
        var indices = new List<int>();
        var groups = diceValues.GroupBy(v => v).OrderByDescending(g => g.Count()).ToList();
        if (groups.Count >= 2)
        {
            int tripleValue = groups[0].Key;
            int pairValue = groups[1].Key;
            
            // 트리플 3개
            for (int i = 0; i < diceValues.Count && indices.Count < 3; i++)
            {
                if (diceValues[i] == tripleValue) indices.Add(i);
            }
            // 페어 2개
            for (int i = 0; i < diceValues.Count && indices.Count < 5; i++)
            {
                if (diceValues[i] == pairValue && !indices.Contains(i)) indices.Add(i);
            }
        }
        return indices;
    }

    // 스트레이트 인덱스 반환
    private List<int> GetStraightIndices(List<int> diceValues, int length)
    {
        var indices = new List<int>();
        
        if (length == 5)
        {
            // 1,2,3,4,5 또는 2,3,4,5,6
            var distinctValues = diceValues.Distinct().OrderBy(v => v).ToList();
            bool is12345 = distinctValues.SequenceEqual(new List<int> { 1, 2, 3, 4, 5 });
            bool is23456 = distinctValues.SequenceEqual(new List<int> { 2, 3, 4, 5, 6 });
            
            if (is12345 || is23456)
            {
                foreach (int value in distinctValues)
                {
                    int idx = diceValues.IndexOf(value);
                    if (idx >= 0 && !indices.Contains(idx))
                    {
                        indices.Add(idx);
                    }
                }
            }
        }
        else if (length == 4)
        {
            // 4개 연속 찾기
            var distinctValues = diceValues.Distinct().OrderBy(v => v).ToList();
            for (int start = 0; start <= distinctValues.Count - 4; start++)
            {
                bool isStraight = true;
                for (int j = 0; j < 3; j++)
                {
                    if (distinctValues[start + j] + 1 != distinctValues[start + j + 1])
                    {
                        isStraight = false;
                        break;
                    }
                }
                if (isStraight)
                {
                    for (int val = distinctValues[start]; val <= distinctValues[start + 3]; val++)
                    {
                        int idx = diceValues.IndexOf(val);
                        if (idx >= 0 && !indices.Contains(idx))
                        {
                            indices.Add(idx);
                        }
                    }
                    break;
                }
            }
        }
        
        return indices;
    }

    // 투페어 인덱스 반환
    private List<int> GetTwoPairIndices(List<int> diceValues)
    {
        var indices = new List<int>();
        var pairs = diceValues.GroupBy(v => v).Where(g => g.Count() >= 2).Take(2).ToList();
        foreach (var pair in pairs)
        {
            int count = 0;
            for (int i = 0; i < diceValues.Count && count < 2; i++)
            {
                if (diceValues[i] == pair.Key && !indices.Contains(i))
                {
                    indices.Add(i);
                    count++;
                }
            }
        }
        return indices;
    }

    // 모든 주사위 인덱스 반환
    private List<int> GetAllIndices(List<int> diceValues)
    {
        var indices = new List<int>();
        for (int i = 0; i < diceValues.Count; i++)
        {
            indices.Add(i);
        }
        return indices;
    }

    //현재 주사위 값들로 만들 수 있는 족보 반환시키기
    public List<AttackJokbo> GetAchievableJokbos(List<int> diceValues)
    {
        List<AttackJokbo> achievableJokbos = new List<AttackJokbo>();

        // '완벽주의자' 유물 보유 여부 확인
        bool hasPerfectionist = GameManager.Instance.activeRelics.Any(r => r.RelicID == "RLC_PERFECTIONIST");

        foreach (var jokboPrototype in allJokbos)
        {
            // 유물 필터링 로직
            string desc = jokboPrototype.Description;
            if (hasPerfectionist && desc.Contains("4연속")) continue; // 4연속 비활성화
            if (!hasPerfectionist && desc.Contains("5연속")) continue; // 5연속 비활성화 (유물 없으면)

            // CheckAndCalculate 및 복사 생성자 사용
            if (jokboPrototype.CheckAndCalculate(diceValues))
            {
                achievableJokbos.Add(new AttackJokbo(jokboPrototype));
            }
        }
        
        return achievableJokbos.OrderByDescending(j => j.BaseDamage).ToList();
    }
}