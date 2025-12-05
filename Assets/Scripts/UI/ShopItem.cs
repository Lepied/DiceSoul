using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ShopItem
{
    public string Name;
    public string Description;
    public int Price;
    public Sprite MainIcon;
    public Sprite SubIcon;
    public Color IconColor;
    public UnityAction OnPurchaseEffect; // 구매 시 실행할 로직(이펙트나 돈까이는거나)

    // 보통 
    public ShopItem(string name, string description, int price, Sprite mainIcon, UnityAction onPurchase)
    {
        this.Name = name;
        this.Description = description;
        this.Price = price;
        this.MainIcon = mainIcon;
        this.SubIcon = null;
        this.IconColor = Color.white;
        this.OnPurchaseEffect = onPurchase;
    }
    //포션용 오버로딩
    public ShopItem(string name, string description, int price, Sprite bottleIcon, Sprite liquidIcon, Color liquidColor, UnityAction onPurchase)
    {
        this.Name = name;
        this.Description = description;
        this.Price = price;
        this.MainIcon = bottleIcon;
        this.SubIcon = liquidIcon;
        this.IconColor = liquidColor;
        this.OnPurchaseEffect = onPurchase;
    }

    public void ExecuteEffect()
    {
        OnPurchaseEffect?.Invoke();
    }
}