using UnityEngine;

/// <summary>
/// 망령 (영혼 타입)
/// Enemy를 상속받고, '영혼' 타입의 데미지 계산 로직을 덮어씁니다.
/// </summary>
public class Wraith : Enemy
{
    /*
    protected override void SetupStats()
    {
        this.enemyName = "망령";
        this.maxHP = 40;
        this.enemyType = EnemyType.Spirit; // 영혼 타입
        this.isBoss = false;
        this.difficultyCost = 10; // 스켈레톤(8)보다 약간 비쌈
        this.minZoneLevel = 2; // 2존부터 등장
    }
    */

    /// <summary>
    /// [!!! 영혼 타입 기믹 !!!]
    /// </summary>
    public override int CalculateDamageTaken(AttackJokbo jokbo)
    {
        int baseDamage = jokbo.BaseDamage;
        string jokboDesc = jokbo.Description; 

        // 1. '마법' 족보에 150% 피해 (약점)
        if (jokboDesc.Contains("모두 짝수") || jokboDesc.Contains("모두 홀수"))
        {
            Debug.Log("영혼: [마법] 족보에 치명타! (150% 데미지)");
            return (int)(baseDamage * 2f);
        }

        // 2. '총합' 족보에 0% 피해 (면역)
        if (jokboDesc.Contains("총합"))
        {
            Debug.Log("영혼: [총합] 물리 공격을 무시합니다! (0 데미지)");
            return 0;
        }

        // 3. 그 외 (트리플, 스트레이트 등) 족보는 100% 피해
        return baseDamage;
    }
}
