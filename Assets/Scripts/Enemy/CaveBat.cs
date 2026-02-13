using UnityEngine;

public class CaveBat : Enemy
{
    [Header("동굴 박쥐 기믹")]
    [Range(0, 1)]
    public float dodgeChance = 0.5f; // 50%

    /// [기믹 1: 회피]
    /// '총합' 족보의 데미지를 50% 확률로 무시합니다.
    public override int CalculateDamageTaken(AttackHand hand)
    {
        // "총합" 족보이고, 50% 확률에 당첨되면
        if (hand.Description.Contains("총합") && Random.value < dodgeChance)
        {
            string text = LocalizationManager.Instance?.GetText("COMBAT_DODGE") ?? "회피!";
            EffectManager.Instance.ShowText(transform, text, Color.cyan);
            return 0; // 데미지 무시
        }

        return base.CalculateDamageTaken(hand);
    }
}