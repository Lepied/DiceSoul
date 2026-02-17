using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 유물 효과 중앙 처리 핸들러
// 모든 게임 이벤트를 구독하고, 보유 유물에 따라 효과 적용
public class RelicEffectHandler : MonoBehaviour
{
    public static RelicEffectHandler Instance { get; private set; }

    // 상태 추적 (1회성 효과용)
    private bool phoenixFeatherUsed = false;
    private bool smallShieldUsedThisZone = false;
    
    // 학자의 서적: 영구 데미지 성장 (미사용 족보당 +1)
    private int scholarsTomeBonusDamage = 0;
    
    // 수동 유물 사용 횟수 추적
    private bool diceCupUsedThisWave = false;
    private bool doubleDiceUsedThisWave = false;
    private bool fateDiceUsedThisZone = false; // 존당 1회로 변경
    private int preserveChargesRemaining = 0; // 주사위 보존 남은 기회

    // 캐싱된 Context (GC 방지)
    private RollContext cachedRollContext = new RollContext();
    private AttackContext cachedAttackContext = new AttackContext();
    private DamageContext cachedDamageContext = new DamageContext();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // 이벤트 구독
        GameEvents.OnDiceRolled += HandleDiceRolled;
        GameEvents.OnDiceRerolled += HandleDiceRerolled;
        GameEvents.OnBeforeAttack += HandleBeforeAttack;
        GameEvents.OnAfterAttack += HandleAfterAttack;
        GameEvents.OnHandComplete += HandleHandComplete;
        GameEvents.OnBeforePlayerDamaged += HandleBeforePlayerDamaged;
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
        GameEvents.OnWaveStart += HandleWaveStart;
        GameEvents.OnWaveEnd += HandleWaveEnd;
        GameEvents.OnZoneStart += HandleZoneStart;
        GameEvents.OnZoneEnd += HandleZoneEnd;
        GameEvents.OnTurnStart += HandleTurnStart;
        GameEvents.OnGoldGain += HandleGoldGain;
        GameEvents.OnShopRefresh += HandleShopRefresh;
        GameEvents.OnRelicAcquire += HandleRelicAcquire;
        GameEvents.OnPlayerHeal += HandlePlayerHeal;
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        GameEvents.OnDiceRolled -= HandleDiceRolled;
        GameEvents.OnDiceRerolled -= HandleDiceRerolled;
        GameEvents.OnBeforeAttack -= HandleBeforeAttack;
        GameEvents.OnAfterAttack -= HandleAfterAttack;
        GameEvents.OnHandComplete -= HandleHandComplete;
        GameEvents.OnBeforePlayerDamaged -= HandleBeforePlayerDamaged;
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
        GameEvents.OnWaveStart -= HandleWaveStart;
        GameEvents.OnWaveEnd -= HandleWaveEnd;
        GameEvents.OnZoneStart -= HandleZoneStart;
        GameEvents.OnZoneEnd -= HandleZoneEnd;
        GameEvents.OnTurnStart -= HandleTurnStart;
        GameEvents.OnGoldGain -= HandleGoldGain;
        GameEvents.OnShopRefresh -= HandleShopRefresh;
        GameEvents.OnRelicAcquire -= HandleRelicAcquire;
    }

    // ===== 헬퍼 메서드 =====

    private bool HasRelic(string relicID)
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.activeRelics.Any(r => r.RelicID == relicID);
    }

    private int GetRelicCount(string relicID)
    {
        if (GameManager.Instance == null) return 0;
        return GameManager.Instance.activeRelics.Count(r => r.RelicID == relicID);
    }

    private Relic GetRelic(string relicID)
    {
        if (GameManager.Instance == null) return null;
        return GameManager.Instance.activeRelics.FirstOrDefault(r => r.RelicID == relicID);
    }

    private List<Relic> GetActiveRelics()
    {
        return GameManager.Instance?.activeRelics ?? new List<Relic>();
    }

    // ===== 이벤트 핸들러 =====

    // 주사위 굴림 후 처리 (값 변환)
    private void HandleDiceRolled(RollContext ctx)
    {
        if (ctx.DiceValues == null) return;

        for (int i = 0; i < ctx.DiceValues.Length; i++)
        {
            // RLC_DIAMOND_DICE: 다이아몬드 주사위 - 최소값 2 (1이 나오면 2로)
            if (HasRelic("RLC_DIAMOND_DICE") && ctx.DiceValues[i] == 1)
            {
                ctx.DiceValues[i] = 2;
                Debug.Log("[유물] 다이아몬드 주사위: 1 → 2");
            }

            // RLC_ALCHEMY: 연금술사의 돌 - 1을 7로
            if (HasRelic("RLC_ALCHEMY") && ctx.DiceValues[i] == 1)
            {
                ctx.DiceValues[i] = 7;
                Debug.Log("[유물] 연금술사의 돌: 1 → 7");
            }

            // RLC_COUNTERWEIGHT: 균형추 - D20이 10 이하면 +5
            if (HasRelic("RLC_COUNTERWEIGHT") && ctx.DiceTypes != null)
            {
                if (ctx.DiceTypes[i] == "D20" && ctx.DiceValues[i] <= 10)
                {
                    ctx.DiceValues[i] += 5;
                    Debug.Log($"[유물] 균형추: D20 +5 → {ctx.DiceValues[i]}");
                }
            }
        }

        // RLC_LODESTONE: 자철석 - 홀수 재굴림
        if (HasRelic("RLC_LODESTONE"))
        {
            ctx.RerollIndices = ctx.RerollIndices ?? new List<int>();
            int oddCount = 0;
            for (int i = 0; i < ctx.DiceValues.Length; i++)
            {
                if (ctx.DiceValues[i] % 2 == 1 && !ctx.RerollIndices.Contains(i))
                {
                    ctx.RerollIndices.Add(i);
                    oddCount++;
                }
            }
        }

        // RLC_TANZANITE: 탄자나이트 - 짝수 재굴림
        if (HasRelic("RLC_TANZANITE"))
        {
            ctx.RerollIndices = ctx.RerollIndices ?? new List<int>();
            int evenCount = 0;
            for (int i = 0; i < ctx.DiceValues.Length; i++)
            {
                if (ctx.DiceValues[i] % 2 == 0 && !ctx.RerollIndices.Contains(i))
                {
                    ctx.RerollIndices.Add(i);
                    evenCount++;
                }
            }
        }

        // RLC_FEATHER: 가벼운 깃털 - 6 재굴림
        if (HasRelic("RLC_FEATHER"))
        {
            ctx.RerollIndices = ctx.RerollIndices ?? new List<int>();
            int sixCount = 0;
            for (int i = 0; i < ctx.DiceValues.Length; i++)
            {
                if (ctx.DiceValues[i] == 6 && !ctx.RerollIndices.Contains(i))
                {
                    ctx.RerollIndices.Add(i);
                    sixCount++;
                }
            }
        }

        // RLC_QUICK_RELOAD: 빠른 장전 - 첫 굴림 시 주사위 2개 재굴림
        if (HasRelic("RLC_QUICK_RELOAD") && ctx.IsFirstRoll)
        {
            ctx.RerollIndices = ctx.RerollIndices ?? new List<int>();
            var relic = GetRelic("RLC_QUICK_RELOAD");
            int rerollCount = relic?.IntValue ?? 2;
            
            // 가장 낮은 주사위들을 찾아서 재굴림
            var sortedIndices = ctx.DiceValues
                .Select((value, index) => new { value, index })
                .Where(x => !ctx.RerollIndices.Contains(x.index))
                .OrderBy(x => x.value)
                .Take(rerollCount)
                .ToList();
            
            foreach (var item in sortedIndices)
            {
                ctx.RerollIndices.Add(item.index);
            }
            
            if (sortedIndices.Count > 0)
            {
                Debug.Log($"[유물] 빠른 장전: {sortedIndices.Count}개 주사위 재굴림");
            }
        }

        // RLC_REGENERATION: 재생의 팔찌 - 굴림 시 5% 확률로 체력 회복
        if (HasRelic("RLC_REGENERATION"))
        {
            var relic = GetRelic("RLC_REGENERATION");
            if (relic != null && Random.value < relic.FloatValue)
            {
                int healAmount = relic.IntValue * GetRelicCount("RLC_REGENERATION");
                // 악마의 계약서가 있으면 회복 불가
                if (!HasRelic("RLC_DEMON_CONTRACT"))
                {
                    GameManager.Instance?.HealPlayer(healAmount);
                    Debug.Log($"[유물] 재생의 팔찌: 체력 +{healAmount}");
                    
                    // 피드백 표시
                    int slotIndex = GameManager.Instance.FindRelicSlotIndex("RLC_REGENERATION");
                    if (UIManager.Instance != null && slotIndex >= 0)
                    {
                        UIManager.Instance.NotifyRelicActivation("Regeneration", slotIndex, healAmount);
                    }
                }
                else
                {
                    Debug.Log("[유물] 악마의 계약서로 인해 회복 불가");
                }
            }
        }

        // RLC_MAGNET: 자석 - 같은 숫자 확률 증가
        if (HasRelic("RLC_MAGNET") && ctx.DiceValues.Length >= 2)
        {
            var relic = GetRelic("RLC_MAGNET");
            float bonusChance = relic?.FloatValue ?? 0.15f;
            
            // 확률적으로 첫 번째 주사위 값으로 다른 주사위를 변환
            if (Random.value < bonusChance)
            {
                int targetValue = ctx.DiceValues[0];
                int randomIndex = Random.Range(1, ctx.DiceValues.Length);
                int oldValue = ctx.DiceValues[randomIndex];
                ctx.DiceValues[randomIndex] = targetValue;
                Debug.Log($"[유물] 자석: 주사위[{randomIndex}] {oldValue} → {targetValue}");
            }
        }
    }

    // 재굴림 시 처리
    private void HandleDiceRerolled(RollContext ctx)
    {
        // RLC_LUCKY_CLOVER: 행운의 네잎클로버 - 재굴림 시 더 높은 숫자
        if (HasRelic("RLC_LUCKY_CLOVER") && ctx.DiceValues != null)
        {
            var relic = GetRelic("RLC_LUCKY_CLOVER");
            float bonusChance = relic?.FloatValue ?? 0.3f;
            
            for (int i = 0; i < ctx.DiceValues.Length; i++)
            {
                // 재굴림된 주사위만 처리 (RerollIndices에 포함된 것)
                if (ctx.RerollIndices != null && ctx.RerollIndices.Contains(i))
                {
                    if (Random.value < bonusChance)
                    {
                        // 현재 값에서 +1~+2 보너스 (최대 6)
                        int bonus = Random.Range(1, 3);
                        int oldValue = ctx.DiceValues[i];
                        ctx.DiceValues[i] = Mathf.Min(ctx.DiceValues[i] + bonus, 6);
                        Debug.Log($"[유물] 네잎클로버: 주사위[{i}] {oldValue} → {ctx.DiceValues[i]}");
                    }
                }
            }
        }
    }

    // 공격 전 처리 (데미지/골드 수정)
    private void HandleBeforeAttack(AttackContext ctx)
    {
        foreach (var relic in GetActiveRelics())
        {
            switch (relic.EffectType)
            {
                // === 고정 데미지 추가 (숫돌, 유리대포, 집중) ===
                case RelicEffectType.AddBaseDamage:
                    ctx.FlatDamageBonus += relic.IntValue;
                    break;

                // === 고정 골드 추가 (녹슨 톱니) ===
                case RelicEffectType.AddBaseGold:
                case RelicEffectType.HandGoldAdd:
                    if (relic.StringValue == "ALL" || 
                        (ctx.hand != null && ctx.hand.Description.Contains(relic.StringValue)))
                    {
                        ctx.FlatGoldBonus += relic.IntValue;
                    }
                    break;

                // === 족보별 데미지 추가 (검술 교본, 가시 장갑 등) ===
                case RelicEffectType.HandDamageAdd:
                    if (ctx.hand != null && ctx.hand.Description.Contains(relic.StringValue))
                    {
                        ctx.FlatDamageBonus += relic.IntValue;
                    }
                    break;

                // === 골드 배율 (황금 주사위) ===
                case RelicEffectType.AddGoldMultiplier:
                    ctx.GoldMultiplier *= relic.FloatValue;
                    break;

                // === 족보별 골드 배율 (백마법서, 흑마법서, 보석왕관 등) ===
                case RelicEffectType.HandGoldMultiplier:
                    if (ctx.hand != null && ctx.hand.Description.Contains(relic.StringValue))
                    {
                        ctx.GoldMultiplier *= relic.FloatValue;
                    }
                    break;

                // === 족보별 데미지 배율 (신규) ===
                case RelicEffectType.HandDamageMultiplier:
                    if (ctx.hand != null && ctx.hand.Description.Contains(relic.StringValue))
                    {
                        ctx.DamageMultiplier *= relic.FloatValue;
                    }
                    break;

                // === 동적 데미지: 골드 비례 (금권 정치) ===
                case RelicEffectType.DynamicDamage_Gold:
                    if (GameManager.Instance != null)
                    {
                        int goldBonus = Mathf.Min(GameManager.Instance.CurrentGold / 100, 50);
                        ctx.FlatDamageBonus += goldBonus;
                    }
                    break;

                // === 동적 데미지: 잃은 체력 비례 (피의 갈증) ===
                case RelicEffectType.DynamicDamage_LostHealth:
                    if (GameManager.Instance != null)
                    {
                        int lostHP = GameManager.Instance.MaxPlayerHealth - GameManager.Instance.PlayerHealth;
                        ctx.FlatDamageBonus += lostHP;
                    }
                    break;

                // === 첫 굴림 보너스 (명함) ===
                case RelicEffectType.RollCountBonus:
                    if (ctx.IsFirstRoll)
                    {
                        ctx.DamageMultiplier *= relic.FloatValue;
                        ctx.GoldMultiplier *= relic.FloatValue;
                        Debug.Log($"[유물] 명함: 첫 굴림 {relic.FloatValue}배!");
                    }
                    break;

                // === 남은 굴림 적을수록 데미지 (모래시계) ===
                case RelicEffectType.DynamicDamage_LowRolls:
                    // 남은 굴림이 적을수록 데미지 증가 (회당 +10%, 최대 30%)
                    float rollBonus = (3 - ctx.RemainingRolls) * relic.FloatValue;
                    ctx.DamageMultiplier += Mathf.Max(0, Mathf.Min(rollBonus, 0.3f));
                    break;

                // === 족보 완성 시 회복 (흡혈귀의 이빨) ===
                case RelicEffectType.HealOnHand:
                    // 악마의 계약서가 있으면 회복 불가
                    if (!HasRelic("RLC_DEMON_CONTRACT"))
                    {
                        ctx.HealAfterAttack += relic.IntValue;
                    }
                    break;

                // === 데미지 배율 + 체력 비용 (도박사의 반지) ===
                case RelicEffectType.DamageMultiplierWithHealthCost:
                    ctx.DamageMultiplier *= relic.FloatValue;
                    break;

                // === 데미지 배율 + 회복 불가 (악마의 계약서) ===
                case RelicEffectType.DamageMultiplierNoHeal:
                    ctx.DamageMultiplier *= relic.FloatValue;
                    break;

                // === 첫 공격 보너스 (날쌤 손놀림, 암살자의 단검) ===
                case RelicEffectType.FirstAttackBonus:
                    if (ctx.IsFirstAttackThisWave)
                    {
                        if (relic.StringValue == "PERCENT_MAX_HP" && ctx.TargetEnemy != null)
                        {
                            // 암살자의 단검: 적 최대 체력의 10%
                            int bonusDmg = Mathf.RoundToInt(ctx.TargetEnemy.maxHP * relic.FloatValue);
                            ctx.FlatDamageBonus += bonusDmg;
                            Debug.Log($"[유물] 암살자의 단검: +{bonusDmg} 데미지");
                        }
                        else
                        {
                            // 날쌤 손놀림: 데미지 50% 증가
                            ctx.DamageMultiplier *= relic.FloatValue;
                            Debug.Log($"[유물] 날쌤 손놀림: 데미지 {relic.FloatValue}배");
                        }
                    }
                    break;

                // === 학자의 서적 보너스 ===
                case RelicEffectType.PermanentDamageGrowth:
                    ctx.FlatDamageBonus += scholarsTomeBonusDamage;
                    break;
                    
                // === 광택 구슬: 연쇄 공격 데미지 증폭 (개당 30%) ===
                case RelicEffectType.ChainDamageBonus:
                    if (StageManager.Instance != null)
                    {
                        int chainCount = StageManager.Instance.GetCurrentChainCount();
                        if (chainCount > 1) // 2번째 공격부터 적용
                        {
                            int relicCount = GetRelicCount("RLC_POLISHED_ORB");
                            float chainBonus = (chainCount - 1) * relic.FloatValue * relicCount;
                            ctx.DamageMultiplier += chainBonus;
                            Debug.Log($"[유물] 광택 구슬: {chainCount}연쇄 공격 - 데미지 +{chainBonus * 100}% (유물 {relicCount}개)");
                        }
                    }
                    break;
            }
        }
    }

    // 공격 후 처리 (회복 등)
    private void HandleAfterAttack(AttackContext ctx)
    {
        // 공격 후 회복 처리
        if (ctx.HealAfterAttack > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.HealPlayer(ctx.HealAfterAttack);
        }
    }

    // 족보 완성 시 처리
    private void HandleHandComplete(HandContext ctx)
    {
        // RLC_VAMPIRE_FANG: 흡혈귀의 이빨 - 족보 완성 시 체력 +10
        if (HasRelic("RLC_VAMPIRE_FANG") && GameManager.Instance != null)
        {
            // 악마의 계약서가 있으면 회복 불가
            if (!HasRelic("RLC_DEMON_CONTRACT"))
            {
                int count = GetRelicCount("RLC_VAMPIRE_FANG");
                GameManager.Instance.HealPlayer(count);
                Debug.Log($"[유물] 흡혈귀의 이빨: 체력 +{count}");
                
                // 피드백 표시
                int slotIndex = GameManager.Instance.FindRelicSlotIndex("RLC_VAMPIRE_FANG");
                if (UIManager.Instance != null && slotIndex >= 0)
                {
                    UIManager.Instance.NotifyRelicActivation("VampireFang", slotIndex, count);
                }
            }
            else
            {
                Debug.Log("[유물] 악마의 계약서로 인해 회복 불가");
            }
        }

        // RLC_TIME_RIFT: 시공의 틈 - 20% 확률로 굴림 횟수 미소모
        if (HasRelic("RLC_TIME_RIFT") && Random.value < 0.2f)
        {
            ctx.ConsumeRoll = false;
            Debug.Log("[유물] 시공의 틈: 굴림 횟수 미소모!");
            
            // 피드백 표시
            int slotIndex = GameManager.Instance.FindRelicSlotIndex("RLC_TIME_RIFT");
            if (UIManager.Instance != null && slotIndex >= 0)
            {
                UIManager.Instance.NotifyRelicActivation("TimeRift", slotIndex);
            }
        }
    }

    // 플레이어 피격 전 처리 (데미지 감소/무효화)
    private void HandleBeforePlayerDamaged(DamageContext ctx)
    {
        // 방어력 계산 (여러 유물에서 합산)
        int totalDefense = 0;
        
        foreach (var relic in GetActiveRelics())
        {
            switch (relic.EffectType)
            {
                case RelicEffectType.AddDefense:
                    totalDefense += relic.IntValue;
                    break;
            }
        }
        
        // 방어력 적용
        if (totalDefense > 0)
        {
            ctx.FinalDamage = Mathf.Max(0, ctx.OriginalDamage - totalDefense);
            Debug.Log($"[유물] 방어력 {totalDefense}: 피해 {ctx.OriginalDamage} → {ctx.FinalDamage}");
        }

        // RLC_SMALL_SHIELD: 작은 방패 - 체력 20% 이하일 때 피해 무효화 (존 당 1회)
        if (HasRelic("RLC_SMALL_SHIELD") && !smallShieldUsedThisZone)
        {
            if (GameManager.Instance != null)
            {
                float hpPercent = (float)GameManager.Instance.PlayerHealth / GameManager.Instance.MaxPlayerHealth;
                if (hpPercent <= 0.2f)
                {
                    ctx.Cancelled = true;
                    smallShieldUsedThisZone = true;
                    Debug.Log("[유물] 작은 방패: 피해 무효화!");
                    
                    // 피드백 표시
                    int slotIndex = GameManager.Instance.FindRelicSlotIndex("RLC_SMALL_SHIELD");
                    if (UIManager.Instance != null && slotIndex >= 0)
                    {
                        UIManager.Instance.NotifyRelicActivation("SmallShield", slotIndex);
                    }
                }
            }
        }
    }

    // 플레이어 사망 시 처리 (부활)
    private void HandlePlayerDeath(DeathContext ctx)
    {
        // RLC_PHOENIX_FEATHER: 불사조의 깃털 - 1회 부활
        if (HasRelic("RLC_PHOENIX_FEATHER") && !phoenixFeatherUsed)
        {
            phoenixFeatherUsed = true;
            ctx.Revived = true;
            ctx.ReviveHP = GameManager.Instance != null ? GameManager.Instance.MaxPlayerHealth / 2 : 10;
            ctx.ReviveSource = "RLC_PHOENIX_FEATHER";
            Debug.Log($"[유물] 불사조의 깃털: 체력 {ctx.ReviveHP}로 부활!");
            
            // 피드백 표시
            int slotIndex = GameManager.Instance.FindRelicSlotIndex("RLC_PHOENIX_FEATHER");
            if (UIManager.Instance != null && slotIndex >= 0)
            {
                UIManager.Instance.NotifyRelicActivation("PhoenixFeather", slotIndex);
            }
        }
    }

    // 날쌘 손놀림 사용 횟수 추적 (웨이브번호 → 사용횟수)
    private Dictionary<int, int> swiftHandsUsedThisWave = new Dictionary<int, int>();

    // 웨이브 시작 시
    private void HandleWaveStart(WaveContext ctx)
    {
        // 웨이브 관련 상태 초기화
        diceCupUsedThisWave = false;
        doubleDiceUsedThisWave = false;
        swiftHandsUsedThisWave.Clear();
        
        int diceCup = GetRelicCount("RLC_DICE_CUP");
        // 주사위 컵: 보존 기회 충전
        if(diceCup > 0)
        {
            preserveChargesRemaining = diceCup;
        }
    }

    // 웨이브 종료 시
    private void HandleWaveEnd(WaveContext ctx)
    {
        // 날쌘 손놀림 사용 횟수 초기화 (매 웨이브마다 초기화)
        if (swiftHandsUsedThisWave.ContainsKey(ctx.WaveNumber))
        {
            int usedCount = swiftHandsUsedThisWave[ctx.WaveNumber];
            swiftHandsUsedThisWave.Remove(ctx.WaveNumber);
            Debug.Log($"[유물] 날쌘 손놀림 웨이브 {ctx.WaveNumber} 종료 - 이번 웨이브 사용: {usedCount}회");
        }
        
        // RLC_SCHOLAR_BOOK: 학자의 서적 - 미사용 족보당 영구 데미지 +1%
        if (HasRelic("RLC_SCHOLAR_BOOK") && ctx.UnusedHandCount > 0)
        {
            int count = GetRelicCount("RLC_SCHOLAR_BOOK");
            int bonusGain = ctx.UnusedHandCount * count;
            scholarsTomeBonusDamage += bonusGain;
            Debug.Log($"[유물] 학자의 서적: 영구 데미지 +{bonusGain}% (총 +{scholarsTomeBonusDamage}%)");
            
            // 피드백 표시
            int slotIndex = GameManager.Instance.FindRelicSlotIndex("RLC_SCHOLAR_BOOK");
            if (UIManager.Instance != null && slotIndex >= 0)
            {
                UIManager.Instance.NotifyRelicActivation("ScholarBook", slotIndex, bonusGain);
            }
        }
    }

    // 존 시작 시
    private void HandleZoneStart(ZoneContext ctx)
    {
        // 존 관련 상태 초기화
        smallShieldUsedThisZone = false;
        fateDiceUsedThisZone = false; // 운명의 주사위 충전
        Debug.Log("[유물] 새 존 시작 - 운명의 주사위 충전됨");
    }

    // 존 종료 시
    private void HandleZoneEnd(ZoneContext ctx)
    {
        // RLC_PIGGY_BANK: 돼지 저금통 - 존 클리어 시 보유 골드 10% 추가
        if (HasRelic("RLC_PIGGY_BANK") && GameManager.Instance != null)
        {
            int bonusGold = Mathf.RoundToInt(GameManager.Instance.CurrentGold * 0.1f);
            GameManager.Instance.AddGold(bonusGold);
            Debug.Log($"[유물] 돼지 저금통: 골드 +{bonusGold}");
        }
    }

    // 턴 시작 시
    private void HandleTurnStart()
    {
        // 턴 관련 상태 초기화
    }

    // 굴림 횟수가 0이 됐을 때 호출 - 날쌘 손놀림 효과
    public bool CheckFreeRollAtZero(int waveNumber)
    {
        // RLC_SWIFT_HANDS: 날쌘 손놀림 - 굴림 가능 횟수가 0일 때 1회 무료 충전
        if (!HasRelic("RLC_SWIFT_HANDS"))
            return false;

        // 웨이브당 보유 개수만큼 사용 가능
        int relicCount = GetRelicCount("RLC_SWIFT_HANDS");
        
        // 이번 웨이브에서 사용한 횟수 확인
        if (!swiftHandsUsedThisWave.ContainsKey(waveNumber))
            swiftHandsUsedThisWave[waveNumber] = 0;
        
        int usedCount = swiftHandsUsedThisWave[waveNumber];
        
        if (usedCount < relicCount)
        {
            swiftHandsUsedThisWave[waveNumber]++;
            Debug.Log($"[유물] 날쌘 손놀림: 무료 굴림 1회 충전! (사용 {usedCount + 1}/{relicCount})");
            
            // 피드백 표시
            int slotIndex = GameManager.Instance.FindRelicSlotIndex("RLC_SWIFT_HANDS");
            if (UIManager.Instance != null && slotIndex >= 0)
            {
                UIManager.Instance.NotifyRelicActivation("SwiftHands", slotIndex);
            }
            
            return true;
        }
        
        Debug.Log($"[유물] 날쌘 손놀림: 이번 웨이브에서 이미 {relicCount}회 모두 사용함");
        return false;
    }

    // 날쌘 손놀림 사용 가능 여부 체크
    public bool CanUseSwiftHands(int waveNumber)
    {
        if (!HasRelic("RLC_SWIFT_HANDS"))
            return false;
            
        int relicCount = GetRelicCount("RLC_SWIFT_HANDS");
        int usedCount = swiftHandsUsedThisWave.ContainsKey(waveNumber) ? swiftHandsUsedThisWave[waveNumber] : 0;
        
        return usedCount < relicCount;
    }

    // 골드 획득 시
    private void HandleGoldGain(GoldContext ctx)
    {
        // RLC_GOLD_DICE: 황금 주사위 - 골드 1.5배
        if (HasRelic("RLC_GOLD_DICE"))
        {
            var relic = GetRelic("RLC_GOLD_DICE");
            ctx.Multiplier *= relic.FloatValue;
        }

        ctx.Calculate();
    }
    //플레이어 회복 시
    private void HandlePlayerHeal(HealContext ctx)
    {
        //회복관련 유물 얻으면 추가하기
    }

    // 상점 새로고침 시
    private void HandleShopRefresh(ShopContext ctx)
    {
        // RLC_LUCKY_CHARM: 행운의 동전 - 상점 25% 할인
        if (HasRelic("RLC_LUCKY_CHARM"))
        {
            ctx.PriceMultiplier *= 0.75f;
        }

        // RLC_MERCHANT_CARD: 상인의 명함 - 리롤 비용 동결
        if (HasRelic("RLC_MERCHANT_CARD"))
        {
            // 리롤 비용 증가 방지 로직은 상점에서 처리
        }

        // RLC_SPRING: 스프링 - 50% 확률로 리롤 비용 반환 (상점 발동이므로 피드백 제외)
        if (HasRelic("RLC_SPRING") && Random.value < 0.5f)
        {
            ctx.FreeRefresh = true;
            Debug.Log("[유물] 스프링: 리롤 비용 반환!");
        }
    }

    // 유물 획득 시
    private void HandleRelicAcquire(RelicContext ctx)
    {
        if (ctx.RelicID == null || GameManager.Instance == null) return;

        var gm = GameManager.Instance;

        switch (ctx.RelicID)
        {
            // === 주사위 추가 유물 ===
            case "RLC_EXTRA_DICE":
                if (gm.playerDiceDeck.Count < gm.maxDiceCount)
                {
                    gm.AddDiceToDeck("D6");
                    Debug.Log("[유물] 여분의 주사위: D6 추가");
                }
                break;

            case "RLC_TINY_DICE":
                if (gm.playerDiceDeck.Count < gm.maxDiceCount)
                {
                    gm.AddDiceToDeck("D4");
                    Debug.Log("[유물] 작은 주사위: D4 추가");
                }
                break;

            // === 주사위 제거 유물 ===
            case "RLC_FOCUS":
                if (gm.playerDiceDeck.Count > gm.minDiceCount)
                {
                    gm.RemoveDiceFromDeck("D6");
                    Debug.Log("[유물] 집중: D6 제거");
                }
                break;

            // === 체력 변경 유물 ===
            case "RLC_GLASS_CANNON":
                gm.ModifyMaxHealth(-50);
                Debug.Log("[유물] 유리 대포: 최대 체력 -50");
                break;

            case "RLC_GAMBLER_RING":
                gm.ModifyMaxHealth(-50);
                Debug.Log("[유물] 도박사의 반지: 최대 체력 -90");
                break;

            case "RLC_HEART_CONTAINER":
                gm.ModifyMaxHealth(25);
                gm.HealPlayer(25);
                Debug.Log("[유물] 생명의 심장: 최대 체력 +25");
                break;

            // === 굴림 횟수 변경 유물 ===
            case "RLC_HEAVY_DICE":
                if (DiceController.Instance != null)
                {
                    DiceController.Instance.ApplyRollBonus(-1);
                    Debug.Log("[유물] 무거운 주사위: 굴림 횟수 -1");
                }
                break;

            case "RLC_CLOVER":
                if (DiceController.Instance != null)
                {
                    DiceController.Instance.ApplyRollBonus(1);
                    Debug.Log("[유물] 네잎클로버: 최대 굴림 횟수 +1");
                }
                break;

            // === 기타 유물 ===
            case "RLC_LIGHTWEIGHT_BAG":
                // GameManager에 maxRelicCapacity 필드가 있다면 처리
                Debug.Log("[유물] 가벼운 가방: 유물 보유 한도 +1");
                break;
        }
    }

    // ===== 수동 발동 유물 =====

    // [수동] 이중 주사위 - 선택한 주사위 값 2배 (웨이브당 1회)
    // diceIndex: 2배로 만들 주사위 인덱스
    // currentValue: 현재 주사위 값
    // 반환: 새로운 값 (실패 시 -1)
    public int UseDoubleDice(int diceIndex, int currentValue)
    {
        if (!HasRelic("RLC_DOUBLE_DICE"))
        {
            Debug.Log("[유물] 이중 주사위를 보유하고 있지 않습니다.");
            return -1;
        }

        if (doubleDiceUsedThisWave)
        {
            Debug.Log("[유물] 이중 주사위: 이번 웨이브에서 이미 사용했습니다.");
            return -1;
        }

        doubleDiceUsedThisWave = true;
        int newValue = currentValue * 2;
        Debug.Log($"[유물] 이중 주사위: 주사위[{diceIndex}] {currentValue} → {newValue}");
        return newValue;
    }
    
    // 주사위 보존 기회 사용
    public bool UsePreserveCharge(int diceIndex)
    {
        if (!HasRelic("RLC_DICE_CUP"))
        {
            return false;
        }
        
        if (preserveChargesRemaining <= 0)
        {
            Debug.Log("[유물] 주사위 컵: 남은 보존 기회가 없습니다.");
            return false;
        }
        
        preserveChargesRemaining--;
        DiceController.Instance.PreserveDice(diceIndex);
        Debug.Log($"[유물] 주사위 컵: 주사위[{diceIndex}] 보존 완료! (남은 기회: {preserveChargesRemaining})");
        return true;
    }
    
    // 보존 해제 시 보존 횟수 복구
    public void RestorePreserveCharge(int diceIndex)
    {
        if (!HasRelic("RLC_DICE_CUP"))
        {
            return;
        }
        
        preserveChargesRemaining++;
        DiceController.Instance.UnpreserveDice(diceIndex);
        
        // 유물 패널 업데이트
        if (UIManager.Instance != null && GameManager.Instance != null)
        {
            UIManager.Instance.UpdateRelicPanel(GameManager.Instance.activeRelics);
        }
    }

    //운명의 주사위 - 모든 주사위 최대값 (존당 1회)
    public bool UseFateDice(int[] diceValues, string[] diceTypes)
    {
        if (!HasRelic("RLC_FATE_DICE"))
        {
            Debug.Log("[유물] 운명의 주사위를 보유하고 있지 않습니다.");
            return false;
        }

        if (fateDiceUsedThisZone)
        {
            Debug.Log("[유물] 운명의 주사위: 이번 존에서 이미 사용했습니다.");
            return false;
        }

        if (diceValues == null || diceTypes == null || diceValues.Length != diceTypes.Length)
        {
            return false;
        }

        fateDiceUsedThisZone = true;

        // 각 주사위를 최대값으로 설정
        for (int i = 0; i < diceValues.Length; i++)
        {
            int maxValue = GetDiceMaxValue(diceTypes[i]);
            diceValues[i] = maxValue;
        }
        
        Debug.Log("[유물] 운명의 주사위: 모든 주사위를 최대값으로 설정!");
        
        // 피드백 표시
        int slotIndex = GameManager.Instance.FindRelicSlotIndex("RLC_FATE_DICE");
        if (UIManager.Instance != null && slotIndex >= 0)
        {
            UIManager.Instance.NotifyRelicActivation("FateDice", slotIndex);
        }
        
        return true;
    }

    // 주사위 타입에 따른 최대값 반환
    private int GetDiceMaxValue(string diceType)
    {
        switch (diceType)
        {
            case "D4": return 4;
            case "D6": return 6;
            case "D8": return 8;
            case "D10": return 10;
            case "D12": return 12;
            case "D20": return 20;
            default: return 6;
        }
    }

    // 수동 유물 사용 가능 여부 체크
    public bool CanUseDiceCup() => HasRelic("RLC_DICE_CUP") && !diceCupUsedThisWave;
    public bool CanUseDoubleDice() => HasRelic("RLC_DOUBLE_DICE") && !doubleDiceUsedThisWave;
    public bool CanUseFateDice() => HasRelic("RLC_FATE_DICE") && !fateDiceUsedThisZone;
    public bool CanUsePreserve() => HasRelic("RLC_DICE_CUP") && preserveChargesRemaining > 0;

    // ===== 런 시작/종료 =====

    // 새 런 시작 시 상태 초기화
    public void ResetForNewRun()
    {
        phoenixFeatherUsed = false;
        smallShieldUsedThisZone = false;
        scholarsTomeBonusDamage = 0;
        diceCupUsedThisWave = false;
        doubleDiceUsedThisWave = false;
        fateDiceUsedThisZone = false;
        swiftHandsUsedThisWave.Clear();
    }
}
