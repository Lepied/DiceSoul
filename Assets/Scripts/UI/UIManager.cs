using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public TextMeshProUGUI goalText;
    public TextMeshProUGUI rollCountText;
    public TextMeshProUGUI healthText;

    public GameObject rewardScreenPanel;
    public Button[] relicChoiceButtons;
    public TextMeshProUGUI[] relicNameTexts;
    public TextMeshProUGUI[] relicDescriptionTexts;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Canvas의 일부라면 DontDestroyOnLoad는 필요 없을 수 있습니다.
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
    }

    /// <summary>
    /// 목표 텍스트를 업데이트합니다.
    /// </summary>
    public void UpdateGoalText(string description)
    {
        if (goalText != null)
        {
            goalText.text = $"목표: {description}";
        }
    }

    /// <summary>
    /// 굴림 횟수 텍스트를 업데이트합니다.
    /// </summary>
    public void UpdateRollCount(int current, int max)
    {
        if (rollCountText != null)
        {
            rollCountText.text = $"굴림 횟수: {current} / {max}";
        }
    }

    /// <summary>
    /// 체력 텍스트를 업데이트합니다.
    /// </summary>
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"체력: {currentHealth} / {maxHealth}";
        }
    }
    /// <summary>
    /// GameManager가 호출. 3개의 유물 옵션으로 보상 화면을 켭니다.
    /// </summary>
    public void ShowRewardScreen(List<Relic> relicOptions)
    {
        if (rewardScreenPanel == null)
        {
            Debug.LogError("Reward Screen Panel이 UIManager에 연결되지 않았습니다!");
            return;
        }

        // 1. 보상 화면 Panel을 켠다
        rewardScreenPanel.SetActive(true);

        // 2. 3개의 버튼에 각각 유물 정보를 설정한다
        for (int i = 0; i < relicChoiceButtons.Length; i++)
        {
            // (안전 장치) 유물 옵션이 3개보다 적게 들어올 경우
            if (i < relicOptions.Count)
            {
                Relic relic = relicOptions[i];

                // 2-1. 버튼 텍스트 설정
                if (relicNameTexts[i] != null)
                    relicNameTexts[i].text = relic.Name;
                if (relicDescriptionTexts[i] != null)
                    relicDescriptionTexts[i].text = relic.Description;

                // 2-2. 버튼 클릭 이벤트 설정 (매우 중요!)
                relicChoiceButtons[i].onClick.RemoveAllListeners(); // 기존 리스너 모두 제거
                relicChoiceButtons[i].onClick.AddListener(() =>
                {
                    // 이 버튼을 누르면...
                    SelectRelic(relic);
                });

                relicChoiceButtons[i].gameObject.SetActive(true);
            }
            else
            {
                // (유물이 3개 미만이면 남는 버튼은 끈다)
                relicChoiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 유물 버튼 중 하나를 클릭했을 때 내부적으로 호출되는 함수
    /// </summary>
    private void SelectRelic(Relic chosenRelic)
    {
        // 1. 보상 화면 Panel을 끈다
        if (rewardScreenPanel != null)
        {
            rewardScreenPanel.SetActive(false);
        }

        // 2. GameManager에게 "이거 선택했어요!"라고 알린다
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRelicSelected(chosenRelic);
        }
    }
}