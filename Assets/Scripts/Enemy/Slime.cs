using UnityEngine;
using System.Collections.Generic;

// 슬라임 (평원 몬스터) 피격 시 50% 확률로 주사위 1개를 1턴 동안 잠급니다.
public class Slime : Enemy
{
    // 주사위 필터링
    private List<Dice> _cachedAvailableDice = new List<Dice>();

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
                _cachedAvailableDice.Clear();
                for (int i = 0; i < activeDice.Count; i++)
                {
                    if (activeDice[i].State == DiceState.Normal)
                    {
                        _cachedAvailableDice.Add(activeDice[i]);
                    }
                }

                if (_cachedAvailableDice.Count > 0)
                {
                    int randomIdx = activeDice.IndexOf(_cachedAvailableDice[Random.Range(0, _cachedAvailableDice.Count)]);
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