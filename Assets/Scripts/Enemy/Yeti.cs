using UnityEngine;
using System.Collections.Generic;


// 예티 
// [기믹 1]: 동면 - 체력이 50% 이하로 떨어지면, 다음 턴 시작 시 체력 50 회복
// [기믹 2]: 분노 - '스트레이트'로 피격 시, 다음 턴까지 '총합' 족보에 면역
public class Yeti : Enemy
{
    [Header("예티 기믹")]
    public int hibernateHealAmount = 50;

    private bool hasHibernated = false; // 동면 사용 여부
    private bool isEnraged = false; // 분노 상태 (총합 면역)

    // 기믹 동면
    // 턴 시작 시(굴림 시) 체력 조건을 확인하여 회복하고, 분노 상태를 초기화합니다.
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);
        if (isDead) return;

        // 분노 상태는 1턴(다음 굴림 전)까지만 유지
        if (isEnraged)
        {
            isEnraged = false;

        }

        // 동면 체크 (체력 50% 이하, 아직 안 썼음)
        if (!hasHibernated && currentHP <= maxHP * 0.5f)
        {
            hasHibernated = true;

            int prevHP = currentHP;
            currentHP = Mathf.Min(currentHP + hibernateHealAmount, maxHP);

            EffectManager.Instance.ShowHeal(transform, currentHP - prevHP);
            UpdateUI();
        }
    }

    //기믹 
    // '스트레이트'로 맞으면 분노

    public override void OnDamageTaken(int damageTaken, AttackHand hand)
    {
        base.OnDamageTaken(damageTaken, hand);
        if (hand == null) return;
        if (isDead) return;

        if (hand.Description.Contains("스트레이트"))
        {
            isEnraged = true;
            string text = LocalizationManager.Instance?.GetText("COMBAT_RAGE") ?? "분노!";
            EffectManager.Instance.ShowText(transform, text, Color.red);
        }
    }


    // 분노 상태일 때 '총합' 족보를 무시
    public override int CalculateDamageTaken(AttackHand hand)
    {
        // 분노 상태 + 총합 공격 = 면역
        if (isEnraged && hand.Description.Contains("총합"))
        {
            string immuneText = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText("ENEMY_EFFECT_IMMUNE")
                : "면역!";
            EffectManager.Instance.ShowText(transform, immuneText, Color.grey);
            return 0;
        }

        return base.CalculateDamageTaken(hand);
    }

    protected override void OnDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(2000, GoldSource.Bonus); // 보스 클리어 보너스
        }
        base.OnDeath();
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_YETI");
    }
}