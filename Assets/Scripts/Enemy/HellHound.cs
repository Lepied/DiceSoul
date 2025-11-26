using UnityEngine;

/// <summary>
/// (신규 파일) 지옥견 (Zone 3: 악마의 성채 일반 몹)
/// [기믹]: 지옥불 - '홀수' 족보로 피격 시 플레이어에게 2 데미지 반사
/// (스탯은 인스펙터에서 설정: Biological 타입)
/// </summary>
public class Hellhound : Enemy
{
    [Header("지옥견 기믹")]
    public int burnDamage = 2;

    public override void OnDamageTaken(int damageTaken, AttackJokbo jokbo)
    {
        base.OnDamageTaken(damageTaken, jokbo);

        if (isDead) return;

        // '홀수' 족보로 공격받았을 때 반격
        if (jokbo.Description.Contains("홀수"))
        {
            if (GameManager.Instance != null)
            {
                Debug.Log($"{enemyName}의 [지옥불]! 플레이어가 화상 피해를 입습니다.");
                EffectManager.Instance.ShowText(transform, "지옥불!", Color.red);
                GameManager.Instance.HealPlayer(-burnDamage);
            }
        }
        //다른것도?
    }
}