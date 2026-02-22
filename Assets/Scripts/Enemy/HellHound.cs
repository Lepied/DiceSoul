using UnityEngine;

//지옥견
//기믹 '홀수' 족보로 피격 시 플레이어에게 2 데미지 반사
public class Hellhound : Enemy
{
    [Header("지옥견 기믹")]
    public int burnDamage = 2;

    public override void OnDamageTaken(int damageTaken, AttackHand hand)
    {
        base.OnDamageTaken(damageTaken, hand);
        if (hand == null) return;
        if (isDead) return;

        // '홀수, 짝수' 족보로 피격 시, 플레이어에게 데미지
        if (hand.Description.Contains("홀수") || hand.Description.Contains("짝수"))
        {
            if (GameManager.Instance != null)
            {
                string text = LocalizationManager.Instance?.GetText("COMBAT_HELLFIRE") ?? "지옥불!";
                EffectManager.Instance.ShowText(transform, text, Color.red);
                GameManager.Instance.DamagePlayer(burnDamage, "지옥견");
            }
        }
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_HELLHOUND");
    }
}