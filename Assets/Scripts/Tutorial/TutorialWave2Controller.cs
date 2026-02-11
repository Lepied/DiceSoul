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
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_WAVE2_INTRO");
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
            LocalizationManager.Instance.GetText("TUTORIAL_WAVE2_ENEMYINFO"),
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
            LocalizationManager.Instance.GetText("TUTORIAL_WAVE2_ROLLCOUNT"),
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
            LocalizationManager.Instance.GetText("TUTORIAL_WAVE2_TOPINFO"),
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
        tutorialManager.tooltipText.text = LocalizationManager.Instance.GetText("TUTORIAL_WAVE2_COMPLETE");
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
