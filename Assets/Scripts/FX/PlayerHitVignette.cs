using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

//플레이어 피격 시 화면 가장자리 붉은 비네팅 효과
public class PlayerHitVignette : MonoBehaviour
{
    public static PlayerHitVignette Instance { get; private set; }

    [Header("비네팅 이미지")]
    public Image vignetteImage;

    [Header("효과 설정")]
    public float lightHitMaxAlpha = 0.3f;
    public float heavyHitMaxAlpha = 0.6f;


    public float fadeInDuration = 0.1f;
    public float fadeOutDuration = 0.4f;

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
            Color c = vignetteColor;
            c.a = 0f;
            vignetteImage.color = c;
            vignetteImage.raycastTarget = false;
        }
    }


    // 비네팅 효과를 재생합니다
    public void PlayHitEffect(bool isHeavyHit)
    {
        if (vignetteImage == null) return;

        // 이전 애니메이션 중단
        currentTween?.Kill();

        float targetAlpha = isHeavyHit ? heavyHitMaxAlpha : lightHitMaxAlpha;

        // 페이드 인 → 페이드 아웃 시퀀스
        Sequence hitSequence = DOTween.Sequence();
        Color targetColor = vignetteColor;
        targetColor.a = targetAlpha;
        hitSequence.Append(vignetteImage.DOColor(targetColor, fadeInDuration));
        targetColor.a = 0f;
        hitSequence.Append(vignetteImage.DOColor(targetColor, fadeOutDuration));

        currentTween = hitSequence;
    }

    ///피해량에 따라 효과 강하고약하고
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
