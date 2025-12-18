using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            LoadRelicIcon("RLC_CLOVER"),
            RelicEffectType.AddMaxRolls,
            intValue: 1,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_WHETSTONE", "숫돌", "모든 족보의 기본 데미지가 5 증가합니다.",
            LoadRelicIcon("RLC_WHETSTONE"),
            RelicEffectType.AddBaseDamage,
            intValue: 5,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_GOLD_DICE", "황금 주사위", "획득하는 점수가 1.5배가 됩니다.",
            LoadRelicIcon("RLC_GOLD_DICE"),
            RelicEffectType.AddGoldMultiplier,
            floatValue: 1.5f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_RUSTY_GEAR", "녹슨 톱니", "모든 족보의 기본 점수가 +3 증가합니다.",
            LoadRelicIcon("RLC_RUSTY_GEAR"),
            RelicEffectType.JokboGoldAdd,
            stringValue: "ALL", // (모든 족보)
            intValue: 3,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_GLASS_CANNON", "유리 대포", "모든 족보의 기본 데미지가 +10 증가하지만, 최대 체력이 2 감소합니다.",
            LoadRelicIcon("RLC_GLASS_CANNON"),
            RelicEffectType.AddBaseDamage, // 1. 데미지 +10
            intValue: 10,
            maxCount: 1
        // (2. 체력 -2 효과는 GameManager.AddRelic에서 따로 처리)
        ));

        AddRelicToDB(new Relic(
            "RLC_PLUTOCRACY", "금권 정치", "현재 보유한 '점수' 100점당 모든 족보의 기본 데미지가 +1 증가합니다. (최대 +50)",
            LoadRelicIcon("RLC_PLUTOCRACY"),
            RelicEffectType.DynamicDamage_Gold, // (새 EffectType)
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_BLOODLUST", "피의 갈증", "플레이어의 잃은 체력 1당 모든 족보의 기본 데미지가 +1 증가합니다.",
            LoadRelicIcon("RLC_BLOODLUST"),
            RelicEffectType.DynamicDamage_LostHealth, // (새 EffectType)
            maxCount: 1
        ));

        // --- 2. 덱 변경 유물 ---
        AddRelicToDB(new Relic(
            "RLC_EXTRA_DICE", "여분의 주사위", "덱에 'D6' 주사위를 영구적으로 1개 추가합니다.",
            LoadRelicIcon("RLC_EXTRA_DICE"),
            RelicEffectType.AddDice,
            stringValue: "D6",
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_TINY_DICE", "작은 주사위", "덱에 'D4' 주사위를 영구적으로 1개 추가합니다.",
            LoadRelicIcon("RLC_TINY_DICE"),
            RelicEffectType.AddDice,
            stringValue: "D4",
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_FOCUS", "집중", "모든 족보의 기본 데미지 +10. 획득 시 덱에서 D6 주사위 1개를 '영구히' 제거합니다.",
            LoadRelicIcon("RLC_FOCUS"),
            RelicEffectType.AddBaseDamage, // 1. 데미지 +10
            intValue: 10,
            maxCount: 1
        // (2. D6 제거 효과는 GameManager.AddRelic에서 따로 처리)
        ));

        // --- 3. 주사위 값 변경 유물 ---
        AddRelicToDB(new Relic(
            "RLC_ALCHEMY", "연금술사의 돌", "'1'이 나온 주사위는 '7'로 취급됩니다.",
            LoadRelicIcon("RLC_ALCHEMY"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_LODESTONE", "자철석", "주사위를 굴린 후, 홀수가 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_LODESTONE"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_TANZANITE", "탄자나이트", "주사위를 굴린 후, 짝수가 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_TANZANITE"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_FEATHER", "가벼운 깃털", "주사위를 굴린 후, '6'이 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_FEATHER"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_COUNTERWEIGHT", "균형추", "주사위를 굴린 후, 'D20' 주사위 값이 10 이하면 +10을 더합니다.",
            LoadRelicIcon("RLC_COUNTERWEIGHT"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));


        // --- 4. 족보 강화 유물 ---
        AddRelicToDB(new Relic(
            "RLC_PERFECTIONIST", "완벽주의자", "'스트레이트 (4연속)'을 비활성화하고, '스트레이트 (5연속)'의 데미지를 +50 증가시킵니다.",
            LoadRelicIcon("RLC_PERFECTIONIST"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "스트레이트 (5연속)",
            intValue: 50,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_SWORD_BOOK", "검술 교본", "모든 '스트레이트' 족보의 기본 데미지가 30 증가합니다.",
            LoadRelicIcon("RLC_SWORD_BOOK"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "스트레이트",
            intValue: 30,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_SPIKE_GLOVE", "가시 돋친 장갑", "모든 '투 페어' 족보의 기본 데미지가 +15 증가합니다.",
            LoadRelicIcon("RLC_SPIKE_GLOVE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "투 페어",
            intValue: 15,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_FOUNDATION", "기틀", "'총합' 족보의 기본 데미지가 10 증가합니다.",
            LoadRelicIcon("RLC_FOUNDATION"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "총합",
            intValue: 10,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_WHITEBOOK", "백마법서", "모든 '모두 짝수' 족보의 획득 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_WHITEBOOK"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "모두 짝수",
            floatValue: 2.0f,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_DARKBOOK", "흑마법서", "모든 '모두 홀수' 족보의 획득 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_DARKBOOK"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "모두 홀수",
            floatValue: 2.0f,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_GEM_CROWN", "보석 박힌 왕관", "'야찌' 족보의 획득 점수가 3배가 됩니다.",
            LoadRelicIcon("RLC_GEM_CROWN"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "야찌",
            floatValue: 3.0f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_SQUARE_PEG", "네모난 못", "'포카드' 족보의 기본 데미지가 +40 증가합니다.",
            LoadRelicIcon("RLC_SQUARE_PEG"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "포카드",
            intValue: 40,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_TRIPOD", "삼각대", "'트리플' 족보의 획득 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_TRIPOD"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "트리플",
            floatValue: 2.0f,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_HOUSE", "집 모형", "'풀 하우스' 족보의 기본 데미지가 +25 증가합니다.",
            LoadRelicIcon("RLC_HOUSE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "풀 하우스",
            intValue: 25,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_ROYAL_SEAL", "왕실의 인장", "'풀 하우스' 족보의 획득 점수가 3배가 됩니다.",
            LoadRelicIcon("RLC_ROYAL_SEAL"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "풀 하우스",
            floatValue: 3.0f,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_HEAVY_DICE", "무거운 주사위", "모든 '총합' 족보의 데미지 +30. 대신 최대 굴림 횟수 -1.",
            LoadRelicIcon("RLC_HEAVY_DICE"),
            RelicEffectType.JokboDamageAdd, // 1. 족보 데미지
            stringValue: "총합",
            intValue: 30,
            maxCount: 1
        // (2. 굴림 횟수 -1 효과는 GameManager.AddRelic에서 따로 처리)
        ));

        //---5. 특수 유물 ---
        AddRelicToDB(new Relic(
            "RLC_BUSINESS_CARD", "명함", "첫 번째 굴림으로 공격할 경우, 해당 족보의 데미지와 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_BUSINESS_CARD"),
            RelicEffectType.RollCountBonus,
            floatValue: 2.0f,
            maxCount: 1
        ));
    }

    private Sprite LoadRelicIcon(string relicID)
    {
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
            bool isUnlocked = masterRelic.IsUnLocked;
            if (!isUnlocked)
            {
                // "Unlock_유물ID" 키가 1이면 해금된 것
                if (PlayerPrefs.GetInt($"Unlock_{masterRelic.RelicID}", 0) == 1)
                    isUnlocked = true;
            }

            if (!isUnlocked) continue;

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