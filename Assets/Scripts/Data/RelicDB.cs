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
        // ==========================================
        // WaveReward - Common
        // ==========================================
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
            "RLC_RUSTY_GEAR", "녹슨 톱니", "모든 족보의 기본 골드가 +3 증가합니다.",
            LoadRelicIcon("RLC_RUSTY_GEAR"),
            RelicEffectType.JokboGoldAdd,
            stringValue: "ALL",
            intValue: 3,
            maxCount: 0
        ));

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
            "RLC_QUICK_RELOAD", "빠른 장전", "첫 굴림 시 주사위 2개를 즉시 재굴림합니다.",
            LoadRelicIcon("RLC_QUICK_RELOAD"),
            RelicEffectType.RerollFirst,
            intValue: 2,
            maxCount: 5
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
            "RLC_SHARP_BLADE", "날카로운 칼날", "모든 스트레이트 족보 데미지가 +5 증가합니다.",
            LoadRelicIcon("RLC_SHARP_BLADE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "스트레이트",
            intValue: 5,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_POLISHED_ORB", "광택 구슬", "모든 족보의 기본 골드가 +2 증가합니다.",
            LoadRelicIcon("RLC_POLISHED_ORB"),
            RelicEffectType.JokboGoldAdd,
            stringValue: "ALL",
            intValue: 2,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_SPIKE_GLOVE", "가시 돋친 장갑", "'투 페어' 족보의 기본 데미지가 +15 증가합니다.",
            LoadRelicIcon("RLC_SPIKE_GLOVE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "투 페어",
            intValue: 15,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_TRIPOD", "삼각대", "'트리플' 족보의 획득 골드가 2배가 됩니다.",
            LoadRelicIcon("RLC_TRIPOD"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "트리플",
            floatValue: 2.0f,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_HOUSE", "집 모형", "'풀 하우스' 족보의 기본 데미지가 +25 증가합니다.",
            LoadRelicIcon("RLC_HOUSE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "풀 하우스",
            intValue: 25,
            maxCount: 0
        ));

        // ==========================================
        // WaveReward - Uncommon
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_SMALL_SHIELD", "작은 방패", "최대 체력 20% 이하일 때 피해를 무효화합니다. (존 당 1회)",
            LoadRelicIcon("RLC_SMALL_SHIELD"),
            RelicEffectType.DamageImmuneLowHP,
            floatValue: 0.2f,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_SWIFT_HANDS", "날쌘 손놀림", "굴림 가능 횟수가 0일 때 1회 무료 충전합니다.",
            LoadRelicIcon("RLC_SWIFT_HANDS"),
            RelicEffectType.FreeRollAtZero,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_WHITEBOOK", "백마법서", "'모두 짝수' 족보의 획득 골드가 2배가 됩니다.",
            LoadRelicIcon("RLC_WHITEBOOK"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "모두 짝수",
            floatValue: 2.0f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_DARKBOOK", "흑마법서", "'모두 홀수' 족보의 획득 골드가 2배가 됩니다.",
            LoadRelicIcon("RLC_DARKBOOK"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "모두 홀수",
            floatValue: 2.0f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_SWORD_BOOK", "검술 교본", "모든 '스트레이트' 족보의 기본 데미지가 +30 증가합니다.",
            LoadRelicIcon("RLC_SWORD_BOOK"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "스트레이트",
            intValue: 30,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_SPIKED_HAMMER", "가시 망치", "'포카드' 족보의 기본 데미지가 +20 증가합니다.",
            LoadRelicIcon("RLC_SPIKED_HAMMER"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "포카드",
            intValue: 20,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_EVEN_BOOK", "짝수의 서", "'모두 짝수'일 때 데미지가 1.5배가 됩니다.",
            LoadRelicIcon("RLC_EVEN_BOOK"),
            RelicEffectType.JokboDamageMultiplier,
            stringValue: "모두 짝수",
            floatValue: 1.5f,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_ODD_BOOK", "홀수의 서", "'모두 홀수'일 때 데미지가 1.5배가 됩니다.",
            LoadRelicIcon("RLC_ODD_BOOK"),
            RelicEffectType.JokboDamageMultiplier,
            stringValue: "모두 홀수",
            floatValue: 1.5f,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_LUCKY_RING", "행운의 반지", "'트리플' 족보의 기본 데미지가 +20 증가합니다.",
            LoadRelicIcon("RLC_LUCKY_RING"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "트리플",
            intValue: 20,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_IRON_FIST", "철제 주먹", "'투 페어' 족보의 기본 데미지가 +10 증가합니다.",
            LoadRelicIcon("RLC_IRON_FIST"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "투 페어",
            intValue: 10,
            maxCount: 0
        ));

        // ==========================================
        // WaveReward - Rare
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_GAMBLER_RING", "도박사의 반지", "모든 족보 데미지 +20%; 최대 체력 -9",
            LoadRelicIcon("RLC_GAMBLER_RING"),
            RelicEffectType.DamageMultiplierWithHealthCost,
            floatValue: 1.2f,
            intValue: -9,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_LUCKY_CLOVER", "행운의 네잎클로버", "재굴림 시 반드시 이전보다 높은 숫자가 나옵니다.",
            LoadRelicIcon("RLC_LUCKY_CLOVER"),
            RelicEffectType.HigherReroll,
            floatValue: 1.0f,
            maxCount: 1
        ));

        // ==========================================
        // ShopOnly - Common
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_TOUGH_ARMOR", "튼튼한 갑옷", "방어력이 3 증가합니다.",
            LoadRelicIcon("RLC_TOUGH_ARMOR"),
            RelicEffectType.AddDefense,
            intValue: 3,
            maxCount: 5
        ));

        // ==========================================
        // ShopOnly - Uncommon
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_LIGHTWEIGHT_BAG", "가벼운 가방", "고유 유물을 제외한 유물 보유 한도가 +1 증가합니다.",
            LoadRelicIcon("RLC_LIGHTWEIGHT_BAG"),
            RelicEffectType.RelicCapacityUp,
            intValue: 1,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_FOCUS", "집중", "모든 족보의 기본 데미지 +20. 획득 시 덱에서 D6 주사위 1개를 제거합니다.",
            LoadRelicIcon("RLC_FOCUS"),
            RelicEffectType.AddBaseDamage,
            intValue: 20,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_REGENERATION", "재생의 팔찌", "매 굴림 시 5% 확률로 체력을 1 회복합니다.",
            LoadRelicIcon("RLC_REGENERATION"),
            RelicEffectType.HealOnRoll,
            floatValue: 0.05f,
            intValue: 1,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_VAMPIRE_FANG", "흡혈귀의 이빨", "족보 완성 시 체력을 +1 회복합니다.",
            LoadRelicIcon("RLC_VAMPIRE_FANG"),
            RelicEffectType.HealOnJokbo,
            intValue: 1,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_MERCHANT_CARD", "상인의 인장", "상점 새로고침 비용이 증가하지 않습니다.",
            LoadRelicIcon("RLC_MERCHANT_CARD"),
            RelicEffectType.ShopRefreshFreeze,
            maxCount: 3
        ));

        // ==========================================
        // ShopOnly - Rare
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_LUCKY_CHARM", "행운의 동전", "상점 판매가가 25% 할인됩니다.",
            LoadRelicIcon("RLC_LUCKY_CHARM"),
            RelicEffectType.ShopDiscount,
            floatValue: 0.75f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_GOLD_DICE", "황금 주사위", "골드 획득이 1.5배가 됩니다.",
            LoadRelicIcon("RLC_GOLD_DICE"),
            RelicEffectType.AddGoldMultiplier,
            floatValue: 1.5f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_GLASS_CANNON", "유리 대포", "모든 족보의 기본 데미지가 +50 증가하지만, 최대 체력이 5 감소합니다.",
            LoadRelicIcon("RLC_GLASS_CANNON"),
            RelicEffectType.AddBaseDamage,
            intValue: 50,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_PLUTOCRACY", "금권 정치", "보유 골드 100당 데미지 +1 (최대 +50)",
            LoadRelicIcon("RLC_PLUTOCRACY"),
            RelicEffectType.DynamicDamage_Gold,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_BLOODLUST", "피의 갈증", "잃은 체력 1당 데미지 +1% (최대 20%)",
            LoadRelicIcon("RLC_BLOODLUST"),
            RelicEffectType.DynamicDamage_LostHealth,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_GEM_CROWN", "보석 박힌 왕관", "'야찌' 족보의 획득 골드가 3배가 됩니다.",
            LoadRelicIcon("RLC_GEM_CROWN"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "야찌",
            floatValue: 3.0f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_PHOENIX_FEATHER", "불사조의 깃털", "게임오버 시 체력 50%로 1회 부활합니다.",
            LoadRelicIcon("RLC_PHOENIX_FEATHER"),
            RelicEffectType.ReviveOnDeath,
            floatValue: 0.5f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_DEMON_CONTRACT", "악마의 계약서", "모든 족보 데미지 +15%; 체력 회복 불가",
            LoadRelicIcon("RLC_DEMON_CONTRACT"),
            RelicEffectType.DamageMultiplierNoHeal,
            floatValue: 1.15f,
            maxCount: 1
        ));

        // ==========================================
        // MaintenanceReward - Common
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_DIAMOND_DICE", "다이아몬드 주사위", "모든 주사위의 최소 눈 결정값이 2로 고정됩니다.",
            LoadRelicIcon("RLC_DIAMOND_DICE"),
            RelicEffectType.FixMinValue,
            intValue: 2,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_MAGNET", "자석", "재굴림 시 선택하지 않은 주사위들이 같은 숫자가 나올 확률이 증가합니다.",
            LoadRelicIcon("RLC_MAGNET"),
            RelicEffectType.SameNumberBonus,
            floatValue: 0.15f,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_SPRING", "스프링", "상품 리롤 시 소모되는 비용이 50% 확률로 반환됩니다.",
            LoadRelicIcon("RLC_SPRING"),
            RelicEffectType.RefundShopRefresh,
            floatValue: 0.5f,
            maxCount: 3
        ));

        // ==========================================
        // MaintenanceReward - Uncommon
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_FEATHER", "가벼운 깃털", "주사위를 굴린 후, '6'이 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_FEATHER"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_DOUBLE_DICE", "이중 주사위", "[사용] 선택한 주사위의 눈을 2배로 계산합니다. (1회)",
            LoadRelicIcon("RLC_DOUBLE_DICE"),
            RelicEffectType.DoubleDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_SCHOLAR_BOOK", "학자의 서적", "미사용 족보로 공격 시 영구 데미지 +1%",
            LoadRelicIcon("RLC_SCHOLAR_BOOK"),
            RelicEffectType.PermanentDamageGrowth,
            maxCount: 3
        ));

        AddRelicToDB(new Relic(
            "RLC_DICE_CUP", "주사위 컵", "[사용] 굴리기 전 주사위 하나를 원하는 눈으로 고정합니다. (1회)",
            LoadRelicIcon("RLC_DICE_CUP"),
            RelicEffectType.FixDiceBeforeRoll,
            intValue: 1,
            maxCount: 1
        ));

        // ==========================================
        // MaintenanceReward - Rare
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_COUNTERWEIGHT", "균형추", "'D20' 주사위 값이 10 이하면 +5를 더합니다.",
            LoadRelicIcon("RLC_COUNTERWEIGHT"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_HOURGLASS", "시간의 모래시계", "남은 굴림 횟수가 적을수록 데미지 상승 (회당 +10%, 최대 30%)",
            LoadRelicIcon("RLC_HOURGLASS"),
            RelicEffectType.DynamicDamage_LowRolls,
            floatValue: 0.1f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_ALCHEMY", "연금술사의 돌", "'1'이 나온 주사위는 '7'로 취급됩니다.",
            LoadRelicIcon("RLC_ALCHEMY"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_PERFECTIONIST", "완벽주의자", "4연속 스트레이트 삭제; 5연속 스트레이트 데미지 +50",
            LoadRelicIcon("RLC_PERFECTIONIST"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "스트레이트 (5연속)",
            intValue: 50,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_HEAVY_DICE", "무거운 주사위", "총합 족보 데미지 +30; 굴림 횟수 -1",
            LoadRelicIcon("RLC_HEAVY_DICE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "총합",
            intValue: 30,
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
            "RLC_SQUARE_PEG", "네모난 못", "'포카드' 족보의 기본 데미지가 +40 증가합니다.",
            LoadRelicIcon("RLC_SQUARE_PEG"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "포카드",
            intValue: 40,
            maxCount: 0
        ));

        AddRelicToDB(new Relic(
            "RLC_ROYAL_SEAL", "왕실의 인장", "'풀 하우스' 족보의 획득 골드가 3배가 됩니다.",
            LoadRelicIcon("RLC_ROYAL_SEAL"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "풀 하우스",
            floatValue: 3.0f,
            maxCount: 0
        ));

        // ==========================================
        // MaintenanceReward - Epic
        // ==========================================
        AddRelicToDB(new Relic(
            "RLC_BUSINESS_CARD", "명함", "첫 굴림 데미지·골드 2배",
            LoadRelicIcon("RLC_BUSINESS_CARD"),
            RelicEffectType.RollCountBonus,
            floatValue: 2.0f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_TIME_RIFT", "시공의 틈", "족보 완성 시 20% 확률로 현재 굴림 횟수를 소모하지 않습니다.",
            LoadRelicIcon("RLC_TIME_RIFT"),
            RelicEffectType.SaveRollChance,
            floatValue: 0.2f,
            maxCount: 1
        ));

        AddRelicToDB(new Relic(
            "RLC_FATE_DICE", "운명의 주사위", "[사용] 모든 주사위를 가장 높은 눈으로 변경합니다. (1회)",
            LoadRelicIcon("RLC_FATE_DICE"),
            RelicEffectType.SetAllToMax,
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