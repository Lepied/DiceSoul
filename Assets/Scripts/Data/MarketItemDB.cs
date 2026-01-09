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
            2000, MarketItemType.Potion, "Insurance_30"));


        // 전투 보조  - 초반용(n웨이브동안지속되는 효과)
        combatSupports.Add(new MarketItem(
            "POT_CROSSBOW", "석궁", "초반 3웨이브 동안 기본 데미지 +2.", 
            250, MarketItemType.Potion, "Buff_Damage_3wave_2"));

        combatSupports.Add(new MarketItem(
            "POT_OINTMENT", "미끈한 연고", "초반 3웨이브 동안, 매 웨이브 시작 시 Shield +3.", 
            200, MarketItemType.Potion, "Buff_Shield_3wave_3"));

        combatSupports.Add(new MarketItem(
            "POT_SPEEEDLOADER", "스피드로더", "초반 3웨이브 동안 리롤 횟수 +2.", 
            500, MarketItemType.Potion, "Buff_Reroll_3wave_2"));
    }

    public MarketItem GetItemByID(string id)
    {
        var item = basicSupplies.Find(x => x.ID == id);
        if (item != null) return item;
        return combatSupports.Find(x => x.ID == id);
    }
}