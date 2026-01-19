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
    public TextMeshProUGUI totalGoldText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI rollCountText;

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

    [Header("상점/정비 UI")]
    public GameObject maintenancePanel;
    public Transform shopItemsContainer;
    public GameObject shopItemSlotPrefab;
    public Button exitShopButton;

    public Button rerollButton;
    public TextMeshProUGUI rerollCostText;

    [Header("통합 툴팁 (재사용)")]
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

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButton);
        }
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
    public void UpdateGold(int gold)
    {
        if (totalGoldText != null) totalGoldText.text = $"Gold: {gold}";
    }
    public void UpdateHealth(int current, int max)
    {
        int shield = GameManager.Instance != null ? GameManager.Instance.CurrentShield : 0;
        if (healthText != null)
        {
            if (shield > 0)
            {
                healthText.text = $"HP: {current} / {max} (+{shield})";
            }
            else
            {
                healthText.text = $"HP: {current} / {max}";
            }
        }
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
        if (enemyDetailHP != null) enemyDetailHP.text = $"HP: {enemy.maxHP} / ATK: {enemy.attackDamage}";
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
                    ShowTemporaryMessage("이중 주사위는 이미 사용했습니다!");
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
                    ShowTemporaryMessage("운명의 주사위는 이미 사용했습니다!");
                }
                break;
                

        }
    }
    
    // 이중 주사위 선택 모드 시작
    private void StartDoubleDiceSelection()
    {
        Debug.Log("[UI] 이중 주사위 사용 - 2배로 만들 주사위를 클릭하세요");
        ShowTemporaryMessage("2배로 만들 주사위를 클릭하세요!");
        
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
            ShowTemporaryMessage("주사위가 없습니다!");
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
            ShowTemporaryMessage("모든 주사위가 최대값이 되었습니다!");
            
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
    
    // 임시 메시지 표시 (2초간)
    private void ShowTemporaryMessage(string message)
    {
        // 기존 메시지 UI가 있으면 사용, 없으면 콘솔 로그
        Debug.Log($"[알림] {message}");
        
        // TODO: 실제 UI 팝업 구현 시 여기에 추가
        // 예: messageText.text = message; StartCoroutine(HideMessageAfterDelay(2f));
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
                attackNameTexts[i].text = jokbo.Description;
                if (jokbo.TargetType == AttackTargetType.Defense)
                {
                    attackValueTexts[i].text = $"Shield: {finalBaseDamage}";
                }
                else
                {
                    attackValueTexts[i].text = $"Dmg: {finalBaseDamage} / Gold: {finalBaseGold}";
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

        // 상점 초기화 (리롤 비용 등 리셋)
        ShopManager.Instance.ResetShop();

        // UI 표시
        UpdateShopUI(ShopManager.Instance.currentShopItems, ShopManager.Instance.currentRerollCost);
        maintenancePanel.SetActive(true);
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
        if (rerollCostText != null) rerollCostText.text = $"{rerollCost} Gold";
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

    public void ShowGameOverScreen(int earnedCurrency)
    {
        HideAllInGameUI();
        
        if (gameOverPanel == null)
        {
            Debug.LogError("[UIManager] gameOverPanel이 null입니다!");
            return;
        }
        
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

        //상단 패널 숨기기
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
            targetSelectionText.text = $"적을 선택하세요 ({currentCount}/{requiredCount})";
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