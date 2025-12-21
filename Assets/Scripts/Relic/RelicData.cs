using UnityEngine;

/// <summary>
/// 유물 데이터를 담는 ScriptableObject
/// CSV에서 자동 생성되거나 Inspector에서 수동 편집 가능
/// </summary>
[CreateAssetMenu(fileName = "NewRelic", menuName = "DiceSoul/Relic Data")]
public class RelicData : ScriptableObject
{
    [Header("기본 정보")]
    public string relicID;
    public string relicName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("분류")]
    public RelicRarity rarity = RelicRarity.Common;
    public RelicDropPool dropPool = RelicDropPool.WaveReward;
    public RelicCategory category = RelicCategory.Utility;

    [Header("효과 설정")]
    public RelicEffectType effectType;
    public RelicTriggerTiming triggerTiming = RelicTriggerTiming.Passive;
    
    [Header("효과 값")]
    public int intValue;
    public float floatValue;
    public string stringValue;

    [Header("제한")]
    [Tooltip("0이면 무제한")]
    public int maxCount = 1;
    
    [Header("해금")]
    public bool isUnlockedByDefault = true;
    [Tooltip("해금 조건 (비어있으면 기본 해금)")]
    public string unlockCondition;

    [Header("특수 설정")]
    [Tooltip("UI 버튼이 필요한 수동 발동 유물인지")]
    public bool requiresManualActivation = false;
    [Tooltip("런 당 사용 가능 횟수 (0이면 무제한)")]
    public int usesPerRun = 0;

    /// <summary>
    /// 유물이 해금되었는지 확인
    /// </summary>
    public bool IsUnlocked()
    {
        if (isUnlockedByDefault) return true;
        return PlayerPrefs.GetInt($"Unlock_{relicID}", 0) == 1;
    }

    /// <summary>
    /// 유물 해금
    /// </summary>
    public void Unlock()
    {
        PlayerPrefs.SetInt($"Unlock_{relicID}", 1);
        PlayerPrefs.Save();
    }
}
