using UnityEngine;
using System.Collections.Generic;

// 서리 위스프
// 기믹- 피격 시 50% 확률로 주사위 1개를 1턴 동안 잠급니다.

public class FrostWisp : Enemy
{
    [Header("서리 위습 기믹")]
    [Range(0, 1)]
    public float freezeChance = 0.5f;
    
    // 주사위 필터링용
    private List<Dice> _cachedAvailableDice = new List<Dice>(); 

    // 피격 시, 50% 확률로 플레이어의 주사위 1개를 얼려
    public override void OnDamageTaken(int damageTaken, AttackHand hand)
    {
        base.OnDamageTaken(damageTaken, hand);
        if(hand == null) return;
        if (isDead) return;

        if (Random.value < freezeChance) 
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
                    string freezeText = LocalizationManager.Instance != null 
                        ? LocalizationManager.Instance.GetText("ENEMY_EFFECT_FREEZE") 
                        : "빙결!";
                    EffectManager.Instance.ShowText(transform, freezeText, Color.cyan);
                }
            }
        }
    }
    

    public override string GetGimmickDescription()
    {
        string baseDesc = LocalizationManager.Instance.GetText("ENEMY_GIMMICK_FROSTWISP");
        
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