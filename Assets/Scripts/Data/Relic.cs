using UnityEngine;

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
    
    JokboScoreAdd,            //  족보 점수 +
    ModifyHealth,             // 최대 체력 변경
    ModifyMaxRolls,           // 최대 굴림 횟수 변경
    RemoveDice,               // 덱에서 주사위 제거
    DynamicDamage_Score,      // 점수 비례 데미지
    DynamicDamage_LostHealth,  // 잃은 체력 비례 데미지
    RollCountBonus // 특정 굴림에서 데미지나 점수 보너스
}

public class Relic
{
    public string RelicID { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Sprite Icon { get; private set; } 
    public RelicEffectType EffectType { get; private set; }

    public bool IsUnLocked = true;
    
    public int IntValue { get; private set; }
    public float FloatValue { get; private set; }
    public string StringValue { get; private set; }
    public int MaxCount { get; private set; } 
    
    // 기본 생성자 (int 또는 float 값 사용)
    public Relic(string relicID, string name, string description, Sprite icon, 
                 RelicEffectType effectType, int intValue = 0, float floatValue = 0f, int maxCount = 0, bool isUnLocked = true)
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

    // 문자열 값(StringValue)을 받는 생성자 (Jokbo, AddDice 등)
    public Relic(string relicID, string name, string description, Sprite icon, 
                 RelicEffectType effectType, string stringValue, int intValue = 0, float floatValue = 0f, int maxCount = 0, bool isUnLocked = true)
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