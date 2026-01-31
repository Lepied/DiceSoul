using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 유물 선택 튜토리얼 - Wave 1 클리어 직후
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
        if (tutorialManager == null)
        {
            Debug.LogError("TutorialManager가 설정되지 않았습니다!");
            return;
        }
        
        isActive = true;
        ShowStep1_RelicIntro();
    }
    
    // Step 1: 유물 시스템 소개 (화면 전체 어둡게)
    private void ShowStep1_RelicIntro()
    {
        if (!isActive) return;
        
        // 화면 전체 어둡게 + 중앙 메시지
        // 마스크로 화면 전체 덮기
        if (tutorialManager.topMask != null)
        {
            tutorialManager.topMask.gameObject.SetActive(true);
            tutorialManager.topMask.anchorMin = Vector2.zero;
            tutorialManager.topMask.anchorMax = Vector2.one;
            tutorialManager.topMask.offsetMin = Vector2.zero;
            tutorialManager.topMask.offsetMax = Vector2.zero;
        }
        
        // 나머지 마스크는 숨기기 (Top 하나로 전체 덮음)
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(false);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(false);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(false);
        
        // 하이라이트 숨기기
        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = relicIntroMessage;
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        // 화살표 숨기기
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
    
    // Step 2: 유물 선택 안내 (밝게)
    private void ShowStep2_SelectRelic()
    {
        if (!isActive) return;
        
        if (relicCardsArea == null)
        {
            Debug.LogWarning("유물 카드 영역이 설정되지 않았습니다!");
            CompleteRelicTutorial();
            return;
        }
        
        tutorialManager.ShowStep(
            relicCardsArea, 
            relicSelectMessage, 
            TooltipPosition.Bottom,
            false  // 유물 클릭으로 자동 진행
        );
        
        // 유물 선택은 외부에서 OnRelicSelected() 호출 필요
    }
    
    // 유물 선택 완료 (외부에서 호출)
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
        
        Debug.Log("유물 튜토리얼 완료!");
        
        // Wave 2 시작 (StageManager가 자동 처리)
    }
    
    public void StopTutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }
}
