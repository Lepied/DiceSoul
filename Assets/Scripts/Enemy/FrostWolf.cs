using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// (신규 파일) 서리 늑대 (Zone 3: 얼음 동굴 일반 몹)
/// [기믹]: 냉기 - 매 턴(굴림 시) 플레이어의 '총합' 족보 데미지가 10씩 감소 (중첩됨)
/// (스탯은 인스펙터에서 설정: Biological 타입)
/// </summary>
public class FrostWolf : Enemy
{
    private int chillStacks = 0;
    public int damageReductionPerStack = 20;

    // 기믹 
    // 플레이어가 주사위를 굴릴 때마다 냉기 중첩이 쌓입니다.
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);

        if (isDead) return;

        chillStacks++;
        string text = LocalizationManager.Instance?.GetText("COMBAT_FROST") ?? "냉기";
        EffectManager.Instance.ShowText(transform, text, Color.cyan);
    }

    // 기믹
    // '총합' 족보 공격 시, 중첩된 냉기만큼 데미지를 감소
    public override int CalculateDamageTaken(AttackHand hand)
    {
        int baseDamage = hand.BaseDamage;

        if (hand.Description.Contains("총합"))
        {
            int reduction = chillStacks * damageReductionPerStack;
            int finalDamage = Mathf.Max(0, baseDamage - reduction);

            string text = LocalizationManager.Instance?.GetText("COMBAT_DECREASE") ?? "감소";
            EffectManager.Instance.ShowText(transform, text, Color.cyan);
            
            return finalDamage;
        }

        // 그 외에는 기본 로직 (Biological 100%)
        return base.CalculateDamageTaken(hand);
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_FROSTWOLF");
    }
}