using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// 유물 슬롯 펄스/글로우 애니메이션
public class RelicSlotAnimator : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [Tooltip("펄스 스케일 배율")]
    public float pulseScale = 1.2f;

    [Tooltip("펄스 지속 시간 (초)")]
    public float pulseDuration = 0.4f;

    [Tooltip("글로우 색상 (통일)")]
    public Color glowColor = new Color(1f, 0.9f, 0.3f, 1f); // 금색

    private UIManager uiManager;

    void Start()
    {
        uiManager = GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    //유물 슬롯 펄스 애니메이션 재생
    public void PlayActivationPulse(int slotIndex)
    {

        Transform relicContainer = uiManager.relicPanel.transform;
        if (slotIndex < 0 || slotIndex >= relicContainer.childCount)
        {
            return;
        }

        GameObject relicSlot = relicContainer.GetChild(slotIndex).gameObject;
        if (relicSlot == null)
        {
            return;
        }

        Image iconImage = relicSlot.GetComponent<Image>();
        Transform iconTransform = relicSlot.transform;

        // 기존 애니메이션 중지
        iconTransform.DOKill();
        iconImage.DOKill();

        // 원본 값 저장
        Vector3 originalScale = iconTransform.localScale;
        Color originalColor = iconImage.color;

        // 펄스 애니메이션 시퀀스
        Sequence pulseSequence = DOTween.Sequence();

        // 크기바꿔
        pulseSequence.Append(iconTransform.DOScale(originalScale * pulseScale, pulseDuration * 0.5f)
            .SetEase(Ease.OutQuad));
        pulseSequence.Append(iconTransform.DOScale(originalScale, pulseDuration * 0.5f)
            .SetEase(Ease.InQuad));

        // 글로우
        Sequence glowSequence = DOTween.Sequence();
        glowSequence.Append(iconImage.DOColor(glowColor, pulseDuration * 0.3f));
        glowSequence.Append(iconImage.DOColor(originalColor, pulseDuration * 0.7f));

        // 동시 재생
        pulseSequence.Play();
        glowSequence.Play();
    }

    // 모든 애니메이션 중지
    public void StopAllAnimations()
    {
        if (uiManager == null || uiManager.relicPanel == null) return;

        Transform relicContainer = uiManager.relicPanel.transform;
        for (int i = 0; i < relicContainer.childCount; i++)
        {
            Transform slotTransform = relicContainer.GetChild(i);
            Image iconImage = slotTransform.GetComponent<Image>();
            
            slotTransform.DOKill();
            if (iconImage != null)
            {
                iconImage.DOKill();
            }
            slotTransform.localScale = Vector3.one;
        }
    }

    void OnDestroy()
    {
        StopAllAnimations();
    }
}
