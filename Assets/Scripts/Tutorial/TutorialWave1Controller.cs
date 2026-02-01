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
    
    [Header("Messages")]
    public string rollButtonMessage = "주사위를 굴려보세요!\n매 턴마다 새로운 주사위가 등장합니다.";
    public string jokboMessage = "족보를 선택해보세요! 적들에게 공격을 할 수 있습니다.";
    public string remainingDiceMessage = "족보에 주사위가 사용되고\n남은 주사위가 있으면 이어서 공격할 수 있습니다.";
    public string completeMessage = "좋습니다! 적에게 데미지를 주었습니다!\n이렇게 전투를 진행합니다.";
        
    private bool isActive = false;
    private bool isCompleted = false;
    
    public void StartWave1Tutorial()
    {
        if (tutorialManager == null)
        {
            return;
        }
        
        isActive = true;
        ShowStep1_RollButton();
    }
    
    private void ShowStep1_RollButton()
    {
        if (!isActive) return;
        
        tutorialManager.ShowStep(
            rollButton.GetComponent<RectTransform>(), 
            rollButtonMessage, 
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
            jokboMessage, 
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
        tutorialManager.tooltipText.text = remainingDiceMessage;

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
        tutorialManager.tooltipText.text = completeMessage;
        
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
