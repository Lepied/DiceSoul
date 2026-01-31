using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Wave 1 튜토리얼 전용 단계 컨트롤러 (간소화 버전)
/// 롤 버튼 → 족보 선택 → 완료 메시지
/// </summary>
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
    
    [Header("Timing Settings")]
    public float waitForJokboDelay = 1.5f;        // 주사위 굴린 후 대기 시간
    public float waitForRemainingMessage = 1.5f;  // 공격 후 남은 주사위 메시지 대기
    public float waitForCompleteDelay = 1.5f;     // 남은 주사위 메시지 후 완료 메시지 대기
    
    private bool isActive = false;
    private bool isCompleted = false;  // 튜토리얼 완료 여부
    
    public void StartWave1Tutorial()
    {
        if (tutorialManager == null)
        {
            Debug.LogError("TutorialManager가 설정되지 않았습니다!");
            return;
        }
        
        isActive = true;
        ShowStep1_RollButton();
    }
    
    // Step 1: 롤 버튼 하이라이트
    private void ShowStep1_RollButton()
    {
        if (!isActive) return;
        
        tutorialManager.ShowStep(
            rollButton.GetComponent<RectTransform>(), 
            rollButtonMessage, 
            TooltipPosition.Top,
            false
        );
        
        // 롤 버튼 클릭 리스너 추가
        rollButton.onClick.AddListener(OnRollButtonClicked);
    }
    
    private void OnRollButtonClicked()
    {
        rollButton.onClick.RemoveListener(OnRollButtonClicked);
        

        ShowStep2_Jokbo();
    }
    
    // Step 2: 족보 버튼 영역 하이라이트
    private void ShowStep2_Jokbo()
    {
        if (!isActive) return;
        
        if (jokboButtonsArea == null)
        {
            Debug.LogWarning("족보 버튼 영역이 설정되지 않았습니다!");
            ShowStep3_RemainingDice();
            return;
        }
        
        tutorialManager.ShowStep(
            jokboButtonsArea, 
            jokboMessage, 
            TooltipPosition.Bottom,
            false
        );
        
        // 족보 선택 감지
        if (StageManager.Instance != null)
        {
            StageManager.Instance.onJokboSelectedCallback = OnJokboSelected;
        }
    }
    
    private void OnJokboSelected()
    {
        // 족보 선택 콜백 제거
        if (StageManager.Instance != null)
        {
            StageManager.Instance.onJokboSelectedCallback = null;
        }
        
        // 족보 선택 후 마스크 숨기기 (어둡게 하지 않음)
        tutorialManager.HideTutorial();
        
        // 공격 애니메이션 대기 후 완료 메시지
        StartCoroutine(WaitAndShowComplete());
    }
    
    private IEnumerator WaitAndShowComplete()
    {
        yield return new WaitForSeconds(waitForRemainingMessage); // 공격 애니메이션 대기
        ShowStep3_RemainingDice();
    }
    
    // Step 3: 남은 주사위 설명 메시지 (화면 중앙)
    private void ShowStep3_RemainingDice()
    {
        if (!isActive) return;
        
        // 화면 중앙에 말풍선만 표시
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = remainingDiceMessage;
        
        // 화면 중앙 위치
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        // 화살표 숨기기
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        // 다음 버튼 표시
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(OnRemainingDiceNext);
    }
    
    private void OnRemainingDiceNext()
    {
        tutorialManager.tooltipPanel.SetActive(false);
        tutorialManager.nextButton.gameObject.SetActive(false);
        StartCoroutine(WaitAndShowFinalComplete());
    }
    
    private IEnumerator WaitAndShowFinalComplete()
    {
        yield return new WaitForSeconds(waitForCompleteDelay);
        ShowStep4_Complete();
    }
    
    // Step 4: 완료 메시지 (화면 중앙, 어둡게 하지 않음)
    private void ShowStep4_Complete()
    {
        if (!isActive) return;
        
        // 화면 중앙에 말풍선만 표시 (어둡게 하지 않음)
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = completeMessage;
        
        // 화면 중앙 위치
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        // 화살표 숨기기
        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }
        
        // 다음 버튼 표시
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteWave1Tutorial);
    }
    
    // Wave 1 튜토리얼 완료
    private void CompleteWave1Tutorial()
    {
        isActive = false;
        isCompleted = true;
        tutorialManager.HideTutorial();
        
        Debug.Log("Wave 1 튜토리얼 완료!");
        
        // GameManager에 튜토리얼 완료 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWave1TutorialComplete();
        }
    }
    
    public bool IsCompleted()
    {
        return isCompleted;
    }
    
    // 외부에서 강제 종료
    public void StopTutorial()
    {
        isActive = false;
        
        // 모든 리스너 제거
        if (rollButton != null)
            rollButton.onClick.RemoveListener(OnRollButtonClicked);
        if (StageManager.Instance != null)
            StageManager.Instance.onJokboSelectedCallback = null;
        
        tutorialManager.HideTutorial();
    }
}
