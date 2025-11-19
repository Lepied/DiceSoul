using UnityEngine;
using System.Collections.Generic;


// [기믹]: OnPlayerRoll - 플레이어가 주사위를 굴릴 때마다 30% 확률로 1의 피해
public class GoblinArcher : Enemy
{
    [Header("고블린 궁수 기믹")]
    [Range(0, 1)]
    public float attackChance = 0.3f; // 30%

    /// [기믹 1: 기회 공격]
    /// 플레이어가 주사위를 굴릴 때마다 30% 확률로 1의 피해를 줍니다.
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues); // 부모 로직 호출

        if (isDead) return;

        if (Random.value < attackChance)
        {
            if (GameManager.Instance != null)
            {
                Debug.Log($"{enemyName}의 [기회 공격]! 플레이어 1 데미지!");
                GameManager.Instance.HealPlayer(-1);
            }
        }
    }

}