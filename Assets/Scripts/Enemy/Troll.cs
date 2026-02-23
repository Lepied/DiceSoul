using UnityEngine;
using System.Collections.Generic; // List<int>

public class Troll : Enemy
{
    private int regenerationAmount = 10; // 턴 당 회복량


    // 기믹
    // 플레이어가 주사위를 굴릴 때마다(매 턴) 체력을 회복
    public override void OnPlayerRoll(List<int> diceValues)
    {
        if (isDead) return;

        currentHP = Mathf.Min(currentHP + regenerationAmount, maxHP);

        EffectManager.Instance.ShowHeal(transform, regenerationAmount);
        UpdateUI();
    }

    // 보스가 죽을 때 추가 점수
    protected override void OnDeath()
    {
        Debug.Log($"{enemyName}이(가) 처치되었습니다! 보스 클리어 보너스!");

        // GameManager에 추가 점수 부여
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(500, GoldSource.Bonus); // 보스 보너스 500점
        }

        // 부모의 OnDeath()를 호출하여 오브젝트를 풀로 반환(비활성화)
        base.OnDeath();
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_TROLL");
    }

}
