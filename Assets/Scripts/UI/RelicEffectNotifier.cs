using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 유물 발동 텍스트 알림 관리
// 주사위 컨트롤러 위에 적과 동일한 스타일로 텍스트 표시
public class RelicEffectNotifier : MonoBehaviour
{
    public static RelicEffectNotifier Instance { get; private set; }

    [Header("알림 위치")]
    public Transform notificationAnchor;

    [Header("큐잉 설정")]
    public float notificationInterval = 0.3f;

    private Queue<RelicNotification> notificationQueue = new Queue<RelicNotification>();
    private bool isShowingNotification = false;

    void Awake()
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

    // 유물 효과 알림 표시 
    public void ShowRelicEffect(string textKey, Color color)
    {
        string localizedText = LocalizationManager.Instance != null 
            ? LocalizationManager.Instance.GetText(textKey) 
            : textKey;

        notificationQueue.Enqueue(new RelicNotification
        {
            text = localizedText,
            color = color
        });

        if (!isShowingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }
    }

    // 유물 효과 알림 표시
    public void ShowRelicEffectWithValue(int value, RelicFeedbackType effectType)
    {
        string text = effectType switch
        {
            RelicFeedbackType.Heal => $"+{value} HP",
            RelicFeedbackType.Gold => $"+{value} Gold",
            RelicFeedbackType.Shield => $"+{value} Shield",
            _ => $"+{value}"
        };

        Color color = GetColorForEffectType(effectType);

        notificationQueue.Enqueue(new RelicNotification
        {
            text = text,
            color = color
        });

        if (!isShowingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }
    }

    // 유물 이름과 숫자 값을 함께 표시
    public void ShowRelicEffectWithNameAndValue(string nameKey, int value, Color color)
    {
        string relicName = LocalizationManager.Instance != null 
            ? LocalizationManager.Instance.GetText(nameKey) 
            : nameKey;

        string text = $"{relicName} +{value}";

        notificationQueue.Enqueue(new RelicNotification
        {
            text = text,
            color = color
        });

        if (!isShowingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }
    }

    // 알림 큐 처리 코루틴
    private IEnumerator ProcessNotificationQueue()
    {
        isShowingNotification = true;

        while (notificationQueue.Count > 0)
        {
            var notification = notificationQueue.Dequeue();

            // EffectManager 사용하여 텍스트 표시
            if (EffectManager.Instance != null && notificationAnchor != null)
            {
                EffectManager.Instance.ShowText(notificationAnchor, notification.text, notification.color);
            }

            // 다음 알림까지 대기
            yield return new WaitForSeconds(notificationInterval);
        }

        isShowingNotification = false;
    }

    // 효과 타입별 색상 반환
    private Color GetColorForEffectType(RelicFeedbackType type)
    {
        return type switch
        {
            RelicFeedbackType.Heal => new Color(0.2f, 1f, 0.2f),      // 밝은 초록
            RelicFeedbackType.Gold => new Color(1f, 0.84f, 0f),       // 금색
            RelicFeedbackType.Shield => new Color(0.5f, 0.8f, 1f),    // 하늘색
            RelicFeedbackType.Damage => new Color(1f, 0.3f, 0.3f),    // 빨간색
            RelicFeedbackType.Nullify => Color.gray,                   // 회색
            RelicFeedbackType.Trigger => new Color(1f, 0.5f, 1f),     // 보라색
            _ => Color.white
        };
    }

    // 알림 큐 초기화
    public void ClearQueue()
    {
        notificationQueue.Clear();
        StopAllCoroutines();
        isShowingNotification = false;
    }
}

// 알림 데이터 구조체
public struct RelicNotification
{
    public string text;
    public Color color;
}

// 유물 피드백 타입
public enum RelicFeedbackType
{
    Heal,      // 체력 회복
    Gold,      // 골드 획득
    Shield,    // 실드 획득
    Damage,    // 데미지 증가
    Nullify,   // 무효화
    Trigger    // 일반 발동
}
