using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialGeneralStoreController : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public RectTransform currencyText;
    public RectTransform basicShelf;
    public RectTransform combatShelf;
    
    private bool isActive = false;
    
    public void StartGeneralStoreTutorial()
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
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_GENERALSTORE_WELCOME");
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep2_Currency);
    }
    
    private void ShowStep2_Currency()
    {
        if (!isActive || currencyText == null) 
        {
            ShowStep3_BasicShelf();
            return;
        }
        
        tutorialManager.ShowStep(currencyText, 
                                LocalizationManager.Instance.GetText("TUTORIAL_GENERALSTORE_CURRENCY"), 
                                TooltipPosition.Bottom,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep3_BasicShelf);
    }
    
    private void ShowStep3_BasicShelf()
    {
        if (!isActive || basicShelf == null) 
        {
            ShowStep4_CombatShelf();
            return;
        }
        
        tutorialManager.ShowStep(basicShelf, 
                                LocalizationManager.Instance.GetText("TUTORIAL_GENERALSTORE_BASIC"), 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep4_CombatShelf);
    }
    
    private void ShowStep4_CombatShelf()
    {
        if (!isActive || combatShelf == null) 
        {
            ShowStep5_Complete();
            return;
        }
        
        tutorialManager.ShowStep(combatShelf, 
                                LocalizationManager.Instance.GetText("TUTORIAL_GENERALSTORE_COMBAT"), 
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
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_GENERALSTORE_COMPLETE");
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteGeneralStoreTutorial);
    }
    
    private void CompleteGeneralStoreTutorial()
    {
        isActive = false;
        
        PlayerPrefs.SetInt("GeneralStoreTutorialCompleted", 1);
        PlayerPrefs.Save();
        
        tutorialManager.HideTutorial();
        
        Debug.Log("잡화점 튜토리얼 완료");
    }
    
    public void StopTutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }
}
