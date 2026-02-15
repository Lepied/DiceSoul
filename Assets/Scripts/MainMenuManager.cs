using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MainMenuManager : MonoBehaviour
{
    [Header("씬 설정")]
    public string gameSceneName = "Game";

    [Header("영구 재화")]
    public GameObject currencyPanel;
    public Image currencyIcon;
    public TextMeshProUGUI metaCurrencyText;
    public string metaCurrencyKey = "MetaCurrency";

    [Header("메인 화면 버튼")]
    public Button startGameButton;
    public Button openUpgradeButton;
    public Button openDeckButton;
    public Button openStoreButton;
    public Button settingsButton;

    [Header("덱 선택 UI")]
    public GameObject deckSelectionPanel;
    public Button closeDeckButton;
    public Transform deckListContent;
    public GameObject deckListItemPrefab;

    [Tooltip("모든 덱 데이터")]
    public List<DeckData> allDecks;

    [Header("캐러셀 & 액션 버튼")]
    public DeckSnapScroller snapScroller;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    [Header("업그레이드 UI")]
    public GameObject upgradeShopPanel;
    public Button closeUpgradeButton;

    [Header("잡화점 UI")]
    public GameObject generalStorePanel;
    public GeneralStoreManager generalStoreManager;
    public Button closeStoreButton;

    [Header("정보 팝업")]
    public GameObject infoPopup;
    public TextMeshProUGUI infoNameText;
    public TextMeshProUGUI infoDescText;

    [Header("설정 패널")]
    public SettingsPanelController settingsPanelController;
    
    [Header("배경음악")]
    public AudioClip mainMenuBGM;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;
    
    [Header("언어 선택 UI")]
    public GameObject languageSelectionPanel;
    public Button koreanButton;
    public Button englishButton;

    // 내부 상태 변수
    private List<DeckListItem> spawnedItems = new List<DeckListItem>();
    private DeckListItem currentFocusedItem; // 현재 중앙에 포커스된 아이템
    private int totalMetaCurrency;
    public string SelectedDeckKey { get; private set; } = "SelectedDeck";

    void Start()
    {
        // BGM 재생
        SoundManager.Instance.PlayBGM(mainMenuBGM, bgmVolume);

        // ===== 1. 최초 언어 선택 체크 =====
        bool hasSelectedLanguage = PlayerPrefs.GetInt("LanguageSelected", 0) == 1;
        if (!hasSelectedLanguage)
        {
            ShowLanguageSelection();
            return;
        }
        
        //튜토리얼 미완료 시 바로 Game 씬으로 이동
        bool tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        if (!tutorialCompleted)
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }
        
        // 세이브 파일 있으면 바로 이어하기로 (단, 방금 튜토리얼 완료한 경우 제외)
        bool justCompletedTutorial = PlayerPrefs.GetInt("JustCompletedTutorial", 0) == 1;
        if (justCompletedTutorial)
        {
            // 플래그 리셋
            PlayerPrefs.DeleteKey("JustCompletedTutorial");
            PlayerPrefs.Save();
            Debug.Log("[MainMenu] 튜토리얼 방금 완료 - 세이브 파일 무시");
        }
        else if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile())
        {
            SaveManager.shouldLoadSave = true;
            SceneManager.LoadScene(gameSceneName);
            return;
        }
        
        InitializeMainMenu();
    }
    
    /// <summary>
    /// 언어 선택 UI 표시
    /// </summary>
    private void ShowLanguageSelection()
    {
        // 모든 메인 메뉴 UI 숨기기
        if (currencyPanel != null) currencyPanel.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        if (openUpgradeButton != null) openUpgradeButton.gameObject.SetActive(false);
        if (openDeckButton != null) openDeckButton.gameObject.SetActive(false);
        if (openStoreButton != null) openStoreButton.gameObject.SetActive(false);
        if (settingsButton != null) settingsButton.gameObject.SetActive(false);
        
        // 언어 선택 패널 표시
        if (languageSelectionPanel != null)
        {
            languageSelectionPanel.SetActive(true);
            
            // 한국어 버튼
            if (koreanButton != null)
            {
                var koreanText = koreanButton.GetComponentInChildren<TextMeshProUGUI>();
                if (koreanText != null) koreanText.text = "한국어";
                koreanButton.onClick.RemoveAllListeners();
                koreanButton.onClick.AddListener(() => OnLanguageSelected(Language.Korean));
            }
            
            // English 버튼
            if (englishButton != null)
            {
                var englishText = englishButton.GetComponentInChildren<TextMeshProUGUI>();
                if (englishText != null) englishText.text = "English";
                englishButton.onClick.RemoveAllListeners();
                englishButton.onClick.AddListener(() => OnLanguageSelected(Language.English));
            }
        }
    }
    
    // 언어 선택 완료 처리
    private void OnLanguageSelected(Language selectedLanguage)
    {
        // LocalizationManager에 언어 설정
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ChangeLanguage(selectedLanguage);
        }
        PlayerPrefs.SetInt("LanguageSelected", 1);
        
        // 튜토리얼 체크
        bool tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        
        // 최초 실행 (언어 선택 직후)이고 튜토리얼 미완료면
        if (!tutorialCompleted)
        {
            PlayerPrefs.SetInt("TutorialCompleted", 0);
        }
        
        PlayerPrefs.Save();
        languageSelectionPanel.SetActive(false);

        if (!tutorialCompleted)
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }
        InitializeMainMenu();
    }
    
    // 메인 메뉴 초기화
    private void InitializeMainMenu()
    {
        // UI 표시
        if (currencyPanel != null) currencyPanel.SetActive(true);
        if (startGameButton != null) startGameButton.gameObject.SetActive(true);
        if (openUpgradeButton != null) openUpgradeButton.gameObject.SetActive(true);
        if (openDeckButton != null) openDeckButton.gameObject.SetActive(true);
        if (openStoreButton != null) openStoreButton.gameObject.SetActive(true);
        if (settingsButton != null) settingsButton.gameObject.SetActive(true);
        
        LoadMetaCurrency();

        // 패널 초기화
        if (deckSelectionPanel != null) deckSelectionPanel.SetActive(false);
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(false);
        if (generalStorePanel != null) generalStorePanel.SetActive(false);
        if (infoPopup != null) infoPopup.SetActive(false);

        // 메인 버튼 리스너 연결
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnOpenSettings);


        // 덱 패널 버튼 연결
        if (openDeckButton != null) openDeckButton.onClick.AddListener(OnOpenDeckPanel);
        if (closeDeckButton != null) closeDeckButton.onClick.AddListener(OnCloseDeckPanel);

        // 상점 버튼 연결
        if (openUpgradeButton != null) openUpgradeButton.onClick.AddListener(OnOpenUpgrade);
        if (closeUpgradeButton != null) closeUpgradeButton.onClick.AddListener(OnCloseUpgrade);

        if (openStoreButton != null) openStoreButton.onClick.AddListener(OnOpenStore);
        if (closeStoreButton != null) closeStoreButton.onClick.AddListener(OnCloseStore);

        // 설정 버튼 연결
        if (settingsButton != null) settingsButton.onClick.AddListener(OnOpenSettings);

        // 덱 목록 생성
        GenerateDeckList();
        
        // 로컬라이제이션 이벤트 구독
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }
    
    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }
    
    private void OnLanguageChanged()
    {
        // 덱 선택 패널이 열려있으면 버튼 텍스트 갱신
        if (deckSelectionPanel != null && deckSelectionPanel.activeSelf && currentFocusedItem != null)
        {
            UpdateActionButtonUI();
        }
    }

    void Update()
    {
        // ESC 키로 설정 열기/닫기
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanelController != null)
            {
                settingsPanelController.ToggleSettings();
            }
        }
    }

    void LoadMetaCurrency()
    {
        totalMetaCurrency = PlayerPrefs.GetInt(metaCurrencyKey, 0);
        if (metaCurrencyText != null)
        {
            metaCurrencyText.text = totalMetaCurrency.ToString();
        }
    }

    // 덱 목록을 생성하고 스냅 스크롤러를 초기화
    void GenerateDeckList()
    {
        if (deckListContent == null) return;

        // 1. 기존 아이템 삭제
        foreach (Transform child in deckListContent)
        {
            Destroy(child.gameObject);
        }
        spawnedItems.Clear();

        // 2. 아이템 생성
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

        // 3. 스크롤러 초기화 (생성된 리스트 전달)
        // (잠시 대기 후 초기화하여 레이아웃이 잡힌 뒤 스냅하도록 함)
        if (snapScroller != null)
        {
            snapScroller.Initialize(spawnedItems);
        }
    }

    //캐러셀 연동 로직
    public void OnDeckFocused(DeckListItem focusedItem)
    {
        currentFocusedItem = focusedItem;

        // 포커스된 아이템에 맞춰 하단 버튼 상태 갱신
        UpdateActionButtonUI();

        //  포커스된 아이템만 확대하거나 강조하는 연출
        foreach (var item in spawnedItems)
        {
            bool isCenter = (item == focusedItem);
            item.SetFocusScale(isCenter);
        }
    }

    //덱 해금관련버튼 갱신
    private void UpdateActionButtonUI()
    {
        if (currentFocusedItem == null || actionButton == null) return;

        DeckData data = currentFocusedItem.Data;

        // 해금 여부 확인
        bool isUnlocked = (data.unlockCost == 0) || (PlayerPrefs.GetInt(data.unlockKey, 0) == 1);

        // 현재 장착 중인지 확인
        string currentSelected = PlayerPrefs.GetString(SelectedDeckKey, "Default");
        bool isSelected = (data.deckKey == currentSelected);

        // 기존 리스너 제거
        actionButton.onClick.RemoveAllListeners();

        if (isUnlocked)
        {
            // [상태 1: 이미 해금됨]
            if (isSelected)
            {
                // [상태 1-A: 이미 장착 중]
                actionButton.interactable = false;
                if (actionButtonText) actionButtonText.text = LocalizationManager.Instance.GetText("MAIN_EQUIPPED");
            }
            else
            {
                // [상태 1-B: 장착 가능]
                actionButton.interactable = true;
                if (actionButtonText) actionButtonText.text = LocalizationManager.Instance.GetText("MAIN_SELECT");
                actionButton.onClick.AddListener(() => SelectDeck(data.deckKey));
            }
        }
        else
        {
            // [상태 2: 잠김 (해금 필요)]

            int currentMoney = PlayerPrefs.GetInt(metaCurrencyKey, 0);
            bool canAfford = currentMoney >= data.unlockCost;

            actionButton.interactable = canAfford;
            string unlockText = LocalizationManager.Instance.GetText("MAIN_UNLOCK");
            string costFormat = LocalizationManager.Instance.GetText("MAIN_SOULS_COST");
            if (actionButtonText) actionButtonText.text = $"{unlockText} ({string.Format(costFormat, data.unlockCost)})";

            actionButton.onClick.AddListener(() =>
            {
                UnlockDeck(data);
            });
        }
    }

    // --- 덱 선택 및 해금 로직 ---

    public void SelectDeck(string deckKey)
    {
        PlayerPrefs.SetString(SelectedDeckKey, deckKey);
        PlayerPrefs.Save();

        // 버튼 상태 즉시 갱신 (선택하기 -> 장착 중)
        UpdateActionButtonUI();

        // 모든 리스트 아이템의 비주얼 갱신 (테두리 표시 등)
        foreach (var item in spawnedItems) item.RefreshVisuals();
    }

    public void UnlockDeck(DeckData data)
    {
        if (totalMetaCurrency >= data.unlockCost)
        {
            // 1. 재화 차감 및 저장
            totalMetaCurrency -= data.unlockCost;
            PlayerPrefs.SetInt(metaCurrencyKey, totalMetaCurrency);
            PlayerPrefs.SetInt(data.unlockKey, 1);
            PlayerPrefs.Save();

            // 2. UI 갱신
            LoadMetaCurrency(); // 상단 재화 텍스트 갱신
            currentFocusedItem.RefreshVisuals(); // 해당 아이템의 자물쇠 오버레이 제거

            // 3. 버튼 상태 갱신 (해금하기 -> 선택하기/장착중)
            // 해금 후 바로 선택되게 하고 싶으면 여기서 SelectDeck 호출
            UpdateActionButtonUI();
        }
    }

    // 아이콘 위에 정보 팝업
    public void ShowInfoPopup(string title, string description, RectTransform targetIcon)
    {
        if (infoPopup == null) return;

        infoPopup.SetActive(true);

        if (infoNameText != null) infoNameText.text = title;
        if (infoDescText != null) infoDescText.text = description;
        LayoutRebuilder.ForceRebuildLayoutImmediate(infoPopup.GetComponent<RectTransform>());

        //  위치 설정 (갱신된 크기를 바탕으로 계산)
        if (targetIcon != null)
        {
            RectTransform popupRect = infoPopup.GetComponent<RectTransform>();
            Vector3 iconPos = targetIcon.position;

            // 아이콘의 위쪽 + 팝업의 반절 높이 + 여유공간
            float yOffsetUp = (targetIcon.rect.height * targetIcon.lossyScale.y / 2f) + 
                              (popupRect.rect.height * popupRect.lossyScale.y / 2f) + 10f;
            Vector3 topPosition = iconPos + new Vector3(0, yOffsetUp, 0);

            // 화면 밖으로 넘치는지 체크
            float popupTopEdge = topPosition.y + (popupRect.rect.height * popupRect.lossyScale.y / 2f);
            if (popupTopEdge > Screen.height)
            {
                // 넘치면 아래쪽에 표시
                infoPopup.transform.position = iconPos + new Vector3(0, -yOffsetUp, 0);
            }
            else
            {
                infoPopup.transform.position = topPosition;
            }
        }
    }

    public void HideInfoPopup()
    {
        if (infoPopup != null) infoPopup.SetActive(false);
    }

    // --- 패널 제어 ---
    public void OnOpenDeckPanel()
    {
        if (deckSelectionPanel != null)
        {
            deckSelectionPanel.SetActive(true);
            // 패널이 열릴 때, 현재 선택된 덱이나 첫 번째 덱으로 스크롤 이동하면 좋음
            // snapScroller.SnapToItem(0); 
        }
    }

    public void OnCloseDeckPanel()
    {
        if (deckSelectionPanel != null) deckSelectionPanel.SetActive(false);
    }

    // --- 상점 패널 제어 ---
    public void OnOpenUpgrade()
    {
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(true);
    }
    public void OnCloseUpgrade()
    {
        if (upgradeShopPanel != null) upgradeShopPanel.SetActive(false);
    }

    private void OnOpenStore()
    {
        if (generalStorePanel != null)
        {
            generalStorePanel.SetActive(true);
        }
        
        if (generalStoreManager != null)
        {
            generalStoreManager.RefreshCurrencyDisplay();
            generalStoreManager.ShowWelcomeMessage();
        }
    }
    private void OnCloseStore()
    {
        if (generalStorePanel != null)
        {
            generalStorePanel.SetActive(false);
            LoadMetaCurrency();
        }
    }

    // --- 설정 패널 제어 ---
    private void OnOpenSettings()
    {
        if (settingsPanelController != null)
        {
            settingsPanelController.OpenSettings();
        }
    }

    // --- 게임 실행 ---
    public void OnStartGame()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadGameWithFade();
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}