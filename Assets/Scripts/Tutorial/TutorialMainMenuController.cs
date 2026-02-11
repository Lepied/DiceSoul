using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class TutorialMainMenuController : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public RectTransform currencyPanel;
    public RectTransform metaShopButton;
    public RectTransform generalStoreButton;
    public RectTransform deckSelectionButton;
    public RectTransform startGameButton;
    
    private bool isActive = false;
    private bool hasShownTutorial = false;
    
    void Start()
    {
        // 메인 메뉴 튜토리얼 완료 여부
        hasShownTutorial = PlayerPrefs.GetInt("MainMenuTutorialCompleted", 0) == 1;
        
        // Zone 1 튜토리얼만 완료했고 메인 메뉴 튜토리얼은 안 했을 때
        bool zone1TutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        
        if (zone1TutorialCompleted && !hasShownTutorial)
        {
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
        isActive = true;
        ShowStep1_Welcome();
    }
    
    private void ShowStep1_Welcome()
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
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_MAINMENU_WELCOME");
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position = 
            new Vector2(Screen.width / 2, Screen.height / 2);
        
        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep2_CurrencyPanel);

    }
    private void ShowStep2_CurrencyPanel()
    {
        if (!isActive || currencyPanel == null) 
        {
            ShowStep3_MetaShop();
            return;
        }
        
        tutorialManager.ShowStep(currencyPanel, 
                                LocalizationManager.Instance.GetText("TUTORIAL_MAINMENU_CURRENCY"), 
                                TooltipPosition.Bottom,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep3_MetaShop);
    }
        
    private void ShowStep3_MetaShop()
    {
        if (!isActive || metaShopButton == null) 
        {
            ShowStep4_GeneralStore();
            return;
        }
        
        tutorialManager.ShowStep(metaShopButton, 
                                LocalizationManager.Instance.GetText("TUTORIAL_MAINMENU_METASHOP"), 
                                TooltipPosition.Right,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep4_GeneralStore);
    }

    private void ShowStep4_GeneralStore()
    {
        if (!isActive || generalStoreButton == null)
        {
            ShowStep5_DeckSelection();
            return;
        }
        
        tutorialManager.ShowStep(generalStoreButton, 
                                LocalizationManager.Instance.GetText("TUTORIAL_MAINMENU_GENERALSTORE"), 
                                TooltipPosition.Right,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep5_DeckSelection);
    }
    
    private void ShowStep5_DeckSelection()
    {
        if (!isActive || deckSelectionButton == null)
        {
            ShowStep6_StartGame();
            return;
        }
        
        tutorialManager.ShowStep(deckSelectionButton, 
                                LocalizationManager.Instance.GetText("TUTORIAL_MAINMENU_DECKSELECTION"), 
                                TooltipPosition.Right,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep6_StartGame);
    }
    
    private void ShowStep6_StartGame()
    {
        if (!isActive || startGameButton == null)
        {
            CompleteMainMenuTutorial();
            return;
        }
        
        tutorialManager.ShowStep(startGameButton, 
                                LocalizationManager.Instance.GetText("TUTORIAL_MAINMENU_STARTGAME"), 
                                TooltipPosition.Top,
                                true);
        
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteMainMenuTutorial);
    }
    
    private void CompleteMainMenuTutorial()
    {
        isActive = false;
        hasShownTutorial = true;
        
        PlayerPrefs.SetInt("MainMenuTutorialCompleted", 1);
        PlayerPrefs.Save();
        
        tutorialManager.HideTutorial();
        
        Debug.Log("메인 메뉴 튜토리얼 끄읕");
    }
    
    public void StopTutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }
}
