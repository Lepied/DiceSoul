using UnityEngine;
using TMPro; 
using System.Collections.Generic; 
using UnityEngine.UI; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("기본 UI 텍스트 요소")]
    public TextMeshProUGUI waveText; 
    public TextMeshProUGUI rollCountText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI totalScoreText; 

    [Header("보상 화면 UI")]
    public GameObject rewardScreenPanel;
    public Button[] relicChoiceButtons; 
    public TextMeshProUGUI[] relicNameTexts;
    public TextMeshProUGUI[] relicDescriptionTexts;
    
    [Header("공격 선택 UI")]
    public GameObject attackOptionsPanel;
    public Button[] attackOptionButtons;
    public TextMeshProUGUI[] attackNameTexts;
    public TextMeshProUGUI[] attackValueTexts;

    [Header("정비 (상점) UI")]
    public GameObject maintenancePanel;
    public Button[] shopItemButtons;
    public TextMeshProUGUI[] shopItemNameTexts;
    public TextMeshProUGUI[] shopItemPriceTexts;
    public Button exitShopButton;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (rewardScreenPanel != null)
        {
            rewardScreenPanel.SetActive(false);
        }
        if (attackOptionsPanel != null)
        {
            attackOptionsPanel.SetActive(false);
        }
        if (maintenancePanel != null)
        {
            maintenancePanel.SetActive(false);
        }
        if (totalScoreText != null)
        {
            totalScoreText.text = "점수: 0";
        }
    }

    public void UpdateWaveText(int zone, int wave)
    {
        if (waveText != null)
        {
            waveText.text = $"Zone {zone} - Wave {wave}";
        }
    }
    public void UpdateRollCount(int current, int max)
    {
        if (rollCountText != null)
        {
            rollCountText.text = $"굴림 횟수: {current} / {max}";
        }
    }
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"체력: {currentHealth} / {maxHealth}";
        }
    }
    public void UpdateScore(int totalScore)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"점수: {totalScore}";
        }
    }
    
    public void ShowRewardScreen(List<Relic> relicOptions)
    {
        if (rewardScreenPanel == null) return;
        
        rewardScreenPanel.SetActive(true);
        for (int i = 0; i < relicChoiceButtons.Length; i++)
        {
            if (i < relicOptions.Count)
            {
                Relic relic = relicOptions[i];
                if (relicNameTexts[i] != null)
                    relicNameTexts[i].text = relic.Name;
                if (relicDescriptionTexts[i] != null)
                    relicDescriptionTexts[i].text = relic.Description;
                relicChoiceButtons[i].onClick.RemoveAllListeners(); 
                relicChoiceButtons[i].onClick.AddListener(() => {
                    SelectRelic(relic);
                });
                relicChoiceButtons[i].gameObject.SetActive(true);
            }
            else
            {
                relicChoiceButtons[i].gameObject.SetActive(false);
            }
        }
    }
    
    private void SelectRelic(Relic chosenRelic)
    {
        if (rewardScreenPanel != null)
        {
            rewardScreenPanel.SetActive(false);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRelicSelected(chosenRelic);
        }
    }

    public void ShowAttackOptions(List<AttackJokbo> attackOptions)
    {
        if (attackOptionsPanel == null) return;
        
        attackOptionsPanel.SetActive(true);
        for (int i = 0; i < attackOptionButtons.Length; i++)
        {
            if (i < attackOptions.Count)
            {
                AttackJokbo jokbo = attackOptions[i];
                if (attackNameTexts[i] != null)
                    attackNameTexts[i].text = jokbo.Description;
                if (attackValueTexts[i] != null)
                    attackValueTexts[i].text = $"데미지: {jokbo.BaseDamage}\n점수: {jokbo.BaseScore}";
                attackOptionButtons[i].onClick.RemoveAllListeners();
                attackOptionButtons[i].onClick.AddListener(() => {
                    SelectAttack(jokbo);
                });
                attackOptionButtons[i].gameObject.SetActive(true);
            }
            else
            {
                attackOptionButtons[i].gameObject.SetActive(false);
            }
        }
    }
    
    private void SelectAttack(AttackJokbo chosenJokbo)
    {
        if (attackOptionsPanel != null)
        {
            attackOptionsPanel.SetActive(false);
        }
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ProcessAttack(chosenJokbo);
        }
    }

    // --- [!!! 핵심 수정 (상점)] ---

    /// <summary>
    /// [수정] GameManager가 호출. '진짜' 상점 아이템 리스트로 UI를 켭니다.
    /// </summary>
    public void ShowMaintenanceScreen(List<ShopItem> shopItems)
    {
        if (maintenancePanel == null)
        {
            Debug.LogError("Maintenance Panel이 UIManager에 연결되지 않았습니다!");
            return;
        }
        
        maintenancePanel.SetActive(true);
        
        // 1. GameManager로부터 'ShopItem' 리스트를 받아와서 버튼 설정
        for(int i=0; i < shopItemButtons.Length; i++)
        {
            // (아이템이 3개 미만일 수 있으므로)
            if (i < shopItems.Count)
            {
                ShopItem item = shopItems[i]; // (지역 변수로 캡처해야 함)
                Button button = shopItemButtons[i]; // (버튼도 캡처)

                // 2. 텍스트 설정
                if (shopItemNameTexts[i] != null)
                    shopItemNameTexts[i].text = item.ItemName;
                if (shopItemPriceTexts[i] != null)
                    shopItemPriceTexts[i].text = item.Price.ToString() + "점";
                
                // 3. 구매 가능 여부 확인 (점수)
                bool canAfford = (GameManager.Instance.CurrentScore >= item.Price);
                button.interactable = canAfford;

                // 4. 버튼 클릭 이벤트 연결
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    SelectShopItem(item, button); // (아이템과 버튼 객체 전달)
                });
                
                button.gameObject.SetActive(true);
            }
            else
            {
                // (아이템이 모자라면 남는 버튼 끄기)
                shopItemButtons[i].gameObject.SetActive(false);
            }
        }
        
        // 5. 상점 나가기 버튼 연결
        if (exitShopButton != null)
        {
            exitShopButton.onClick.RemoveAllListeners();
            exitShopButton.onClick.AddListener(CloseMaintenanceScreen);
        }
    }
    
    /// <summary>
    /// [수정] 상점 아이템을 클릭했을 때 ShopManager 호출
    /// </summary>
    private void SelectShopItem(ShopItem item, Button button)
    {
        if (ShopManager.Instance == null) return;
        
        ShopManager.Instance.BuyItem(item, button);
        
        // (구매 후 즉시 점수 UI 갱신)
        UpdateScore(GameManager.Instance.CurrentScore);
        
        // (TODO: 점수가 모자라게 된 다른 버튼들 비활성화 갱신)
    }

    /// <summary>
    /// 상점 나가기 버튼을 클릭했을 때 (변경 없음)
    /// </summary>
    private void CloseMaintenanceScreen()
    {
        if (maintenancePanel != null)
        {
            maintenancePanel.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndMaintenancePhase();
        }
    }
}

