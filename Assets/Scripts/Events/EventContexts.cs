using System.Collections.Generic;

/// <summary>
/// 주사위 굴림 컨텍스트
/// </summary>
public class RollContext
{
    public int[] DiceValues;              // 주사위 값 배열
    public string[] DiceTypes;            // 주사위 타입 (D4, D6, D20 등)
    public bool IsFirstRoll;              // 첫 굴림인지
    public int RollCount;                 // 현재 몇 번째 굴림인지
    public List<int> RerollIndices;       // 재굴림할 인덱스 목록
    public bool CancelReroll;             // 재굴림 취소
    
    public void Reset()
    {
        DiceValues = null;
        DiceTypes = null;
        IsFirstRoll = false;
        RollCount = 0;
        RerollIndices?.Clear();
        CancelReroll = false;
    }
}

/// <summary>
/// 공격 컨텍스트
/// </summary>
public class AttackContext
{
    public AttackJokbo Jokbo;             // 사용된 족보
    public int BaseDamage;                // 기본 데미지
    public int FlatDamageBonus;           // 고정 데미지 추가
    public float DamageMultiplier = 1f;   // 데미지 배율
    public int FinalDamage;               // 최종 데미지 (계산 후)
    
    public int BaseGold;                  // 기본 골드
    public int FlatGoldBonus;             // 고정 골드 추가
    public float GoldMultiplier = 1f;     // 골드 배율
    public int FinalGold;                 // 최종 골드
    
    public bool IsFirstRoll;              // 첫 굴림 공격인지
    public bool IsFirstAttackThisWave;    // 웨이브의 첫 공격인지
    public int RemainingRolls;            // 남은 굴림 횟수
    public int HealAfterAttack;           // 공격 후 회복량
    public Enemy TargetEnemy;             // 공격 대상 적
    
    public void Reset()
    {
        Jokbo = null;
        BaseDamage = 0;
        FlatDamageBonus = 0;
        DamageMultiplier = 1f;
        FinalDamage = 0;
        BaseGold = 0;
        FlatGoldBonus = 0;
        GoldMultiplier = 1f;
        FinalGold = 0;
        IsFirstRoll = false;
        IsFirstAttackThisWave = false;
        RemainingRolls = 0;
        HealAfterAttack = 0;
        TargetEnemy = null;
    }
    
    /// <summary>
    /// 최종 값 계산
    /// </summary>
    public void Calculate()
    {
        FinalDamage = (int)((BaseDamage + FlatDamageBonus) * DamageMultiplier);
        FinalGold = (int)((BaseGold + FlatGoldBonus) * GoldMultiplier);
    }
    
    /// <summary>
    /// 최종 데미지 계산 후 반환
    /// </summary>
    public int CalculateFinalDamage()
    {
        return (int)((BaseDamage + FlatDamageBonus) * DamageMultiplier);
    }
    
    /// <summary>
    /// 최종 골드 계산 후 반환
    /// </summary>
    public int CalculateFinalGold()
    {
        return (int)((BaseGold + FlatGoldBonus) * GoldMultiplier);
    }
}

/// <summary>
/// 족보 컨텍스트
/// </summary>
public class JokboContext
{
    public AttackJokbo Jokbo;
    public int[] DiceValues;
    public bool ConsumeRoll = true;       // 굴림 횟수 소모 여부
    
    public void Reset()
    {
        Jokbo = null;
        DiceValues = null;
        ConsumeRoll = true;
    }
}

/// <summary>
/// 피해 컨텍스트
/// </summary>
public class DamageContext
{
    public int OriginalDamage;            // 원래 데미지
    public int FinalDamage;               // 최종 데미지 (감소 적용 후)
    public bool Cancelled;                // 데미지 무효화
    public string Source;                 // 데미지 출처
    
    public void Reset()
    {
        OriginalDamage = 0;
        FinalDamage = 0;
        Cancelled = false;
        Source = null;
    }
}

/// <summary>
/// 사망 컨텍스트
/// </summary>
public class DeathContext
{
    public bool Revived;                  // 부활했는지
    public int ReviveHP;                  // 부활 시 체력
    public string ReviveSource;           // 부활 원인 (유물 ID 등)
    
    public void Reset()
    {
        Revived = false;
        ReviveHP = 0;
        ReviveSource = null;
    }
}

/// <summary>
/// 회복 컨텍스트
/// </summary>
public class HealContext
{
    public int Amount;
    public string Source;
    public bool Cancelled;                // 회복 불가 (악마의 계약서)
    
    public void Reset()
    {
        Amount = 0;
        Source = null;
        Cancelled = false;
    }
}

/// <summary>
/// 웨이브 컨텍스트
/// </summary>
public class WaveContext
{
    public int ZoneNumber;
    public int WaveNumber;
    public bool IsBossWave;
    public int UnusedJokboCount;          // 미사용 족보 수 (학자의 서적용)
    
    public void Reset()
    {
        ZoneNumber = 0;
        WaveNumber = 0;
        IsBossWave = false;
        UnusedJokboCount = 0;
    }
}

/// <summary>
/// 존 컨텍스트
/// </summary>
public class ZoneContext
{
    public int ZoneNumber;
    public string ZoneName;               // 존 이름
    
    public void Reset()
    {
        ZoneNumber = 0;
        ZoneName = null;
    }
}

/// <summary>
/// 골드 컨텍스트
/// </summary>
public class GoldContext
{
    public int BaseAmount;
    public float Multiplier = 1f;
    public int FinalAmount;
    public string Source;
    
    public void Reset()
    {
        BaseAmount = 0;
        Multiplier = 1f;
        FinalAmount = 0;
        Source = null;
    }
    
    public void Calculate()
    {
        FinalAmount = (int)(BaseAmount * Multiplier);
    }
}

/// <summary>
/// 상점 컨텍스트
/// </summary>
public class ShopContext
{
    public int RefreshCost;
    public float PriceMultiplier = 1f;    // 가격 배율 (할인 등)
    public bool FreeRefresh;              // 무료 새로고침
    
    public void Reset()
    {
        RefreshCost = 0;
        PriceMultiplier = 1f;
        FreeRefresh = false;
    }
}

/// <summary>
/// 유물 컨텍스트
/// </summary>
public class RelicContext
{
    public RelicData RelicData;
    public string RelicID;
    public string RelicName;              // 유물 이름
    
    public void Reset()
    {
        RelicData = null;
        RelicID = null;
        RelicName = null;
    }
}
