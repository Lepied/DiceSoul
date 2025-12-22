using System.Collections.Generic;

// 주사위 굴림 컨텍스트
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

// 공격 컨텍스트

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
    

    // 최종 값 계산
    public void Calculate()
    {
        FinalDamage = (int)((BaseDamage + FlatDamageBonus) * DamageMultiplier);
        FinalGold = (int)((BaseGold + FlatGoldBonus) * GoldMultiplier);
    }
    

    // 최종 데미지 계산 후 반환
    public int CalculateFinalDamage()
    {
        return (int)((BaseDamage + FlatDamageBonus) * DamageMultiplier);
    }
    

    // 최종 골드 계산 후 반환
    public int CalculateFinalGold()
    {
        return (int)((BaseGold + FlatGoldBonus) * GoldMultiplier);
    }
}

// 족보 컨텍스트
public class JokboContext
{
    public AttackJokbo Jokbo;             // 선택된 족보
    public List<AttackJokbo> AchievedJokbos; // 달성한 족보 목록
    public int[] DiceValues;              // 주사위 값들 (int[])
    public List<int> DiceValuesList;      // 주사위 값들 (List<int>)
    public bool ConsumeRoll = true;       // 굴림 횟수 소모 여부
    public int BonusDamage;               // 추가 데미지
    public int BonusGold;                 // 추가 골드
    
    public void Reset()
    {
        Jokbo = null;
        AchievedJokbos = null;
        DiceValues = null;
        DiceValuesList = null;
        ConsumeRoll = true;
        BonusDamage = 0;
        BonusGold = 0;
    }
}

// 피해 컨텍스트

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

// 사망 컨텍스트
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

// 회복 컨텍스트
public class HealContext
{
    public int OriginalAmount;            // 원래 회복량
    public int FinalAmount;               // 최종 회복량
    public int Amount;                    // (deprecated) FinalAmount 사용
    public string Source;                 // 회복 출처
    public bool Cancelled;                // 회복 불가 (악마의 계약서)
    
    public void Reset()
    {
        OriginalAmount = 0;
        FinalAmount = 0;
        Amount = 0;
        Source = null;
        Cancelled = false;
    }
}

// 웨이브 컨텍스트
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

// 존 컨텍스트
public class ZoneContext
{
    public int ZoneNumber;
    public string ZoneName;        // 존 이름
    
    public void Reset()
    {
        ZoneNumber = 0;
        ZoneName = null;
    }
}

// 골드 컨텍스트
public class GoldContext
{
    public int OriginalAmount;            // 원래 골드량
    public int BaseAmount;                // 기본 골드량 (OriginalAmount와 동일)
    public float Multiplier = 1f;         // 골드 배율
    public int FinalAmount;               // 최종 골드량
    public string Source;                 // 골드 출처 (Jokbo, Bonus 등)
    
    public void Reset()
    {
        OriginalAmount = 0;
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

// 상점 컨텍스트
public class ShopContext
{
    public System.Collections.Generic.List<ShopItem> Items; // 현재 상점 아이템 목록
    public int RerollCost;               // 리롤 비용
    public int RefreshCost;              // 새로고침 비용 (RerollCost와 동일)
    public float PriceMultiplier = 1f;   // 가격 배율 (할인 등)
    public bool FreeRefresh;             // 무료 새로고침
    public string PurchasedItemName;     // 구매한 아이템 이름
    public int PurchasedItemPrice;       // 구매한 아이템 가격
    
    public void Reset()
    {
        Items = null;
        RerollCost = 0;
        RefreshCost = 0;
        PriceMultiplier = 1f;
        FreeRefresh = false;
        PurchasedItemName = null;
        PurchasedItemPrice = 0;
    }
}


//유물 컨텍스트
public class RelicContext
{
    public RelicData RelicData;           // SO 참조 (새 시스템)
    public Relic Relic;                   // 유물 객체 참조 (레거시 호환)
    public string RelicID;
    public string RelicName;              // 유물 이름
    
    public void Reset()
    {
        RelicData = null;
        Relic = null;
        RelicID = null;
        RelicName = null;
    }
}
