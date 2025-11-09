using UnityEngine;
using TMPro; 
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq; 
using UnityEngine.EventSystems; 
using UnityEngine.SceneManagement; // [!!! 신규 추가 !!!] 씬 이동

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. '게임 오버 UI' 섹션 (gameOverPanel, earnedCurrencyText 등) 변수 추가
/// 2. 'ShowGameOverScreen' 함수 추가 (GameManager가 호출)
/// 3. 'OnMainMenuButton' 함수 추가 (메인 메뉴 씬 로드)
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("기본 UI 텍스트")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI rollCountText;

    [Header("공격 선택 UI")]
    public GameObject attackOptionsPanel;
    public GameObject[] attackOptionButtons; 
    public TextMeshProUGUI[] attackNameTexts;
    public TextMeshProUGUI[] attackValueTexts;

    [Header("보상/상점 UI")]
    public GameObject rewardScreenPanel;
    public Button[] relicChoiceButtons; 
    public TextMeshProUGUI[] relicNameTexts;
    public TextMeshProUGUI[] relicDescriptionTexts;
    public GameObject maintenancePanel;
    public Button[] shopItemButtons;
    public TextMeshProUGUI[] shopItemNameTexts;
    public TextMeshProUGUI[] shopItemPriceTexts;
    public Button exitShopButton;
    
    [Header("적 정보 UI")]
    public Button waveInfoToggleButton; 
    public GameObject waveInfoPanel;
    public GameObject enemyInfoIconPrefab;
    public GameObject enemyDetailPopup;
    public Image enemyDetailIcon; 
    public TextMeshProUGUI enemyDetailName;
    public TextMeshProUGUI enemyDetailHP;
    public TextMeshProUGUI enemyDetailType;
    public TextMeshProUGUI enemyDetailGimmick; 

    [Header("보유 유물 UI")]
    public GameObject relicPanel;
    public GameObject relicIconPrefab;
    public GameObject relicDetailPopup;
    public TextMeshProUGUI relicDetailName;
    public TextMeshProUGUI relicDetailDescription;

    // [!!! 신규 추가 !!!]
    [Header("게임 오버 UI")]
    [Tooltip("게임 오버 시 활성화될 패널")]
    public GameObject gameOverPanel;
    [Tooltip("획득한 영구 재화를 표시할 텍스트")]
    public TextMeshProUGUI earnedCurrencyText;
    [Tooltip("메인 메뉴 씬으로 돌아갈 버튼")]
    public Button mainMenuButton;
    [Tooltip("돌아갈 메인 메뉴 씬의 이름")]
    public string mainMenuSceneName = "MainMenu"; // (씬 이름이 다르면 수정)


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
        if (attackOptionsPanel != null) attackOptionsPanel.SetActive(false);
        if (rewardScreenPanel != null) rewardScreenPanel.SetActive(false);
        if (maintenancePanel != null) maintenancePanel.SetActive(false);
        if (waveInfoPanel != null) waveInfoPanel.SetActive(false);
        if (enemyDetailPopup != null) enemyDetailPopup.SetActive(false); 
        if (waveInfoToggleButton != null)
        {
            waveInfoToggleButton.onClick.AddListener(ToggleWaveInfoPanel);
        }
        
        if (relicPanel != null) relicPanel.SetActive(true); 
        if (relicDetailPopup != null) relicDetailPopup.SetActive(false); 
        
        // [신규] 게임 오버 UI 초기화
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButton);
        }
    }

    // ... (기본 UI 함수들은 동일) ...
    public void ToggleWaveInfoPanel() 
    {
        if (waveInfoPanel != null)
        {
            waveInfoPanel.SetActive(!waveInfoPanel.activeSelf);
            if (!waveInfoPanel.activeSelf)
            {
                HideEnemyDetail();
            }
        }
    }
    public void UpdateWaveText(int zone, int wave) 
    {
        if (waveText != null) waveText.text = $"Zone {zone} - Wave {wave}";
    }
    public void UpdateScore(int score) 
    {
        if (totalScoreText != null) totalScoreText.text = $"Score: {score}";
    }
    public void ClearScoreText() 
    { 
        if (totalScoreText != null) totalScoreText.text = "Score: 0";
    }
    public void UpdateHealth(int current, int max) 
    {
        if (healthText != null) healthText.text = $"Health: {current} / {max}";
    }
    public void UpdateRollCount(int current, int max) 
    {
        if (rollCountText != null) rollCountText.text = $"Roll: {current} / {max}";
    }

    // ... (적 정보 UI 함수들은 동일) ...
    public void UpdateWaveInfoPanel(List<Enemy> activeEnemies) 
    {
        if (waveInfoPanel == null || enemyInfoIconPrefab == null) return;
        foreach (Transform child in waveInfoPanel.transform)
        {
            Destroy(child.gameObject);
        }
        var enemyGroups = activeEnemies.Where(e => e != null)
                                       .GroupBy(e => e.enemyName)
                                       .OrderBy(g => g.First().isBoss); 
        foreach (var group in enemyGroups)
        {
            Enemy enemyData = group.First(); 
            int count = group.Count(); 
            GameObject iconGO = Instantiate(enemyInfoIconPrefab, waveInfoPanel.transform);
            Image faceImage = iconGO.GetComponentInChildren<Image>();
            SpriteRenderer enemySprite = enemyData.GetComponent<SpriteRenderer>();
            if (faceImage != null && enemySprite != null)
            {
                faceImage.sprite = enemySprite.sprite; 
            }
            TextMeshProUGUI countText = iconGO.GetComponentInChildren<TextMeshProUGUI>();
            if (countText != null)
            {
                countText.text = (count > 1) ? $"x{count}" : ""; 
            }
            EventTrigger trigger = iconGO.GetComponent<EventTrigger>();
            if (trigger == null) trigger = iconGO.AddComponent<EventTrigger>(); 
            trigger.triggers.Clear();
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { 
                ShowEnemyDetail(enemyData, iconGO.GetComponent<RectTransform>()); 
            });
            trigger.triggers.Add(entryEnter);
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { 
                HideEnemyDetail(); 
            });
            trigger.triggers.Add(entryExit);
        }
    }
    public void ShowEnemyDetail(Enemy enemy, RectTransform iconRect) 
    {
        if (enemyDetailPopup == null) return;
        enemyDetailPopup.SetActive(true);
        if (enemyDetailIcon != null)
        {
            SpriteRenderer enemySprite = enemy.GetComponent<SpriteRenderer>();
            if (enemySprite != null) enemyDetailIcon.sprite = enemySprite.sprite; 
        }
        if (enemyDetailName != null) enemyDetailName.text = enemy.enemyName + (enemy.isBoss ? " (Boss)" : "");
        if (enemyDetailHP != null) enemyDetailHP.text = $"HP: {enemy.maxHP}";
        if (enemyDetailType != null)
        {
            enemyDetailType.text = $"타입: {enemy.enemyType.ToString()}";
        }
        if (enemyDetailGimmick != null)
        {
            enemyDetailGimmick.text = enemy.GetGimmickDescription();
        }
        
        enemyDetailPopup.transform.position = iconRect.transform.position;
        float offset = (iconRect.rect.width * iconRect.lossyScale.x / 2) + (enemyDetailPopup.GetComponent<RectTransform>().rect.width * enemyDetailPopup.GetComponent<RectTransform>().lossyScale.x / 2) + 10f;
        enemyDetailPopup.transform.position += new Vector3(offset, 0, 0);
    }
    public void HideEnemyDetail() 
    {
        if (enemyDetailPopup != null)
        {
            enemyDetailPopup.SetActive(false);
        }
    }

    // ... (보유 유물 UI 함수들은 동일) ...
    public void UpdateRelicPanel(List<Relic> playerRelics)
    {
        if (relicPanel == null || relicIconPrefab == null) return;

        foreach (Transform child in relicPanel.transform)
        {
            Destroy(child.gameObject);
        }

        var relicGroups = playerRelics.GroupBy(r => r.RelicID);

        foreach (var group in relicGroups)
        {
            Relic relicData = group.First(); 
            int count = group.Count(); 

            GameObject iconGO = Instantiate(relicIconPrefab, relicPanel.transform);
            
            Image relicImage = iconGO.GetComponent<Image>();
            if (relicImage != null)
            {
                relicImage.sprite = relicData.Icon;
            }

            TextMeshProUGUI countText = iconGO.GetComponentInChildren<TextMeshProUGUI>();
            if (countText != null)
            {
                countText.text = (count > 1) ? $"x{count}" : ""; 
            }

            EventTrigger trigger = iconGO.GetComponent<EventTrigger>();
            if (trigger == null) trigger = iconGO.AddComponent<EventTrigger>(); 
            trigger.triggers.Clear();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { 
                ShowRelicDetail(relicData, iconGO.GetComponent<RectTransform>()); 
            });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { 
                HideRelicDetail(); 
            });
            trigger.triggers.Add(entryExit);
        }
    }
    public void ShowRelicDetail(Relic relic, RectTransform iconRect)
    {
        if (relicDetailPopup == null) return;

        relicDetailPopup.SetActive(true);
        
        if (relicDetailName != null) relicDetailName.text = relic.Name;
        if (relicDetailDescription != null) relicDetailDescription.text = relic.Description;

        Vector3 iconBottomPosition = iconRect.transform.position - new Vector3(0, iconRect.rect.height / 2 * iconRect.lossyScale.y, 0);
        
        relicDetailPopup.transform.position = iconBottomPosition;
    }
    public void HideRelicDetail()
    {
        if (relicDetailPopup != null)
        {
            relicDetailPopup.SetActive(false);
        }
    }

    // ... (공격/보상/상점 함수들은 동일) ...
    public void ShowAttackOptions(List<AttackJokbo> jokbos) 
    {
        if (attackOptionsPanel == null) return;
        attackOptionsPanel.SetActive(true);
        for (int i = 0; i < attackOptionButtons.Length; i++)
        {
            if (i < jokbos.Count)
            {
                AttackJokbo jokbo = jokbos[i]; 
                attackNameTexts[i].text = jokbo.Description;
                attackValueTexts[i].text = $"Dmg: {jokbo.BaseDamage} / Score: {jokbo.BaseScore}";
                EventTrigger trigger = attackOptionButtons[i].GetComponent<EventTrigger>();
                if (trigger == null) trigger = attackOptionButtons[i].gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear(); 
                EventTrigger.Entry entryEnter = new EventTrigger.Entry();
                entryEnter.eventID = EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((data) => { 
                    if(StageManager.Instance != null) StageManager.Instance.ShowAttackPreview(jokbo); 
                });
                trigger.triggers.Add(entryEnter);
                EventTrigger.Entry entryExit = new EventTrigger.Entry();
                entryExit.eventID = EventTriggerType.PointerExit;
                entryExit.callback.AddListener((data) => { 
                    if(StageManager.Instance != null) StageManager.Instance.HideAllAttackPreviews(); 
                });
                trigger.triggers.Add(entryExit);
                EventTrigger.Entry entryClick = new EventTrigger.Entry();
                entryClick.eventID = EventTriggerType.PointerClick;
                entryClick.callback.AddListener((data) => { 
                    SelectAttack(jokbo); 
                });
                trigger.triggers.Add(entryClick);
                attackOptionButtons[i].gameObject.SetActive(true);
            }
            else
            {
                attackOptionButtons[i].gameObject.SetActive(false);
            }
        }
    }
    private void SelectAttack(AttackJokbo jokbo) 
    {
        attackOptionsPanel.SetActive(false);
        if(StageManager.Instance != null)
        {
            StageManager.Instance.HideAllAttackPreviews(); 
            StageManager.Instance.ProcessAttack(jokbo); 
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
                relicNameTexts[i].text = relic.Name;
                relicDescriptionTexts[i].text = relic.Description;
                relicChoiceButtons[i].onClick.RemoveAllListeners();
                relicChoiceButtons[i].onClick.AddListener(() => {
                    GameManager.Instance.AddRelic(relic);
                    rewardScreenPanel.SetActive(false);
                });
                relicChoiceButtons[i].gameObject.SetActive(true);
            }
            else
            {
                relicChoiceButtons[i].gameObject.SetActive(false);
            }
        }
    }
    public void StartMaintenancePhase() 
    {
        if (ShopManager.Instance == null) return;
        ShopManager.Instance.GenerateShopItems();
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
                shopItemNameTexts[i].text = item.Name;
                shopItemPriceTexts[i].text = $"{item.Price} Score";
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
        exitShopButton.onClick.RemoveAllListeners();
        exitShopButton.onClick.AddListener(ExitShop);
    }
    private void SelectShopItem(ShopItem item) 
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.BuyItem(item);
        }
    }
    private void ExitShop() 
    {
        maintenancePanel.SetActive(false);
        if (StageManager.Instance != null)
        {
            StageManager.Instance.PrepareNextWave();
        }
    }
    public bool IsShopOpen() 
    {
        return maintenancePanel != null && maintenancePanel.activeSelf;
    }


    // [!!! 신규 추가 !!!]
    /// <summary>
    /// (GameManager가 호출) 게임 오버 패널을 띄웁니다.
    /// </summary>
    public void ShowGameOverScreen(int earnedCurrency)
    {
        if (gameOverPanel == null) return;

        // 다른 모든 UI 패널 숨기기
        if (attackOptionsPanel != null) attackOptionsPanel.SetActive(false);
        if (rewardScreenPanel != null) rewardScreenPanel.SetActive(false);
        if (maintenancePanel != null) maintenancePanel.SetActive(false);
        if (waveInfoPanel != null) waveInfoPanel.SetActive(false);
        if (enemyDetailPopup != null) enemyDetailPopup.SetActive(false);
        if (relicDetailPopup != null) relicDetailPopup.SetActive(false);

        // 게임 오버 패널 켜기
        gameOverPanel.SetActive(true);

        // 획득 재화 텍스트 설정
        if (earnedCurrencyText != null)
        {
            earnedCurrencyText.text = $"획득한 영혼의 파편: {earnedCurrency}";
        }
    }

    /// <summary>
    /// (메인 메뉴 버튼 클릭 시) 메인 메뉴 씬을 로드합니다.
    /// </summary>
    private void OnMainMenuButton()
    {
        // (참고) 씬을 로드하기 전에 DOTween 트윈을 모두 멈추는 것이 좋습니다.
        DG.Tweening.DOTween.KillAll();
        
        SceneManager.LoadScene(mainMenuSceneName);
    }
}