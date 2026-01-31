using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 메인 메뉴 튜토리얼 - 메타샵과 잡화점 안내
/// Zone 1 튜토리얼 완료 후 메인 메뉴로 돌아왔을 때 실행
/// </summary>
public class TutorialMainMenuController : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public RectTransform metaShopButton;
    public RectTransform generalStoreButton;
    public RectTransform deckSelectionButton;      // 덱 선택 버튼 추가
    public RectTransform startGameButton;
    
    [Header("Messages")]
    public string welcomeMessage = "튜토리얼을 완료했습니다!\n이제 메인 메뉴의 기능들을 알아봅시다.";
    public string metaShopMessage = "메타샵에서는 영구 업그레이드를 구매할 수 있습니다.\n게임이 끝나도 유지되는 강력한 효과들이에요!";
    public string generalStoreMessage = "잡화점에서는 일시적인 버프를 구매할 수 있습니다.\n다음 런에만 적용됩니다.";
    public string deckSelectionMessage = "덱 선택에서 다양한 덱을 해금하고 선택할 수 있습니다.\n각 덱마다 고유한 플레이 스타일이 있어요!";
    public string startGameMessage = "이제 새로운 런을 시작해보세요!\n행운을 빕니다!";
    
    private bool isActive = false;
    private bool hasShownTutorial = false;
    
    void Start()
    {
        // 메인 메뉴 튜토리얼 완료 여부 확인
        hasShownTutorial = PlayerPrefs.GetInt("MainMenuTutorialCompleted", 0) == 1;
        
        // Zone 1 튜토리얼만 완료했고 메인 메뉴 튜토리얼은 안 했을 때
        bool zone1TutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        
        if (zone1TutorialCompleted && !hasShownTutorial)
        {
            // 약간의 딜레이 후 시작
            StartCoroutine(WaitAndStart());
        }
    }
    
    private IEnumerator WaitAndStart()
    {
        yield return new WaitForSeconds(1f);
        StartMainMenuTutorial();
    }
    
    public void StartMainMenuTutorial()
    {
        if (tutorialManager == null)
        {
            Debug.LogError("TutorialManager가 설정되지 않았습니다!");
            return;
        }
        
        isActive = true;
        ShowStep1_Welcome();
    }
    
    // Step 1: 환영 메시지
    private void ShowStep1_Welcome()
    {
        if (!isActive) return;
        
        // 화면 중앙에 메시지
        // 마스크 모두 표시
        if (tutorialManager.topMask != null) tutorialManager.topMask.gameObject.SetActive(true);
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(true);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(true);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(true);
        
        // 하이라이트 숨기기
        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }
        
        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = welcomeMessage;
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
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
        ShowStep2_MetaShop();
    }
    
    // Step 2: 메타샵 안내
    private void ShowStep2_MetaShop()
    {
        if (!isActive || metaShopButton == null) 
        {
            // 메타샵 버튼이 없으면 잡화점으로 건너뛰기
            ShowStep3_GeneralStore();
            return;
        }
        
        tutorialManager.ShowStep(metaShopButton, 
                                metaShopMessage, 
                                TooltipPosition.Right,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep3_GeneralStore);
    }
    
    // Step 3: 잡화점 안내
    private void ShowStep3_GeneralStore()
    {
        if (!isActive || generalStoreButton == null)
        {
            // 잡화점 버튼이 없으면 덱 선택으로 건너뛰기
            ShowStep4_DeckSelection();
            return;
        }
        
        tutorialManager.ShowStep(generalStoreButton, 
                                generalStoreMessage, 
                                TooltipPosition.Right,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep4_DeckSelection);
    }
    
    // Step 4: 덱 선택 안내
    private void ShowStep4_DeckSelection()
    {
        if (!isActive || deckSelectionButton == null)
        {
            // 덱 선택 버튼이 없으면 게임 시작으로 건너뛰기
            ShowStep5_StartGame();
            return;
        }
        
        tutorialManager.ShowStep(deckSelectionButton, 
                                deckSelectionMessage, 
                                TooltipPosition.Right,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep5_StartGame);
    }
    
    // Step 5: 게임 시작 안내
    private void ShowStep5_StartGame()
    {
        if (!isActive || startGameButton == null)
        {
            CompleteMainMenuTutorial();
            return;
        }
        
        tutorialManager.ShowStep(startGameButton, 
                                startGameMessage, 
                                TooltipPosition.Top,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteMainMenuTutorial);
    }
    
    // 메인 메뉴 튜토리얼 완료
    private void CompleteMainMenuTutorial()
    {
        isActive = false;
        hasShownTutorial = true;
        
        // 완료 저장
        PlayerPrefs.SetInt("MainMenuTutorialCompleted", 1);
        PlayerPrefs.Save();
        
        tutorialManager.HideTutorial();
        
        Debug.Log("메인 메뉴 튜토리얼 완료!");
    }
    
    public void StopTutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }
}
