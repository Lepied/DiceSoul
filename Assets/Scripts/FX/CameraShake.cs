using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Transform cameraTransform;
    private Vector3 originalPosition;
    private Tween currentShakeTween;

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

        cameraTransform = Camera.main.transform;
        originalPosition = cameraTransform.localPosition;
    }

    // 카메라 흔들기
    /// <param name="intensity">흔들림 강도</param>
    /// <param name="duration">지속 시간</param>
    public void Shake(float intensity, float duration = 0.5f)
    {
        if (intensity <= 0) return;
        if (cameraTransform == null) return;

        // 이전 쉐이크가 진행 중이면 중단
        currentShakeTween?.Kill();

        // DOTween으로 카메라 흔들기
        currentShakeTween = cameraTransform.DOShakePosition(
            duration,           // 지속 시간
            intensity,          // 강도
            40,                 // 진동 횟수
            180f,                // 랜덤
            false,              // snapping false가 부드럽게 true가 딱딱쓰
            true                // fadeOut
        ).SetUpdate(true)       // 히트스톱 무시
        .OnComplete(() =>
        {
            // 완료 후 원래 위치로
            cameraTransform.localPosition = originalPosition;
            currentShakeTween = null;
        });
    }

    void OnDestroy()
    {
        currentShakeTween?.Kill();
    }
}
