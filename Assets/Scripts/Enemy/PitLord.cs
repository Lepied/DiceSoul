using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// (신규 파일) 핏 로드 (Zone 3: 악마의 성채 보스)
/// [기믹]: 강화 - 매 턴(굴림 시) '총합' 족보에 대한 저항력이 10%씩 증가 (최대 50%)
/// (스탯은 인스펙터에서 설정: Armored, Boss=true)
/// </summary>
public class PitLord : Enemy
{
    private float currentResistance = 0f; // 현재 추가 저항 수치 (0.0 ~ 0.5)

    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);

        if (isDead) return;

        // 매 턴 저항력 10% 증가 (최대 50%)
        if (currentResistance < 0.5f)
        {
            currentResistance += 0.1f;
            Debug.Log($"{enemyName}의 피부가 단단해집니다! (총합 저항: {currentResistance * 100:F0}%)");
            EffectManager.Instance.ShowText(transform.position, "강화", Color.grey);
        }
    }

    public override int CalculateDamageTaken(AttackJokbo jokbo)
    {
        int baseDmg = jokbo.BaseDamage;

        // '총합' 족보일 경우, 추가 저항력 적용
        if (jokbo.Description.Contains("총합"))
        {
            // 기본 Armored는 50% 데미지만 받음. 여기에 추가 저항 적용
            
            float multiplier = 0.5f - currentResistance; 
            if (multiplier < 0) multiplier = 0;

            return (int)(baseDmg * multiplier);
        }

        
        return base.CalculateDamageTaken(jokbo);
    }

    protected override void OnDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(2000); // 보스 클리어 보너스
        }
        base.OnDeath();
    }
}