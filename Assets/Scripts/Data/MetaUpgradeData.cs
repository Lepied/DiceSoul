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
    
    // 현재 언어에 맞는 업그레이드 이름 반환
    public string GetLocalizedName()
    {
        if (LocalizationManager.Instance != null)
        {
            string snakeCaseKey = ConvertToSnakeCase(effectType.ToString());
            string key = $"META_{snakeCaseKey}_NAME";
            return LocalizationManager.Instance.GetText(key);
        }
        return displayName;
    }
    
    // 현재 언어에 맞는 업그레이드 설명 반환
    public string GetLocalizedDescription()
    {
        if (LocalizationManager.Instance != null)
        {
            string snakeCaseKey = ConvertToSnakeCase(effectType.ToString());
            string key = $"META_{snakeCaseKey}_DESC";
            return LocalizationManager.Instance.GetText(key);
        }
        return description;
    }
    
    private string ConvertToSnakeCase(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            input, 
            "([a-z])([A-Z])", 
            "$1_$2"
        ).ToUpper();
    }
}

// 효과 종류
public enum MetaEffectType
{
    // 방어 카테고리
    MaxHealth,              // 1: 최대 체력
    ZoneStartHeal,          // 2:- 존 시작 시 회복
    WaveStartShield,        // 3: 웨이브 시작 시 실드
    DamageReduction,        // 4A:받는 피해 감소
    WaveEndHeal,            // 4B:  웨이브 종료 시 회복
    ShieldPerReroll,        // 4C: 리롤당 실드 획득
    ShieldCarryOver,        // 5:실드 이월
    FirstHitImmune,         // 6: 첫 피격 무효화
    Revive,                 // 7: 사망 시 부활
    
    // 공격 카테고리 (Offense)
    BaseDamage,             // 1: 기본 데미지
    StartDamage,            // 2: 웨이브 시작 데미지
    CritChance,             // 3: 치명타 확률
    RerollDamageBonus,      // 4A:리롤당 데미지
    FourDiceDamageBonus,    // 4B: 주사위 4개이상 사용하면 데미지 보너스
    SplashDamage,           // 4C:스플래시 데미지
    ComboBonus,             // 5: 연속 공격 데미지
    StartDiceBonus,         // 6: 시작 주사위 추가
    CritMultiplier,         // 7:치명타 배율
    
    // 유틸 카테고리 (Utility)
    StartGold,              // 1: 시작 골드
    GoldBonus,              // 2: 족보 골드 보너스
    ShopDiscount,           // 3: 상점 할인
    GoldMultiplier,         // 4A: 골드 배율
    RareRelicRate,          // 4B: 레어 유물 확률
    MaxRerolls,             // 4C: 최대 리롤
    ShopRefreshCostFixed,   // 5: 상점 새로고침 비용 고정
    InterestRate,           // 6:존 클리어 시 골드 추가
    StartingRelicChoice,    // 7: 시작 유물 선택

}

public enum MetaCategory
{
    Defense,   // 방어
    Offense,   // 공격
    Utility    // 유틸
}