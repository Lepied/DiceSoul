using UnityEngine;


// 콘솔 명령어로 유물을 테스트할 수 있는 간단한 디버그 헬퍼
// Unity 에디터에서 Debug.Log로 명령 실행 가능
public static class RelicDebugCommands
{
    // 유물 추가: RelicDebugCommands.Add("RLC_VAMPIRE_FANG")
    public static void Add(string relicID)
    {
        if (RelicDB.Instance == null || GameManager.Instance == null)
        {
            Debug.LogError("[RelicDebug] RelicDB 또는 GameManager 없음");
            return;
        }

        var relic = RelicDB.Instance.GetRelicByID(relicID);
        if (relic != null)
        {
            GameManager.Instance.AddRelic(relic);
            Debug.Log($"<color=green>[+]</color> {relic.Name} ({relicID}) 추가됨");
        }
        else
        {
            Debug.LogError($"[RelicDebug] 유물 없음: {relicID}");
            ListSimilar(relicID);
        }
    }

    // 여러 유물 한번에 추가
    // RelicDebugCommands.AddMultiple("RLC_VAMPIRE_FANG", "RLC_PHOENIX_FEATHER", "RLC_TOUGH_ARMOR")
    public static void AddMultiple(params string[] relicIDs)
    {
        foreach (var id in relicIDs)
        {
            Add(id);
        }
    }

    // 유물 제거: RelicDebugCommands.Remove("RLC_VAMPIRE_FANG")
    public static void Remove(string relicID)
    {
        if (GameManager.Instance == null) return;

        var relic = GameManager.Instance.activeRelics.Find(r => r.RelicID == relicID);
        if (relic != null)
        {
            GameManager.Instance.activeRelics.Remove(relic);
            Debug.Log($"<color=red>[-]</color> {relic.Name} ({relicID}) 제거됨");
        }
        else
        {
            Debug.LogWarning($"[RelicDebug] 보유하지 않은 유물: {relicID}");
        }
    }

    // 모든 유물 제거
    public static void ClearAll()
    {
        if (GameManager.Instance == null) return;
        int count = GameManager.Instance.activeRelics.Count;
        GameManager.Instance.activeRelics.Clear();
        Debug.Log($"<color=yellow>[!]</color> 모든 유물 {count}개 제거됨");
    }

    // 현재 보유 유물 목록
    public static void List()
    {
        if (GameManager.Instance == null) return;

        Debug.Log("===== 보유 유물 =====");
        var grouped = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var r in GameManager.Instance.activeRelics)
        {
            if (grouped.ContainsKey(r.RelicID))
                grouped[r.RelicID]++;
            else
                grouped[r.RelicID] = 1;
        }

        foreach (var kvp in grouped)
        {
            var relic = GameManager.Instance.activeRelics.Find(r => r.RelicID == kvp.Key);
            Debug.Log($"  {relic.Name} x{kvp.Value} ({kvp.Key})");
        }
        Debug.Log($"===== 총 {GameManager.Instance.activeRelics.Count}개 =====");
    }

    // 비슷한 유물 ID 찾기
    public static void ListSimilar(string partialID)
    {
        if (RelicDB.Instance == null) return;

        var allRelicsField = typeof(RelicDB).GetField("allRelics",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (allRelicsField == null) return;

        var allRelics = allRelicsField.GetValue(RelicDB.Instance) 
            as System.Collections.Generic.Dictionary<string, Relic>;
        if (allRelics == null) return;

        Debug.Log($"'{partialID}' 와 비슷한 유물:");
        string lowerPartial = partialID.ToLower();
        foreach (var kvp in allRelics)
        {
            if (kvp.Key.ToLower().Contains(lowerPartial) || 
                kvp.Value.Name.ToLower().Contains(lowerPartial))
            {
                Debug.Log($"  → {kvp.Key}: {kvp.Value.Name}");
            }
        }
    }

    // ===== 퀵 테스트 프리셋 =====

    // 생존 테스트 세트 (방어/회복 유물)
    public static void PresetSurvival()
    {
        ClearAll();
        AddMultiple(
            "RLC_VAMPIRE_FANG",
            "RLC_PHOENIX_FEATHER",
            "RLC_SMALL_SHIELD",
            "RLC_TOUGH_ARMOR",
            "RLC_REGENERATION"
        );
        Debug.Log("<color=cyan>[프리셋]</color> 생존 테스트 세트 적용됨");
    }

    // 공격 테스트 세트 (데미지 유물)
    public static void PresetDamage()
    {
        ClearAll();
        AddMultiple(
            "RLC_WHETSTONE",
            "RLC_GLASS_CANNON",
            "RLC_BLOODLUST",
            "RLC_HOURGLASS",
            "RLC_SWIFT_HANDS"
        );
        Debug.Log("<color=cyan>[프리셋]</color> 공격 테스트 세트 적용됨");
    }

    // 주사위 조작 테스트 세트
    public static void PresetDiceManip()
    {
        ClearAll();
        AddMultiple(
            "RLC_ALCHEMY",
            "RLC_DIAMOND_DICE",
            "RLC_QUICK_RELOAD",
            "RLC_LUCKY_CLOVER",
            "RLC_COUNTERWEIGHT"
        );
        Debug.Log("<color=cyan>[프리셋]</color> 주사위 조작 테스트 세트 적용됨");
    }

    // 경제 테스트 세트
    public static void PresetEconomy()
    {
        ClearAll();
        AddMultiple(
            "RLC_GOLD_DICE",
            "RLC_LUCKY_CHARM",
            "RLC_MERCHANT_CARD",
            "RLC_SPRING",
            "RLC_PLUTOCRACY"
        );
        Debug.Log("<color=cyan>[프리셋]</color> 경제 테스트 세트 적용됨");
    }

    // ===== 게임 상태 치트 =====

    public static void Heal(int amount = 999)
    {
        GameManager.Instance?.HealPlayer(amount);
        Debug.Log($"<color=green>[치트]</color> 체력 +{amount}");
    }

    public static void Damage(int amount = 5)
    {
        if (GameManager.Instance == null) return;
        
        // DamageContext를 통해 피해 처리
        var ctx = new DamageContext
        {
            OriginalDamage = amount,
            FinalDamage = amount,
            Source = "Debug"
        };
        GameEvents.RaiseBeforePlayerDamaged(ctx);
        
        if (!ctx.Cancelled)
        {
            GameManager.Instance.PlayerHealth -= ctx.FinalDamage;
            Debug.Log($"<color=red>[치트]</color> 데미지 {ctx.FinalDamage}");
        }
    }

    public static void Gold(int amount = 1000)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.AddGold(amount);
        Debug.Log($"<color=yellow>[치트]</color> 골드 +{amount}");
    }

    public static void Rolls(int amount = 5)
    {
        var diceController = UnityEngine.Object.FindAnyObjectByType<DiceController>();
        if (diceController != null)
        {
            diceController.ApplyRollBonus(amount);
            Debug.Log($"<color=cyan>[치트]</color> 최대 굴림 횟수 +{amount}");
        }
    }

    public static void KillEnemy()
    {
        var stageManager = StageManager.Instance;
        if (stageManager == null) return;
        
        // activeEnemies 필드 접근 (리플렉션)
        var enemiesField = typeof(StageManager).GetField("activeEnemies",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemiesField == null) return;
        
        var enemies = enemiesField.GetValue(stageManager) as System.Collections.Generic.List<Enemy>;
        if (enemies != null && enemies.Count > 0)
        {
            var enemy = enemies.Find(e => e != null && !e.isDead);
            if (enemy != null)
            {
                // currentHP는 protected set이므로 리플렉션 사용
                var hpProp = typeof(Enemy).GetProperty("currentHP");
                if (hpProp != null)
                {
                    hpProp.SetValue(enemy, 0);
                }
                Debug.Log("<color=red>[치트]</color> 현재 적 처치");
            }
        }
    }

    public static void Status()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log("[Status] GameManager 없음");
            return;
        }

        var gm = GameManager.Instance;
        var dc = UnityEngine.Object.FindAnyObjectByType<DiceController>();
        
        Debug.Log("===== 게임 상태 =====");
        Debug.Log($"  체력: {gm.PlayerHealth}/{gm.MaxPlayerHealth}");
        Debug.Log($"  골드: {gm.CurrentGold}");
        if (dc != null)
            Debug.Log($"  굴림: {dc.currentRollCount}/{dc.maxRolls}");
        Debug.Log($"  유물: {gm.activeRelics.Count}개");
        Debug.Log("====================");
    }
}
