using UnityEngine;

/// <summary>
/// 슬라임 (평원 몬스터)
/// [기믹]: 피격 시 50% 확률로 주사위 1개를 강제 킵(Keep)시킵니다.
/// </summary>
public class Slime : Enemy
{
 
    public override void OnDamageTaken(int damageTaken, AttackJokbo jokbo)
    {
        
        if (isDead) return;

        // 50% 확률 (Random.value는 0.0f ~ 1.0f 사이의 값)
        if (Random.value < 0.5f) 
        {
           
            if (DiceController.Instance != null)
            {
                
                EffectManager.Instance.ShowText(transform.position, "끈끈이!", Color.green);
                DiceController.Instance.ForceKeepRandomDice();
            }
        }
    }

}