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
}
