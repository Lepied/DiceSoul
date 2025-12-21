// 유물 관련 Enum 정의

public enum RelicRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}

public enum RelicDropPool
{
    WaveReward,         // 일반 웨이브 보상
    ShopOnly,           // 상점 전용
    MaintenanceReward   // 정비 단계 보상
}

public enum RelicCategory
{
    Utility,            // 유틸리티 (굴림 횟수, 리롤 등)
    CharacterStat,      // 캐릭터 스탯 (데미지, 체력 등)
    JokboSpecific,      // 족보 특화
    DiceRelated,        // 주사위 관련 (변환, 재굴림 등)
    Economy,            // 경제 관련 (골드, 상점 등)
    Survival            // 생존 유틸리티 (회복, 부활 등)
}

// 유물 효과가 발동하는 타이밍
public enum RelicTriggerTiming
{
    Passive,            // 항상 적용 (스탯 증가 등)
    OnAcquire,          // 획득 시 1회
    OnWaveStart,        // 웨이브 시작 시
    OnRoll,             // 주사위 굴림 후
    OnReroll,           // 재굴림 시
    OnBeforeAttack,     // 공격 계산 전
    OnAfterAttack,      // 공격 후
    OnJokboComplete,    // 족보 완성 시
    OnTakeDamage,       // 피격 시
    OnPlayerDeath,      // 사망 시
    OnShopRefresh,      // 상점 새로고침 시
    OnZoneStart,        // 존 시작 시
    Manual              // 수동 발동 (UI 버튼)
}
