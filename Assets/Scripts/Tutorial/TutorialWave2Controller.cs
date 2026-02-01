using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialWave2Controller : MonoBehaviour
{
    [Header("References")]
    public TutorialManager tutorialManager;
    public Button enemyInfoButton;
    public RectTransform rollCountUI;
    public RectTransform topInfoPanel;

    [Header("Messages")]
    public string enemyIntroMessage = "이번 웨이브부터 다양한 적들이 등장합니다!\n적마다 특별한 능력을 가지고 있습니다.";
    public string enemyInfoButtonMessage = "적 정보 버튼을 눌러\n적들의 능력을 확인할 수 있습니다";
    public string rollCountMessage = "리롤 횟수가 정해져 있습니다.\n이 안에 적을 모두 처치하지 못하면 피해를 받습니다";
    public string topInfoMessage = "현재 HP, 획득한 골드, 스테이지, 획득한 유물을\n이곳에서 확인할 수 있습니다.";
    public string completeMessage = "좋습니다!\n이제 혼자서 플레이해보세요.";

    private bool isActive = false;

    public void StartWave2Tutorial()
    {
        isActive = true;
        ShowStep1_EnemyIntro();
    }

    private void ShowStep1_EnemyIntro()
    {
        if (!isActive) return;

        if (tutorialManager.topMask != null) tutorialManager.topMask.gameObject.SetActive(true);
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(true);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(true);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(true);

        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }

        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = enemyIntroMessage;
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position =
            new Vector2(Screen.width / 2, Screen.height / 2);

        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }

        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }

        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(() =>
        {
            tutorialManager.HideTutorial();
            ShowStep2_EnemyInfoButton();
        });
    }

    private void ShowStep2_EnemyInfoButton()
    {
        if (!isActive) return;

        if (enemyInfoButton == null)
        {
            ShowStep3_RollCount();
            return;
        }

        tutorialManager.ShowStep(
            enemyInfoButton.GetComponent<RectTransform>(),
            enemyInfoButtonMessage,
            TooltipPosition.Right,
            true
        );

        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(() =>
        {
            StartCoroutine(WaitAndShowStep3());
        });
    }

    private IEnumerator WaitAndShowStep3()
    {
        tutorialManager.HideTutorial();
        yield return new WaitForSeconds(0.1f);
        ShowStep3_RollCount();
    }

    private void ShowStep3_RollCount()
    {
        if (!isActive) return;

        if (rollCountUI == null)
        {
            ShowStep4_TopInfoPanel();
            return;
        }

        tutorialManager.ShowStep(
            rollCountUI,
            rollCountMessage,
            TooltipPosition.Bottom,
            true
        );

        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(() =>
        {
            StartCoroutine(WaitAndShowStep4());
        });
    }

    private IEnumerator WaitAndShowStep4()
    {
        tutorialManager.HideTutorial();
        yield return new WaitForSeconds(0.1f);
        ShowStep4_TopInfoPanel();
    }

    private void ShowStep4_TopInfoPanel()
    {
        if (!isActive) return;

        if (topInfoPanel == null)
        {
            ShowStep5_Complete();
            return;
        }

        tutorialManager.ShowStep(
            topInfoPanel,
            topInfoMessage,
            TooltipPosition.Bottom,
            true
        );

        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(ShowStep5_Complete);
    }

    private void ShowStep5_Complete()
    {
        if (!isActive) return;

        if (tutorialManager.topMask != null) tutorialManager.topMask.gameObject.SetActive(true);
        if (tutorialManager.bottomMask != null) tutorialManager.bottomMask.gameObject.SetActive(true);
        if (tutorialManager.leftMask != null) tutorialManager.leftMask.gameObject.SetActive(true);
        if (tutorialManager.rightMask != null) tutorialManager.rightMask.gameObject.SetActive(true);

        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }

        tutorialManager.tooltipPanel.SetActive(true);
        tutorialManager.tooltipText.text = completeMessage;
        tutorialManager.tooltipPanel.GetComponent<RectTransform>().position =
            new Vector2(Screen.width / 2, Screen.height / 2);

        if (tutorialManager.tooltipArrow != null)
        {
            tutorialManager.tooltipArrow.gameObject.SetActive(false);
        }

        if (tutorialManager.highlightRect != null)
        {
            tutorialManager.highlightRect.gameObject.SetActive(false);
        }

        tutorialManager.nextButton.gameObject.SetActive(true);
        tutorialManager.nextButton.onClick.RemoveAllListeners();
        tutorialManager.nextButton.onClick.AddListener(CompleteWave2Tutorial);
    }

    private void CompleteWave2Tutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }

    public void StopTutorial()
    {
        isActive = false;
        tutorialManager.HideTutorial();
    }
}
