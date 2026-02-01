using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialShopController : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public RectTransform maintenancePanel;
    public RectTransform shopItemsContainer;
    public Button rerollButton;
    public Button exitShopButton;
    
    [Header("Messages")]
    public string shopWelcomeMessage = "상점에 오신 것을 환영합니다!";
    public string shopItemsMessage = "골드로 주사위, 유물, 포션 등을 구매할 수 있습니다.\n각 상품을 클릭해서 확인해보세요!";
    public string rerollMessage = "마음에 드는 상품이 없다면 리롤할 수 있습니다!\n(골드 필요)";
    public string exitMessage = "원하는 상품을 구매하셨나요?\n나가기 버튼으로 다음 존으로 이동하세요!";
    
    private bool isActive = false;
    
    public void StartShopTutorial()
    {
        isActive = true;
        ShowStep1_ShopWelcome();
    }
    
    private void ShowStep1_ShopWelcome()
    {
        if (!isActive) return;
        
        if (maintenancePanel == null)
        {
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
    
    private void ShowStep2_ShopItems()
    {
        if (!isActive) return;
        
        if (shopItemsContainer == null)
        {
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
    
    private void ShowStep3_Reroll()
    {
        if (!isActive) return;
        
        if (rerollButton == null)
        {
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
    
    private void ShowStep4_Exit()
    {
        if (!isActive) return;
        
        if (exitShopButton == null)
        {
            CompleteShopTutorial();
            return;
        }
        
        tutorialManager.ShowStep(exitShopButton.GetComponent<RectTransform>(), 
                                exitMessage, 
                                TooltipPosition.Auto,
                                false);
        
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
