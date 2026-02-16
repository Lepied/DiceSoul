using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class GeneralStoreManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Transform basicShelf; // 기초 보급품
    public Transform combatShelf; // 전투 버프
    public GameObject marketSlotPrefab;
    public MainMenuManager mainMenuManager;
    public TextMeshProUGUI currencyText;

    [Header("NPC 대사")]
    public TextMeshProUGUI npcDialogueText;
    public GameObject npcDialogueBubble;
    public float dialogueDuration = 3f; // 대사 표시 시간
    private Coroutine hideDialogueCoroutine; // 대화 숨김 코루틴 추적용

    [Header("사운드")]
    public SoundConfig purchaseSuccessSound;

    // 저장 키
    private const string KEY_LAST_RUN = "Store_LastRunCount";
    private const string KEY_STOCK_BASIC = "Store_Stock_Basic_";
    private const string KEY_STOCK_COMBAT = "Store_Stock_Combat_";

    private bool isInitialized = false;

    void Awake()
    {

        npcDialogueBubble.SetActive(false);

    }

    void Start()
    {
        if (!isInitialized)
        {
            CheckAndRestock();
            DisplayShop();
            RefreshCurrencyDisplay();
            isInitialized = true;
        }
    }
    
    void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += RefreshCurrencyDisplay;
        }
        
        // 튜토리얼 체크
        bool tutorialCompleted = PlayerPrefs.GetInt("GeneralStoreTutorialCompleted", 0) == 1;
        if (!tutorialCompleted)
        {
            TutorialGeneralStoreController tutorial = FindFirstObjectByType<TutorialGeneralStoreController>();
            if (tutorial != null)
            {
                Invoke(nameof(StartGeneralStoreTutorial), 0.1f);
            }
        }
    }
    
    private void StartGeneralStoreTutorial()
    {
        TutorialGeneralStoreController tutorial = FindFirstObjectByType<TutorialGeneralStoreController>();
        if (tutorial != null)
        {
            tutorial.StartGeneralStoreTutorial();
        }
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= RefreshCurrencyDisplay;
        }

        if (hideDialogueCoroutine != null)
        {
            StopCoroutine(hideDialogueCoroutine);
            hideDialogueCoroutine = null;
        }
    }


    public void ShowWelcomeMessage()
    {
        if (npcDialogueBubble != null && npcDialogueText != null)
        {
            ShowDialogue(GetRandomWelcomeDialogue());
        }
    }

    private void CheckAndRestock()
    {
        int currentRunCount = PlayerPrefs.GetInt("TotalRunCount", 0);
        int lastRestockRun = PlayerPrefs.GetInt(KEY_LAST_RUN, -1);

        if (currentRunCount > lastRestockRun || !PlayerPrefs.HasKey(KEY_STOCK_BASIC + "0"))
        {
            Restock();
            PlayerPrefs.SetInt(KEY_LAST_RUN, currentRunCount);
            PlayerPrefs.Save();
        }
    }

    private void Restock()
    {
        if (MarketItemDB.Instance == null) return;

        // 구매 내역 초기화
        for (int i = 0; i < 10; i++)
        {
            PlayerPrefs.DeleteKey($"Store_Bought_Basic_{i}");
            PlayerPrefs.DeleteKey($"Store_Bought_Combat_{i}");
        }
        PlayerPrefs.Save();

        //기초 보급품 3개 
        var basicItems = MarketItemDB.Instance.basicSupplies.OrderBy(x => Random.value).Take(3).ToList();
        for (int i = 0; i < basicItems.Count; i++)
            PlayerPrefs.SetString(KEY_STOCK_BASIC + i, basicItems[i].ID);

        // 전투 보조 2개
        var combatItems = MarketItemDB.Instance.combatSupports.OrderBy(x => Random.value).Take(2).ToList();
        for (int i = 0; i < combatItems.Count; i++)
            PlayerPrefs.SetString(KEY_STOCK_COMBAT + i, combatItems[i].ID);

        PlayerPrefs.Save();
    }

    private void DisplayShop()
    {
        // 기존 슬롯 삭제
        foreach (Transform t in basicShelf) Destroy(t.gameObject);
        foreach (Transform t in combatShelf) Destroy(t.gameObject);

        // 기초 보급품 진열
        for (int i = 0; i < 3; i++)
        {
            string id = PlayerPrefs.GetString(KEY_STOCK_BASIC + i);
            MarketItem item = MarketItemDB.Instance.GetItemByID(id);
            bool isBought = PlayerPrefs.HasKey($"Store_Bought_Basic_{i}");

            CreateSlot(item, basicShelf, i, isBought, "Basic");
        }

        // 전투 보조 진열
        for (int i = 0; i < 2; i++)
        {
            string id = PlayerPrefs.GetString(KEY_STOCK_COMBAT + i);
            MarketItem item = MarketItemDB.Instance.GetItemByID(id);
            bool isBought = PlayerPrefs.HasKey($"Store_Bought_Combat_{i}");

            CreateSlot(item, combatShelf, i, isBought, "Combat");
        }
    }

    private void CreateSlot(MarketItem item, Transform parent, int index, bool isBought, string typeKey)
    {
        if (item == null) return;

        GameObject go = Instantiate(marketSlotPrefab, parent);
        MarketSlot slot = go.GetComponent<MarketSlot>();
        if (slot != null)
        {
            slot.Setup(item, index, isBought, typeKey, this, mainMenuManager);
        }
    }

    public void TryBuyItem(MarketItem item, int index, string typeKey)
    {
        int currency = PlayerPrefs.GetInt("MetaCurrency", 0);

        if (currency >= item.Price)
        {
            //구매
            currency -= item.Price;
            PlayerPrefs.SetInt("MetaCurrency", currency);

            SoundManager.Instance.PlaySoundConfig(purchaseSuccessSound);

            // 효과 저장
            string buffs = PlayerPrefs.GetString("NextRunBuffs", "");
            PlayerPrefs.SetString("NextRunBuffs", buffs + item.EffectKey + ",");

            // 품절 처리
            PlayerPrefs.SetInt($"Store_Bought_{typeKey}_{index}", 1);
            PlayerPrefs.Save();

            // UI 갱신 
            DisplayShop();
            RefreshCurrencyDisplay();
            ShowDialogue(GetRandomPurchaseDialogue());
        }
        else
        {
            ShowDialogue(GetRandomInsufficientDialogue());
        }
    }

    public void RefreshCurrencyDisplay()
    {
        int currency = PlayerPrefs.GetInt("MetaCurrency", 0);
        if (currencyText != null)
        {
            currencyText.text = $": {currency}";
        }
    }

    // NPC 대사 표시
    private void ShowDialogue(string dialogue)
    {

        npcDialogueText.text = dialogue;
        npcDialogueBubble.SetActive(true);

        // 이전 코루틴이 실행 중이면 중지
        if (hideDialogueCoroutine != null)
        {
            StopCoroutine(hideDialogueCoroutine);
        }

        hideDialogueCoroutine = StartCoroutine(HideDialogueAfterDelay());
    }

    // 일정 시간 후 말풍선 숨기기
    private System.Collections.IEnumerator HideDialogueAfterDelay()
    {
        yield return new UnityEngine.WaitForSeconds(dialogueDuration);

        if (npcDialogueBubble != null)
        {
            npcDialogueBubble.SetActive(false);
        }
    }

    // 환영 대사
    private string GetRandomWelcomeDialogue()
    {
        string[] keys = new string[]
        {
            "MARKET_NPC_WELCOME_1",
            "MARKET_NPC_WELCOME_2",
            "MARKET_NPC_WELCOME_3",
            "MARKET_NPC_WELCOME_4",
        };
        string randomKey = keys[Random.Range(0, keys.Length)];
        return LocalizationManager.Instance.GetText(randomKey);
    }

    // 구매 성공 대사
    private string GetRandomPurchaseDialogue()
    {
        string[] keys = new string[]
        {
            "MARKET_NPC_PURCHASE_1",
            "MARKET_NPC_PURCHASE_2",
            "MARKET_NPC_PURCHASE_3",
        };
        string randomKey = keys[Random.Range(0, keys.Length)];
        return LocalizationManager.Instance.GetText(randomKey);
    }

    // 재화 부족 대사
    private string GetRandomInsufficientDialogue()
    {
        string[] keys = new string[]
        {
            "MARKET_NPC_INSUFFICIENT_1",
            "MARKET_NPC_INSUFFICIENT_2",
            "MARKET_NPC_INSUFFICIENT_3",
        };
        string randomKey = keys[Random.Range(0, keys.Length)];
        return LocalizationManager.Instance.GetText(randomKey);
    }
}