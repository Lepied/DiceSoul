using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("씬 설정")]
    public string gameSceneName = "Game";

    [Header("영구 재화")]
    public TextMeshProUGUI metaCurrencyText;
    public string metaCurrencyKey = "MetaCurrency";
    
    [Header("메인 화면 버튼")]
    public Button startGameButton;
    public Button openShopButton; 
    public Button openDeckButton; //  덱 관리 패널 열기 버튼
    public Button quitGameButton;

    [Header("덱 선택/해금 UI (별도 패널)")]
    public GameObject deckSelectionPanel; // 덱 목록이 뜨는 팝업창
    public Button closeDeckButton;        //  덱 패널 닫기
    public Transform deckListContent;     // (ScrollView의 Content)
    
    [Tooltip("덱 리스트 아이템 프리팹")]
    public GameObject deckListItemPrefab;

    [Tooltip("게임에 등장할 모든 덱 데이터 (SO)")]
    public List<DeckData> allDecks; 

    // (기존 상점 UI - 덱 이외의 강화를 위해 남겨둠, 필요 없으면 무시 가능)
    [Header("일반 상점 UI (옵션)")]
    public GameObject upgradeShopPanel;
    public Button closeShopButton;

    // 생성된 아이템들의 리스트 (관리용)
    private List<DeckListItem> spawnedItems = new List<DeckListItem>();
    private int totalMetaCurrency;
    
    // 키 값
    public string SelectedDeckKey { get; private set; } = "SelectedDeck";

    void Start()
    {
        LoadMetaCurrency();
        
        // 패널 초기화 (모두 닫기)
        if (deckSelectionPanel != null) deckSelectionPanel.SetActive(false);
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(false);

        // 버튼 리스너 연결
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGame);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(OnQuitGame);
        
        // [신규] 덱 버튼 연결
        if (openDeckButton != null) openDeckButton.onClick.AddListener(OnOpenDeckPanel);
        if (closeDeckButton != null) closeDeckButton.onClick.AddListener(OnCloseDeckPanel);

        // (기존) 상점 버튼 연결
        if (openShopButton != null) openShopButton.onClick.AddListener(OnOpenShop);
        if (closeShopButton != null) closeShopButton.onClick.AddListener(OnCloseShop);

        // 덱 목록 생성 (미리 생성해둠)
        GenerateDeckList();
    }

    void LoadMetaCurrency()
    {
        totalMetaCurrency = PlayerPrefs.GetInt(metaCurrencyKey, 0);
        if (metaCurrencyText != null)
        {
            metaCurrencyText.text = $"보유 파편: {totalMetaCurrency}";
        }
    }

    /// <summary>
    /// DeckData 리스트를 순회하며 UI를 생성합니다.
    /// (이제 deckSelectionPanel 안의 Content에 생성됩니다)
    /// </summary>
    void GenerateDeckList()
    {
        if (deckListContent == null) return;

        // 기존 아이템 정리
        foreach (Transform child in deckListContent)
        {
            Destroy(child.gameObject);
        }
        spawnedItems.Clear();

        // 현재 선택된 덱 키 로드
        string currentSelected = PlayerPrefs.GetString(SelectedDeckKey, "Default");

        // 데이터 기반 생성
        foreach (DeckData data in allDecks)
        {
            if (data == null) continue;

            GameObject itemObj = Instantiate(deckListItemPrefab, deckListContent);
            DeckListItem itemScript = itemObj.GetComponent<DeckListItem>();
            
            if (itemScript != null)
            {
                itemScript.Setup(data, this);
                spawnedItems.Add(itemScript);
            }
        }
    }

    // --- 덱 패널 제어 ---
    public void OnOpenDeckPanel()
    {
        if (deckSelectionPanel != null) deckSelectionPanel.SetActive(true);
    }

    public void OnCloseDeckPanel()
    {
        if (deckSelectionPanel != null) deckSelectionPanel.SetActive(false);
    }

    // --- 덱 로직 ---

    public void SelectDeck(string deckKey)
    {
        PlayerPrefs.SetString(SelectedDeckKey, deckKey);
        PlayerPrefs.Save();
        
        // 모든 UI 갱신
        RefreshAllItems();
    }

    public void UnlockDeck(DeckData data)
    {
        if (totalMetaCurrency >= data.unlockCost)
        {
            totalMetaCurrency -= data.unlockCost;
            
            PlayerPrefs.SetInt(metaCurrencyKey, totalMetaCurrency);
            PlayerPrefs.SetInt(data.unlockKey, 1);
            PlayerPrefs.Save();

            LoadMetaCurrency(); // 돈 갱신
            RefreshAllItems();  // 버튼 상태 갱신
            
            // 해금 즉시 선택
            SelectDeck(data.deckKey);
        }
    }

    private void RefreshAllItems()
    {
        foreach (var item in spawnedItems)
        {
            item.RefreshState();
        }
    }

    // --- 게임 실행 ---

    public void OnStartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void OnQuitGame()
    {
        Application.Quit();
    }

    // --- (기존 상점 로직 유지 - 필요 시 사용) ---
    public void OnOpenShop()
    {
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(true);
    }
    public void OnCloseShop()
    {
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(false);
    }
}