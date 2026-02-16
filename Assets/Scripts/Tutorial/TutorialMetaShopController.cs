using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TutorialMetaShopController : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public RectTransform mapContentTransform;
    public GameObject detailPanel;
    public RectTransform buyButton;
    public RectTransform resetAllButton;
    
    [HideInInspector]
    public bool waitingForSlotClick = false;
    [HideInInspector]
    public MetaShopSlot targetSlotForTutorial;
    
    private bool isActive = false;
    private MetaShopManager metaShopManager;
    
    void Start()
    {
        metaShopManager = FindFirstObjectByType<MetaShopManager>();
    }
    
    public void StartMetaShopTutorial()
    {
        isActive = true;
        ShowStep1_Welcome();
    }
    
    private void ShowStep1_Welcome()
    {
        if (!isActive) return;
        
        // 화면 중앙 메시지
        if (tutorialManager.topMask != null) tutorialManager.topMask.gameObject.SetActive(true);
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(true);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(true);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(true);
        
        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_WELCOME");
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep2_Category);
    }
    
    private void ShowStep2_Category()
    {
        if (!isActive || mapContentTransform == null) 
        {
            ShowStep3_Node();
            return;
        }
        
        tutorialManager.ShowStep(mapContentTransform, 
                                LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_CATEGORY"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep3_Node);
    }
    
    private void ShowStep3_Node()
    {
        if (!isActive) return;
        
        // 첫 번째 구매 가능한 슬롯 찾기
        targetSlotForTutorial = FindFirstPurchasableSlot();
        
        if (targetSlotForTutorial == null)
        {
            ShowStep5_DetailPanel(); // 슬롯이 없으면 스킵
            return;
        }
        
        tutorialManager.ShowStep(targetSlotForTutorial.GetComponent<RectTransform>(), 
                                LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_NODE"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep4_ClickPrompt);
    }
    
    private void ShowStep4_ClickPrompt()
    {
        if (!isActive || targetSlotForTutorial == null)
        {
            ShowStep5_DetailPanel();
            return;
        }
        
        waitingForSlotClick = true;
        
        tutorialManager.ShowStep(targetSlotForTutorial.GetComponent<RectTransform>(), 
                                LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_CLICKPROMPT"), 
                                TooltipPosition.Auto,
                                false); // nextButton 비활성화
    }
    
    // MetaShopSlot에서 호출됨
    public void OnSlotClicked()
    {
        if (!isActive || !waitingForSlotClick) return;
        
        waitingForSlotClick = false;
        
        // 잠시 대기 후 다음 단계
        StartCoroutine(WaitAndShowDetailPanel());
    }
    
    private IEnumerator WaitAndShowDetailPanel()
    {
        tutorialManager.HideTutorial();
        yield return new WaitForSeconds(0.3f);
        ShowStep5_DetailPanel();
    }
    
    private void ShowStep5_DetailPanel()
    {
        if (!isActive) return;
        
        // detailPanel이 활성화되어 있어야 함
        if (detailPanel != null && !detailPanel.activeSelf)
        {
            // 강제로 활성화 (targetSlot이 있으면)
            if (targetSlotForTutorial != null && metaShopManager != null)
            {
                metaShopManager.OnSlotClicked(targetSlotForTutorial);
            }
        }
        
        if (detailPanel == null || !detailPanel.activeSelf)
        {
            ShowStep6_BuyButton();
            return;
        }
        
        tutorialManager.ShowStep(detailPanel.GetComponent<RectTransform>(), 
                                LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_DETAILPANEL"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep6_BuyButton);
    }
    
    private void ShowStep6_BuyButton()
    {
        if (!isActive || buyButton == null) 
        {
            ShowStep7_Level();
            return;
        }
        
        tutorialManager.ShowStep(buyButton, 
                                LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_BUYBUTTON"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep7_Level);
    }
    
    private void ShowStep7_Level()
    {
        if (!isActive || detailPanel == null || !detailPanel.activeSelf) 
        {
            ShowStep8_Reset();
            return;
        }
        
        tutorialManager.ShowStep(detailPanel.GetComponent<RectTransform>(), 
                                LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_LEVEL"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep8_Reset);
    }
    
    private void ShowStep8_Reset()
    {
        if (!isActive || resetAllButton == null) 
        {
            ShowStep9_Complete();
            return;
        }
        
        tutorialManager.ShowStep(resetAllButton, 
                                LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_RESET"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep9_Complete);
    }
    
    private void ShowStep9_Complete()
    {
        if (!isActive) return;
        
        // detailPanel 닫기
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        
        // 화면 중앙 완료 메시지
        if (tutorialManager.topMask != null) tutorialManager.topMask.gameObject.SetActive(true);
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(true);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(true);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(true);
        
        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_METASHOP_COMPLETE");
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteMetaShopTutorial);
    }
    
    private void CompleteMetaShopTutorial()
    {
        isActive = false;
        
        PlayerPrefs.SetInt("MetaShopTutorialCompleted", 1);
        PlayerPrefs.Save();
        
        tutorialManager.HideTutorial();
        
        Debug.Log("지휘소 튜토리얼 완료");
    }
    
    public void StopTutorial()
    {
        isActive = false;
        waitingForSlotClick = false;
        tutorialManager.HideTutorial();
    }
    
    // 구매 가능한 첫 번째 슬롯 찾기
    private MetaShopSlot FindFirstPurchasableSlot()
    {
        if (metaShopManager == null || metaShopManager.allSlots == null)
            return null;
        
        foreach (var slot in metaShopManager.allSlots)
        {
            if (slot == null || slot.data == null) continue;
            
            // 잠금 해제되어 있고, 최대 레벨이 아닌 슬롯
            if (slot.IsUnlocked())
            {
                int currentLevel = PlayerPrefs.GetInt(slot.data.id, 0);
                if (currentLevel < slot.data.maxLevel)
                {
                    return slot;
                }
            }
        }
        
        return null;
    }
}
