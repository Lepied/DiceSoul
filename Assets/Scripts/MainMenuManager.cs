using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. "Warrior" -> "단골(Regular)"로 이름 변경
/// 2. 'OnStartGame' 함수가 "Default" 덱을 선택하도록 되돌림
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("씬(Scene) 설정")]
    [Tooltip("로드할 게임 씬의 이름")]
    public string gameSceneName = "Game"; 

    [Header("영구 재화 (Meta)")]
    [Tooltip("영구 재화를 표시할 텍스트")]
    public TextMeshProUGUI metaCurrencyText;
    [Tooltip("GameManager의 metaCurrencySaveKey와 '반드시' 일치해야 함")]
    public string metaCurrencyKey = "MetaCurrency";
    
    [Header("기본 버튼")]
    public Button startGameButton;
    public Button openShopButton;
    public Button quitGameButton;

    [Header("강화 상점 UI")]
    public GameObject upgradeShopPanel;
    public Button closeShopButton;
    
    // [!!! 신규 추가 !!!] "단골" 덱 해금 UI
    [Header("단골 덱 해금 (예시)")]
    public Button unlockRegularDeckButton; 
    public TextMeshProUGUI regularDeckCostText;
    public int regularDeckCost = 1000;

    // (TODO: '도박사 덱' 등 다른 해금 버튼들)
    // public Button unlockGamblerDeckButton; 

    // 내부 변수
    private int totalMetaCurrency;
    private string selectedDeckKey = "SelectedDeck"; 
    private string regularDeckUnlockedKey = "RegularDeckUnlocked"; // [!!! 신규 추가 !!!]

    void Start()
    {
        LoadMetaCurrency();

        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(false);
        
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGame);
        if (openShopButton != null) openShopButton.onClick.AddListener(OnOpenShop);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(OnQuitGame);
        if (closeShopButton != null) closeShopButton.onClick.AddListener(OnCloseShop);
        
        InitializeShopButtons();
    }

    void LoadMetaCurrency()
    {
        totalMetaCurrency = PlayerPrefs.GetInt(metaCurrencyKey, 0);
        if (metaCurrencyText != null)
        {
            metaCurrencyText.text = $"영혼의 파편: {totalMetaCurrency}";
        }
    }

    /// <summary>
    /// [수정] "단골 덱" 해금 로직 추가
    /// </summary>
    void InitializeShopButtons()
    {
        // [!!! 신규 추가 !!!] "단골" 덱 로직
        if (unlockRegularDeckButton != null)
        {
            bool isRegularUnlocked = PlayerPrefs.GetInt(regularDeckUnlockedKey, 0) == 1;

            if (isRegularUnlocked)
            {
                unlockRegularDeckButton.interactable = false;
                regularDeckCostText.text = "해금 완료";
            }
            else
            {
                regularDeckCostText.text = $"{regularDeckCost} 파편";
                // 돈이 충분한지 확인하여 버튼 활성화
                unlockRegularDeckButton.interactable = (totalMetaCurrency >= regularDeckCost);
                // 버튼에 '구매' 함수 연결
                unlockRegularDeckButton.onClick.AddListener(() => OnUnlockDeck(regularDeckUnlockedKey, regularDeckCost));
            }
        }
        
        // (TODO: '도박사 덱' 등 다른 해금 덱 로직)
    }

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// [게임 시작] 버튼 클릭 시 "Default" 덱을 선택한 것으로 저장합니다.
    /// (나중에 덱 선택 UI를 만들면 이 값을 변경해야 함)
    /// </summary>
    public void OnStartGame()
    {
        // (TODO: 덱 선택 UI에서 값을 가져오도록 수정)
        
        // "Default" 덱을 선택한 것으로 PlayerPrefs에 저장
        PlayerPrefs.SetString(selectedDeckKey, "Default");
        
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnOpenShop()
    {
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(true);
    }

    public void OnCloseShop()
    {
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(false);
    }

    public void OnQuitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// (상점) 덱 해금 버튼 클릭
    /// </summary>
    private void OnUnlockDeck(string unlockKey, int cost)
    {
        if (totalMetaCurrency >= cost)
        {
            totalMetaCurrency -= cost;
            
            PlayerPrefs.SetInt(metaCurrencyKey, totalMetaCurrency);
            PlayerPrefs.SetInt(unlockKey, 1); 
            PlayerPrefs.Save();

            LoadMetaCurrency();
            InitializeShopButtons(); 
        }
    }
}