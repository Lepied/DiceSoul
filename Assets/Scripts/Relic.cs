using UnityEngine;

// 나중에 유물 효과를 구분하기 위한 Enum(열거형)
// 2단계(지금)에서는 선언만 해두고, 3단계(다음)에서 이 효과를 실제로 구현합니다.
public enum RelicEffectType
{
    // 점수/계산 관련
    AddScoreMultiplier,     // 점수 배율 추가
    ModifyDiceValue,        // 특정 주사위 값 변경 (예: '1'을 '7'로)
    
    // 주사위/굴림 관련
    AddMaxRolls,            // 최대 굴림 횟수 +1
    AddDice,                // 주사위 개수 +1
    KeepDiceOnFail,         // 실패 시 '킵'한 주사위 유지
    
    // 재화/상점 관련
    AddGoldPerWin,          // 승리 시 골드 추가
    ShopDiscount            // 상점 할인
}

// SO 대신 사용할 유물 데이터 클래스
public class Relic
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public RelicEffectType EffectType { get; private set; } // 이 유물의 핵심 효과
    
    // 효과에 필요한 값 (예: 배율 +2, 굴림 +1)
    public float EffectValue { get; private set; }
    public int EffectIntValue { get; private set; } // (정수 값이 필요할 경우)

    // 생성자
    public Relic(string name, string description, RelicEffectType effectType, float effectValue = 0, int effectIntValue = 0)
    {
        this.Name = name;
        this.Description = description;
        this.EffectType = effectType;
        this.EffectValue = effectValue;
        this.EffectIntValue = effectIntValue;
    }
}