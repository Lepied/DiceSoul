using UnityEngine;

/// <summary>
/// 살아있는 갑옷 (장갑 타입)
/// Enemy를 상속받고, '장갑' 타입의 데미지 계산 로직을 덮어씁니다.
/// </summary>
public class LivingArmor : Enemy
{
    /*
    protected override void SetupStats()
    {
        this.enemyName = "리빙 아머";
        this.maxHP = 80;
        this.enemyType = EnemyType.Armored; // 장갑 타입
        this.isBoss = false;
        this.difficultyCost = 12; // 스켈레톤(8)보다 비쌈
        this.minZoneLevel = 2; // 2존부터 등장
    }
    */

    /// <summary>
    /// [!!! 장갑 타입 기믹 !!!]
    /// 'Enemy' 부모의 데미지 계산 함수를 덮어쓰기(override)
    /// </summary>
    public override int CalculateDamageTaken(AttackJokbo jokbo)
    {
        int baseDamage = jokbo.BaseDamage;
        string jokboDesc = jokbo.Description; // (AttackDB.cs의 족보 이름)

        // '풀 하우스', '포카드', '야찌' 같은 강력한 족보는 100% 데미지 (관통)
        if (jokboDesc.Contains("풀 하우스") || 
            jokboDesc.Contains("포카드") || 
            jokboDesc.Contains("야찌"))
        {
            Debug.Log("장갑: 관통! (100% 데미지)");
            return baseDamage;
        }

        // 그 외 모든 '일반' 공격(총합, 트리플, 스트레이트 등)은 50% 감소
        Debug.Log("장갑: 피해를 50% 감소시킵니다.");
        return baseDamage / 2;
    }
}
