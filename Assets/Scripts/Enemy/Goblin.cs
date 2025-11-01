using UnityEngine;

/// <summary>
/// Goblin: Enemy를 상속받음.
/// 기본 Biological 타입이므로 CalculateDamageTaken은 덮어쓰지 않음.
/// </summary>
public class Goblin : Enemy
{
    /// <summary>
    /// 'Enemy' 부모의 SetupStats 함수를 덮어쓰기(override)
    /// </summary>
    protected override void SetupStats()
    {
        this.enemyName = "고블린";
        this.maxHP = 30;
        this.enemyType = EnemyType.Biological;
        this.difficultyCost = 5;
        this.minZoneLevel = 1;
    }
}
