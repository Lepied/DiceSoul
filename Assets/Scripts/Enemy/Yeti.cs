using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 예티 
/// [기믹 1]: 동면 - 체력이 50% 이하로 떨어지면, 다음 턴 시작 시 체력 50 회복 (1회 한정)
/// [기믹 2]: 분노 - '스트레이트'로 피격 시, 다음 턴까지 '총합' 족보에 면역
/// </summary>
public class Yeti : Enemy
{
    [Header("예티 기믹")]
    public int hibernateHealAmount = 50;
    
    private bool hasHibernated = false; // 동면 사용 여부
    private bool isEnraged = false; // 분노 상태 (총합 면역)

    // [기믹 1: 동면] & [기믹 2: 분노 해제]
    // 턴 시작 시(굴림 시) 체력 조건을 확인하여 회복하고, 분노 상태를 초기화합니다.
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);
        if (isDead) return;

        // 분노 상태는 1턴(다음 굴림 전)까지만 유지
        if (isEnraged)
        {
            isEnraged = false;
            Debug.Log($"[{enemyName}]의 분노가 가라앉았습니다.");
        }

        // 동면 체크 (체력 50% 이하, 아직 안 썼음)
        if (!hasHibernated && currentHP <= maxHP * 0.5f)
        {
            hasHibernated = true;
            
            int prevHP = currentHP;
            currentHP = Mathf.Min(currentHP + hibernateHealAmount, maxHP);
            
            Debug.Log($"[{enemyName}]이(가) 위기를 느끼고 [동면]합니다! 체력 회복: +{currentHP - prevHP}");
            EffectManager.Instance.ShowHeal(transform, currentHP - prevHP);
            UpdateUI();
        }
    }

    //[기믹 2: 분노 발동]
    // '스트레이트'로 맞으면 분노하여 방어력을 올립니다.

    public override void OnDamageTaken(int damageTaken, AttackJokbo jokbo)
    {
        base.OnDamageTaken(damageTaken, jokbo);
        if(jokbo == null) return;
        if (isDead) return;

        if (jokbo.Description.Contains("스트레이트"))
        {
            isEnraged = true;
            EffectManager.Instance.ShowText(transform, "분노!", Color.red);
            Debug.Log($"[{enemyName}]이(가) 강력한 공격에 [분노]합니다! (다음 턴 '총합' 면역)");
        }
    }

    /// <summary>
    /// [기믹 2: 분노 방어]
    /// 분노 상태일 때 '총합' 족보를 무시합니다.
    /// </summary>
    public override int CalculateDamageTaken(AttackJokbo jokbo)
    {
        // 분노 상태 + 총합 공격 = 면역
        if (isEnraged && jokbo.Description.Contains("총합"))
        {
            string immuneText = LocalizationManager.Instance != null 
                ? LocalizationManager.Instance.GetText("ENEMY_EFFECT_IMMUNE") 
                : "면역!";
            EffectManager.Instance.ShowText(transform, immuneText, Color.grey);
            return 0;
        }

        return base.CalculateDamageTaken(jokbo);
    }

    protected override void OnDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(2000); // 보스 클리어 보너스
        }
        base.OnDeath();
    }
}