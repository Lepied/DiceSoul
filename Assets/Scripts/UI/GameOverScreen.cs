using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    [Header("UI 요소")]
    public GameObject gameOverPanel;
    public CanvasGroup canvasGroup;

    [Header("텍스트")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI waveReachedText;
    public TextMeshProUGUI playTimeText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI goldEarnedText;
    public TextMeshProUGUI metaCurrencyText;
    public TextMeshProUGUI maxDamageText;
    public TextMeshProUGUI maxChainText;
    public TextMeshProUGUI mostUsedJokboText;

    [Header("유물 표시")]
    public Transform relicContainer;
    public GameObject relicIconPrefab;
    public TextMeshProUGUI relicCountText;

    [Header("버튼")]
    public Button mainMenuButton;
    public Button restartButton;

    [Header("애니메이션 설정")]
    public float fadeInDuration = 0.5f;
    public float countUpDuration = 1.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    void Start()
    {
        // 버튼 이벤트 연결
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    // 게임오버 화면 표시
    public void ShowGameOver(float delay = 0f)
    {
        if (gameOverPanel == null)
        {
            return;
        }

        DOVirtual.DelayedCall(delay, () =>
        {
            gameOverPanel.SetActive(true);
            UpdateStatistics();
            PlayFadeInAnimation();
        });
    }

    // 통계 업데이트
    private void UpdateStatistics()
    {
        if (GameManager.Instance == null) return;

        // 기본 정보
        if (waveReachedText != null)
        {
            string zoneText = LocalizationManager.Instance.GetText("RESULT_ZONE");
            string waveText = LocalizationManager.Instance.GetText("RESULT_WAVE");
            waveReachedText.text = $"{zoneText} {GameManager.Instance.CurrentZone} - {waveText} {GameManager.Instance.CurrentWave}";
        }

        if (playTimeText != null)
        {
            playTimeText.text = $"{GameManager.Instance.GetFormattedPlayTime()}";
        }

        if (killsText != null)
        {
            string killsLabel = LocalizationManager.Instance.GetText("RESULT_KILLS");
            string killsUnit = LocalizationManager.Instance.GetText("RESULT_KILLS_UNIT");
            killsText.text = $"{killsLabel}: {GameManager.Instance.totalKills}{killsUnit}";
        }

        // 자원
        if (goldEarnedText != null)
        {
            string goldLabel = LocalizationManager.Instance.GetText("RESULT_GOLD_EARNED");
            goldEarnedText.text = $"{goldLabel}: {GameManager.Instance.totalGoldEarned}G";
        }

        // 메타 재화 계산
        int metaCurrency = CalculateMetaCurrency();
        if (metaCurrencyText != null)
        {
            string metaLabel = LocalizationManager.Instance.GetText("RESULT_META_CURRENCY");
            metaCurrencyText.text = $"{metaLabel}: +{metaCurrency}";
        }

        // 하이라이트
        if (maxDamageText != null)
        {
            string damageLabel = LocalizationManager.Instance.GetText("RESULT_MAX_DAMAGE");
            maxDamageText.text = $"{damageLabel}: {GameManager.Instance.maxDamageDealt}";
        }

        if (maxChainText != null)
        {
            string chainLabel = LocalizationManager.Instance.GetText("RESULT_MAX_CHAIN");
            maxChainText.text = $"{chainLabel}: {GameManager.Instance.maxChainCount}";
        }

        if (mostUsedJokboText != null)
        {
            string mostUsed = GameManager.Instance.GetMostUsedJokbo();
            int count = 0;
            if (GameManager.Instance.jokboUsageCount.ContainsKey(mostUsed))
            {
                count = GameManager.Instance.jokboUsageCount[mostUsed];
            }
            string mostUsedLabel = LocalizationManager.Instance.GetText("RESULT_MOST_USED");
            
            // 족보 이름 로컬라이제이션
            string localizedJokbo = mostUsed;
            string jokboKey = AttackJokbo.DescriptionToKey(mostUsed);
            if (!string.IsNullOrEmpty(jokboKey) && LocalizationManager.Instance != null)
            {
                localizedJokbo = LocalizationManager.Instance.GetText(jokboKey);
            }
            
            mostUsedJokboText.text = $"{mostUsedLabel}: {localizedJokbo} x {count}";
        }

        // 유물 표시
        DisplayRelics();

        // 메타 재화 얻기
        AddMetaCurrency(metaCurrency);
    }

    // 메타 재화 계산
    private int CalculateMetaCurrency()
    {
        int total = 0;

        // 도달한 존/웨이브
        int zoneBonus = (GameManager.Instance.CurrentZone - 1) * 10;
        int waveBonus = GameManager.Instance.CurrentWave * 2;
        total += zoneBonus + waveBonus;

        // 보스 처치
        total += GameManager.Instance.bossesDefeated * 10;

        // 획득한 인게임 골드
        int goldBonus = GameManager.Instance.totalGoldEarned / 10;
        total += goldBonus;

        // 완벽한 웨이브(안맞고 깬 웨이브)
        total += GameManager.Instance.perfectWaves * 5;

        return total;
    }

    // 메타 재화 지급
    private void AddMetaCurrency(int amount)
    {
        if (GameManager.Instance == null) return;

        // 기존 메타 재화 가져오기
        string key = GameManager.Instance.metaCurrencySaveKey;
        int current = PlayerPrefs.GetInt(key, 0);
        int newTotal = current + amount;

        PlayerPrefs.SetInt(key, newTotal);
        PlayerPrefs.Save();
    }

    // 유물 표시
    private void DisplayRelics()
    {
        if (relicContainer == null || relicIconPrefab == null) return;

        foreach (Transform child in relicContainer)
        {
            Destroy(child.gameObject);
        }

        if (GameManager.Instance == null) return;

        int relicCount = GameManager.Instance.activeRelics.Count;

        // 유물 개수 표시
        if (relicCountText != null)
        {
            string relicLabel = LocalizationManager.Instance.GetText("RESULT_RELIC_COUNT");
            string relicUnit = LocalizationManager.Instance.GetText("RESULT_RELIC_COUNT_UNIT");
            relicCountText.text = $"{relicLabel}: {relicCount}{relicUnit}";
        }

        var relicGroups = GameManager.Instance.activeRelics.GroupBy(r => r.RelicID);
        
        // 유물 아이콘 생성
        foreach (var group in relicGroups)
        {
            Relic relicData = group.First();
            int count = group.Count();
            
            GameObject iconObj = Instantiate(relicIconPrefab, relicContainer);
            Image iconImage = iconObj.GetComponent<Image>();
            if (iconImage != null && relicData.Icon != null)
            {
                iconImage.sprite = relicData.Icon;
            }

            TextMeshProUGUI countText = iconObj.GetComponentInChildren<TextMeshProUGUI>();
            if (countText != null)
            {
                countText.text = (count > 1) ? $"x{count}" : "";
            }
        }
    }

    // 페이드 인 애니메이션
    private void PlayFadeInAnimation()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);

        PlayCountUpAnimations();
    }

    // 숫자 카운트업 애니메이션
    private void PlayCountUpAnimations()
    {
        // 골드 카운트업
        if (goldEarnedText != null)
        {
            int targetGold = GameManager.Instance.totalGoldEarned;
            string goldLabel = LocalizationManager.Instance.GetText("RESULT_GOLD_EARNED");
            DOVirtual.Int(0,targetGold, countUpDuration, value =>
            {
                goldEarnedText.text = $"{goldLabel}: {value}G";
            }).SetEase(Ease.OutQuad);
        }

        // 처치 수 카운트업
        if (killsText != null)
        {
            int targetKills = GameManager.Instance.totalKills;
            string killsLabel = LocalizationManager.Instance.GetText("RESULT_KILLS");
            string killsUnit = LocalizationManager.Instance.GetText("RESULT_KILLS_UNIT");
            DOVirtual.Int(0, targetKills, countUpDuration, value =>
            {
                killsText.text = $"{killsLabel}: {value}{killsUnit}";
            }).SetEase(Ease.OutQuad);
        }
    }

    // 메인 메뉴 버튼
    private void OnMainMenuClicked()
    {
        Debug.Log("[게임오버] 메인 메뉴로 이동");
        canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
        {
            // GameOverDirector의 전환 효과 재생
            if (GameOverDirector.Instance != null)
            {
                GameOverDirector.Instance.PlayTransitionToMainMenu();
            }
        });


    }

    // 재시작 버튼
    private void OnRestartClicked()
    {
        Debug.Log("[게임오버] 재시작");

        // 게임오버 스크린 페이드 아웃

        canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
        {
            if (GameOverDirector.Instance != null)
            {
                GameOverDirector.Instance.PlayTransitionToRestart();
            }
        });

    }

    // 외부에서 게임오버 트리거
    public static void TriggerGameOver(float delay = 0.5f)
    {
        Debug.Log($"[GameOverScreen] TriggerGameOver 호출됨. Instance: {Instance != null}, delay: {delay}");
        if (Instance != null)
        {
            Instance.ShowGameOver(delay);
        }
        else
        {
            Debug.LogError("[GameOverScreen] Instance가 null입니다!");
        }
    }
}
