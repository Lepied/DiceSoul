using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MetaShopManager : MonoBehaviour
{
    [Header("설정")]
    public string currencySaveKey = "MetaCurrency";
    public List<MetaShopSlot> allSlots;
    
    [Header("메타 업그레이드 데이터")]
    public List<MetaUpgradeData> allMetaUpgrades;

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
        if (allMetaUpgrades == null || allMetaUpgrades.Count == 0)
        {
            MetaUpgradeData[] loadedData = Resources.LoadAll<MetaUpgradeData>("MetaUpgrades");
            allMetaUpgrades = new List<MetaUpgradeData>(loadedData);
        }
        
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
        
        // 잠금 상태 체크
        bool isLocked = !IsUpgradeUnlocked(data);
        
        if (isLocked)
        {
            detailEffect.text = "이전 단계를 완료하세요";
            buyCostText.text = "-";
            buyButton.interactable = false;
            return;
        }

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
    
    // 업그레이드 잠김 여부 체크
    private bool IsUpgradeUnlocked(MetaUpgradeData data)
    {
        if (data == null) return false;
        
        // 1단계는 항상 잠김 해제
        if (data.tier == 1)
            return true;
        
        // 같은 카테고리의 이전 단계가 하나라도 완료되었는지 체크
        int previousTier = data.tier - 1;
        
        if (allMetaUpgrades == null || allMetaUpgrades.Count == 0)
            return false;
        
        foreach (var other in allMetaUpgrades)
        {
            if (other.category == data.category && other.tier == previousTier)
            {
                int otherLevel = PlayerPrefs.GetInt(other.id, 0);
                if (otherLevel >= other.maxLevel)
                    return true;
            }
        }
        
        return false;
    }

    private void TryBuyUpgrade()
    {
        if (currentSlot == null) return;

        MetaUpgradeData data = currentSlot.data;
        
        // 잠금 체크
        if (!IsUpgradeUnlocked(data))
        {
            return;
        }
        
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
            
            // 다른 슬롯들도 갱신
            foreach (var slot in allSlots)
            {
                if (slot != null) slot.RefreshUI();
            }
            
        }
    }
}