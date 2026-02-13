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

    /// <summary>
    /// [기믹 1: 냉기 중첩]
    /// 플레이어가 주사위를 굴릴 때마다 냉기 중첩이 쌓입니다.
    /// </summary>
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);
        
        if (isDead) return;

        chillStacks++;
        Debug.Log($"[{enemyName}] 주변의 공기가 차가워집니다... (냉기 중첩: {chillStacks})");
        string text = LocalizationManager.Instance?.GetText("COMBAT_FROST") ?? "냉기";
        EffectManager.Instance.ShowText(transform, text, Color.cyan);
    }

    /// <summary>
    /// [기믹 1 적용: 데미지 감소]
    /// '총합' 족보 공격 시, 중첩된 냉기만큼 데미지를 감소시킵니다.
    /// </summary>
    public override int CalculateDamageTaken(AttackHand hand)
    {
        int baseDamage = hand.BaseDamage;

        if (hand.Description.Contains("총합"))
        {
            int reduction = chillStacks * damageReductionPerStack;
            int finalDamage = Mathf.Max(0, baseDamage - reduction);
            
            string text = LocalizationManager.Instance?.GetText("COMBAT_DECREASE") ?? "감소";
            EffectManager.Instance.ShowText(transform, text, Color.cyan);
            Debug.Log($"[{enemyName}] 냉기로 인해 [총합] 데미지가 {reduction} 감소했습니다. ({baseDamage} -> {finalDamage})");
            return finalDamage;
        }

        // 그 외에는 기본 로직 (Biological 100%)
        return base.CalculateDamageTaken(hand);
    }
}