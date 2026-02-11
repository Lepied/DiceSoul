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
    
    // 현재 언어에 맞는 아이템 이름 반환
    public string GetLocalizedName()
    {
        if (LocalizationManager.Instance != null)
        {
            string key = $"MARKET_{ID}_NAME";
            return LocalizationManager.Instance.GetText(key);
        }
        return Name;
    }
    // 현재 언어에 맞는 아이템 설명 반환
    public string GetLocalizedDescription()
    {
        if (LocalizationManager.Instance != null)
        {
            string key = $"MARKET_{ID}_DESC";
            return LocalizationManager.Instance.GetText(key);
        }
        return Description;
    }
}