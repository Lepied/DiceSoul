using UnityEngine;

/// <summary>
/// (수정됨) 임프 (Zone 3: 악마의 성채 일반 몹)
/// 타입: Spirit -> Biological (생체)
/// [기믹]: 마력 흡수 - '총합' 족보로 공격받으면 데미지 대신 체력 회복
/// (스탯은 인스펙터에서 설정: Biological 타입)
/// </summary>
public class Imp : Enemy
{
    [Header("임프 기믹")]
    public bool absorbTotalDamage = true;

    /// <summary>
    /// [기믹: 마력 흡수]
    /// 생체 타입이지만, '총합' 족보 공격은 체력으로 흡수합니다.
    /// </summary>
    public override int CalculateDamageTaken(AttackJokbo jokbo)
    {
        string desc = jokbo.Description;

        // "총합" 족보로 공격받았을 때
        if (desc.Contains("총합"))
        {

            EffectManager.Instance.ShowText(transform, "ABSORB", Color.green);
            EffectManager.Instance.ShowHeal(transform, jokbo.BaseDamage);
            HealSelf(jokbo.BaseDamage);
            return 0;
        }

        // 그 외에는 부모(Biological)의 기본 로직 (100% 피해)
        return base.CalculateDamageTaken(jokbo);
    }

    private void HealSelf(int amount)
    {
        // 이미 죽었으면 회복 불가
        if (isDead) return;

        int prevHP = currentHP;
        currentHP = Mathf.Min(currentHP + amount, maxHP);

        if (currentHP > prevHP)
        {
            Debug.Log($"{enemyName} 체력 회복: +{currentHP - prevHP}");
            UpdateUI();
        }
    }
}