using UnityEngine;

[CreateAssetMenu(fileName = "New MetaUpgrade", menuName = "DiceSoul/Meta Upgrade Data")]
public class MetaUpgradeData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;             // 저장용 ID
    public string displayName;    // 표시 이름
    [TextArea] public string description; // 설명 텍스트
    public Sprite icon;           // 아이콘

    [Header("레벨 및 비용")]
    public int maxLevel = 5;
    public int baseCost = 100;
    public float costMultiplier = 1.5f; // 레벨업 배율

    [Header("카테고리 및 단계")]
    public MetaCategory category = MetaCategory.Defense;
    public int tier = 1;
    public string prerequisiteID = "";  // 이전 단계 ID 

    [Header("효과 설정")]
    public MetaEffectType effectType; 
    public float effectValuePerLevel;

    // 레벨별 비용 계산
    public int GetCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
    }

    // 현재 레벨의 총 효과 계산
    public float GetTotalEffect(int currentLevel)
    {
        return currentLevel * effectValuePerLevel;
    }
}

// 효과 종류
public enum MetaEffectType
{
    // 방어 카테고리
    MaxHealth,              // 1: 기초 보강 - 최대 체력
    ZoneStartHeal,          // 2: 성벽 수리 - 존 시작 시 회복
    WaveStartShield,        // 3: 재생성 장벽 - 웨이브 시작 시 실드
    DamageReduction,        // 4A: 강철 피부 - 받는 피해 감소
    WaveEndHeal,            // 4B: 재생성 장벽 → 웨이브 종료 시 회복
    ShieldPerReroll,        // 4C: 구르기 - 리롤당 실드 획득
    ShieldCarryOver,        // 5: 보존의 장막 - 실드 이월
    FirstHitImmune,         // 6: 절대 방어 - 첫 피격 무효화
    Revive,                 // 7: 수호천사 - 사망 시 부활
    
    // 공격 카테고리 (Offense)
    BaseDamage,             // 1: 발리스타 연마 - 기본 데미지
    StartDamage,            // 2: 제압 사격 - 웨이브 시작 데미지
    CritChance,             // 3: 약점 포착 - 치명타 확률
    RerollDamageBonus,      // 4A: 도박사의 손기술 - 리롤당 데미지
    FourDiceDamageBonus,    // 4B: 족보의 완성 - 주사위 4개이상 사용하면 데미지 보너스
    SplashDamage,           // 4C: 광역 충격 - 스플래시 데미지
    ComboBonus,             // 5: 콤보 마스터 - 연속 공격 데미지
    StartDiceBonus,         // 6: 시작 주사위+ - 시작 주사위 추가
    CritMultiplier,         // 7: 치명적인 일격 - 치명타 배율
    
    // ============================================
    // 유틸 카테고리 (Utility)
    // ============================================
    StartGold,              // 시작 골드
    GoldMultiplier,         // 골드 배율
    MaxRerolls,             // 최대 리롤
    RelicDropRate,          // 유물 드롭률
    GoldBonus,              // 족보 골드 보너스
    ShopDiscount,           // 상점 할인
    RareRelicRate,          // 레어 유물 확률
    
    // ============================================
    // 레거시 / 미사용
    // ============================================
    StartShield,            // (미사용)
    RepairAmount,           // (미사용)
}

public enum MetaCategory
{
    Defense,   // 방어
    Offense,   // 공격
    Utility    // 유틸
}