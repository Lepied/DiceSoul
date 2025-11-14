using UnityEngine;

/// <summary>
/// 슬라임 (평원 몬스터)
/// [기믹]: 피격 시 50% 확률로 주사위 1개를 강제 킵(Keep)시킵니다.
/// </summary>
public class Slime : Enemy
{
    // (스탯은 인스펙터에서 설정)

    /// <summary>
    /// [슬라임 기믹: 끈끈이]
    /// 피격 시, 50% 확률로 플레이어의 주사위 1개를
    /// 다음 굴림까지 강제로 '킵(Keep)' 상태로 만듭니다.
    /// </summary>
    public override void OnDamageTaken(int damageTaken, AttackJokbo jokbo)
    {
        
        if (isDead) return;

        // 50% 확률 (Random.value는 0.0f ~ 1.0f 사이의 값)
        if (Random.value < 0.5f) 
        {
            // DiceController의 싱글톤 인스턴스에 접근
            if (DiceController.Instance != null)
            {
                Debug.Log($"{enemyName}이(가) '끈끈이' 효과를 발동!");
                // DiceController에 새 함수 호출
                DiceController.Instance.ForceKeepRandomDice();
            }
        }
    }

}