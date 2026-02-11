using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 리치 (Zone 2: 묘지 보스)
/// '언데드' 타입, 'isBoss' = true
/// [기믹 1]: OnPlayerRoll - 굴린 주사위의 '최대값'만큼 체력 회복
/// [기믹 2]: OnWaveStart - '스켈레톤' 2마리 소환
/// </summary>
public class Lich : Enemy
{
    [Header("리치 기믹")]
    [Tooltip("OnWaveStart 시 소환할 부하 프리팹 (Skeleton_Prefab)")]
    public GameObject minionPrefab;
    public int minionsToSpawn = 2;

    // 
    // maxHP: 300, enemyType: Undead, isBoss: true,
    //     difficultyCost: 100, minZoneLevel: 2

    // [보스 기믹 1: 영혼 흡수]
    // 플레이어가 주사위를 굴릴 때마다, 굴린 주사위의 '최대값'만큼 체력을 회복합니다.

    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues); 
        
        if (isDead || diceValues == null || diceValues.Count == 0) return;

        // 굴린 주사위 중 가장 높은 값만큼 회복
        int healAmount = diceValues.Max(); 
        currentHP = Mathf.Min(currentHP + healAmount, maxHP);

        EffectManager.Instance.ShowHeal(transform, healAmount);

        Debug.Log($"{enemyName}이(가) [영혼 흡수]로 체력을 {healAmount} 회복합니다! (현재: {currentHP})");
        UpdateUI(); 
    }

    /// <summary>
    /// [보스 기믹 2: 부하 소환]
    /// 웨이브 시작 시 (스폰 시) 'minionPrefab'을 2마리 소환합니다.
    /// </summary>
    public override void OnWaveStart(List<Enemy> allies)
    {
        base.OnWaveStart(allies); 

        if (isDead) return;

        if (StageManager.Instance != null && minionPrefab != null)
        {
            Debug.Log($"{enemyName}이(가) [부하 소환]을 시전!");
            
            // StageManager의 새 헬퍼 함수를 호출하여 부하를 스폰
            StageManager.Instance.SpawnEnemiesForBoss(minionPrefab, minionsToSpawn);
        }
    }

    /// <summary>
    /// [기믹 3: 언데드 타입]
    /// (Skeleton.cs의 내성/약점 로직과 동일)
    /// </summary>
    public override int CalculateDamageTaken(AttackHand hand)
    {
        int baseDamage = hand.BaseDamage;
        string handDesc = hand.Description; 

        // 고급 족보 150%
        if (handDesc.Contains("트리플") || 
            handDesc.Contains("포카드") || 
            handDesc.Contains("풀 하우스") || 
            handDesc.Contains("야찌"))
        {
            Debug.Log("언데드: [고급 족보]에 치명타! (150% 데미지)");
            return (int)(baseDamage * 1.5f);
        }
        else
        {
            // 그 외 50%
            Debug.Log("언데드: [기본 족보] 피해를 50% 감소시킵니다.");
            return baseDamage / 2; 
        }
    }
    
    protected override void OnDeath()
    {
        Debug.Log($"{enemyName}이(가) 처치되었습니다! 보스 클리어 보너스!");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(1000); // 보스 보너스 1000점
        }
        
        base.OnDeath(); 
    }
}