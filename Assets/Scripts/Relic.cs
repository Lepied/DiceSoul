using UnityEngine;

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. 'public Sprite Icon' 프로퍼티 추가
/// 2. 생성자가 Icon을 받도록 수정
/// </summary>
public enum RelicEffectType
{
    // 스탯 (매 턴 적용)
    AddMaxRolls,
    AddBaseDamage,        // 모든 족보 데미지 +
    
    // 주사위 덱 (획득 시 적용)
    AddDice,              // 덱에 주사위 추가
    
    // 주사위 굴림 (굴림 시 적용)
    ModifyDiceValue,      // '1'을 '7'로, '홀수' 다시 굴리기 등

    // 점수/데미지 (판정 시 적용)
    AddScoreMultiplier,   // 모든 점수 배율
    
    JokboDamageAdd,       // '특정' 족보 데미지 +
    JokboScoreMultiplier  // '특정' 족보 점수 배율 x
}

public class Relic
{
    public string RelicID { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Sprite Icon { get; private set; } // [!!! 신규 추가 !!!]
    public RelicEffectType EffectType { get; private set; }
    
    public int IntValue { get; private set; }
    public float FloatValue { get; private set; }
    public string StringValue { get; private set; }
    public int MaxCount { get; private set; } 

    /// <summary>
    /// 기본 생성자 (int 또는 float 값 사용)
    /// </summary>
    public Relic(string relicID, string name, string description, Sprite icon, 
                 RelicEffectType effectType, int intValue = 0, float floatValue = 0f, int maxCount = 0)
    {
        this.RelicID = relicID;
        this.Name = name;
        this.Description = description;
        this.Icon = icon; // [!!! 신규 추가 !!!]
        this.EffectType = effectType;
        this.IntValue = intValue;
        this.FloatValue = floatValue;
        this.StringValue = string.Empty;
        this.MaxCount = maxCount;
    }

    /// <summary>
    /// 문자열 값(StringValue)을 받는 생성자 (Jokbo, AddDice 등)
    /// </summary>
    public Relic(string relicID, string name, string description, Sprite icon, 
                 RelicEffectType effectType, string stringValue, int intValue = 0, float floatValue = 0f, int maxCount = 0)
    {
        this.RelicID = relicID;
        this.Name = name;
        this.Description = description;
        this.Icon = icon; // [!!! 신규 추가 !!!]
        this.EffectType = effectType;
        this.IntValue = intValue;
        this.FloatValue = floatValue;
        this.StringValue = stringValue;
        this.MaxCount = maxCount;
    }
}

