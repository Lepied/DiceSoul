using UnityEngine;
using UnityEngine.UI;
using System.Collections;

//상점 튜토리얼 - Zone 1 클리어 후 진행
public class TutorialShopController : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public RectTransform maintenancePanel;      // 상점 전체 패널
    public RectTransform shopItemsContainer;    // 상품 컨테이너
    public Button rerollButton;                 // 리롤 버튼
    public Button exitShopButton;               // 나가기/확인 버튼
    
    [Header("Messages")]
    public string shopWelcomeMessage = "상점에 오신 것을 환영합니다!";
    public string shopItemsMessage = "골드로 주사위, 유물, 포션 등을 구매할 수 있습니다.\n각 상품을 클릭해서 확인해보세요!";
    public string rerollMessage = "마음에 드는 상품이 없다면 리롤할 수 있습니다!\n(골드 필요)";
    public string exitMessage = "원하는 상품을 구매하셨나요?\n나가기 버튼으로 다음 존으로 이동하세요!";
    
    private bool isActive = false;
    
    public void StartShopTutorial()
    {
        if (tutorialManager == null)
        {
            Debug.LogError("TutorialManager가 설정되지 않았습니다!");
            return;
        }
        
        isActive = true;
        ShowStep1_ShopWelcome();
    }
    
    // Step S-1: 상점 환영 메시지
    private void ShowStep1_ShopWelcome()
    {
        if (!isActive) return;
        
        if (maintenancePanel == null)
        {
            Debug.LogWarning("상점 패널이 설정되지 않았습니다!");
            CompleteShopTutorial();
            return;
        }
        
        tutorialManager.ShowStep(maintenancePanel, 
                                shopWelcomeMessage, 
                                TooltipPosition.Top,
                                true);
        
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
        ShowStep2_ShopItems();
    }
    
    // Step S-2: 상품 컨테이너
    private void ShowStep2_ShopItems()
    {
        if (!isActive) return;
        
        if (shopItemsContainer == null)
        {
            Debug.LogWarning("상품 컨테이너가 설정되지 않았습니다!");
            ShowStep3_Reroll();
            return;
        }
        
        tutorialManager.ShowStep(shopItemsContainer, 
                                shopItemsMessage, 
                                TooltipPosition.Top,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep3_Reroll);
    }
    
    // Step S-3: 리롤 버튼
    private void ShowStep3_Reroll()
    {
        if (!isActive) return;
        
        if (rerollButton == null)
        {
            Debug.LogWarning("리롤 버튼이 설정되지 않았습니다!");
            ShowStep4_Exit();
            return;
        }
        
        tutorialManager.ShowStep(rerollButton.GetComponent<RectTransform>(), 
                                rerollMessage, 
                                TooltipPosition.Auto,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep4_Exit);
    }
    
    // Step S-4: 나가기 버튼
    private void ShowStep4_Exit()
    {
        if (!isActive) return;
        
        if (exitShopButton == null)
        {
            Debug.LogWarning("나가기 버튼이 설정되지 않았습니다!");
            CompleteShopTutorial();
            return;
        }
        
        tutorialManager.ShowStep(exitShopButton.GetComponent<RectTransform>(), 
                                exitMessage, 
                                TooltipPosition.Auto,
                                false); // 나가기 버튼 클릭으로 종료
        
        exitShopButton.onClick.AddListener(OnExitClicked);
    }
    
    private void OnExitClicked()
    {
        exitShopButton.onClick.RemoveListener(OnExitClicked);
        CompleteShopTutorial();
    }
    
    // 상점 튜토리얼 완료
    private void CompleteShopTutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
        
        Debug.Log("상점 튜토리얼 완료!");
        
        // Zone 1 튜토리얼 전체 완료 → 메인 메뉴로 이동
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTutorialCompleted();
        }
    }
    
    public void StopTutorial()
    {
        isActive = false;
        
        if (exitShopButton != null)
            exitShopButton.onClick.RemoveListener(OnExitClicked);
        
        tutorialManager.HideTutorial();
    }
}
