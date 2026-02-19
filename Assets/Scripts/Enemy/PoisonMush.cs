using UnityEngine;
using System.Collections.Generic;

// 독버섯
public class PoisonMush : Enemy
{
    [Header("독버섯 기믹")]
    public int turnsToPoisonTrigger = 3;

    private int poisonDamage = 0;
    private int poisonTurnCount = 0;

    //기믹
    // 매 턴마다 주사위 개수만큼 독을 누적하고, 2턴 후 발동
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);
        if (isDead) return;

        // 주사위 개수만큼 독 누적
        int diceCount = diceValues.Count;
        poisonDamage += diceCount;
        poisonTurnCount++;

        // 독 누적 표시
        string text = $"{LocalizationManager.Instance?.GetText("COMBAT_POISON") ?? "독"} {poisonDamage}";
        EffectManager.Instance.ShowText(transform, text, new Color(0.5f, 1f, 0.3f)); // 연두색

        // 3턴 후 독 발동
        if (poisonTurnCount >= turnsToPoisonTrigger)
        {
            if (GameManager.Instance != null && poisonDamage > 0)
            {
                string triggerText = LocalizationManager.Instance?.GetText("COMBAT_POISON_TRIGGER") ?? "맹독 포자!";
                EffectManager.Instance.ShowText(transform, triggerText, Color.red);

                GameManager.Instance.DamagePlayer(poisonDamage, "맹독 포자");
            }

            // 초기화
            poisonDamage = 0;
            poisonTurnCount = 0;
        }
    }


    ///기믹 : 수비 족보 공격 시 독 초기화
    public override void OnDamageTaken(int damageTaken, AttackHand hand)
    {
        base.OnDamageTaken(damageTaken, hand);

        // 수비 족보로 공격받으면 독 제거
        if (hand != null && hand.Description.Contains("수비"))
        {
            if (poisonDamage > 0)
            {
                string text = LocalizationManager.Instance?.GetText("COMBAT_DETOXIFY") ?? "해독!";
                EffectManager.Instance.ShowText(transform, text, Color.white);

                poisonDamage = 0;
                poisonTurnCount = 0;
            }
        }
    }

    void OnDisable()
    {
        // 비활성화 시 독 초기화
        poisonDamage = 0;
        poisonTurnCount = 0;
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_POISONMUSH");
    }
}
