using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialWave1Controller : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public Button rollButton;
    public RectTransform jokboButtonsArea;
    
    private bool isActive = false;
    private bool isCompleted = false;
    
    public void StartWave1Tutorial()
    {
        if (tutorialManager == null)
        {
            Debug.LogError("[TutorialWave1] TutorialManager가 할당되지 않았습니다!");
            return;
        }
        
        Debug.Log("[TutorialWave1] StartWave1Tutorial() 시작");
        isActive = true;
        ShowStep1_RollButton();
    }
    
    private void ShowStep1_RollButton()
    {
        if (!isActive) return;
        
        Debug.Log("[TutorialWave1] ShowStep1_RollButton() - 주사위 굴리기 튜토리얼 시작");
        
        if (rollButton == null)
        {
            Debug.LogError("[TutorialWave1] rollButton이 할당되지 않았습니다!");
            return;
        }
        
        tutorialManager.ShowStep(
            rollButton.GetComponent<RectTransform>(), 
            LocalizationManager.Instance.GetText("TUTORIAL_WAVE1_ROLL"), 
            TooltipPosition.Top,
            false
        );
        rollButton.onClick.AddListener(OnRollButtonClicked);
    }
    
    private void OnRollButtonClicked()
    {
        rollButton.onClick.RemoveListener(OnRollButtonClicked);
        

        ShowStep2_Jokbo();
    }
    
    private void ShowStep2_Jokbo()
    {
        if (!isActive) return;
        
        if (jokboButtonsArea == null)
        {
            ShowStep3_RemainingDice();
            return;
        }
        
        tutorialManager.ShowStep(
            jokboButtonsArea, 
            LocalizationManager.Instance.GetText("TUTORIAL_WAVE1_JOKBO"), 
            TooltipPosition.Bottom,
            false
        );
        
        if (StageManager.Instance != null)
        {
            StageManager.Instance.onJokboSelectedCallback = OnJokboSelected;
        }
    }
    
    private void OnJokboSelected()
    {

        if (StageManager.Instance != null)
        {
            StageManager.Instance.onJokboSelectedCallback = null;
        }
        
        tutorialManager.HideTutorial();
        
        ShowStep3_RemainingDice();
    }
    
    private void ShowStep3_RemainingDice()
    {
        if (!isActive) return;
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_WAVE1_REMAINING");

        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(OnRemainingDiceNext);
    }
    
    private void OnRemainingDiceNext()
    {
        tutorialManager.tooltipPanel.SetActive(false);
        tutorialManager.nextButton.gameObject.SetActive(false);
        ShowStep4_Complete();
    }
    
    private void ShowStep4_Complete()
    {
        if (!isActive) return;
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_WAVE1_COMPLETE");
        
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteWave1Tutorial);
    }
    
    private void CompleteWave1Tutorial()
    {
        isActive = false;
        isCompleted = true;
        tutorialManager.HideTutorial();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWave1TutorialComplete();
        }
    }
    
    public bool IsCompleted()
    {
        return isCompleted;
    }
    
    public void StopTutorial()
    {
        isActive = false;
        
        if (rollButton != null)
            rollButton.onClick.RemoveListener(OnRollButtonClicked);
        if (StageManager.Instance != null)
            StageManager.Instance.onJokboSelectedCallback = null;
        
        tutorialManager.HideTutorial();
    }
}
