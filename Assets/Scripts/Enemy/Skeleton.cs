using UnityEngine;

/// <summary>
/// Skeleton: Enemy를 상속받음.
/// Undead 타입이므로 데미지 계산 로직(CalculateDamageTaken)을 덮어씀.
/// </summary>
public class Skeleton : Enemy
{
    /*
    protected override void SetupStats()
    {
        this.enemyName = "스켈레톤";
        this.maxHP = 40;
        this.enemyType = EnemyType.Undead;
        this.difficultyCost = 8;
        this.minZoneLevel = 1; // (나중에 2존부터 나오게 할 수도 있음)
    }
    */

    /// <summary>
    /// 'Enemy' 부모의 데미지 계산 함수를 덮어쓰기(override)
    /// </summary>
    public override int CalculateDamageTaken(AttackJokbo jokbo)
    {
        int baseDamage = jokbo.BaseDamage;
        string jokboDesc = jokbo.Description;

        // "트리플", "포카드", "풀 하우스", "야찌" 족보는 150% 피해
        if (jokboDesc.Contains("트리플") || 
            jokboDesc.Contains("포카드") || 
            jokboDesc.Contains("풀 하우스") || 
            jokboDesc.Contains("야찌"))
        {
            Debug.Log("언데드: [고급 족보]에 치명타! (150% 데미지)");
            return (int)(baseDamage * 1.5f);
        }
        else
        {
            // 그 외 모든 공격은 50% 피해
            Debug.Log("언데드: [기본 족보] 피해를 50% 감소시킵니다.");
            return baseDamage / 2;
        }
    }
}
