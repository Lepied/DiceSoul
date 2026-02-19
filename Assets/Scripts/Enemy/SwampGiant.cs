using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 늪지 거인 Swamp 보스
public class SwampGiant : Enemy
{
    [Header("늪지 거인 기믹")]
    public int crushAmount = 2;
    [Range(0, 1)]
    public float enrageHealthPercent = 0.5f; // 50%
    public int enrageHealPercent = 5; // 5%

    private bool isEnraged = false;

    // 기믹
    // 플레이어가 주사위를 굴릴 때마다 가장 높은 주사위의 눈을 깎음
    // HP 50% 이하 시 2개로 증가하고 재생 효과
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);

        //HP 50% 이하 분노
        if (!isEnraged && currentHP <= maxHP * enrageHealthPercent)
        {
            isEnraged = true;
            string enrageText = LocalizationManager.Instance?.GetText("COMBAT_ENRAGE") ?? "분노!";
            EffectManager.Instance.ShowText(transform, enrageText, Color.red);
        }

        if (DiceController.Instance == null) return;

        var activeDice = DiceController.Instance.activeDice;
        if (activeDice.Count == 0) return;

        // 분노 상태면 주사위 2개, 아니면 1개
        int crushCount = isEnraged ? 2 : 1;
        var topDice = activeDice.OrderByDescending(d => d.Value).Take(crushCount).ToList();

        foreach (var dice in topDice)
        {
            int newValue = Mathf.Max(1, dice.Value - crushAmount);

            if (dice.Value != newValue)
            {
                dice.UpdateVisual(newValue);
            }
        }

        // 짓누르기 텍스트 표시
        if (topDice.Count > 0)
        {
            string crushText = LocalizationManager.Instance?.GetText("COMBAT_CRUSH") ?? "짓누름!";
            EffectManager.Instance.ShowText(transform, crushText, new Color(0.6f, 0.4f, 0.2f)); // 갈색
        }

        // 분노 상태일 때 재생
        if (isEnraged && currentHP < maxHP)
        {
            int healAmount = Mathf.Max(1, maxHP * enrageHealPercent / 100);
            currentHP = Mathf.Min(currentHP + healAmount, maxHP);
            UpdateUI();
            EffectManager.Instance.ShowHeal(transform, healAmount);
        }
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_SWAMPGIANT");
    }
}
