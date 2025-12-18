using UnityEngine;

public enum MarketItemType { Potion, RentalRelic }

[System.Serializable]
public class MarketItem
{
    public string ID { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Price { get; private set; }
    public MarketItemType Type { get; private set; }
    public Sprite Icon { get; private set; }

    // GameManager나 유물 시스템에 전달할 키값
    public string EffectKey { get; private set; } 

    public MarketItem(string id, string name, string description, int price, MarketItemType type, string effectKey)
    {
        this.ID = id;
        this.Name = name;
        this.Description = description;
        this.Price = price;
        this.Type = type;
        this.EffectKey = effectKey;
        
        this.Icon = Resources.Load<Sprite>($"MarketIcons/{id}");
    }
}