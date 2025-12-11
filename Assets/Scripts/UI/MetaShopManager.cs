using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MetaShopManager : MonoBehaviour
{
    [Header("설정")]
    public string currencySaveKey = "MetaCurrency";
    public List<MetaShopSlot> allSlots;

    [Header("UI - 상단")]
    public TextMeshProUGUI currencyText;

    [Header("UI - 하단 상세 정보창")]
    public GameObject detailPanel; 
    public TextMeshProUGUI detailTitle;
    public TextMeshProUGUI detailDesc;
    public TextMeshProUGUI detailEffect;
    public Button buyButton;
    public TextMeshProUGUI buyCostText;

    private MetaShopSlot currentSlot;

    void Start()
    {
        if (allSlots == null || allSlots.Count == 0)
        {
            // 씬에 있는 모든 MetaShopSlot을 찾아서 리스트에 담음
            allSlots = new List<MetaShopSlot>(GetComponentsInChildren<MetaShopSlot>(true));
        }
        UpdateCurrencyUI();
        
        // 모든 슬롯 초기화
        foreach (var slot in allSlots)
        {
            slot.Setup(this);
        }

        if (detailPanel != null) detailPanel.SetActive(false);
        if (buyButton != null) buyButton.onClick.AddListener(TryBuyUpgrade);
    }

    public void UpdateCurrencyUI()
    {
        int souls = PlayerPrefs.GetInt(currencySaveKey, 0);
        if (currencyText != null) currencyText.text = $"{souls}";
    }

    // 슬롯 클릭 시 호출
    public void OnSlotClicked(MetaShopSlot slot)
    {
        currentSlot = slot;
        UpdateDetailPanel();
        if (detailPanel != null) detailPanel.SetActive(true);
    }

    private void UpdateDetailPanel()
    {
        if (currentSlot == null) return;

        MetaUpgradeData data = currentSlot.data;
        int currentLevel = PlayerPrefs.GetInt(data.id, 0);

        detailTitle.text = data.displayName;
        detailDesc.text = data.description;

        // 만렙 체크
        if (currentLevel >= data.maxLevel)
        {
            detailEffect.text = "최고 레벨 도달";
            buyCostText.text = "-";
            buyButton.interactable = false;
        }
        else
        {
            float curVal = data.GetTotalEffect(currentLevel);
            float nextVal = data.GetTotalEffect(currentLevel + 1);
            int cost = data.GetCost(currentLevel);
            int mySouls = PlayerPrefs.GetInt(currencySaveKey, 0);

            detailEffect.text = $"현재 효과: {curVal} ▶ <color=green>{nextVal}</color>";
            buyCostText.text = $"{cost} 마석";
            buyButton.interactable = (mySouls >= cost);
        }
    }

    private void TryBuyUpgrade()
    {
        if (currentSlot == null) return;

        MetaUpgradeData data = currentSlot.data;
        int currentLevel = PlayerPrefs.GetInt(data.id, 0);
        int cost = data.GetCost(currentLevel);
        int mySouls = PlayerPrefs.GetInt(currencySaveKey, 0);

        if (mySouls >= cost && currentLevel < data.maxLevel)
        {
            // 결제 & 저장
            PlayerPrefs.SetInt(currencySaveKey, mySouls - cost);
            PlayerPrefs.SetInt(data.id, currentLevel + 1);
            PlayerPrefs.Save();

            // UI 갱신
            UpdateCurrencyUI();
            currentSlot.RefreshUI(); 
            UpdateDetailPanel();   
            
            Debug.Log($"구매 성공: {data.displayName} Lv.{currentLevel + 1}");
        }
    }
}