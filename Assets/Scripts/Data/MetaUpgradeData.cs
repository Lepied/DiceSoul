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
    // === 방어 카테고리 ===
    MaxHealth,          // 최대 체력
    Defense,            // 방어력
    WaveEndHeal,        // 웨이브 종료 시 회복
    InsuranceDiscount,  // 보험 할인
    StartShield,        // 시작 실드
    LowHPDefense,       // 체력낮을떄 방어
    CritImmune,         // 치명타 무효
    
    // === 공격 카테고리 ===
    BaseDamage,         // 기본 데미지
    StartDamage,        // 웨이브 시작 데미지
    CritChance,         // 치명타 확률
    RerollDamageBonus,  // 리롤당 데미지
    FourDiceDamageBonus,// 4주사위 이상 족보 데미지
    SplashDamage,       // 스플래시 데미지
    ComboBonus,         // 연속공격할떄 추가 데미지
    StartDiceBonus,     // 시작 주사위 추가
    CritMultiplier,     // 치명타 배율
    
    // === 유틸 카테고리 ===
    StartGold,          // 시작 골드
    GoldMultiplier,     // 골드 배율
    MaxRerolls,         // 최대 리롤
    RelicDropRate,      // 유물 드롭률
    GoldBonus,          // 족보 골드 보너스
    ShopDiscount,       // 상점 할인
    RareRelicRate,      // 레어 유물 확률
    
    // 미사용
    RepairAmount,       // 긴급 복구  < 이거사용하도록바꾸기?
}

public enum MetaCategory
{
    Defense,   // 방어
    Offense,   // 공격
    Utility    // 유틸
}