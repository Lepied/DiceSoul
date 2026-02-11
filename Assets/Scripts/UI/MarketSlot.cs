using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MarketSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 연결")]
    public Image iconImage;
    public Button buyButton;
    public GameObject soldOutPanel; // 구매 시 덮어씌울 패널

    private MarketItem myItem;
    private int myIndex;
    private string myTypeKey; // "Basic" or "Combat"
    private GeneralStoreManager manager;
    private MainMenuManager mainMenuManager;
    private RectTransform myRect;

    void Awake()
    {
        myRect = GetComponent<RectTransform>();
    }

    public void Setup(MarketItem item, int index, bool isBought, string typeKey, GeneralStoreManager manager, MainMenuManager mainMenuManager)
    {
        this.myItem = item;
        this.myIndex = index;
        this.myTypeKey = typeKey;
        this.manager = manager;
        this.mainMenuManager = mainMenuManager;

        // 아이콘만 표시
        if (iconImage != null) iconImage.sprite = item.Icon;

        // 버튼 설정
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClick);
        }

        // 구매 상태 처리
        SetSoldOut(isBought);
    }

    private void OnBuyClick()
    {
        if (manager != null)
        {
            manager.TryBuyItem(myItem, myIndex, myTypeKey);
        }
    }

    public void SetSoldOut(bool isSoldOut)
    {
        if (buyButton != null) buyButton.interactable = !isSoldOut;
        if (soldOutPanel != null) soldOutPanel.SetActive(isSoldOut);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myItem == null || mainMenuManager == null || myRect == null) return;
        
        string name = myItem.GetLocalizedName();
        string desc = myItem.GetLocalizedDescription();
        
        string priceLabel = LocalizationManager.Instance.GetText("MARKET_PRICE");
        string priceFormat = LocalizationManager.Instance.GetText("MAIN_SOULS_COST");
        string priceText = string.Format(priceFormat, myItem.Price);
        
        string fullDescription = $"{desc}\n\n{priceLabel}: {priceText}";
        
        mainMenuManager.ShowInfoPopup(name, fullDescription, myRect);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mainMenuManager != null)
        {
            mainMenuManager.HideInfoPopup();
        }
    }}