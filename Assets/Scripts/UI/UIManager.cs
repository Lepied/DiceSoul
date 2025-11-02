using UnityEngine;
using TMPro; // TextMeshPro 사용
using System.Collections.Generic; // List 사용
using UnityEngine.UI; // Button 사용

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("기본 UI 텍스트 요소")]
    [Tooltip("현재 웨이브/존을 표시할 텍스트")]
    public TextMeshProUGUI waveText;
    [Tooltip("현재 총 점수(재화)를 표시할 텍스트")]
    public TextMeshProUGUI totalScoreText;
    [Tooltip("굴림 횟수를 표시할 텍스트 (예: 1/3)")]
    public TextMeshProUGUI rollCountText;
    [Tooltip("플레이어 체력을 표시할 텍스트 (예: 10/10)")]
    public TextMeshProUGUI healthText;

    [Header("보상 화면 UI (웨이브 클리어)")]
    [Tooltip("보상 선택 화면 전체 Panel")]
    public GameObject rewardScreenPanel;
    [Tooltip("유물 선택지 3개를 담을 버튼 배열")]
    public Button[] relicChoiceButtons; 
    [Tooltip("유물 버튼의 '이름' 텍스트 (TextMeshPro)")]
    public TextMeshProUGUI[] relicNameTexts;
    [Tooltip("유물 버튼의 '설명' 텍스트 (TextMeshPro)")]
    public TextMeshProUGUI[] relicDescriptionTexts;
    
    [Header("공격 선택 UI (굴림 이후)")]
    [Tooltip("공격 족보 선택 Panel")]
    public GameObject attackOptionsPanel;
    [Tooltip("공격 족보 버튼 배열")]
    public Button[] attackOptionButtons;
    [Tooltip("공격 족보 이름 텍스트 배열")]
    public TextMeshProUGUI[] attackNameTexts;
    [Tooltip("공격 족보 데미지/점수 텍스트 배열")]
    public TextMeshProUGUI[] attackValueTexts;

    [Header("정비 (상점) UI (존 클리어)")]
    [Tooltip("상점 화면 전체 Panel")]
    public GameObject maintenancePanel;
    [Tooltip("상점 아이템 버튼 배열 (예: 3개)")]
    public Button[] shopItemButtons;
    [Tooltip("상점 아이템 이름 텍스트 배열")]
    public TextMeshProUGUI[] shopItemNameTexts;
    [Tooltip("상점 아이템 가격 텍스트 배열")]
    public TextMeshProUGUI[] shopItemPriceTexts;
    [Tooltip("상점 나가기 버튼")]
    public Button exitShopButton;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Canvas는 씬과 함께 로드/언로드되는 것이 일반적
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 모든 패널을 끈 상태로 시작
        if (rewardScreenPanel != null) rewardScreenPanel.SetActive(false);
        if (attackOptionsPanel != null) attackOptionsPanel.SetActive(false);
        if (maintenancePanel != null) maintenancePanel.SetActive(false);

        // 상점 나가기 버튼 이벤트 연결
        if (exitShopButton != null)
        {
            exitShopButton.onClick.AddListener(OnExitShopButton);
        }
    }

    // --- 기본 UI 업데이트 ---
    public void UpdateWaveText(int zone, int wave)
    {
        if (waveText != null)
        {
            waveText.text = $"Zone {zone} - Wave {wave}";
        }
    }
    
    public void UpdateScore(int totalScore)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"Score: {totalScore}";
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
    
    public void ClearScoreText() // (점수 계산기용 - 현재 미사용)
    {
        // (이전 점수 시스템의 잔재)
    }

    // --- 1. 유물 보상 화면 (웨이브 클리어) ---
    public void ShowRewardScreen(List<Relic> relicOptions)
    {
        if (rewardScreenPanel == null) return;
        rewardScreenPanel.SetActive(true);

        for (int i = 0; i < relicChoiceButtons.Length; i++)
        {
            if (i < relicOptions.Count)
            {
                Relic relic = relicOptions[i];
                if (relicNameTexts[i] != null) relicNameTexts[i].text = relic.Name;
                if (relicDescriptionTexts[i] != null) relicDescriptionTexts[i].text = relic.Description;

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
        if (rewardScreenPanel != null) rewardScreenPanel.SetActive(false);
        
        // [!!! 오류 수정 3 !!!]
        // GameManager의 함수 이름이 OnRelicSelected -> AddRelic 으로 변경되었습니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddRelic(chosenRelic);
        }
    }

    // --- 2. 공격 선택 화면 (굴림 이후) ---
    public void ShowAttackOptions(List<AttackJokbo> jokbos)
    {
        if (attackOptionsPanel == null) return;
        attackOptionsPanel.SetActive(true);

        for (int i = 0; i < attackOptionButtons.Length; i++)
        {
            if (i < jokbos.Count)
            {
                AttackJokbo jokbo = jokbos[i];
                if (attackNameTexts[i] != null) attackNameTexts[i].text = jokbo.Description;
                if (attackValueTexts[i] != null) attackValueTexts[i].text = $"피해량: {jokbo.BaseDamage} / 점수: {jokbo.BaseScore}";

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
        if (attackOptionsPanel != null) attackOptionsPanel.SetActive(false);
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ProcessAttack(chosenJokbo);
        }
    }

    // --- 3. 정비 (상점) 화면 (존 클리어) ---

    /// <summary>
    /// [!!! 오류 수정 2 !!!]
    /// GameManager가 호출할 StartMaintenancePhase 함수를 새로 생성합니다.
    /// </summary>
    public void StartMaintenancePhase()
    {
        if (ShopManager.Instance == null)
        {
            Debug.LogError("ShopManager가 없습니다!");
            return;
        }
        
        // 1. ShopManager에게 아이템 리스트를 생성하라고 지시
        ShopManager.Instance.GenerateShopItems();
        
        // 2. 생성된 아이템 리스트로 상점 화면을 켬
        ShowMaintenanceScreen(ShopManager.Instance.currentShopItems);
    }
    
    public void ShowMaintenanceScreen(List<ShopItem> items)
    {
        if (maintenancePanel == null) return;
        maintenancePanel.SetActive(true);

        for (int i = 0; i < shopItemButtons.Length; i++)
        {
            if (i < items.Count)
            {
                ShopItem item = items[i];
                
                // [!!! 오류 수정 1 !!!]
                // item.ItemName -> item.Name (ShopItem.cs의 프로퍼티 이름)
                if (shopItemNameTexts[i] != null) shopItemNameTexts[i].text = item.Name;
                if (shopItemPriceTexts[i] != null) shopItemPriceTexts[i].text = $"{item.Price} 점수";

                shopItemButtons[i].onClick.RemoveAllListeners();
                shopItemButtons[i].onClick.AddListener(() => {
                    SelectShopItem(item);
                });
                shopItemButtons[i].gameObject.SetActive(true);
            }
            else
            {
                shopItemButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void SelectShopItem(ShopItem item)
    {
        if (ShopManager.Instance == null) return;
        
        // [!!! 오류 수정 1 !!!]
        // BuyItem은 인자를 1개(item)만 받습니다.
        ShopManager.Instance.BuyItem(item);
    }

    private void OnExitShopButton()
    {
        if (maintenancePanel != null) maintenancePanel.SetActive(false);
        
        // 상점이 닫혔으므로, StageManager에게 다음 웨이브(다음 존의 첫 웨이브)를 준비시킴
        if (StageManager.Instance != null)
        {
            StageManager.Instance.PrepareNextWave();
        }
    }

    /// <summary>
    /// [!!! 오류 수정 1 !!!]
    /// GameManager가 상점 상태를 확인할 수 있도록 IsShopOpen 함수를 새로 생성합니다.
    /// </summary>
    public bool IsShopOpen()
    {
        if (maintenancePanel == null) return false;
        return maintenancePanel.activeSelf;
    }
}

