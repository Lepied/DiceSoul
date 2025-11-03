using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [수정] "자철석(RLC_LODESTONE)" 유물을 데이터베이스에 추가
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
            RelicEffectType.AddMaxRolls,
            intValue: 1
        ));
        
        AddRelicToDB(new Relic(
            "RLC_WHETSTONE", "숫돌", "모든 족보의 기본 데미지가 5 증가합니다.",
            RelicEffectType.AddBaseDamage,
            intValue: 5
        ));

        AddRelicToDB(new Relic(
            "RLC_GOLD_DICE", "황금 주사위", "획득하는 점수가 1.5배가 됩니다.",
            RelicEffectType.AddScoreMultiplier,
            floatValue: 1.5f
        ));

        // --- 2. 덱 변경 유물 (획득 즉시) ---
        AddRelicToDB(new Relic(
            "RLC_EXTRA_DICE", "여분의 주사위", "덱에 'D6' 주사위를 영구적으로 1개 추가합니다. (최대 8개)",
            RelicEffectType.AddDice, 
            stringValue: "D6" 
        ));
        
        // --- 3. 주사위 값 변경 유물 (매 굴림 시) ---
        AddRelicToDB(new Relic(
            "RLC_ALCHEMY", "연금술사의 돌", "'1'이 나온 주사위는 '7'로 취급됩니다.",
            RelicEffectType.ModifyDiceValue
        ));

        // [!!! 신규 유물 추가 !!!]
        AddRelicToDB(new Relic(
            "RLC_LODESTONE", "자철석", "주사위를 굴린 후, 홀수가 나온 주사위를 한 번 다시 굴립니다.",
            RelicEffectType.ModifyDiceValue 
            // (GameManager.ApplyDiceModificationRelics에서 이 ID를 하드코딩하여 처리)
        ));
    }

    private void AddRelicToDB(Relic relic)
    {
        if (allRelics.ContainsKey(relic.ID))
        {
            Debug.LogWarning($"Relic ID 중복: {relic.ID}. 덮어씁니다.");
            allRelics[relic.ID] = relic;
        }
        else
        {
            allRelics.Add(relic.ID, relic);
        }
    }

    public List<Relic> GetRandomRelics(int count)
    {
        if (allRelics.Count == 0) return new List<Relic>();
        List<Relic> shuffledRelics = allRelics.Values.OrderBy(x => Random.value).ToList();
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

