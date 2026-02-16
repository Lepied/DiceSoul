using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TutorialDeckSelectionController : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public RectTransform snapScrollerArea;
    public RectTransform actionButton;
    
    private bool isActive = false;
    private MainMenuManager mainMenuManager;
    private DeckListItem currentFocusedItem;
    
    void Start()
    {
        mainMenuManager = FindFirstObjectByType<MainMenuManager>();
    }
    
    public void StartDeckSelectionTutorial()
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
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_DECKSELECTION_WELCOME");
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep2_Carousel);
    }
    
    private void ShowStep2_Carousel()
    {
        if (!isActive || snapScrollerArea == null) 
        {
            ShowStep3_Info();
            return;
        }
        
        tutorialManager.ShowStep(snapScrollerArea, 
                                LocalizationManager.Instance.GetText("TUTORIAL_DECKSELECTION_CAROUSEL"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep3_Info);
    }
    
    private void ShowStep3_Info()
    {
        if (!isActive) return;
        
        // MainMenuManager의 currentFocusedItem 가져오기
        if (mainMenuManager != null)
        {
            currentFocusedItem = GetCurrentFocusedItem();
        }
        
        if (currentFocusedItem == null)
        {
            ShowStep4_ActionButton();
            return;
        }
        
        tutorialManager.ShowStep(currentFocusedItem.GetComponent<RectTransform>(), 
                                LocalizationManager.Instance.GetText("TUTORIAL_DECKSELECTION_INFO"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep4_ActionButton);
    }
    
    private void ShowStep4_ActionButton()
    {
        if (!isActive || actionButton == null) 
        {
            ShowStep5_Complete();
            return;
        }
        
        tutorialManager.ShowStep(actionButton, 
                                LocalizationManager.Instance.GetText("TUTORIAL_DECKSELECTION_ACTION"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep5_Complete);
    }
    
    private void ShowStep5_Complete()
    {
        if (!isActive) return;
        
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
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_DECKSELECTION_COMPLETE");
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteDeckSelectionTutorial);
    }
    
    private void CompleteDeckSelectionTutorial()
    {
        isActive = false;
        
        PlayerPrefs.SetInt("DeckSelectionTutorialCompleted", 1);
        PlayerPrefs.Save();
        
        tutorialManager.HideTutorial();
        
        Debug.Log("병참 튜토리얼 완료");
    }
    
    public void StopTutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }
    
    // MainMenuManager의 private 변수 currentFocusedItem에 접근하기 위한 헬퍼
    private DeckListItem GetCurrentFocusedItem()
    {
        if (mainMenuManager == null) return null;
        
        // snapScroller를 통해 현재 중앙 아이템 찾기
        DeckSnapScroller scroller = mainMenuManager.snapScroller;
        if (scroller != null)
        {
            // DeckSnapScroller의 현재 중앙 인덱스를 구해서 해당 아이템 반환
            // 또는 모든 DeckListItem을 찾아서 중앙에 가장 가까운 것 찾기
            DeckListItem[] allItems = FindObjectsByType<DeckListItem>(FindObjectsSortMode.None);
            
            if (allItems.Length > 0)
            {
                // 첫 번째 아이템 반환 (보통 기본 덱)
                return allItems[0];
            }
        }
        
        return null;
    }
}
