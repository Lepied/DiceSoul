using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

public class ShopItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 연결")]
    public Image itemIconImage;
    public Image itemOverlayImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private ShopItem myItem;
    private UIManager uiManager;

    public void Setup(ShopItem item, UIManager manager)
    {
        this.myItem = item;
        this.uiManager = manager;

        // 텍스트 갱신
        if (nameText) nameText.text = item.Name;
        
        // 유물인 경우 획득 가능 여부 체크
        bool canAcquire = true;
        if (item.ItemType == ShopItemType.Relic && item.RelicData != null)
        {
            canAcquire = GameManager.Instance.CanAcquireRelic(item.RelicData);
            
            // 최대 개수 정보 표시
            int currentCount = GameManager.Instance.activeRelics.Count(r => r.RelicID == item.RelicData.RelicID);
            int effectiveMax = GameManager.Instance.GetEffectiveMaxCount(item.RelicData.RelicID, item.RelicData.MaxCount);
            
            if (effectiveMax > 0 && nameText != null)
            {
                nameText.text = $"{item.Name} ({currentCount}/{effectiveMax})";
            }
        }
        
        if (priceText)
        {
            priceText.text = $"{item.Price}";
            // 돈 부족하거나 획득 불가하면 빨간색
            bool canAfford = GameManager.Instance.CurrentGold >= item.Price;
            bool canBuy = canAfford && canAcquire;
            priceText.color = canBuy ? Color.white : Color.red;
            buyButton.interactable = canBuy;
        }

        // 아이콘 갱신 로직
        if (itemIconImage)
        {
            itemIconImage.sprite = item.MainIcon;
            itemIconImage.enabled = (item.MainIcon != null);
        }

        //내용물 이미지 처리
        if (itemOverlayImage)
        {
            if (item.SubIcon != null)
            {
                itemOverlayImage.enabled = true;
                itemOverlayImage.sprite = item.SubIcon;
                itemOverlayImage.color = item.IconColor; // 지정된 색상 적용
            }
            else
            {
                itemOverlayImage.enabled = false; // 내용물 없으면 끔
            }
        }

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClick);
    }

    private void OnBuyClick()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.BuyItem(myItem);
        }
    }

    // --- 툴팁 이벤트 (UIManager에 통합된 툴팁 요청) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.ShowGenericTooltip(myItem.Name, myItem.Description, GetComponent<RectTransform>());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.HideGenericTooltip();
        }
    }
}