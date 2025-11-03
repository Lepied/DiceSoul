using UnityEngine;
using TMPro; 
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq; // GroupBy 사용
using UnityEngine.EventSystems; // EventTrigger 사용

/// <summary>
/// [수정] 
/// 1. 'enemyDetailType' (TextMeshProUGUI) 변수 추가
/// 2. 'ShowEnemyDetail' 함수가 'enemyDetailType' 텍스트를 적의 타입으로 설정
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
    public Button[] attackOptionButtons; 
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
    
    [Tooltip("적의 타입을 표시할 텍스트 (예: '타입: Undead')")]
    public TextMeshProUGUI enemyDetailType; // [!!! 신규 추가 !!!]
    
    public TextMeshProUGUI enemyDetailGimmick; 
    
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
    }

    public void ToggleWaveInfoPanel()
    {
        if (waveInfoPanel != null)
        {
            waveInfoPanel.SetActive(!waveInfoPanel.activeSelf);
        }
    }

    public void UpdateWaveText(int zone, int wave)
    {
        if (waveText != null)
        {
            waveText.text = $"Zone {zone} - Wave {wave}";
        }
    }
    public void UpdateScore(int score)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = $"Score: {score}";
        }
    }
    public void ClearScoreText() { }
    public void UpdateHealth(int current, int max)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {current} / {max}";
        }
    }
    public void UpdateRollCount(int current, int max)
    {
        if (rollCountText != null)
        {
            rollCountText.text = $"Roll: {current} / {max}";
        }
    }


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

    /// <summary>
    /// [수정] 적 상세정보 팝업에 '타입' 정보를 추가합니다.
    /// </summary>
    public void ShowEnemyDetail(Enemy enemy, RectTransform iconRect)
    {
        if (enemyDetailPopup == null) return;

        // 1. 팝업 UI에 적 정보 채우기
        if (enemyDetailIcon != null)
        {
            SpriteRenderer enemySprite = enemy.GetComponent<SpriteRenderer>();
            if (enemySprite != null) enemyDetailIcon.sprite = enemySprite.sprite; 
        }
        if (enemyDetailName != null) enemyDetailName.text = enemy.enemyName + (enemy.isBoss ? " (Boss)" : "");
        if (enemyDetailHP != null) enemyDetailHP.text = $"HP: {enemy.maxHP}";
        
        // [!!! 신규 추가 !!!]
        if (enemyDetailType != null)
        {
            // (EnemyType.Biological -> "Biological" 문자열로 변환)
            enemyDetailType.text = $"타입: {enemy.enemyType.ToString()}";
        }

        if (enemyDetailGimmick != null)
        {
            enemyDetailGimmick.text = enemy.GetGimmickDescription();
        }

        // 2. 팝업 위치를 아이콘의 '월드 위치'로 이동
        enemyDetailPopup.transform.position = iconRect.transform.position;
        
        float offset = (iconRect.rect.width / 2) + (enemyDetailPopup.GetComponent<RectTransform>().rect.width / 2) + 10f;
        enemyDetailPopup.transform.position += new Vector3(offset, 0, 0);

        // 3. 팝업 켜기
        enemyDetailPopup.SetActive(true);
    }

    public void HideEnemyDetail()
    {
        if (enemyDetailPopup != null)
        {
            enemyDetailPopup.SetActive(false);
        }
    }

    // --- (이하 공격/보상/상점 함수들은 변경점 없음) ---
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
    private void SelectAttack(AttackJokbo jokbo)
    {
        attackOptionsPanel.SetActive(false);
        if (StageManager.Instance != null)
        {
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
}

