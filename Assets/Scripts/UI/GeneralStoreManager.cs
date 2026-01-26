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

    // 저장 키
    private const string KEY_LAST_RUN = "Store_LastRunCount";
    private const string KEY_STOCK_BASIC = "Store_Stock_Basic_";
    private const string KEY_STOCK_COMBAT = "Store_Stock_Combat_";

    void Start()
    {
        if (npcDialogueBubble != null)
        {
            npcDialogueBubble.SetActive(false);
        }
        
        CheckAndRestock();
        DisplayShop();
        RefreshCurrencyDisplay();
    }
    
    // 잡화점 패널이 활성화될 때마다 인사말 표시
    void OnEnable()
    {
        if (npcDialogueBubble != null && npcDialogueText != null)
        {
            StartCoroutine(ShowDelayedWelcome());
        }
    }
    
    private System.Collections.IEnumerator ShowDelayedWelcome()
    {
        yield return new UnityEngine.WaitForSeconds(0.5f);
        ShowDialogue(GetRandomWelcomeDialogue());
    }

    private void CheckAndRestock()
    {
        // 총 플레이 횟수를 확인 / PlayerPrefs로 관리
        int currentRunCount = PlayerPrefs.GetInt("TotalRunCount", 0); 
        int lastRestockRun = PlayerPrefs.GetInt(KEY_LAST_RUN, -1);

        // 새 런이 시작되었거나 재고 데이터가 없으면 리스톡
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
        for(int i=0; i<10; i++) {
            PlayerPrefs.DeleteKey($"Store_Bought_Basic_{i}");
            PlayerPrefs.DeleteKey($"Store_Bought_Combat_{i}");
        }
        PlayerPrefs.Save();

        // 1. 기초 보급품 3개 랜덤 선정
        var basicItems = MarketItemDB.Instance.basicSupplies.OrderBy(x => Random.value).Take(3).ToList();
        for (int i = 0; i < basicItems.Count; i++)
            PlayerPrefs.SetString(KEY_STOCK_BASIC + i, basicItems[i].ID);

        // 2. 전투 보조 2개 랜덤 선정
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

            // 효과 저장 (다음 런 적용을 위해)
            string buffs = PlayerPrefs.GetString("NextRunBuffs", "");
            PlayerPrefs.SetString("NextRunBuffs", buffs + item.EffectKey + ",");

            // 품절 처리
            PlayerPrefs.SetInt($"Store_Bought_{typeKey}_{index}", 1);
            PlayerPrefs.Save();

            // UI 갱신 
            DisplayShop();
            RefreshCurrencyDisplay();
            ShowDialogue(GetRandomPurchaseDialogue());
            
            // 상단 재화 텍스트 갱신 로직 필요
            Debug.Log($"구매 성공: {item.Name}");
        }
        else
        {
            ShowDialogue(GetRandomInsufficientDialogue());
            Debug.Log("마석이 부족합니다.");
        }
    }

    public void RefreshCurrencyDisplay()
    {
        int currency = PlayerPrefs.GetInt("MetaCurrency", 0);
        if (currencyText != null)
        {
            currencyText.text = $"마석 : {currency}";
        }
    }
    
    // NPC 대사 표시
    private void ShowDialogue(string dialogue)
    {
        npcDialogueText.text = dialogue;
        npcDialogueBubble.SetActive(true);


        StopAllCoroutines();
        StartCoroutine(HideDialogueAfterDelay());
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
        string[] dialogues = new string[]
        {
            "어서오세요, 성주님!",
            "환영합니다!\n좋은 물건들이 많아요.",
            "오늘도 좋은 하루네요.\n무엇을 찾으시나요?",
            "이번 출전에\n필요한 게 있으신가요?",
        };
        return dialogues[Random.Range(0, dialogues.Length)];
    }
    
    // 구매 성공 대사
    private string GetRandomPurchaseDialogue()
    {
        string[] dialogues = new string[]
        {
            "좋은 선택이에요!",
            "감사합니다.\n도움이 되길 바래요!",
            "훌륭한 거래였어요",
        };
        return dialogues[Random.Range(0, dialogues.Length)];
    }
    
    // 재화 부족 대사
    private string GetRandomInsufficientDialogue()
    {
        string[] dialogues = new string[]
        {
            "마석이 조금 부족하신 것 같아요...",
            "조금 더 필요하신 것 같아요...",
            "저, 죄송하지만 마석이 부족해요.",
        };
        return dialogues[Random.Range(0, dialogues.Length)];
    }
}