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
                                LocalizationManager.Instance.GetText("TUTORIAL_SHOP_WELCOME"), 
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
                                LocalizationManager.Instance.GetText("TUTORIAL_SHOP_ITEMS"), 
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
                                LocalizationManager.Instance.GetText("TUTORIAL_SHOP_REROLL"), 
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
                                LocalizationManager.Instance.GetText("TUTORIAL_SHOP_EXIT"), 
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
            // 튜토리얼 완료 처리
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.SetInt("JustCompletedTutorial", 1); // 메인 메뉴에서 세이브 파일 로드 방지
            PlayerPrefs.Save();
            Debug.Log("[TutorialShop] 튜토리얼 완료 - 메인 메뉴로 이동");
            
            // 세이브 파일 삭제
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSaveFile();
                Debug.Log("[TutorialShop] 튜토리얼 세이브 파일 삭제");
            }
            
            // 메인 메뉴로 이동
            UIManager.Instance.FadeOut(1.0f, () =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });
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
