using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 플레이어 피격 시 화면 가장자리 붉은 비네팅 효과
/// </summary>
public class PlayerHitVignette : MonoBehaviour
{
    public static PlayerHitVignette Instance { get; private set; }

    [Header("비네팅 이미지")]
    [Tooltip("화면 전체를 덮는 Image 컴포넌트 (Radial Gradient 스프라이트 권장)")]
    public Image vignetteImage;

    [Header("효과 설정")]
    [Tooltip("약한 피격 시 최대 알파값 (0-1)")]
    public float lightHitMaxAlpha = 0.3f;

    [Tooltip("강한 피격 시 최대 알파값 (0-1)")]
    public float heavyHitMaxAlpha = 0.6f;

    [Tooltip("페이드 인 시간")]
    public float fadeInDuration = 0.1f;

    [Tooltip("페이드 아웃 시간")]
    public float fadeOutDuration = 0.4f;

    [Tooltip("비네팅 색상")]
    public Color vignetteColor = new Color(1f, 0f, 0f, 1f); // 붉은색

    private Tween currentTween;

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

        if (vignetteImage != null)
        {
            // 초기 상태: 완전히 투명
            Color c = vignetteColor;
            c.a = 0f;
            vignetteImage.color = c;
            vignetteImage.raycastTarget = false; // UI 클릭 차단 방지
        }
        else
        {
            Debug.LogWarning("[PlayerHitVignette] vignetteImage가 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// 비네팅 효과를 재생합니다
    /// </summary>
    /// <param name="isHeavyHit">강한 피격인지 여부 (40 이상)</param>
    public void PlayHitEffect(bool isHeavyHit)
    {
        if (vignetteImage == null) return;

        // 이전 애니메이션 중단
        currentTween?.Kill();

        float targetAlpha = isHeavyHit ? heavyHitMaxAlpha : lightHitMaxAlpha;

        // 페이드 인 → 페이드 아웃 시퀀스
        Sequence hitSequence = DOTween.Sequence();

        // 1. 빠르게 페이드 인
        Color targetColor = vignetteColor;
        targetColor.a = targetAlpha;
        hitSequence.Append(vignetteImage.DOColor(targetColor, fadeInDuration));

        // 2. 천천히 페이드 아웃
        targetColor.a = 0f;
        hitSequence.Append(vignetteImage.DOColor(targetColor, fadeOutDuration));

        currentTween = hitSequence;
    }

    /// <summary>
    /// 피해량에 따라 자동으로 강도를 결정하여 효과 재생
    /// </summary>
    /// <param name="damageAmount">받은 피해량</param>
    public void PlayHitEffectByDamage(int damageAmount)
    {
        bool isHeavyHit = damageAmount >= 40;
        PlayHitEffect(isHeavyHit);
    }

    void OnDestroy()
    {
        currentTween?.Kill();
    }
}
