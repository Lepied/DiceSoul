using UnityEngine;
using System.Collections.Generic;

public class MarketItemDB : MonoBehaviour
{
    public static MarketItemDB Instance { get; private set; }

    // 기초 보급품 리스트
    public List<MarketItem> basicSupplies = new List<MarketItem>();
    // 전투 보조 리스트
    public List<MarketItem> combatSupports = new List<MarketItem>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMarket();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeMarket()
    {
        // 기초 보급품 런시작하면 해당 런동안 유지됨.
        basicSupplies.Add(new MarketItem(
            "POT_RATION", "전투 식량", "다음 런 시작 시 최대 체력 +30.", 
            300, MarketItemType.Potion, "MaxHealth_30"));

        basicSupplies.Add(new MarketItem(
            "POT_FUND", "비상금", "다음 런 시작 시 골드 +150.", 
            300, MarketItemType.Potion, "StartGold_150"));

        basicSupplies.Add(new MarketItem(
            "POT_DICE", "보급 주사위", "시작 덱에 'D6' 주사위 1개를 추가합니다.", 
            500, MarketItemType.Potion, "AddDice_D6"));

        basicSupplies.Add(new MarketItem(
            "POT_INSURANCE", "여행자 보험", "사망 시, 획득한 골드의 30%를 마석으로 환급받습니다.", 
            1000, MarketItemType.Potion, "Insurance_30"));

        basicSupplies.Add(new MarketItem(
            "POT_RELIC_PACK", "유물 꾸러미", "랜덤 유물 1개 획득", 
            800, MarketItemType.Potion, "RandomRelic_Common_1"));

        // 전투 보조  - 초반용(n웨이브동안지속되는 효과)
        combatSupports.Add(new MarketItem(
            "POT_CROSSBOW", "석궁", "초반 5웨이브 동안 모든 데미지 +5.", 
            300, MarketItemType.Potion, "Buff_Damage_5wave_5"));

        combatSupports.Add(new MarketItem(
            "POT_OINTMENT", "미끈한 연고", "초반 5웨이브 동안, 매 웨이브 시작 시 Shield + 10.", 
            300, MarketItemType.Potion, "Buff_Shield_5wave_10"));

        combatSupports.Add(new MarketItem(
            "POT_SPEEEDLOADER", "스피드로더", "초반 5웨이브 동안 리롤 횟수 +2.", 
            500, MarketItemType.Potion, "Buff_Reroll_5wave_2"));

        combatSupports.Add(new MarketItem(
            "POT_LUCKY_COIN", "행운의 동전", "초반 5웨이브 동안 치명타 확률 +20%.", 
            500, MarketItemType.Potion, "Buff_CritChance_5wave_20"));

        combatSupports.Add(new MarketItem(
            "POT_TACTICIAN_MAP", "전략가의 지도", "초반 5웨이브 동안, 매 웨이브 시작 시 골드 +20.", 
            400, MarketItemType.Potion, "Buff_WaveGold_5wave_20"));
    }

    public MarketItem GetItemByID(string id)
    {
        var item = basicSupplies.Find(x => x.ID == id);
        if (item != null) return item;
        return combatSupports.Find(x => x.ID == id);
    }
}