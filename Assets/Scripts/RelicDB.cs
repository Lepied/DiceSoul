using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. InitializeRelics에서 유물 생성 시 'Icon'을 로드하여 전달
/// 2. (참고) "RelicIcons/RLC_CLOVER" 같은 경로에 스프라이트 파일이 있어야 함
/// </summary>
public class RelicDB : MonoBehaviour
{
    public static RelicDB Instance { get; private set; }

    private Dictionary<string, Relic> allRelics = new Dictionary<string, Relic>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeRelics(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeRelics()
    {
        // --- 1. 스탯 유물 ---
        AddRelicToDB(new Relic(
            "RLC_CLOVER", "네잎클로버", "최대 굴림 횟수가 1 증가합니다.",
            LoadRelicIcon("RLC_CLOVER"), // [!!! 신규 추가 !!!]
            RelicEffectType.AddMaxRolls,
            intValue: 1,
            maxCount: 3
        ));
        
        AddRelicToDB(new Relic(
            "RLC_WHETSTONE", "숫돌", "모든 족보의 기본 데미지가 5 증가합니다.",
            LoadRelicIcon("RLC_WHETSTONE"), // [!!! 신규 추가 !!!]
            RelicEffectType.AddBaseDamage,
            intValue: 5,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_GOLD_DICE", "황금 주사위", "획득하는 점수가 1.5배가 됩니다.",
            LoadRelicIcon("RLC_GOLD_DICE"), // [!!! 신규 추가 !!!]
            RelicEffectType.AddScoreMultiplier,
            floatValue: 1.5f,
            maxCount: 1
        ));

        // --- 2. 덱 변경 유물 ---
        AddRelicToDB(new Relic(
            "RLC_EXTRA_DICE", "여분의 주사위", "덱에 'D6' 주사위를 영구적으로 1개 추가합니다.",
            LoadRelicIcon("RLC_EXTRA_DICE"), // [!!! 신규 추가 !!!]
            RelicEffectType.AddDice, 
            stringValue: "D6",
            maxCount: 3
        ));
        
        // --- 3. 주사위 값 변경 유물 ---
        AddRelicToDB(new Relic(
            "RLC_ALCHEMY", "연금술사의 돌", "'1'이 나온 주사위는 '7'로 취급됩니다.",
            LoadRelicIcon("RLC_ALCHEMY"), // [!!! 신규 추가 !!!]
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_LODESTONE", "자철석", "주사위를 굴린 후, 홀수가 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_LODESTONE"), // [!!! 신규 추가 !!!]
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_TANZANITE", "탄자나이트", "주사위를 굴린 후, 짝수가 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_TANZANITE"), // [!!! 신규 추가 !!!]
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        // --- 4. 족보 강화 유물 ---
        AddRelicToDB(new Relic(
            "RLC_SWORD_BOOK", "검술 교본", "모든 '스트레이트' 족보의 기본 데미지가 30 증가합니다.",
            LoadRelicIcon("RLC_SWORD_BOOK"), // [!!! 신규 추가 !!!]
            RelicEffectType.JokboDamageAdd,
            stringValue: "스트레이트",
            intValue: 30,
            maxCount: 0
        ));
        
        AddRelicToDB(new Relic(
            "RLC_FOUNDATION", "기틀", "'총합' 족보의 기본 데미지가 10 증가합니다.",
            LoadRelicIcon("RLC_FOUNDATION"), // [!!! 신규 추가 !!!]
            RelicEffectType.JokboDamageAdd, 
            stringValue: "총합", 
            intValue: 10,
            maxCount: 0
        ));
        
        AddRelicToDB(new Relic(
            "RLC_WHITEBOOK", "백마법서", "모든 '모두 짝수' 족보의 획득 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_WHITEBOOK"), 
            RelicEffectType.JokboScoreMultiplier, 
            stringValue: "모두 짝수", 
            floatValue: 2.0f,
            maxCount: 0
        ));
        
        AddRelicToDB(new Relic(
            "RLC_DARKBOOK", "흑마법서", "모든 '모두 홀수' 족보의 획득 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_DARKBOOK"), 
            RelicEffectType.JokboScoreMultiplier, 
            stringValue: "모두 홀수", 
            floatValue: 2.0f,
            maxCount: 0
        ));
    }
    
    /// <summary>
    /// [!!! 신규 추가 !!!]
    /// "Resources/RelicIcons/" 폴더에서 스프라이트를 로드하는 헬퍼 함수
    /// </summary>
    private Sprite LoadRelicIcon(string relicID)
    {
        // (참고) 아이콘이 없으면 null을 반환 (에러 방지)
        return Resources.Load<Sprite>("RelicIcons/" + relicID);
    }

    private void AddRelicToDB(Relic relic)
    {
        if (allRelics.ContainsKey(relic.RelicID))
        {
            Debug.LogWarning($"Relic ID 중복: {relic.RelicID}. 덮어씁니다.");
            allRelics[relic.RelicID] = relic;
        }
        else
        {
            allRelics.Add(relic.RelicID, relic);
        }
    }

    /// <summary>
    /// 획득 횟수 한도를 초과한 유물을 '제외'하고 랜덤 목록을 반환합니다.
    /// </summary>
    public List<Relic> GetRandomRelics(int count)
    {
        if (allRelics.Count == 0 || GameManager.Instance == null) return new List<Relic>();
        
        List<Relic> playerRelics = GameManager.Instance.activeRelics;

        Dictionary<string, int> playerRelicCounts = new Dictionary<string, int>();
        foreach (Relic relic in playerRelics)
        {
            if (playerRelicCounts.ContainsKey(relic.RelicID))
            {
                playerRelicCounts[relic.RelicID]++;
            }
            else
            {
                playerRelicCounts.Add(relic.RelicID, 1);
            }
        }

        List<Relic> availablePool = new List<Relic>();
        foreach (Relic masterRelic in allRelics.Values)
        {
            if (masterRelic.MaxCount == 0)
            {
                availablePool.Add(masterRelic);
                continue;
            }
            playerRelicCounts.TryGetValue(masterRelic.RelicID, out int currentCount);
            if (currentCount < masterRelic.MaxCount)
            {
                availablePool.Add(masterRelic);
            }
        }

        List<Relic> shuffledRelics = availablePool.OrderBy(x => Random.value).ToList();
        return shuffledRelics.Take(count).ToList();
    }
    
    public Relic GetRelicByID(string relicID)
    {
        if (allRelics.ContainsKey(relicID))
        {
            return allRelics[relicID];
        }
        return null;
    }
}
