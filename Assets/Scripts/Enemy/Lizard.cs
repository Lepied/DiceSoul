using UnityEngine;
using System.Collections.Generic;

// 리자드맨
public class Lizard : Enemy
{
    [Header("리자드맨 기믹")]
    public int counterDamage = 3;

    private int attackCountThisTurn = 0;

    // 기믹
    // 한 턴에 여러 번 공격받으면 2번째부터 반격
    public override void OnDamageTaken(int damageTaken, AttackHand hand)
    {
        base.OnDamageTaken(damageTaken, hand);
        if (isDead) return;

        attackCountThisTurn++;

        // 2번째 공격부터 반격
        if (attackCountThisTurn >= 2)
        {
            if (GameManager.Instance != null)
            {
                string text = LocalizationManager.Instance?.GetText("COMBAT_COUNTER") ?? "꼬리 반격!";
                EffectManager.Instance.ShowText(transform, text, Color.yellow);

                GameManager.Instance.DamagePlayer(counterDamage, enemyName + "의 반격");
                Debug.Log($"[{enemyName}] 재빠른 반격! {counterDamage} 데미지");
            }
        }
    }

    // 새 턴 시작 시 공격 카운터 초기화
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);
        attackCountThisTurn = 0;
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_LIZARD");
    }
}
