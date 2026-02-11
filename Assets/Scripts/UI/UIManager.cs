using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("기본 UI 텍스트")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI totalGoldText;
    public TextMeshProUGUI healthText;

    [Header("공격 선택 UI")]
    public GameObject attackOptionsPanel;
    public CanvasGroup attackOptionsPanelCanvasGroup;
    public Button toggleAttackOptionsButton;
    public Sprite arrowDownSprite;
    public Sprite arrowUpSprite;
    public GameObject[] attackOptionButtons;
    public TextMeshProUGUI[] attackNameTexts;
    public TextMeshProUGUI[] attackValueTexts;
    public Button prevJokboPageButton;
    public Button nextJokboPageButton;
    private List<AttackJokbo> currentJokboList = new List<AttackJokbo>();
    private int currentJokboPage = 0;
    private const int jokbosPerPage = 4;

    [Header("탄겟 선택 UI")]
    public GameObject targetSelectionPanel;
    public TextMeshProUGUI targetSelectionText;
    public Button confirmTargetButton;
    public Button cancelTargetButton;

    [Header("보상 UI")]
    public GameObject rewardScreenPanel;
    public Button[] relicChoiceButtons;
    public TextMeshProUGUI[] relicNameTexts;
    public TextMeshProUGUI[] relicDescriptionTexts;
    public Image[] relicIconImages;

    [Header("상점/정비 UI")]
    public GameObject maintenancePanel;
    public Transform shopItemsContainer;
    public GameObject shopItemSlotPrefab;
    public Button exitShopButton;

    public Button rerollButton;
    public TextMeshProUGUI rerollCostText;

    [Header("통합 툴팁")]
    public GameObject genericTooltipPopup;
    public TextMeshProUGUI tooltipTitle;
    public TextMeshProUGUI tooltipDesc;

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

    [Header("주사위 인벤토리 UI")]
    public Transform diceInventoryContainer;
    public GameObject diceInventoryItemPrefab;

    [Header("인게임 UI")]
    public GameObject rollPanel;
    public GameObject infoButton;

    [Header("전환 연출")]
    public CanvasGroup fadeCanvasGroup;
    public CanvasGroup zoneTitleGroup;
    public TextMeshProUGUI zoneTitleText;

    [Header("설정 패널")]
    public SettingsPanelController settingsPanelController;

    // Zone 연출중인지
    private bool isZoneTitlePlaying = false;


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
        if (genericTooltipPopup != null) genericTooltipPopup.SetActive(false);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);

        if (waveInfoToggleButton != null)
        {
            waveInfoToggleButton.onClick.AddListener(ToggleWaveInfoPanel);
        }
        
        if (toggleAttackOptionsButton != null)
        {
            toggleAttackOptionsButton.onClick.AddListener(ToggleAttackOptionsPanel);
        }
        
        if (prevJokboPageButton != null)
        {
            prevJokboPageButton.onClick.AddListener(ShowPrevJokboPage);
        }
        if (nextJokboPageButton != null)
        {
            nextJokboPageButton.onClick.AddListener(ShowNextJokboPage);
        }
        
        // CanvasGroup 넣기
        if (attackOptionsPanel != null && attackOptionsPanelCanvasGroup == null)
        {
            attackOptionsPanelCanvasGroup = attackOptionsPanel.GetComponent<CanvasGroup>();
            if (attackOptionsPanelCanvasGroup == null)
            {
                attackOptionsPanelCanvasGroup = attackOptionsPanel.AddComponent<CanvasGroup>();
            }
        }

        if (relicPanel != null) relicPanel.SetActive(true);
        if (relicDetailPopup != null) relicDetailPopup.SetActive(false);

        if (rerollButton != null) rerollButton.onClick.AddListener(OnRerollClick);
        if (exitShopButton != null)
        {
            exitShopButton.onClick.RemoveAllListeners();
            exitShopButton.onClick.AddListener(ExitShop);
        }

        // 타겟 선택 버튼 연결
        if (confirmTargetButton != null)
        {
            confirmTargetButton.onClick.AddListener(OnConfirmTargetButton);
        }
        if (cancelTargetButton != null)
        {
            cancelTargetButton.onClick.AddListener(OnCancelTargetButton);
        }
    }

    void Update()
    {
        // ESC 키로 설정 패널 열기/닫기
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !isZoneTitlePlaying)
        {
            if (settingsPanelController != null)
            {
                if (settingsPanelController.IsOpen())
                {
                    // 확인 팝업이 떠있지 않을 때만 닫기
                    if (!settingsPanelController.IsConfirmPopupActive())
                    {
                        settingsPanelController.CloseSettings();
                    }
                }
                else
                {
                    // 설정 패널이 닫혀있으면 열기
                    settingsPanelController.OpenSettings();
                }
            }
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
        if (waveText != null)
        {
            string zoneLabel = LocalizationManager.Instance.GetText("INGAME_ZONE");
            string waveLabel = LocalizationManager.Instance.GetText("INGAME_WAVE");
            waveText.text = $"{zoneLabel} {zone} - {waveLabel} {wave}";
        }
        UpdateDiceInventoryUI();
    }
    public void UpdateGold(int gold)
    {
        if (totalGoldText != null)
        {
            string goldLabel = LocalizationManager.Instance.GetText("INGAME_GOLD");
            totalGoldText.text = $"{goldLabel}: {gold}";
        }
    }
    public void UpdateHealth(int current, int max)
    {
        int shield = GameManager.Instance != null ? GameManager.Instance.CurrentShield : 0;
        if (healthText != null)
        {
            string hpLabel = LocalizationManager.Instance.GetText("INGAME_HP");
            if (shield > 0)
            {
                string shieldPrefix = LocalizationManager.Instance.GetText("INGAME_SHIELD_PREFIX");
                string shieldSuffix = LocalizationManager.Instance.GetText("INGAME_SHIELD_SUFFIX");
                healthText.text = $"{hpLabel}: {current} / {max} {shieldPrefix}{shield}{shieldSuffix}";
            }
            else
            {
                healthText.text = $"{hpLabel}: {current} / {max}";
            }
        }
    }

    // 주사위 덱의 타입별 개수 계산
    private Dictionary<string, int> GetDiceDeckCounts()
    {
        var counts = new Dictionary<string, int>();
        
        if (GameManager.Instance == null || GameManager.Instance.playerDiceDeck == null)
            return counts;
        
        foreach (string diceType in GameManager.Instance.playerDiceDeck)
        {
            if (counts.ContainsKey(diceType))
                counts[diceType]++;
            else
                counts[diceType] = 1;
        }
        
        return counts;
    }

    // 주사위 인벤토리 UI 업데이트
    public void UpdateDiceInventoryUI()
    {
        if (diceInventoryContainer == null || diceInventoryItemPrefab == null)
            return;
        foreach (Transform child in diceInventoryContainer)
        {
            Destroy(child.gameObject);
        }
        
        var diceCounts = GetDiceDeckCounts();
        
        var sortedCounts = diceCounts.OrderBy(kvp => 
        {
            string type = kvp.Key;
            if (type.StartsWith("D") && int.TryParse(type.Substring(1), out int sides))
                return sides;
            return 0;
        });
        
        foreach (var kvp in sortedCounts)
        {
            string diceType = kvp.Key;
            int count = kvp.Value;
            
            GameObject item = Instantiate(diceInventoryItemPrefab, diceInventoryContainer);
            
            // Icon 설정 (서브 스프라이트 로드하기)
            Transform iconTransform = item.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();
                if (icon != null)
                {
                    Sprite[] sprites = Resources.LoadAll<Sprite>("DiceIcons/white_dice_icons");
                    Sprite iconSprite = System.Array.Find(sprites, s => s.name == $"{diceType}_Icon");
                    
                    icon.sprite = iconSprite;
                }
            }
            
            Transform countTextTransform = item.transform.Find("CountText");
            if (countTextTransform != null)
            {
            TextMeshProUGUI countText = countTextTransform.GetComponent<TextMeshProUGUI>();
                if (countText != null)
                {
                    countText.text = $"x{count}";
                }
            }
        }
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
        isZoneTitlePlaying = true;

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
        
        // 4. 연출 완료
        seq.OnComplete(() => {
            isZoneTitlePlaying = false;
        });
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
        
        if (enemyDetailName != null)
        {
            string bossSuffix = enemy.isBoss ? " " + LocalizationManager.Instance.GetText("INGAME_BOSS_SUFFIX") : "";
            enemyDetailName.text = enemy.GetLocalizedName() + bossSuffix;
        }
        
        if (enemyDetailHP != null)
        {
            string hpLabel = LocalizationManager.Instance.GetText("INGAME_HP");
            string atkLabel = LocalizationManager.Instance.GetText("INGAME_ATK");
            enemyDetailHP.text = $"{hpLabel}: {enemy.maxHP} / {atkLabel}: {enemy.attackDamage}";
        }
        
        if (enemyDetailType != null)
        {
            string typeLabel = LocalizationManager.Instance.GetText("INGAME_ENEMY_TYPE");
            enemyDetailType.text = $"{typeLabel}: {enemy.enemyType.ToString()}";
        }
        
        if (enemyDetailGimmick != null)
        {
            enemyDetailGimmick.text = enemy.GetGimmickDescription();
        }

        enemyDetailPopup.transform.position = iconRect.transform.position;
        float offset = iconRect.rect.width * iconRect.lossyScale.x + 40f;
        enemyDetailPopup.transform.position += new Vector3(offset, -100f, 0);
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
            
            // 수동 유물인지 확인
            bool isManualRelic = IsManualRelic(relicData.RelicID);
            
            // EventTrigger 설정 (마우스 오버 툴팁)
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
            
            // 수동 유물이면 클릭 이벤트 추가
            if (isManualRelic)
            {
                Button relicButton = iconGO.GetComponent<Button>();
                if (relicButton == null) relicButton = iconGO.AddComponent<Button>();
                
                // 클릭 이벤트 연결
                relicButton.onClick.RemoveAllListeners();
                string relicID = relicData.RelicID; // 람다 캡처용
                relicButton.onClick.AddListener(() => OnManualRelicClicked(relicID));
                
                // 사용 가능 여부에 따라 시각 효과 업데이트
                UpdateManualRelicVisual(iconGO, relicID);
            }
        }
    }
    
    // 수동 유물 여부 확인
    private bool IsManualRelic(string relicID)
    {
        return relicID == "RLC_DOUBLE_DICE" || relicID == "RLC_FATE_DICE";
    }
    
    // 수동 유물 클릭 핸들러
    private void OnManualRelicClicked(string relicID)
    {
        if (RelicEffectHandler.Instance == null)
        {
            Debug.LogWarning("[UIManager] RelicEffectHandler가 없습니다.");
            return;
        }
        
        switch (relicID)
        {
            case "RLC_DOUBLE_DICE":
                if (RelicEffectHandler.Instance.CanUseDoubleDice())
                {
                    // 주사위 선택 모드 시작
                    StartDoubleDiceSelection();
                }
                else
                {
                    Debug.Log("[유물] 이중 주사위: 이번 웨이브에서 이미 사용했습니다.");
                }
                break;
                
            case "RLC_FATE_DICE":
                if (RelicEffectHandler.Instance.CanUseFateDice())
                {
                    // 운명의 주사위 즉시 사용
                    UseFateDice();
                }
                else
                {
                    Debug.Log("[유물] 운명의 주사위: 이번 런에서 이미 사용했습니다.");
                }
                break;
                

        }
    }
    
    // 이중 주사위 선택 모드 시작
    private void StartDoubleDiceSelection()
    {
        Debug.Log("[UI] 이중 주사위 사용 - 2배로 만들 주사위를 클릭하세요");
        
        // DiceController에 선택 모드 활성화 신호 보내기
        if (DiceController.Instance != null)
        {
            DiceController.Instance.StartDoubleDiceSelectionMode();
        }
    }
    
    // 운명의 주사위 사용
    private void UseFateDice()
    {
        if (DiceController.Instance == null)
        {
            Debug.LogWarning("[UIManager] DiceController가 없습니다.");
            return;
        }
        
        // 현재 주사위 값 가져오기
        List<int> currentValues = DiceController.Instance.currentValues;
        List<string> diceTypes = DiceController.Instance.GetDiceTypes();
        
        if (currentValues.Count == 0)
        {
            return;
        }
        
        // 배열로 변환
        int[] values = currentValues.ToArray();
        string[] types = diceTypes.ToArray();
        
        // 운명의 주사위 사용
        bool success = RelicEffectHandler.Instance.UseFateDice(values, types);
        
        if (success)
        {
            // DiceController에 변경된 값 적용
            DiceController.Instance.ApplyFateDiceValues(values);
            
            // 유물 패널 업데이트 (회색 처리)
            if (GameManager.Instance != null)
            {
                UpdateRelicPanel(GameManager.Instance.activeRelics);
            }
        }
    }
    
    // 수동 유물 시각 효과 업데이트
    private void UpdateManualRelicVisual(GameObject iconGO, string relicID)
    {
        if (RelicEffectHandler.Instance == null) return;
        
        Image relicImage = iconGO.GetComponent<Image>();
        if (relicImage == null) return;
        
        bool canUse = false;
        
        switch (relicID)
        {
            case "RLC_DOUBLE_DICE":
                canUse = RelicEffectHandler.Instance.CanUseDoubleDice();
                break;
            case "RLC_FATE_DICE":
                canUse = RelicEffectHandler.Instance.CanUseFateDice();
                break;
        }
        
        // 사용 가능하면 밝게, 불가능하면 어둡게
        Color color = canUse ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        relicImage.color = color;
        
        // 버튼 활성화 여부도 설정
        Button button = iconGO.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = canUse;
        }
    }
    
    public void ShowRelicDetail(Relic relic, RectTransform iconRect)
    {
        if (relicDetailPopup == null) return;
        relicDetailPopup.SetActive(true);
        if (relicDetailName != null) relicDetailName.text = relic.GetLocalizedName();
        if (relicDetailDescription != null) relicDetailDescription.text = relic.GetLocalizedDescription();
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

    public void ShowAttackOptions(List<AttackJokbo> jokbos)
    {
        if (attackOptionsPanel == null) return;
        attackOptionsPanel.SetActive(true);
        
        // 족보 리스트 정렬 수비는 맨뒤로 보내기
        currentJokboList = jokbos.OrderBy(j => j.TargetType == AttackTargetType.Defense ? 1 : 0).ToList();
        currentJokboPage = 0;
        
        RectTransform panelRect = attackOptionsPanel.GetComponent<RectTransform>();
        
        if (attackOptionsPanelCanvasGroup != null)
        {
            attackOptionsPanelCanvasGroup.alpha = 1f;
        }
        
        // 토글 버튼 활성화 및 패널 하단에 딱 붙여서 배치
        if (toggleAttackOptionsButton != null)
        {
            toggleAttackOptionsButton.gameObject.SetActive(true);
            RectTransform buttonRect = toggleAttackOptionsButton.GetComponent<RectTransform>();
            if (panelRect != null && buttonRect != null)
            {
                // 패널 하단 위치 = 패널의 현재 y위치 - 패널 높이
                float panelBottom = panelRect.anchoredPosition.y - panelRect.sizeDelta.y;
                buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, panelBottom);
            }
        }
        
        // 패널 펼침 상태로 초기화
        isAttackOptionsPanelOpen = true;
        UpdateToggleButtonArrow();

        if (StageManager.Instance != null) StageManager.Instance.HideAllAttackPreviews();
        
        // 첫 페이지 표시
        ShowJokboPage(currentJokboPage);
    }
    
    private void ShowJokboPage(int pageIndex)
    {
        int totalPages = Mathf.CeilToInt((float)currentJokboList.Count / jokbosPerPage);
        int startIndex = pageIndex * jokbosPerPage;
        int endIndex = Mathf.Min(startIndex + jokbosPerPage, currentJokboList.Count);
        
        // 페이지 버튼 표시/숨김 (이동 가능할 때만 표시)
        if (prevJokboPageButton != null)
        {
            prevJokboPageButton.gameObject.SetActive(pageIndex > 0);
        }
        if (nextJokboPageButton != null)
        {
            nextJokboPageButton.gameObject.SetActive(pageIndex < totalPages - 1);
        }
        
        // 현재 페이지의 족보들 표시
        for (int i = 0; i < attackOptionButtons.Length; i++)
        {
            int jokboIndex = startIndex + i;
            
            if (jokboIndex < endIndex)
            {
                AttackJokbo jokbo = currentJokboList[jokboIndex];

                // 1. 텍스트 설정을 위해 '최종' 데미지/금화를 '미리' 계산
                (int finalBaseDamage, int finalBaseGold) =
                    StageManager.Instance.GetPreviewValues(jokbo);

                // 2. 최종 계산된 값으로 텍스트 설정
                attackNameTexts[i].text = jokbo.GetLocalizedDescription();
                if (jokbo.TargetType == AttackTargetType.Defense)
                {
                    string shieldLabel = LocalizationManager.Instance.GetText("INGAME_SHIELD");
                    attackValueTexts[i].text = $"{shieldLabel}: {finalBaseDamage}";
                }
                else
                {
                    string dmgLabel = LocalizationManager.Instance.GetText("INGAME_DMG");
                    string goldLabel = LocalizationManager.Instance.GetText("INGAME_GOLD");
                    attackValueTexts[i].text = $"{dmgLabel}: {finalBaseDamage}\n{goldLabel}: {finalBaseGold}";
                }

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
    private void ShowPrevJokboPage()
    {
        if (currentJokboPage > 0)
        {
            currentJokboPage--;
            ShowJokboPage(currentJokboPage);
        }
    }
    
    private void ShowNextJokboPage()
    {
        int totalPages = Mathf.CeilToInt((float)currentJokboList.Count / jokbosPerPage);
        if (currentJokboPage < totalPages - 1)
        {
            currentJokboPage++;
            ShowJokboPage(currentJokboPage);
        }
    }

    private void SelectAttack(AttackJokbo jokbo) // (원본 족보가 전달됨)
    {
        attackOptionsPanel.SetActive(false);
        
        // 토글 버튼도 숨김
        if (toggleAttackOptionsButton != null)
        {
            toggleAttackOptionsButton.gameObject.SetActive(false);
        }
        
        if (StageManager.Instance != null)
        {
            StageManager.Instance.HideAllAttackPreviews();
            StageManager.Instance.ProcessAttack(jokbo); // 원본 족보로 공격 실행
        }
    }
    
    // 족보 UI 토글
    private bool isAttackOptionsPanelOpen = true;
    
    public void ToggleAttackOptionsPanel()
    {
        if (attackOptionsPanel == null || attackOptionsPanelCanvasGroup == null) return;
        if (toggleAttackOptionsButton == null) return;
        
        isAttackOptionsPanelOpen = !isAttackOptionsPanelOpen;
        
        RectTransform panelRect = attackOptionsPanel.GetComponent<RectTransform>();
        RectTransform buttonRect = toggleAttackOptionsButton.GetComponent<RectTransform>();
        if (panelRect == null || buttonRect == null) return;
        
        Sequence toggleSeq = DOTween.Sequence();
        
        if (isAttackOptionsPanelOpen)
        {
            // 펼치기: 위에서 아래로 슬라이드 + 페이드인
            attackOptionsPanel.SetActive(true);
            
            // 시작 위치 설정
            float hideYPos = panelRect.sizeDelta.y + 40;
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, hideYPos);
            attackOptionsPanelCanvasGroup.alpha = 0;
            float buttonHideY = hideYPos - panelRect.sizeDelta.y;
            buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, buttonHideY);
            
            // 패널 애니메이션
            toggleSeq.Append(panelRect.DOAnchorPosY(0, 0.2f).SetEase(Ease.OutQuad));
            toggleSeq.Join(attackOptionsPanelCanvasGroup.DOFade(1f, 0.2f));
            float panelBottomWhenOpen = 0 - panelRect.sizeDelta.y;
            toggleSeq.Join(buttonRect.DOAnchorPosY(panelBottomWhenOpen, 0.2f).SetEase(Ease.OutQuad));
        }
        else
        {
            // 숨기기: 위로 슬라이드 + 페이드아웃
            float hideYPos = panelRect.sizeDelta.y + 40;
            
            // 패널 애니메이션
            toggleSeq.Append(panelRect.DOAnchorPosY(hideYPos, 0.2f).SetEase(Ease.InQuad));
            toggleSeq.Join(attackOptionsPanelCanvasGroup.DOFade(0f, 0.2f));
            // 버튼 목표 위치
            float buttonTargetY = hideYPos - panelRect.sizeDelta.y;
            toggleSeq.Join(buttonRect.DOAnchorPosY(buttonTargetY, 0.2f).SetEase(Ease.InQuad));
            
            // 프리뷰도 숨김
            if (StageManager.Instance != null)
            {
                StageManager.Instance.HideAllAttackPreviews();
            }
        }
        
        // 화살표 이미지 교체
        UpdateToggleButtonArrow();
    }
    
    // 토글 버튼 화살표 이미지 업데이트
    private void UpdateToggleButtonArrow()
    {
        if (toggleAttackOptionsButton == null) return;
        
        Image arrowImage = toggleAttackOptionsButton.GetComponent<Image>();
        if (arrowImage == null)
        {
            arrowImage = toggleAttackOptionsButton.GetComponentInChildren<Image>();
        }
        
        if (arrowImage != null && arrowDownSprite != null && arrowUpSprite != null)
        {
            arrowImage.sprite = isAttackOptionsPanelOpen ? arrowDownSprite : arrowUpSprite;
        }
    }
    
    // 족보 UI 강제로 숨기기
    public void HideAttackOptionsPanel()
    {
        if (attackOptionsPanel != null)
        {
            attackOptionsPanel.SetActive(false);
            if (StageManager.Instance != null)
            {
                StageManager.Instance.HideAllAttackPreviews();
            }
        }
    }
    
    // 족보 UI 표시 여부 확인
    public bool IsAttackOptionsPanelVisible()
    {
        return attackOptionsPanel != null && attackOptionsPanel.activeSelf;
    }

    public void ShowRewardScreen(List<Relic> relicOptions, System.Action onRelicSelected = null)
    {
        if (rewardScreenPanel == null) return;
        rewardScreenPanel.SetActive(true);
        for (int i = 0; i < relicChoiceButtons.Length; i++)
        {
            if (i < relicOptions.Count)
            {
                Relic relic = relicOptions[i];
                relicNameTexts[i].text = relic.GetLocalizedName();
                relicIconImages[i].sprite = relic.Icon;

                // 획득 가능 여부 체크 및 설명 업데이트
                bool canAcquire = GameManager.Instance.CanAcquireRelic(relic);
                int currentCount = GameManager.Instance.activeRelics.Count(r => r.RelicID == relic.RelicID);
                int effectiveMax = GameManager.Instance.GetEffectiveMaxCount(relic.RelicID, relic.MaxCount);


                string description = relic.GetLocalizedDescription();
                if (effectiveMax > 0)
                {
                    string ownedLabel = LocalizationManager.Instance.GetText("UI_OWNED");
                    description += $"\n<color=#888888>{ownedLabel}: {currentCount}/{effectiveMax}</color>";
                }
                relicDescriptionTexts[i].text = description;
                
                // 버튼 설정
                relicChoiceButtons[i].onClick.RemoveAllListeners();
                relicChoiceButtons[i].interactable = canAcquire;
                
                if (canAcquire)
                {
                    relicChoiceButtons[i].onClick.AddListener(() =>
                    {
                        GameManager.Instance.AddRelic(relic);
                        rewardScreenPanel.SetActive(false);
                        onRelicSelected?.Invoke();
                        
                        // 튜토리얼 모드일 때 유물 선택 알림
                        if (GameManager.Instance.isTutorialMode)
                        {
                            TutorialRelicController relicTutorial = FindFirstObjectByType<TutorialRelicController>();
                            if (relicTutorial != null)
                            {
                                relicTutorial.OnRelicSelected();
                            }
                        }
                    });
                }
                else
                {
                    // 최대 개수 도달 시 회색 처리
                    var colors = relicChoiceButtons[i].colors;
                    colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    relicChoiceButtons[i].colors = colors;
                }
                
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

        // 상점 초기화 (리롤 비용 등 리셋)
        ShopManager.Instance.ResetShop();

        // UI 표시
        UpdateShopUI(ShopManager.Instance.currentShopItems, ShopManager.Instance.currentRerollCost);
        maintenancePanel.SetActive(true);
        
        // 튜토리얼 모드이고 Zone 1 클리어 후라면 상점 튜토리얼 시작
        if (GameManager.Instance != null && GameManager.Instance.isTutorialMode)
        {
            TutorialShopController shopTutorial = FindFirstObjectByType<TutorialShopController>();
            if (shopTutorial != null)
            {
                Invoke(nameof(StartShopTutorial), 0.5f);
            }
        }
    }
    
    private void StartShopTutorial()
    {
        TutorialShopController shopTutorial = FindFirstObjectByType<TutorialShopController>();
        if (shopTutorial != null)
        {
            shopTutorial.StartShopTutorial();
        }
    }
    public void UpdateShopUI(List<ShopItem> items, int rerollCost)
    {
        if (maintenancePanel == null) return;

        // 1. 기존 슬롯 제거
        foreach (Transform child in shopItemsContainer) Destroy(child.gameObject);

        // 2. 새 슬롯 생성
        foreach (var item in items)
        {
            GameObject slotObj = Instantiate(shopItemSlotPrefab, shopItemsContainer);
            ShopItemSlot slotScript = slotObj.GetComponent<ShopItemSlot>();
            if (slotScript != null)
            {
                slotScript.Setup(item, this);
            }
        }

        // 3. 리롤 버튼 업데이트
        if (rerollCostText != null)
        {
            string goldSuffix = LocalizationManager.Instance.GetText("INGAME_REROLL_COST_SUFFIX");
            rerollCostText.text = $"{rerollCost} {goldSuffix}";
        }
        if (rerollButton != null)
        {
            rerollButton.interactable = GameManager.Instance.CurrentGold >= rerollCost;
        }
    }
    private void OnRerollClick()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RerollShop();
        }
    }

    private void ExitShop()
    {
        maintenancePanel.SetActive(false);
        HideGenericTooltip();

        if (GameManager.Instance.CurrentWave == 1 && WaveGenerator.Instance != null)
        {
            int currentZone = GameManager.Instance.CurrentZone;
            ZoneData zone = WaveGenerator.Instance.GetCurrentZoneData(currentZone);
            string zoneName = zone != null ? zone.zoneName : "알 수 없음";
            ShowZoneTitle($"Zone {currentZone}: {zoneName}");
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.PrepareNextWave();
        }
    }
    public bool IsShopOpen() { return maintenancePanel != null && maintenancePanel.activeSelf; }

    public void ShowGenericTooltip(string title, string description, RectTransform targetRect)
    {
        if (genericTooltipPopup == null) return;

        genericTooltipPopup.SetActive(true);
        if (tooltipTitle != null) tooltipTitle.text = title;
        if (tooltipDesc != null) tooltipDesc.text = description;

        // 위치 잡기 (기존 팝업 로직 재사용)
        LayoutRebuilder.ForceRebuildLayoutImmediate(genericTooltipPopup.GetComponent<RectTransform>());

        Vector3 iconPos = targetRect.position;
        RectTransform popupRect = genericTooltipPopup.GetComponent<RectTransform>();

        // 아이콘 위쪽에 띄우기
        float yOffset = (targetRect.rect.height * targetRect.lossyScale.y / 2f) +
                        (popupRect.rect.height * popupRect.lossyScale.y / 2f) + 10f;

        genericTooltipPopup.transform.position = iconPos + new Vector3(0, yOffset, 0);
    }

    public void HideGenericTooltip()
    {
        if (genericTooltipPopup != null) genericTooltipPopup.SetActive(false);
    }

    public void HideAllInGameUI()
    {
        // 주요 패널들 비활성화
        if (attackOptionsPanel != null) attackOptionsPanel.SetActive(false);
        if (rewardScreenPanel != null) rewardScreenPanel.SetActive(false);
        if (maintenancePanel != null) maintenancePanel.SetActive(false);
        if (waveInfoPanel != null) waveInfoPanel.SetActive(false);
        if (relicPanel != null) relicPanel.SetActive(false);
        if (enemyDetailPopup != null) enemyDetailPopup.SetActive(false);
        if (relicDetailPopup != null) relicDetailPopup.SetActive(false);
        if (genericTooltipPopup != null) genericTooltipPopup.SetActive(false);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);
 
        // RollPanel과 InfoButton 숨김
        if (rollPanel != null) rollPanel.SetActive(false);
        if (infoButton != null) infoButton.SetActive(false);

        // 상단 패널 숨기기
        if (waveText != null) waveText.gameObject.SetActive(false);
        if (totalGoldText != null) totalGoldText.gameObject.SetActive(false);
        if (healthText != null) healthText.gameObject.SetActive(false);

        if (waveText != null && waveText.transform.parent != null)
        {
            waveText.transform.parent.gameObject.SetActive(false);
        }
    }

    // === 타겟 선택 UI 메서드 ===
    
    public void ShowTargetSelectionMode(int requiredCount, int currentCount)
    {
        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.SetActive(true);
        }
        
        UpdateTargetSelectionText(requiredCount, currentCount);
        
        // 확인 버튼은 필요한 수만큼 선택되면 활성화
        if (confirmTargetButton != null)
        {
            confirmTargetButton.interactable = (currentCount >= requiredCount);
        }
    }

    public void HideTargetSelectionMode()
    {
        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.SetActive(false);
        }
    }

    public void UpdateTargetSelectionText(int requiredCount, int currentCount)
    {
        if (targetSelectionText != null)
        {
            string selectLabel = LocalizationManager.Instance.GetText("INGAME_TARGET_SELECT");
            targetSelectionText.text = $"{selectLabel} ({currentCount}/{requiredCount})";
        }
    }

    private void OnConfirmTargetButton()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ConfirmTargetSelection();
        }
    }

    private void OnCancelTargetButton()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.CancelTargetSelection();
        }
    }
}