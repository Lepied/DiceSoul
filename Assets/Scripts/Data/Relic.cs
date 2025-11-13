using UnityEngine;

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. 'RelicEffectType' Enum에 6개의 새로운 기믹 타입 추가
///    (JokboScoreAdd, ModifyHealth, ModifyMaxRolls, RemoveDice, DynamicDamage_Score, DynamicDamage_LostHealth)
/// </summary>
public enum RelicEffectType
{
    // 스탯 (매 턴 적용)
    AddMaxRolls,
    AddBaseDamage,        
    
    // 주사위 덱 (획득 시 적용)
    AddDice,              
    
    // 주사위 굴림 (굴림 시 적용)
    ModifyDiceValue,      

    // 점수/데미지 (판정 시 적용)
    AddScoreMultiplier,   
    JokboDamageAdd,       
    JokboScoreMultiplier, 
    

    JokboScoreAdd,            // (RLC_RUSTY_GEAR) '특정' 족보 점수 +
    ModifyHealth,             // (RLC_GLASS_CANNON) 획득 시 최대 체력 변경
    ModifyMaxRolls,           // (RLC_HEAVY_DICE) 획득 시 최대 굴림 횟수 변경
    RemoveDice,               // (RLC_FOCUS) 획득 시 덱에서 주사위 제거
    DynamicDamage_Score,      // (RLC_PLUTOCRACY) 점수 비례 데미지
    DynamicDamage_LostHealth,  // (RLC_BLOODLUST) 잃은 체력 비례 데미지

    RollCountBonus // 특정 굴림에서 데미지나 점수 보너스
}

public class Relic
{
    public string RelicID { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Sprite Icon { get; private set; } 
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
        this.Icon = icon; 
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
        this.Icon = icon; 
        this.EffectType = effectType;
        this.IntValue = intValue;
        this.FloatValue = floatValue;
        this.StringValue = stringValue;
        this.MaxCount = maxCount;
    }
}