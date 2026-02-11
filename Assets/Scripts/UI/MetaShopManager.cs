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
    
    [Header("연결선 설정")]
    public Transform mapContentTransform;
    public GameObject upgradePanel;

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
        
        // 노드 연결선 생성 (패널을 임시로 활성화)
        bool wasActive = upgradePanel != null && upgradePanel.activeSelf;
        
        if (upgradePanel != null && !wasActive)
        {
            upgradePanel.SetActive(true);
        }
        
        CreateConnectionLines();
        
        if (upgradePanel != null && !wasActive)
        {
            upgradePanel.SetActive(false);
        }
        
        // 로컬라이제이션 이벤트 구독
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += RefreshCurrentDetail;
        }
    }
    
    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= RefreshCurrentDetail;
        }
    }
    
    private void RefreshCurrentDetail()
    {
        // 언어 변경 시 현재 열린 상세창 갱신
        if (currentSlot != null && detailPanel != null && detailPanel.activeSelf)
        {
            UpdateDetailPanel();
        }
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

        detailTitle.text = data.GetLocalizedName();
        detailDesc.text = data.GetLocalizedDescription();
        
        // 잠금 상태 체크
        bool isLocked = !IsUpgradeUnlocked(data);
        
        if (isLocked)
        {
            detailEffect.text = LocalizationManager.Instance.GetText("META_LOCKED");
            buyCostText.text = "-";
            buyButton.interactable = false;
            return;
        }

        // 만렙 체크
        if (currentLevel >= data.maxLevel)
        {
            detailEffect.text = LocalizationManager.Instance.GetText("META_MAX_LEVEL");
            buyCostText.text = "-";
            buyButton.interactable = false;
        }
        else
        {
            float curVal = data.GetTotalEffect(currentLevel);
            float nextVal = data.GetTotalEffect(currentLevel + 1);
            int cost = data.GetCost(currentLevel);
            int mySouls = PlayerPrefs.GetInt(currencySaveKey, 0);

            string effectLabel = LocalizationManager.Instance.GetText("META_CURRENT_EFFECT");
            string arrow = LocalizationManager.Instance.GetText("META_NEXT_ARROW");
            detailEffect.text = $"{effectLabel}: {curVal} {arrow} <color=green>{nextVal}</color>";
            
            string costFormat = LocalizationManager.Instance.GetText("MAIN_SOULS_COST");
            buyCostText.text = string.Format(costFormat, cost);
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
    
    // 노드 간 연결선 생성
    private void CreateConnectionLines()
    {
        Transform linesContainer = mapContentTransform.Find("ConnectionLines");

        // 슬롯마다 ID로 매핑해서 빠른검색
        Dictionary<string, MetaShopSlot> slotMap = new Dictionary<string, MetaShopSlot>();
        foreach (var slot in allSlots)
        {
            if (slot != null && slot.data != null)
            {
                slotMap[slot.data.id] = slot;
            }
        }
        
        // 각 업그레이드마다 연결선
        foreach (var upgrade in allMetaUpgrades)
        {
            if (upgrade.tier == 1) continue;
            
            // 같은 카테고리의 이전 단계 노드들 찾기
            List<MetaUpgradeData> parentUpgrades = allMetaUpgrades.FindAll(u => 
                u != null &&
                u.category == upgrade.category && 
                u.tier == upgrade.tier - 1
            );
            
            // 각 노드마다 선 그리기
            foreach (var parent in parentUpgrades)
            {
                if (slotMap.ContainsKey(parent.id) && slotMap.ContainsKey(upgrade.id))
                {
                    CreateLine(
                        linesContainer,
                        slotMap[parent.id],
                        slotMap[upgrade.id],
                        upgrade.category
                    );
                }
            }
        }
    }
    
    // 두 위치 사이에 선 생성
    private void CreateLine(Transform container, MetaShopSlot fromSlot, MetaShopSlot toSlot, MetaCategory category)
    {
        if (fromSlot == null || toSlot == null) return;
        
        // 선 오브젝트 생성
        GameObject lineObj = new GameObject($"Line_{fromSlot.data.id}_to_{toSlot.data.id}");
        lineObj.transform.SetParent(container, false);
        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.sprite = null; // 기본 흰색 사각형
        lineImage.raycastTarget = false;
        
        //색
        lineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.35f);
        
        // 위치/크기/회전
        RectTransform fromRect = fromSlot.GetComponent<RectTransform>();
        RectTransform toRect = toSlot.GetComponent<RectTransform>();
        
        if (fromRect == null || toRect == null) return;
        
        Vector2 startPos = fromRect.anchoredPosition;
        Vector2 endPos = toRect.anchoredPosition;
        Vector2 midPoint = (startPos + endPos) / 2f;
        
        float distance = Vector2.Distance(startPos, endPos);
        float angle = Mathf.Atan2(endPos.y - startPos.y, endPos.x - startPos.x) * Mathf.Rad2Deg;
        
        // 선 설정
        lineRect.anchoredPosition = midPoint;
        lineRect.sizeDelta = new Vector2(distance, 3f); // 높이=3px
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}