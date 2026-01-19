using UnityEngine;
using System.Linq;

/// <summary>
/// (신규 파일) 서리 위스프 (Zone 3: 얼음 동굴 일반 몹)
/// 대체: IceMephit -> FrostWisp
/// [기믹]: OnDamageTaken - 피격 시 50% 확률로 주사위 1개를 1턴 동안 잠급니다.
/// (스탯은 인스펙터에서 설정: Spirit 타입)
/// </summary>
public class FrostWisp : Enemy
{
    [Header("서리 위스프 기믹")]
    [Range(0, 1)]
    public float freezeChance = 0.5f; 

    /// <summary>
    /// [기믹: 빙결]
    /// 피격 시, 50% 확률로 플레이어의 주사위 1개를 얼려서(잠금)
    /// 족보 계산에 포함되지 않게 만듭니다.
    /// </summary>
    public override void OnDamageTaken(int damageTaken, AttackJokbo jokbo)
    {
        base.OnDamageTaken(damageTaken, jokbo);
        if(jokbo == null) return;
        if (isDead) return;

        if (Random.value < freezeChance) 
        {
            if (DiceController.Instance != null)
            {
                var activeDice = DiceController.Instance.activeDice;
                // Normal 상태인 주사위만 선택 가능
                var availableDice = activeDice.Where(d => d.State == DiceState.Normal).ToList();
                
                if (availableDice.Count > 0)
                {
                    int randomIdx = activeDice.IndexOf(availableDice[Random.Range(0, availableDice.Count)]);
                    DiceController.Instance.LockDice(randomIdx, 1); // 1턴 동안 잠금
                    EffectManager.Instance.ShowText(transform, "빙결!", Color.cyan);
                    Debug.Log($"[{enemyName}] '빙결' 효과 발동! 주사위가 얼어붙습니다.");
                }
            }
        }
    }
    
    // CalculateDamageTaken은 부모(Spirit 타입) 로직을 따름
    // (물리 면역, 마법 약점)
}