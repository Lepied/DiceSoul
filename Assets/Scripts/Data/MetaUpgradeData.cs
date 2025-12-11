using UnityEngine;

[CreateAssetMenu(fileName = "New MetaUpgrade", menuName = "Dice Rogue/Meta Upgrade Data")]
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
    MaxHealth,          // 성벽 체력
    RepairAmount,       // 긴급 복구
    Shield,             // 임시 체력
    BaseDamage,         // 공격력
    StartDamage,        // 시작 데미지(웨이브시작시 데미지 주고 시작)
    StartGold,          // 전쟁 자금(웨이브 시작시 골드(점수)주고 시작)
    ScoreBonus          // 주사위 족보 완성할때마다 얻는 점수 보너스
}