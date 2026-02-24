using UnityEngine;
using System.Linq;

/// <summary>
/// 슬라임 (평원 몬스터)
/// [기믹]: 피격 시 50% 확률로 주사위 1개를 1턴 동안 잠급니다.
/// </summary>
public class Slime : Enemy
{

    public override void OnDamageTaken(int damageTaken, AttackHand hand)
    {
        base.OnDamageTaken(damageTaken, hand);
        if (hand == null) return;
        if (isDead) return;

        // 50% 확률 (Random.value는 0.0f ~ 1.0f 사이의 값)
        if (Random.value < 0.5f)
        {
            if (DiceController.Instance != null)
            {
                var activeDice = DiceController.Instance.activeDice;
                var availableDice = activeDice.Where(d => d.State == DiceState.Normal).ToList();

                if (availableDice.Count > 0)
                {
                    int randomIdx = activeDice.IndexOf(availableDice[Random.Range(0, availableDice.Count)]);
                    DiceController.Instance.LockDice(randomIdx, 1); // 1턴 동안 잠금
                    string text = LocalizationManager.Instance?.GetText("COMBAT_STICKY") ?? "끈끈이!";
                    EffectManager.Instance.ShowText(transform, text, Color.green);
                    Debug.Log($"[{enemyName}] 주사위 1개를 끈끈하게 만들었습니다!");
                }
            }
        }
    }

    public override string GetGimmickDescription()
    {
        string baseDesc = LocalizationManager.Instance.GetText("ENEMY_GIMMICK_SLIME");
        
        // 잠금 설명 추가
        if (LocalizationManager.Instance != null)
        {
            string lockTitle = LocalizationManager.Instance.GetText("MECHANIC_LOCK_TITLE");
            string lockDesc = LocalizationManager.Instance.GetText("MECHANIC_LOCK_DESC");
            baseDesc += $"\n\n{lockTitle}: {lockDesc}";
        }
        
        return baseDesc;
    }
}