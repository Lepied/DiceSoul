using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI References")]
    public RectTransform topMask;
    public RectTransform bottomMask;
    public RectTransform leftMask;
    public RectTransform rightMask;
    public RectTransform highlightRect;
    public Image highlightImage;
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;
    public RectTransform tooltipArrow;
    public Button nextButton;
    private TMP_Text nextButtonText;

    [Header("Settings")]
    public float tooltipOffset = 80f;
    public float highlightPadding = 20f;
    public float pulseDuration = 0.6f;

    private Sequence pulseSequence;

    // 캐싱용 변수
    private Vector3[] targetCornersCache = new Vector3[4];
    private float cachedTargetWidth;
    private float cachedTargetHeight;
    private Vector2 cachedTargetCenter;
    private float cachedHalfWidth;
    private float cachedHalfHeight;
    private RectTransform cachedTarget; 

    private void Awake()
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

    private void Start()
    {
        HideTutorial();

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
            nextButtonText = nextButton.GetComponentInChildren<TMP_Text>();
            UpdateNextButtonText();
        }

        // 언어 변경 이벤트 구독
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateNextButtonText;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateNextButtonText;
        }
    }

    private void UpdateNextButtonText()
    {
        if (nextButtonText != null && LocalizationManager.Instance != null)
        {
            nextButtonText.text = LocalizationManager.Instance.GetText("TUTORIAL_NEXT_BUTTON");
        }
    }

    // 단계
    public void ShowStep(RectTransform target, string message, TooltipPosition position, bool showNextBtn = true)
    {
        CacheTargetInfo(target);
        ShowMasks();
        HighlightUI();
        UpdateMaskPositions();
        ShowTooltip(message, position);
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(showNextBtn);
        }
    }

    // 타겟 정보 캐싱
    private void CacheTargetInfo(RectTransform target)
    {
        cachedTarget = target;
        target.GetWorldCorners(targetCornersCache);

        cachedTargetWidth = Vector3.Distance(targetCornersCache[0], targetCornersCache[3]);
        cachedTargetHeight = Vector3.Distance(targetCornersCache[0], targetCornersCache[1]);
        cachedTargetCenter = (targetCornersCache[0] + targetCornersCache[2]) / 2f;

        cachedHalfWidth = (cachedTargetWidth + highlightPadding * 2f) / 2f;
        cachedHalfHeight = (cachedTargetHeight + highlightPadding * 2f) / 2f;
    }

    // 마스크 패널 표시
    private void ShowMasks()
    {
        if (topMask != null) topMask.gameObject.SetActive(true);
        if (bottomMask != null) bottomMask.gameObject.SetActive(true);
        if (leftMask != null) leftMask.gameObject.SetActive(true);
        if (rightMask != null) rightMask.gameObject.SetActive(true);
    }

    // 마스크 위치 업데이트
    private void UpdateMaskPositions()
    {
        // Canvas RectTransform 가져오기
        Canvas canvas = topMask.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // 월드 좌표를 Canvas 로컬 좌표로 변환
        Vector2 localCenter;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            cachedTargetCenter, 
            canvas.worldCamera, 
            out localCenter
        );

        // Canvas 로컬 좌표계에서 타겟 영역 계산
        float targetLeft = localCenter.x - cachedHalfWidth;
        float targetRight = localCenter.x + cachedHalfWidth;
        float targetBottom = localCenter.y - cachedHalfHeight;
        float targetTop = localCenter.y + cachedHalfHeight;

        // Canvas 중심 기준으로 변환 (0,0이 좌하단)
        float canvasHalfWidth = canvasWidth / 2f;
        float canvasHalfHeight = canvasHeight / 2f;

        targetLeft += canvasHalfWidth;
        targetRight += canvasHalfWidth;
        targetBottom += canvasHalfHeight;
        targetTop += canvasHalfHeight;

        // Top Mask (상단 영역) - Stretch Anchor 사용
        if (topMask != null)
        {
            topMask.anchorMin = new Vector2(0, targetTop / canvasHeight);
            topMask.anchorMax = new Vector2(1, 1);
            topMask.offsetMin = Vector2.zero;
            topMask.offsetMax = Vector2.zero;
        }

        // Bottom Mask (하단 영역) - Stretch Anchor 사용
        if (bottomMask != null)
        {
            bottomMask.anchorMin = new Vector2(0, 0);
            bottomMask.anchorMax = new Vector2(1, targetBottom / canvasHeight);
            bottomMask.offsetMin = Vector2.zero;
            bottomMask.offsetMax = Vector2.zero;
        }

        // Left Mask (좌측 영역) - Stretch Anchor 사용
        if (leftMask != null)
        {
            leftMask.anchorMin = new Vector2(0, targetBottom / canvasHeight);
            leftMask.anchorMax = new Vector2(targetLeft / canvasWidth, targetTop / canvasHeight);
            leftMask.offsetMin = Vector2.zero;
            leftMask.offsetMax = Vector2.zero;
        }

        // Right Mask (우측 영역) - Stretch Anchor 사용
        if (rightMask != null)
        {
            rightMask.anchorMin = new Vector2(targetRight / canvasWidth, targetBottom / canvasHeight);
            rightMask.anchorMax = new Vector2(1, targetTop / canvasHeight);
            rightMask.offsetMin = Vector2.zero;
            rightMask.offsetMax = Vector2.zero;
        }
    }

    // UI 하이라이트
    private void HighlightUI()
    {
        highlightRect.gameObject.SetActive(true);

        highlightRect.position = cachedTargetCenter;
        highlightRect.sizeDelta = new Vector2(cachedTargetWidth + highlightPadding * 2f, cachedTargetHeight + highlightPadding * 2f);

        pulseSequence?.Kill();
        highlightRect.localScale = Vector3.one;
        pulseSequence = DOTween.Sequence();
        pulseSequence.Append(highlightRect.DOScale(1.05f, pulseDuration))
                     .Append(highlightRect.DOScale(1.0f, pulseDuration))
                     .SetLoops(-1);
    }

    // 말풍선 표시
    private void ShowTooltip(string message, TooltipPosition position)
    {
        tooltipPanel.SetActive(true);
        tooltipText.text = message;

        // 말풍선 위치
        Vector2 tooltipPos = CalculateTooltipPosition(position);
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipPos = ClampToScreen(tooltipPos, tooltipRect);
        tooltipRect.position = tooltipPos;
        SetArrowDirection(position);
    }

    // 화면 안으로 위치
    private Vector2 ClampToScreen(Vector2 position, RectTransform tooltipRect)
    {
        // 말풍선의 크기
        float tooltipWidth = tooltipRect.sizeDelta.x;
        float tooltipHeight = tooltipRect.sizeDelta.y;
        float highlightLeft = cachedTargetCenter.x - cachedHalfWidth;
        float highlightRight = cachedTargetCenter.x + cachedHalfWidth;
        float highlightBottom = cachedTargetCenter.y - cachedHalfHeight;
        float highlightTop = cachedTargetCenter.y + cachedHalfHeight;

        // 화면 경계
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float padding = 20f;

        float tooltipLeft = position.x - tooltipWidth / 2f;
        float tooltipRight = position.x + tooltipWidth / 2f;
        float tooltipBottom = position.y - tooltipHeight / 2f;
        float tooltipTop = position.y + tooltipHeight / 2f;

        // 겹침 확인
        bool overlapsX = tooltipRight > highlightLeft && tooltipLeft < highlightRight;
        bool overlapsY = tooltipTop > highlightBottom && tooltipBottom < highlightTop;

        if (overlapsX && overlapsY)
        {
            float moveUp = highlightTop - tooltipBottom + padding;
            float moveDown = tooltipTop - highlightBottom + padding;
            float moveLeft = tooltipRight - highlightLeft + padding;
            float moveRight = highlightRight - tooltipLeft + padding;

            float minMove = Mathf.Min(moveUp, moveDown, moveLeft, moveRight);

            if (minMove == moveUp && position.y + moveUp + tooltipHeight / 2f <= screenHeight - padding)
            {
                position.y += moveUp;
            }
            else if (minMove == moveDown && position.y - moveDown - tooltipHeight / 2f >= padding)
            {
                position.y -= moveDown;
            }
            else if (minMove == moveLeft && position.x - moveLeft - tooltipWidth / 2f >= padding)
            {
                position.x -= moveLeft;
            }
            else if (minMove == moveRight && position.x + moveRight + tooltipWidth / 2f <= screenWidth - padding)
            {
                position.x += moveRight;
            }
        }

        // 화면 경계 내로
        float minX = tooltipWidth / 2 + padding;
        float maxX = screenWidth - tooltipWidth / 2 - padding;
        position.x = Mathf.Clamp(position.x, minX, maxX);

        float minY = tooltipHeight / 2 + padding;
        float maxY = screenHeight - tooltipHeight / 2 - padding;
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    // 말풍선 위치계산
    private Vector2 CalculateTooltipPosition(TooltipPosition position)
    {
        if (position == TooltipPosition.Auto)
        {
            return AutoCalculatePosition();
        }

        Vector2 tooltipPos = cachedTargetCenter;

        switch (position)
        {
            case TooltipPosition.Top:
                tooltipPos += Vector2.up * (cachedTargetHeight / 2 + highlightPadding + tooltipOffset);
                break;
            case TooltipPosition.Bottom:
                tooltipPos += Vector2.down * (cachedTargetHeight / 2 + highlightPadding + tooltipOffset);
                break;
            case TooltipPosition.Left:
                tooltipPos += Vector2.left * (cachedTargetWidth / 2 + highlightPadding + tooltipOffset);
                break;
            case TooltipPosition.Right:
                tooltipPos += Vector2.right * (cachedTargetWidth / 2 + highlightPadding + tooltipOffset);
                break;
        }

        return tooltipPos;
    }

    // 자동 위치 계산
    private Vector2 AutoCalculatePosition()
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        // 화면 여유 공간 확인
        float topSpace = screenSize.y - cachedTargetCenter.y;
        float bottomSpace = cachedTargetCenter.y;
        float leftSpace = cachedTargetCenter.x;
        float rightSpace = screenSize.x - cachedTargetCenter.x;

        // 가장 여유 공간이 큰 방향 선택
        float maxSpace = Mathf.Max(topSpace, bottomSpace, leftSpace, rightSpace);

        if (maxSpace == topSpace)
            return CalculateTooltipPosition(TooltipPosition.Top);
        else if (maxSpace == bottomSpace)
            return CalculateTooltipPosition(TooltipPosition.Bottom);
        else if (maxSpace == leftSpace)
            return CalculateTooltipPosition(TooltipPosition.Left);
        else
            return CalculateTooltipPosition(TooltipPosition.Right);
    }

    private void SetArrowDirection(TooltipPosition position)
    {
        if (tooltipArrow == null) return;

        switch (position)
        {
            case TooltipPosition.Top:
                tooltipArrow.localRotation = Quaternion.Euler(0, 0, -90);
                break;
            case TooltipPosition.Bottom:
                tooltipArrow.localRotation = Quaternion.Euler(0, 0, 90);
                break;
            case TooltipPosition.Left:
                tooltipArrow.localRotation = Quaternion.Euler(0, 0, 0);
                break;
            case TooltipPosition.Right:
                tooltipArrow.localRotation = Quaternion.Euler(0, 0, 180);
                break;
        }
    }

    // 다음 버튼
    private void OnNextButtonClicked()
    {
        // 다음 단계 진행은 컨트롤러에서하기
    }

    // 튜토리얼 숨기기
    public void HideTutorial()
    {
        // 마스크 패널 비활성화
        topMask.gameObject.SetActive(false);
        bottomMask.gameObject.SetActive(false);
        leftMask.gameObject.SetActive(false);
        rightMask.gameObject.SetActive(false);

        highlightRect.localScale = Vector3.one;
        highlightRect.sizeDelta = Vector2.zero;
        highlightRect.position = Vector2.zero;
        highlightRect.gameObject.SetActive(false);

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        pulseSequence?.Kill();
    }
}

public enum TooltipPosition
{
    Top,
    Bottom,
    Left,
    Right,
    Auto
}
