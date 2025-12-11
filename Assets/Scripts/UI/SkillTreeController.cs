using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SKillTreeController : MonoBehaviour
{
    public static SKillTreeController Instance { get; private set; }

    [Header("연결")]
    public ScrollRect mapScrollRect;
    public RectTransform mapContent;
    public RectTransform mapViewport;

    [Header("설정")]
    public float moveDuration = 0.5f;
    public Ease moveEase = Ease.OutExpo;

    void Awake()
    {
        Instance = this;
    }

    public void FocusOnSlot(RectTransform targetSlot)
    {
        if (mapScrollRect == null || mapContent == null) return;

        mapContent.DOKill(); // 기존 움직임 중단

        // 1. 목표 위치 계산 (뷰포트 중앙으로 오도록)
        Vector2 targetPos = -targetSlot.anchoredPosition;

        // 2. 지도 밖으로 나가지 않게 제한
        Vector2 clampedPos = ClampPosition(targetPos);

        // 3. 이동
        mapContent.DOAnchorPos(clampedPos, moveDuration).SetEase(moveEase);
    }

    private Vector2 ClampPosition(Vector2 targetPos)
    {
        // 뷰포트와 콘텐츠 크기 차이 계산
        float diffX = (mapContent.rect.width - mapViewport.rect.width) / 2;
        float diffY = (mapContent.rect.height - mapViewport.rect.height) / 2;

        // 이동 가능한 범위 제한
        float x = Mathf.Clamp(targetPos.x, -diffX, diffX);
        float y = Mathf.Clamp(targetPos.y, -diffY, diffY);

        return new Vector2(x, y);
    }
}