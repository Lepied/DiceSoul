using UnityEngine;

public enum RelicEffectType
{
    None = 0,
    
    // ===== 스탯 관련 (Passive) =====
    AddMaxRolls,              // 최대 굴림 횟수 +
    AddBaseDamage,            // 모든 족보 데미지 +
    AddBaseGold,              // 모든 족보 골드 +
    AddDamagePercent,         // 모든 족보 데미지 %
    AddGoldMultiplier,        // 골드 획득 배율
    ModifyHealth,             // 최대 체력 변경
    ModifyMaxRolls,           // 최대 굴림 횟수 변경
    AddDefense,               // 방어력 +
    
    // ===== 주사위 덱 (OnAcquire) =====
    AddDice,                  // 덱에 주사위 추가
    RemoveDice,               // 덱에서 주사위 제거
    
    // ===== 주사위 굴림 (OnRoll) =====
    ModifyDiceValue,          // 주사위 값 변환 (연금술사돌 등)
    RerollOdds,               // 홀수 재굴림 (자철석)
    RerollEvens,              // 짝수 재굴림 (탄자나이트)
    RerollSixes,              // 6 재굴림 (가벼운 깃털)
    RerollFirst,              // 첫 굴림 시 일부 재굴림 (빠른 장전)
    FixMinValue,              // 최소값 고정 (철제 주사위)
    BonusOnLowD20,            // D20 낮으면 보너스 (균형추)
    
    // ===== 족보 특화 (OnBeforeAttack) =====
    JokboDamageAdd,           // 특정 족보 데미지 +
    JokboGoldAdd,             // 특정 족보 골드 +
    JokboGoldMultiplier,      // 특정 족보 골드 배율
    JokboDamageMultiplier,    // 특정 족보 데미지 배율
    DisableJokbo,             // 특정 족보 비활성화
    
    // ===== 동적 데미지 (OnBeforeAttack) =====
    DynamicDamage_Gold,       // 보유 골드 비례 데미지
    DynamicDamage_LostHealth, // 잃은 체력 비례 데미지
    DynamicDamage_LowRolls,   // 남은 굴림 적을수록 데미지 (모래시계)
    
    // ===== 특수 보너스 =====
    RollCountBonus,           // 첫 굴림 보너스 (명함)
    FirstAttackBonus,         // 첫 공격 보너스 (날쌘 손놀림)
    ChainDamageBonus,         // 연쇄 공격 데미지 증폭 (광택 구슬)
    
    // ===== 생존/회복 =====
    HealOnJokbo,              // 족보 완성 시 회복 (흡혈귀 이빨)
    HealOnRoll,               // 굴림 시 확률 회복 (재생 팔찌)
    ReviveOnDeath,            // 사망 시 부활 (불사조 깃털)
    DamageImmuneLowHP,        // 저체력 시 피해 무효 (작은 방패)
    FreeRollOnZero,           // 굴림 0일 때 무료 충전
    FreeRollAtZero,           // 굴림 0일 때 무료 충전 (날쌘 손놀림)
    
    // ===== 경제 =====
    ShopDiscount,             // 상점 할인 (행운의 동전)
    ShopRefreshFreeze,        // 상점 리롤 비용 동결 (상인의 명함)
    RefundShopRefresh,        // 상점 리롤 비용 확률 반환 (스프링)
    RelicCapacityUp,          // 유물 보유 한도 증가 (가벼운 가방)
    
    // ===== 수동 발동 (Manual) =====
    FixDiceBeforeRoll,        // 굴리기 전 주사위 고정 (주사위 컵)
    DoubleDiceValue,          // 선택 주사위 2배 (이중 주사위)
    SetAllToMax,              // 모든 주사위 최대값 (운명의 주사위)
    
    // ===== 기타 =====
    HigherReroll,             // 재굴림 시 더 높은 숫자 (행운의 네잎클로버)
    SaveRollChance,           // 굴림 횟수 미소모 확률 (시공의 틈)
    PermanentDamageGrowth,    // 미사용 족보로 영구 성장 (학자의 서적)
    SameNumberBonus,          // 같은 숫자 확률 증가 (자석)
    
    // ===== 복합 효과 =====
    DamageMultiplierWithHealthCost,  // 데미지 증가 + 체력 감소 (도박사의 반지)
    DamageMultiplierNoHeal           // 데미지 증가 + 회복 불가 (악마의 계약서)
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
    
    // 로컬라이제이션된 이름 반환
    public string GetLocalizedName()
    {
        if (LocalizationManager.Instance != null)
        {
            string key = $"{RelicID}_NAME";
            string localized = LocalizationManager.Instance.GetText(key);
            if (!localized.StartsWith("[")) // 키가 존재하면
            {
                return localized;
            }
        }
        return Name;
    }
    
    // 로컬라이제이션된 설명 반환
    public string GetLocalizedDescription()
    {
        if (LocalizationManager.Instance != null)
        {
            string key = $"{RelicID}_DESC";
            string localized = LocalizationManager.Instance.GetText(key);
            if (!localized.StartsWith("[")) // 키가 존재하면
            {
                return localized;
            }
        }
        return Description;
    }
}