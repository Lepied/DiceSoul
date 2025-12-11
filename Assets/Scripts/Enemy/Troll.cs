using UnityEngine;
using System.Collections.Generic; // List<int>

/// <summary>
/// 트롤 (첫 번째 보스)
/// 생체(Biological) 타입이며, isBoss 플래그가 True입니다.
/// [보스 기믹]: 매 턴(플레이어 굴림 시) 체력을 회복합니다.
/// </summary>
public class Troll : Enemy
{
    private int regenerationAmount = 10; // 턴 당 회복량

    /*
    protected override void SetupStats()
    {
        this.enemyName = "트롤";
        this.maxHP = 200; // 보스 체력
        this.enemyType = EnemyType.Biological; // 생체 타입
        this.isBoss = true; // [!!!] 보스 플래그
        this.difficultyCost = 50; // 높은 난이도 비용
        this.minZoneLevel = 1; // 1존의 마지막 웨이브(예: 5)에 등장
    }
    */

    /// <summary>
    /// [보스 기믹 1: 재생]
    /// 플레이어가 주사위를 굴릴 때마다(매 턴) 체력을 회복합니다.
    /// </summary>
    public override void OnPlayerRoll(List<int> diceValues)
    {
        if (isDead) return;

        currentHP = Mathf.Min(currentHP + regenerationAmount, maxHP);
        
        EffectManager.Instance.ShowHeal(transform, regenerationAmount);
        UpdateUI(); // (UpdateUI는 부모(Enemy.cs)의 protected 함수)
    }

    /// <summary>
    /// [보스 기믹 2: 보상]
    /// 보스가 죽을 때 추가 점수를 줍니다.
    /// </summary>
    protected override void OnDeath()
    {
        Debug.Log($"{enemyName}이(가) 처치되었습니다! 보스 클리어 보너스!");
        
        // GameManager에 추가 점수 부여
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(500); // 보스 보너스 500점
        }
        
        // 부모의 OnDeath()를 호출하여 오브젝트를 풀로 반환(비활성화)
        base.OnDeath(); 
    }

}
