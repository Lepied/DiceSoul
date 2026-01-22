using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MetaShopSlot : MonoBehaviour
{
    [Header("데이터 연결")]
    public MetaUpgradeData data;

    [Header("UI 컴포넌트")]
    public Image iconImage;
    public Button clickButton;

    [Header("원형 레벨 표시 설정")]
    public Transform pipsContainer;
    public GameObject pipPrefab;      // 레벨 표시용
    public float pipRadius = 90f;     // 반지름
    public float startAngle = 90f;    // 시작 각도


    private List<MetaPip> pips = new List<MetaPip>();
    private MetaShopManager manager;

    public void Setup(MetaShopManager shopManager)
    {
        this.manager = shopManager;

        if (data != null && iconImage != null)
        {
            iconImage.sprite = data.icon;
        }
        
        // 버튼 클릭 시: 매니저 호출 + 카메라 이동 요청
        if (clickButton != null)
        {
            clickButton.onClick.RemoveAllListeners();
            clickButton.onClick.AddListener(() =>
            {
                manager.OnSlotClicked(this);
                if (SKillTreeController.Instance != null)
                    SKillTreeController.Instance.FocusOnSlot(GetComponent<RectTransform>());
            });
        }

        CreateCircularPips();
        RefreshUI();
    }

    private void CreateCircularPips()
    {
        foreach (Transform child in pipsContainer) Destroy(child.gameObject);
        pips.Clear();

        if (data == null || pipPrefab == null) return;

        // 360도 전체에 균등 배치
        float angleStep = 360f / Mathf.Max(1, data.maxLevel);

        for (int i = 0; i < data.maxLevel; i++)
        {
            GameObject pip = Instantiate(pipPrefab, pipsContainer);

            // 각도 계산 
            float angleRad = (startAngle - (i * angleStep)) * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * pipRadius;
            float y = Mathf.Sin(angleRad) * pipRadius;

            pip.transform.localPosition = new Vector3(x, y, 0);
            MetaPip pipScript = pip.GetComponent<MetaPip>();
            if (pipScript != null)
            {
                pips.Add(pipScript);
            }
        }
    }

    public void RefreshUI()
    {
        if (data == null) return;
        
        // 현재 레벨
        int currentLevel = PlayerPrefs.GetInt(data.id, 0);
        
        // 해금햇는지안햇는지
        bool isLocked = !IsUnlocked();

        if (iconImage != null)
        {
            iconImage.color = isLocked 
                ? new Color(0.5f, 0.5f, 0.5f, 1f) 
                : new Color(255f/255f, 140f/255f, 140f/255f, 1f);
        }
        
        if (clickButton != null)
        {
            clickButton.interactable = !isLocked;
        }
        
        // 레벨 표시
        if (currentLevel == 0 && isLocked)
        {
            if (pipsContainer != null) pipsContainer.gameObject.SetActive(false);
        }
        else
        {
            // 레벨이 1 이상이거나 해금된 경우 pip 표시
            if (pipsContainer != null) pipsContainer.gameObject.SetActive(true);

            for (int i = 0; i < pips.Count; i++)
            {
                bool isPipUnlocked = (i < currentLevel);
                if (pips[i] != null) pips[i].SetStatus(isPipUnlocked);
            }
        }
    }
    
    // 해금 여부 체크
    private bool IsUnlocked()
    {
        if (data == null) return false;
        
        // 1단계는 항상 해금
        if (data.tier == 1)
            return true;
        
        // prerequisiteID가 있으면 그것을 우선 체크
        if (!string.IsNullOrEmpty(data.prerequisiteID))
        {
            int prereqLevel = PlayerPrefs.GetInt(data.prerequisiteID, 0);
            
            // 선행 업그레이드의 maxLevel 찾기
            if (manager != null && manager.allMetaUpgrades != null)
            {
                foreach (var other in manager.allMetaUpgrades)
                {
                    if (other.id == data.prerequisiteID)
                    {
                        bool unlocked = prereqLevel >= other.maxLevel;
                        return unlocked;
                    }
                }
            }
            return false;
        }
        
        // prerequisiteID가 없으면
        int previousTier = data.tier - 1;
        
        if (manager == null || manager.allMetaUpgrades == null)
            return false;
        
        foreach (var other in manager.allMetaUpgrades)
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
}