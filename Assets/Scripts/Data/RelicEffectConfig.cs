using UnityEngine;
using System.Collections.Generic;


//어떤 유물이 어떤 시각적 피드백 할지
public static class RelicEffectConfig
{
    public static Dictionary<string, RelicFeedbackData> FeedbackMap = new Dictionary<string, RelicFeedbackData>
    {
        // 중요 1회성 유물
        { "FateDice", new RelicFeedbackData
        {
            showText = true,
            textKey = "RLC_FATE_DICE_TRIGGER",
            color = new Color(1f, 0.84f, 0f) // 금색
        }},

        { "SmallShield", new RelicFeedbackData
        {
            showText = true,
            textKey = "RLC_SMALL_SHIELD_TRIGGER",
            color = Color.gray
        }},

        { "SwiftHands", new RelicFeedbackData
        {
            showText = true,
            textKey = "RLC_SWIFT_HANDS_TRIGGER",
            color = Color.white
        }},

        { "PhoenixFeather", new RelicFeedbackData
        {
            showText = true,
            textKey = "RLC_PHOENIX_FEATHER_TRIGGER",
            color = new Color(1f, 0.5f, 0f) // 주황색
        }},

        // 자주 발동하는 유물
        { "VampireFang", new RelicFeedbackData
        {
            showText = true,
            showValue = true,
            textKey = "RLC_VAMPIRE_FANG_TRIGGER",
            color = new Color(1f, 0.5f, 1f),
            effectType = RelicFeedbackType.Heal
        }},

        { "Regeneration", new RelicFeedbackData
        {
            showText = true,
            showValue = true,
            textKey = "RLC_REGENERATION_TRIGGER",
            color = new Color(0.2f, 1f, 0.2f),
            effectType = RelicFeedbackType.Heal
        }},

        // 확률 발동 유물
        { "TimeRift", new RelicFeedbackData
        {
            showText = true,
            textKey = "RLC_TIME_RIFT_TRIGGER",
            color = new Color(0.8f, 0.3f, 1f) // 보라색
        }},

        // 기타 트리거 유물들
        { "DoubleDice", new RelicFeedbackData
        {
            showText = true,
            textKey = "RLC_DOUBLE_DICE_TRIGGER",
            color = new Color(0.5f, 0.8f, 1f) // 하늘색
        }},

        { "ScholarBook", new RelicFeedbackData
        {
            showText = true,
            showValue = true,
            textKey = "RLC_SCHOLAR_BOOK_TRIGGER",
            color = new Color(1f, 0.9f, 0.5f), // 연한 금색
            effectType = RelicFeedbackType.Damage
        }},
    };

    // 유물이 피드백을 표시하는지 확인
    public static bool ShouldShowFeedback(string relicId)
    {
        return FeedbackMap.ContainsKey(relicId);
    }

    // 유물의 피드백 데이터 가져오기
    public static RelicFeedbackData GetFeedbackData(string relicId)
    {
        if (FeedbackMap.TryGetValue(relicId, out var data))
        {
            return data;
        }
        return null;
    }
}


// 유물 피드백 데이터
public class RelicFeedbackData
{
    public bool showText = false;           // 텍스트 표시 여부
    public bool showValue = false;          // 숫자 값 표시 여부
    public string textKey = "";             // 로컬라이제이션 키
    public Color color = Color.white;       // 텍스트 색상
    public RelicFeedbackType effectType = RelicFeedbackType.Trigger; // 효과 타입
}
