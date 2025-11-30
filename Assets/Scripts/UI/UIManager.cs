using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;

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

    [Header("전환 연출")]
    public CanvasGroup fadeCanvasGroup;
    public CanvasGroup zoneTitleGroup;
    public TextMeshProUGUI zoneTitleText;

    [Header("게임 오버 UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI earnedCurrencyText;
    public Button mainMenuButton;
    public string mainMenuSceneName = "MainMenu";


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

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButton);
        }
    }

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
    public void UpdateHealth(int current, int max)
    {
        if (healthText != null) healthText.text = $"Health: {current} / {max}";
    }
    public void UpdateRollCount(int current, int max)
    {
        if (rollCountText != null) rollCountText.text = $"Roll: {current} / {max}";
    }

    public void FadeIn(float duration = 2.0f)
    {
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource != null)
        {
            // 원래 볼륨으로 복구
            SoundManager.Instance.bgmSource.DOFade(0.5f, duration);
        }
        fadeCanvasGroup.alpha = 1f; // 검은색 상태에서 시작
        fadeCanvasGroup.blocksRaycasts = true; // 터치 막기
        fadeCanvasGroup.DOFade(0f, duration).OnComplete(() =>
        {
            fadeCanvasGroup.blocksRaycasts = false; // 끝나면 터치 허용
        });
    }

    public void FadeOut(float duration = 2.0f, System.Action onComplete = null)
    {
        fadeCanvasGroup.blocksRaycasts = true;
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource != null)
        {
            SoundManager.Instance.bgmSource.DOFade(0f, duration);
        }
        fadeCanvasGroup.DOFade(1f, duration).OnComplete(() =>
        {
            onComplete?.Invoke(); // 다 어두워지면 실행할 함수 뭐 씬이동같은거
        });
    }

    public void ShowZoneTitle(string zoneName)
    {
        zoneTitleText.text = zoneName;

        Sequence seq = DOTween.Sequence();
        // 1. 텍스트 등장 
        zoneTitleGroup.alpha = 0;
        zoneTitleGroup.transform.localScale = Vector3.one * 1.2f;

        seq.Append(zoneTitleGroup.DOFade(1, 0.5f));
        seq.Join(zoneTitleGroup.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack));

        // 2. 잠시 대기
        seq.AppendInterval(1.0f);

        // 3. 사라짐
        seq.Append(zoneTitleGroup.DOFade(0, 0.5f));
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
            trigger.triggers.Clear();
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) =>
            {
                ShowEnemyDetail(enemyData, iconGO.GetComponent<RectTransform>());
            });
            trigger.triggers.Add(entryEnter);
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) =>
            {
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
            entryEnter.callback.AddListener((data) =>
            {
                ShowRelicDetail(relicData, iconGO.GetComponent<RectTransform>());
            });
            trigger.triggers.Add(entryEnter);
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) =>
            {
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
        float yOffset = (relicDetailPopup.GetComponent<RectTransform>().rect.height / 2 * relicDetailPopup.GetComponent<RectTransform>().lossyScale.y) + 5f;
        relicDetailPopup.transform.position -= new Vector3(0, yOffset, 0);
    }
    public void HideRelicDetail()
    {
        if (relicDetailPopup != null)
        {
            relicDetailPopup.SetActive(false);
        }
    }


    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// 1. '원본 족보' 리스트(jokbos)를 받습니다.
    /// 2. 텍스트를 채울 때, StageManager.ShowAttackPreview()를 '임시'로 호출하여
    ///    '최종 계산된' 데미지/점수를 가져와 텍스트에 표시합니다.
    /// </summary>
    public void ShowAttackOptions(List<AttackJokbo> jokbos)
    {
        if (attackOptionsPanel == null) return;
        attackOptionsPanel.SetActive(true);

        if (StageManager.Instance != null) StageManager.Instance.HideAllAttackPreviews();

        for (int i = 0; i < attackOptionButtons.Length; i++)
        {
            if (i < jokbos.Count)
            {
                AttackJokbo jokbo = jokbos[i]; // (이것은 '원본' 족보)

                // 1. [!!!] 텍스트 설정을 위해 '최종' 데미지/점수를 '미리' 계산합니다.
                // (StageManager의 ShowAttackPreview 로직을 일부 가져옴)
                (int finalBaseDamage, int finalBaseScore) =
                    StageManager.Instance.GetPreviewValues(jokbo); // [!!!] StageManager에 새 헬퍼 함수 요청

                // 2. '최종 계산된' 값으로 텍스트 설정
                attackNameTexts[i].text = jokbo.Description;
                attackValueTexts[i].text = $"Dmg: {finalBaseDamage} / Score: {finalBaseScore}";

                // 3. EventTrigger 가져오기
                EventTrigger trigger = attackOptionButtons[i].GetComponent<EventTrigger>();
                if (trigger == null) trigger = attackOptionButtons[i].gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear();

                // 4. PointerEnter (마우스 올림) 이벤트 -> '원본' 족보 전달
                EventTrigger.Entry entryEnter = new EventTrigger.Entry();
                entryEnter.eventID = EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((data) =>
                {
                    if (StageManager.Instance != null) StageManager.Instance.ShowAttackPreview(jokbo);
                });
                trigger.triggers.Add(entryEnter);

                // 5. PointerExit (마우스 벗어남) 이벤트
                EventTrigger.Entry entryExit = new EventTrigger.Entry();
                entryExit.eventID = EventTriggerType.PointerExit;
                entryExit.callback.AddListener((data) =>
                {
                    if (StageManager.Instance != null) StageManager.Instance.HideAllAttackPreviews();
                });
                trigger.triggers.Add(entryExit);

                // 6. PointerClick (클릭) 이벤트 -> '원본' 족보 전달
                EventTrigger.Entry entryClick = new EventTrigger.Entry();
                entryClick.eventID = EventTriggerType.PointerClick;
                entryClick.callback.AddListener((data) =>
                {
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

    private void SelectAttack(AttackJokbo jokbo) // (원본 족보가 전달됨)
    {
        attackOptionsPanel.SetActive(false);
        if (StageManager.Instance != null)
        {
            StageManager.Instance.HideAllAttackPreviews();
            StageManager.Instance.ProcessAttack(jokbo); // 원본 족보로 공격 실행
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
                relicChoiceButtons[i].onClick.AddListener(() =>
                {
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
                shopItemButtons[i].onClick.AddListener(() =>
                {
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

    public void ShowGameOverScreen(int earnedCurrency)
    {
        if (gameOverPanel == null) return;
        if (attackOptionsPanel != null) attackOptionsPanel.SetActive(false);
        if (rewardScreenPanel != null) rewardScreenPanel.SetActive(false);
        if (maintenancePanel != null) maintenancePanel.SetActive(false);
        if (waveInfoPanel != null) waveInfoPanel.SetActive(false);
        if (enemyDetailPopup != null) enemyDetailPopup.SetActive(false);
        if (relicDetailPopup != null) relicDetailPopup.SetActive(false);
        gameOverPanel.SetActive(true);
        if (earnedCurrencyText != null)
        {
            earnedCurrencyText.text = $"획득한 영혼의 파편: {earnedCurrency}";
        }
    }

    private void OnMainMenuButton()
    {
        DG.Tweening.DOTween.KillAll();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}