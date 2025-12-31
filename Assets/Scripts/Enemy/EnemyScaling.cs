using UnityEngine;

/// <summary>
/// 적 스탯 스케일링 시스템
/// 존/웨이브 진행도 + 플레이어 파워에 따라 적의 HP와 공격력을 동적으로 조정합니다.
/// </summary>
public static class EnemyScaling
{
    // ===== 존별 기본 배율 테이블 =====
    // Zone 1~5까지의 HP/공격력 기본 배율
    private static readonly float[] zoneHPMultipliers = { 1.0f, 1.8f, 3.2f, 5.5f, 8.5f };
    private static readonly float[] zoneDamageMultipliers = { 1.0f, 1.4f, 2.0f, 2.8f, 3.6f };
    
    // ===== 웨이브별 증가율 =====
    private const float waveHPStep = 0.10f;      // 웨이브당 +10% HP
    private const float waveDamageStep = 0.05f;  // 웨이브당 +5% 공격력
    
    // ===== 플레이어 파워 보정 계수 =====
    private const float diceHPBonus = 0.15f;      // 주사위 1개당 +15% HP
    private const float diceDamageBonus = 0.05f;  // 주사위 1개당 +5% 공격력
    private const float rerollHPBonus = 0.10f;    // 리롤 1회당 +10% HP
    private const float rerollDamageBonus = 0.03f;// 리롤 1회당 +3% 공격력
    private const float relicHPBonus = 0.03f;     // 유물 1개당 +3% HP
    private const float relicDamageBonus = 0.01f; // 유물 1개당 +1% 공격력
    
    // ===== 기준값 (플레이어 초기 스펙) =====
    private const int baseDiceCount = 5;
    private const int baseRerollCount = 3;
    
    /// <summary>
    /// 스케일링 적용된 최종 HP 계산
    /// </summary>
    /// <param name="baseHP">적의 기본 HP (인스펙터 설정값)</param>
    /// <param name="zone">현재 존 번호 (1~5)</param>
    /// <param name="wave">현재 웨이브 번호 (1~5)</param>
    /// <param name="isBoss">보스 몬스터 여부</param>
    /// <returns>스케일링 적용된 최종 HP</returns>
    public static int GetScaledHP(int baseHP, int zone, int wave, bool isBoss = false)
    {
        // 1. 존 기본 배율
        float zoneMult = GetZoneHPMultiplier(zone);
        
        // 2. 웨이브 누적 배율
        float waveMult = 1.0f + (wave - 1) * waveHPStep;
        
        // 3. 플레이어 파워 보정
        float playerPowerMult = GetPlayerPowerMultiplier(true);
        
        // 4. 보스 보정 (기본 스케일링만 적용)
        float bossMult = 1.0f; // 보스도 일반 적과 동일한 스케일링
        
        // 5. 최종 계산
        float finalHP = baseHP * zoneMult * waveMult * playerPowerMult * bossMult;
        
        int result = Mathf.RoundToInt(finalHP);
        
        // 디버그 로그 (개발 중 밸런스 확인용)
        if (Application.isEditor)
        {
            Debug.Log($"[EnemyScaling] HP: {baseHP} → {result} " +
                      $"(Zone×{zoneMult:F2} Wave×{waveMult:F2} Power×{playerPowerMult:F2} Boss×{bossMult:F2})");
        }
        
        return Mathf.Max(1, result); // 최소 1
    }
    
    /// <summary>
    /// 스케일링 적용된 최종 공격력 계산
    /// </summary>
    public static int GetScaledDamage(int baseDamage, int zone, int wave, bool isBoss = false)
    {
        float zoneMult = GetZoneDamageMultiplier(zone);
        float waveMult = 1.0f + (wave - 1) * waveDamageStep;
        float playerPowerMult = GetPlayerPowerMultiplier(false);
        float bossMult = 1.0f; // 보스도 일반 적과 동일한 스케일링
        
        float finalDamage = baseDamage * zoneMult * waveMult * playerPowerMult * bossMult;
        
        return Mathf.Max(1, Mathf.RoundToInt(finalDamage));
    }
    
    // ===== 내부 헬퍼 함수 =====
    
    private static float GetZoneHPMultiplier(int zone)
    {
        int index = Mathf.Clamp(zone - 1, 0, zoneHPMultipliers.Length - 1);
        return zoneHPMultipliers[index];
    }
    
    private static float GetZoneDamageMultiplier(int zone)
    {
        int index = Mathf.Clamp(zone - 1, 0, zoneDamageMultipliers.Length - 1);
        return zoneDamageMultipliers[index];
    }
    
    private static float GetPlayerPowerMultiplier(bool forHP)
    {
        // GameManager에서 플레이어 정보 가져오기
        if (GameManager.Instance == null) return 1.0f;
        
        int currentDiceCount = GameManager.Instance.playerDiceDeck.Count;
        int currentRelicCount = GameManager.Instance.activeRelics.Count;
        
        // DiceController에서 최대 리롤 횟수 가져오기
        int currentMaxRerolls = 3; // 기본값
        if (DiceController.Instance != null)
        {
            currentMaxRerolls = DiceController.Instance.maxRolls;
        }
        
        // 플레이어 파워 요소별 배율 계산
        float diceFactor = 1.0f + Mathf.Max(0, currentDiceCount - baseDiceCount) * (forHP ? diceHPBonus : diceDamageBonus);
        float rerollFactor = 1.0f + Mathf.Max(0, currentMaxRerolls - baseRerollCount) * (forHP ? rerollHPBonus : rerollDamageBonus);
        float relicFactor = 1.0f + currentRelicCount * (forHP ? relicHPBonus : relicDamageBonus);
        
        return diceFactor * rerollFactor * relicFactor;
    }
    
    /// <summary>
    /// 디버그용: 현재 플레이어 파워 정보 출력
    /// </summary>
    public static void LogPlayerPowerStatus()
    {
        if (GameManager.Instance == null) return;
        
        int diceCount = GameManager.Instance.playerDiceDeck.Count;
        int relicCount = GameManager.Instance.activeRelics.Count;
        int maxRerolls = DiceController.Instance != null ? DiceController.Instance.maxRolls : 3;
        
        float hpMult = GetPlayerPowerMultiplier(true);
        float dmgMult = GetPlayerPowerMultiplier(false);
        
        Debug.Log($"[PlayerPower] 주사위: {diceCount}, 리롤: {maxRerolls}, 유물: {relicCount} " +
                  $"→ 적 HP배율: {hpMult:F2}x, 공격력배율: {dmgMult:F2}x");
    }
}
