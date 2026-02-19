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

    public override void OnDamageTaken(int damageTaken, AttackHand hand)
    {
        base.OnDamageTaken(damageTaken, hand);
        if (hand == null) return;
        if (isDead) return;

        // '홀수' 족보로 공격받았을 때 반격
        if (hand.Description.Contains("홀수"))
        {
            if (GameManager.Instance != null)
            {
                Debug.Log($"{enemyName}의 [지옥불]! 플레이어가 화상 피해를 입습니다.");
                string text = LocalizationManager.Instance?.GetText("COMBAT_HELLFIRE") ?? "지옥불!";
                EffectManager.Instance.ShowText(transform, text, Color.red);
                GameManager.Instance.DamagePlayer(burnDamage, "지옥견");
            }
        }
        //다른것도?
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_HELLHOUND");
    }
}