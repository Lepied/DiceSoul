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
    
    [Header("Messages")]
    public string welcomeMessage = "튜토리얼을 완료했습니다!\n이제 메인 메뉴의 기능들을 알아봅시다.";
    public string currencyMessage = "출정을 통해 마석을 얻을 수 있고,\n얻은 마석은 여기에 표시됩니다.";
    public string metaShopMessage = "메타샵에서는 영구 업그레이드를 구매할 수 있습니다.\n게임이 끝나도 유지되는 강력한 효과들이에요!";
    public string generalStoreMessage = "잡화점에서는 일시적인 버프를 구매할 수 있습니다.\n다음 런에만 적용됩니다.";
    public string deckSelectionMessage = "덱 선택에서 다양한 덱을 해금하고 선택할 수 있습니다.\n각 덱마다 고유한 플레이 스타일이 있어요!";
    public string startGameMessage = "이제 새로운 런을 시작해보세요!\n행운을 빕니다!";
    
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
        tutorialManager.tooltipText.text = welcomeMessage;
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
                                currencyMessage, 
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
                                metaShopMessage, 
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
                                generalStoreMessage, 
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
                                deckSelectionMessage, 
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
                                startGameMessage, 
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
