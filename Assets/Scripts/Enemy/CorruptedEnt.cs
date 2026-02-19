using UnityEngine;
using DG.Tweening;

//부패한 엔트
public class CorruptedEnt : Enemy
{

    [Range(0, 1)]
    public float reviveHealthPercent = 0.3f; // 30%

    private bool hasRevived = false;

    //기믹
    // 처음 죽을 때 최대 HP의 30%로 부활
    protected override void OnDeath()
    {
        if (!hasRevived)
        {
            // 부활
            hasRevived = true;
            isDead = false;

            currentHP = Mathf.Max(1, Mathf.RoundToInt(maxHP * reviveHealthPercent));
            UpdateUI();

            string text = LocalizationManager.Instance?.GetText("COMBAT_REVIVE") ?? "뿌리 재생!";
            EffectManager.Instance.ShowText(transform, text, Color.green);

            // 재생 이펙트
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color originalColor = sr.color;
                sr.color = Color.green;
                DOTween.To(() => sr.color, x => sr.color = x, originalColor, 0.5f);
            }
            return;
        }

        base.OnDeath();
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_CORRUPTEDENT");
    }
}
