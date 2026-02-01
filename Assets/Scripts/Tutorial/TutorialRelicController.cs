using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialRelicController : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public RectTransform relicSelectPanel;
    public RectTransform relicCardsArea;
    
    [Header("Messages")]
    public string relicIntroMessage = "유물을 획득할 수 있습니다!\n유물은 전투에 도움을 주는 강력한 아이템입니다.";
    public string relicSelectMessage = "원하는 유물을 클릭하세요!\n각 유물의 효과를 확인해보세요.";
    
    private bool isActive = false;
    
    public void StartRelicTutorial()
    {
        isActive = true;
        ShowStep1_RelicIntro();
    }
    
    private void ShowStep1_RelicIntro()
    {
        if (!isActive) return;
        
        if (tutorialManager.topMask != null)
        {
            tutorialManager.topMask.gameObject.SetActive(true);
            tutorialManager.topMask.anchorMin = Vector2.zero;
            tutorialManager.topMask.anchorMax = Vector2.one;
            tutorialManager.topMask.offsetMin = Vector2.zero;
            tutorialManager.topMask.offsetMax = Vector2.zero;
        }
        
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(false);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(false);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(false);

        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = relicIntroMessage;
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(() => 
        {
            tutorialManager.HideTutorial();
            StartCoroutine(WaitAndShowStep2());
        });
    }
    
    private IEnumerator WaitAndShowStep2()
    {
        yield return new WaitForSeconds(0.3f);
        ShowStep2_SelectRelic();
    }
    
    private void ShowStep2_SelectRelic()
    {
        if (!isActive) return;
        
        if (relicCardsArea == null)
        {
            CompleteRelicTutorial();
            return;
        }
        
        tutorialManager.ShowStep(
            relicCardsArea, 
            relicSelectMessage, 
            TooltipPosition.Bottom,
            false 
        );
        
    }
    
    public void OnRelicSelected()
    {
        if (!isActive) return;
        
        CompleteRelicTutorial();
    }
    
    // 유물 튜토리얼 완료
    private void CompleteRelicTutorial()
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
