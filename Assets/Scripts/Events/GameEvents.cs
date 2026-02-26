using System;
using System.Collections.Generic;

// 게임 내 모든 이벤트 정의
// 유물, 버프, 디버프 등이 이 이벤트를 구독하여 효과 발동
public static class GameEvents
{
    ///주사위 관련
    // 주사위 굴림 직후 
    public static event Action<RollContext> OnDiceRolled;
    
    // 재굴림 시
    public static event Action<RollContext> OnDiceRerolled;
    
    // ===== 공격/데미지 관련 =====
    // 공격 데미지 계산 전
    public static event Action<AttackContext> OnBeforeAttack;
    
    // 공격 후
    public static event Action<AttackContext> OnAfterAttack;
    
    // 족보 완성 시
    public static event Action<HandContext> OnHandComplete;
    

    /// 플레이어 상태
    // 플레이어 피격 전
    public static event Action<DamageContext> OnBeforePlayerDamaged;
    
    // 플레이어 피격 후
    public static event Action<DamageContext> OnAfterPlayerDamaged;
    
    // 플레이어 사망 시 (부활 체크)
    public static event Action<DeathContext> OnPlayerDeath;
    
    // 플레이어 회복 시
    public static event Action<HealContext> OnPlayerHeal;
    
    ///게임 흐름
    // 웨이브 시작
    public static event Action<WaveContext> OnWaveStart;
    
    // 웨이브 클리어
    public static event Action<WaveContext> OnWaveEnd;
    
    // 존 시작
    public static event Action<ZoneContext> OnZoneStart;
    
    // 존 클리어
    public static event Action<ZoneContext> OnZoneEnd;
    
    // 턴 시작
    public static event Action OnTurnStart;
    
    // 턴 종료
    public static event Action OnTurnEnd;
    
    /// 경제/상점
    // 골드 획득 시
    public static event Action<GoldContext> OnGoldGain;
    
    // 상점 새로고침 시
    public static event Action<ShopContext> OnShopRefresh;
    
    // 상점 구매 시
    public static event Action<ShopContext> OnShopPurchase;
    
    ///유물
    // 유물 획득 시
    public static event Action<RelicContext> OnRelicAcquire;
    
    // 이벤트 발생 메서드
    public static void RaiseDiceRolled(RollContext ctx) => OnDiceRolled?.Invoke(ctx);
    public static void RaiseDiceRerolled(RollContext ctx) => OnDiceRerolled?.Invoke(ctx);
    public static void RaiseBeforeAttack(AttackContext ctx) => OnBeforeAttack?.Invoke(ctx);
    public static void RaiseAfterAttack(AttackContext ctx) => OnAfterAttack?.Invoke(ctx);
    public static void RaiseHandComplete(HandContext ctx) => OnHandComplete?.Invoke(ctx);
    public static void RaiseBeforePlayerDamaged(DamageContext ctx) => OnBeforePlayerDamaged?.Invoke(ctx);
    public static void RaiseAfterPlayerDamaged(DamageContext ctx) => OnAfterPlayerDamaged?.Invoke(ctx);
    public static void RaisePlayerDeath(DeathContext ctx) => OnPlayerDeath?.Invoke(ctx);
    public static void RaisePlayerHeal(HealContext ctx) => OnPlayerHeal?.Invoke(ctx);
    public static void RaiseWaveStart(WaveContext ctx) => OnWaveStart?.Invoke(ctx);
    public static void RaiseWaveEnd(WaveContext ctx) => OnWaveEnd?.Invoke(ctx);
    public static void RaiseZoneStart(ZoneContext ctx) => OnZoneStart?.Invoke(ctx);
    public static void RaiseZoneEnd(ZoneContext ctx) => OnZoneEnd?.Invoke(ctx);
    public static void RaiseTurnStart() => OnTurnStart?.Invoke();
    public static void RaiseTurnEnd() => OnTurnEnd?.Invoke();
    public static void RaiseGoldGain(GoldContext ctx) => OnGoldGain?.Invoke(ctx);
    public static void RaiseShopRefresh(ShopContext ctx) => OnShopRefresh?.Invoke(ctx);
    public static void RaiseShopPurchase(ShopContext ctx) => OnShopPurchase?.Invoke(ctx);
    public static void RaiseRelicAcquire(RelicContext ctx) => OnRelicAcquire?.Invoke(ctx);
    
    // 모든 이벤트 구독 해제 
    public static void ClearAllEvents()
    {
        OnDiceRolled = null;
        OnDiceRerolled = null;
        OnBeforeAttack = null;
        OnAfterAttack = null;
        OnHandComplete = null;
        OnBeforePlayerDamaged = null;
        OnAfterPlayerDamaged = null;
        OnPlayerDeath = null;
        OnPlayerHeal = null;
        OnWaveStart = null;
        OnWaveEnd = null;
        OnZoneStart = null;
        OnZoneEnd = null;
        OnTurnStart = null;
        OnTurnEnd = null;
        OnGoldGain = null;
        OnShopRefresh = null;
        OnShopPurchase = null;
        OnRelicAcquire = null;
    }
}
