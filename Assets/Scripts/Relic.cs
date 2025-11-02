using UnityEngine;

/// <summary>
/// 유물이 가질 수 있는 '지속 효과'의 종류를 정의합니다.
/// GameManager의 ApplyAllRelicEffects 함수가 이 타입을 읽어들입니다.
/// </summary>
public enum RelicEffectType
{
    // 굴림/주사위 관련
    AddMaxRolls,            // 최대 굴림 횟수 +1
    AddDice,                // 주사위 개수 +1
    
    // 점수/데미지 관련
    AddScoreMultiplier,     // 점수 배율 추가 (예: 1.5f)
    ModifyDiceValue,        // 특정 주사위 값 변경 (예: '1'을 '7'로)
    AddBaseDamage,          // 모든 족보의 기본 데미지 +5

    // 기타
    None // 효과 없음 (데이터만)
}

/// <summary>
/// '지속 효과'를 가지는 유물 데이터 클래스입니다.
/// </summary>
public class Relic
{
    public string ID { get; private set; } // "RELIC_CLOVER", "RELIC_GOLD_DICE"
    public string Name { get; private set; }
    public string Description { get; private set; }
    public RelicEffectType EffectType { get; private set; }
    
    // 효과에 필요한 값들
    public int IntValue { get; private set; } // (예: 굴림 +1)
    public float FloatValue { get; private set; } // (예: 배율 1.5f)
    public string StringValue { get; private set; } // (예: "D8")

    // 생성자
    public Relic(string id, string name, string description, RelicEffectType effectType, 
                 int intValue = 0, float floatValue = 0f, string stringValue = null)
    {
        this.ID = id;
        this.Name = name;
        this.Description = description;
        this.EffectType = effectType;
        this.IntValue = intValue;
        this.FloatValue = floatValue;
        this.StringValue = stringValue;
    }
}
