using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. 'DeckInfo' 클래스(Serializable)를 사용하여 여러 덱을 리스트로 관리합니다.
/// 2. 덱 선택 / 해금 로직을 자동화했습니다. (인스펙터에서 설정 가능)
/// 3. StartGame 시 선택된 덱(Key)을 저장합니다.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [System.Serializable]
    public class DeckInfo
    {
        public string deckKey;      // 예: "Default", "단골", "도박사", "마법사"
        public string deckName;     // 표시 이름
        public int unlockCost;      // 해금 비용 (0이면 기본 해금)
        public string unlockKey;    // PlayerPrefs 키 (예: "Unlocked_Gambler")

        [Header("UI 연결")]
        public Button selectButton;       // 덱 선택 버튼
        public GameObject lockedOverlay;  // 잠김 상태일 때 덮을 패널
        public Button unlockButton;       // 해금 버튼
        public TextMeshProUGUI costText;  // 비용 텍스트
        public GameObject selectedHighlight; // 선택됨 표시
    }

    [Header("씬 설정")]
    public string gameSceneName = "Game";

    [Header("재화")]
    public TextMeshProUGUI metaCurrencyText;
    public string metaCurrencyKey = "MetaCurrency";

    [Header("메인 버튼")]
    public Button startGameButton;
    public Button quitGameButton;

    [Header("덱 목록 설정")]
    public List<DeckInfo> deckList; // [!!!] 여기서 모든 덱을 관리

    // 내부 변수
    private int totalMetaCurrency;
    private string selectedDeckKey = "SelectedDeck";
    private string currentSelectedDeck = "Default";

    void Start()
    {
        LoadMetaCurrency();
        
        // 버튼 리스너 연결
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGame);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(OnQuitGame);

        // 저장된 '마지막 선택 덱' 불러오기
        currentSelectedDeck = PlayerPrefs.GetString(selectedDeckKey, "Default");

        InitializeDeckUI();
    }

    void LoadMetaCurrency()
    {
        totalMetaCurrency = PlayerPrefs.GetInt(metaCurrencyKey, 0);
        if (metaCurrencyText != null)
        {
            metaCurrencyText.text = $"{totalMetaCurrency}";
        }
    }

    /// <summary>
    /// 모든 덱의 UI 상태(잠김/해금/선택됨)를 갱신합니다.
    /// </summary>
    void InitializeDeckUI()
    {
        foreach (var deck in deckList)
        {
            // 1. 해금 여부 확인
            // (비용이 0이면 기본 해금, 아니면 PlayerPrefs 확인)
            bool isUnlocked = (deck.unlockCost == 0) || (PlayerPrefs.GetInt(deck.unlockKey, 0) == 1);

            if (isUnlocked)
            {
                // [해금됨]
                if (deck.lockedOverlay != null) deck.lockedOverlay.SetActive(false);
                if (deck.selectButton != null)
                {
                    deck.selectButton.interactable = true;
                    // 선택 버튼 클릭 시 -> 이 덱을 선택
                    deck.selectButton.onClick.RemoveAllListeners();
                    deck.selectButton.onClick.AddListener(() => OnSelectDeck(deck.deckKey));
                }
                
                // 선택 상태 표시
                bool isSelected = (deck.deckKey == currentSelectedDeck);
                if (deck.selectedHighlight != null) deck.selectedHighlight.SetActive(isSelected);
            }
            else
            {
                // [잠김]
                if (deck.lockedOverlay != null) deck.lockedOverlay.SetActive(true);
                if (deck.selectButton != null) deck.selectButton.interactable = false; // 선택 불가
                
                if (deck.unlockButton != null)
                {
                    // 돈이 충분한지 확인
                    deck.unlockButton.interactable = (totalMetaCurrency >= deck.unlockCost);
                    
                    deck.unlockButton.onClick.RemoveAllListeners();
                    deck.unlockButton.onClick.AddListener(() => OnUnlockDeck(deck));
                }
                if (deck.costText != null)
                {
                    deck.costText.text = $"{deck.unlockCost}";
                }
                if (deck.selectedHighlight != null) deck.selectedHighlight.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 덱 선택 처리
    /// </summary>
    public void OnSelectDeck(string deckKey)
    {
        currentSelectedDeck = deckKey;
        PlayerPrefs.SetString(selectedDeckKey, currentSelectedDeck);
        PlayerPrefs.Save();
        
        // UI 갱신 (하이라이트 이동)
        InitializeDeckUI();
    }

    /// <summary>
    /// 덱 해금 처리
    /// </summary>
    public void OnUnlockDeck(DeckInfo deck)
    {
        if (totalMetaCurrency >= deck.unlockCost)
        {
            totalMetaCurrency -= deck.unlockCost;
            
            PlayerPrefs.SetInt(metaCurrencyKey, totalMetaCurrency);
            PlayerPrefs.SetInt(deck.unlockKey, 1); // 해금 저장
            PlayerPrefs.Save();

            LoadMetaCurrency();
            InitializeDeckUI(); // UI 갱신
            
            // 해금 즉시 선택해 주는 센스
            OnSelectDeck(deck.deckKey);
        }
    }

    public void OnStartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuitGame()
    {
        Application.Quit();
    }
}