using UnityEngine;

// 아이템의 종류를 구분
public enum ShopItemType
{
    Relic,          // 새 유물 1개
    DiceUpgrade,    // 주사위 변경 (D6 -> D8)
    DiceAdd,        // 주사위 슬롯 추가
    Heal,           // 체력 회복
    RerollShop      // 상점 새로고침
}

public class ShopItem
{
    public string ItemName { get; private set; }
    public string Description { get; private set; }
    public int Price { get; private set; }
    public ShopItemType ItemType { get; private set; }

    // (나중에 유물이나 주사위 업그레이드 정보를 담을 변수)
    public Relic RelicData { get; private set; }
    // public DiceType NewDiceType { get; private set; } 

    // 생성자 (예시)
    public ShopItem(string name, string desc, int price, ShopItemType type, Relic relic = null)
    {
        this.ItemName = name;
        this.Description = desc;
        this.Price = price;
        this.ItemType = type;
        this.RelicData = relic;
    }
}
