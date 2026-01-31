using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialWave2Controller : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public Button enemyInfoButton;
    
    [Header("Messages")]
    public string enemyIntroMessage = "이번 웨이브부터 다양한 적들이 등장합니다!\n적마다 특별한 능력을 가지고 있어요.";
    public string enemyInfoButtonMessage = "적 정보 버튼을 눌러\n적들의 능력을 확인할 수 있습니다!";
    public string completeMessage = "좋습니다!\n이제 혼자서 플레이해보세요.";
    
    private bool isActive = false;
    
    public void StartWave2Tutorial()
    {
        isActive = true;
        ShowStep1_EnemyIntro();
    }

    private void ShowStep1_EnemyIntro()
    {
        if (!isActive) return;
        
        if (tutorialManager.topMask != null) tutorialManager.topMask.gameObject.SetActive(true);
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(true);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(true);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(true);
        
        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = enemyIntroMessage;
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(() => 
        {
            StartCoroutine(WaitAndShowStep2());
        });
    }
    
    private IEnumerator WaitAndShowStep2()
    {
        tutorialManager.HideTutorial();
        yield return new WaitForSeconds(0.3f);
        ShowStep2_EnemyInfoButton();
    }
    
    private void ShowStep2_EnemyInfoButton()
    {
        if (!isActive) return;
        
        if (enemyInfoButton == null)
        {
            ShowStep3_Complete();
            return;
        }
        
        tutorialManager.ShowStep(
            enemyInfoButton.GetComponent<RectTransform>(), 
            enemyInfoButtonMessage, 
            TooltipPosition.Right,
            true
        );
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep3_Complete);
    }
    
    private void ShowStep3_Complete()
    {
        if (!isActive) return;
        
        if (tutorialManager.topMask != null) tutorialManager.topMask.gameObject.SetActive(true);
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(true);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(true);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(true);
        
        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = completeMessage;
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteWave2Tutorial);
    }
    
    private void CompleteWave2Tutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }
    
    public void StopTutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }
}
