using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("연결")]
    public DiceController diceController;

    [Header("UI 연결")]

    [Tooltip("존(Zone)이 바뀔 때 교체될 배경 SpriteRenderer (GameObject)")]
    public SpriteRenderer backgroundRenderer;

    [Header("웨이브/적 설정")]
    public Transform[] enemySpawnPoints;

    public float spawnSpreadRadius = 0.5f;
    public float screenEdgePadding = 1.0f;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private bool isWaitingForAttackChoice = false;

    private Camera mainCam;
    private Vector2 minViewBoundary;
    private Vector2 maxViewBoundary;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (diceController == null)
        {
            diceController = FindFirstObjectByType<DiceController>();
            if (diceController == null)
                Debug.LogError("씬에 DiceController가 없습니다!");
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없습니다!");
            return;
        }
        mainCam = Camera.main;
        if (mainCam != null)
        {
            minViewBoundary = mainCam.ViewportToWorldPoint(new Vector2(0, 0));
            maxViewBoundary = mainCam.ViewportToWorldPoint(new Vector2(1, 1));
            minViewBoundary.x += screenEdgePadding;
            minViewBoundary.y += screenEdgePadding;
            maxViewBoundary.x -= screenEdgePadding;
            maxViewBoundary.y -= screenEdgePadding;
        }
        else
        {
            Debug.LogError("Main Camera가 없습니다! 스폰 위치가 제한되지 않습니다.");
        }
        PrepareNextWave();
    }

    public void OnRollFinished(List<int> currentDiceValues)
    {
        StartCoroutine(ProcessDiceResults(currentDiceValues));
    }

    //유물 효과 적용 및 연출 처리 코루틴
    private IEnumerator ProcessDiceResults(List<int> initialValues)
    {
        // 1. 적 턴 반응 / 기믹 처리
        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            enemy.OnPlayerRoll(initialValues);
        }

        // 2. 이벤트 시스템으로 유물 효과 적용
        RollContext rollCtx = new RollContext
        {
            DiceValues = initialValues.ToArray(),
            DiceTypes = GameManager.Instance.playerDiceDeck.ToArray(),
            IsFirstRoll = (diceController.currentRollCount == 1),
            RerollIndices = new List<int>()
        };
        GameEvents.RaiseDiceRolled(rollCtx);
        
        // 이벤트에서 수정된 값 적용
        List<int> modifiedValues = rollCtx.DiceValues.ToList();

        bool anyChange = false;

        // 3. 값이 바뀐 주사위 찾아내서 연출 재생
        for (int i = 0; i < initialValues.Count; i++)
        {
            if (initialValues[i] != modifiedValues[i])
            {
                anyChange = true;
                int oldVal = initialValues[i];
                int newVal = modifiedValues[i];

                // 여기서 연출 고르기. 각 유물 효과 적용해서 구분시키기
                if (oldVal == 1 && newVal == 7)
                {
                    // 연금술사의 돌 등: 뾰로롱 연출
                    DiceController.Instance.PlayMagicChangeVisual(i, newVal);
                }
                else
                {
                    // 가벼운 깃털, 자철석 등: 다시 굴리기 연출
                    DiceController.Instance.PlayRerollVisual(i, newVal);
                }
            }
        }
        
        // 4. RerollIndices가 있으면 해당 주사위 재굴림 처리
        if (rollCtx.RerollIndices != null && rollCtx.RerollIndices.Count > 0)
        {
            anyChange = true;
            foreach (int idx in rollCtx.RerollIndices)
            {
                if (idx >= 0 && idx < modifiedValues.Count)
                {
                    string diceType = GameManager.Instance.playerDiceDeck[idx];
                    int newVal = RollSingleDice(diceType);
                    DiceController.Instance.PlayRerollVisual(idx, newVal);
                    modifiedValues[idx] = newVal;
                }
            }
        }

        // 5. 변화가 있었다면, 연출이 끝날 때까지 잠시 대기
        if (anyChange)
        {
            yield return new WaitForSeconds(0.6f);
        }
        //최종값으로 족보 계산
        CheckJokbo(modifiedValues);
    }
    
    // 단일 주사위 굴림 헬퍼
    private int RollSingleDice(string diceType)
    {
        return diceType switch
        {
            "D4" => Random.Range(1, 5),
            "D8" => Random.Range(1, 9),
            "D10" => Random.Range(1, 11),
            "D12" => Random.Range(1, 13),
            "D20" => Random.Range(1, 21),
            _ => Random.Range(1, 7) // D6 기본
        };
    }

    //족보 계산 및 UI 표시
    private void CheckJokbo(List<int> finalValues)
    {
        if (AttackDB.Instance == null) return;

        List<AttackJokbo> achievableJokbos = AttackDB.Instance.GetAchievableJokbos(finalValues);

        if (achievableJokbos.Count > 0)
        {
            // 이벤트 시스템: 족보 완성 이벤트
            JokboContext jokboCtx = new JokboContext
            {
                AchievedJokbos = achievableJokbos,
                DiceValues = finalValues.ToArray(),
                BonusDamage = 0,
                BonusGold = 0
            };
            GameEvents.RaiseJokboComplete(jokboCtx);
            
            diceController.SetRollButtonInteractable(false);
            isWaitingForAttackChoice = true;
            if (UIManager.Instance != null)
            {
                List<AttackJokbo> previewJokbos = new List<AttackJokbo>();
                foreach (var jokbo in achievableJokbos)
                {
                    (int finalBaseDamage, int finalBaseGold) = GetPreviewValues(jokbo);
                    previewJokbos.Add(new AttackJokbo(
                        jokbo.Description,
                        finalBaseDamage,
                        finalBaseGold,
                        jokbo.CheckLogic
                    ));
                }
                UIManager.Instance.ShowAttackOptions(previewJokbos);
            }
        }
        else
        {
            // 족보 실패 시 처리
            if (diceController.currentRollCount >= diceController.maxRolls)
            {
                GameManager.Instance.ProcessWaveClear(false, 0);
            }
            else
            {
                diceController.SetRollButtonInteractable(true);
            }
        }
    }
    public void ProcessAttack(AttackJokbo chosenJokbo)
    {
        if (!isWaitingForAttackChoice) return;

        (int finalDamage, int finalGold) = GetPreviewValues(chosenJokbo);

        //이벤트 시스템: 공격 전 이벤트 발생 (유물이 데미지/골드 수정 가능)
        AttackContext attackCtx = new AttackContext
        {
            Jokbo = chosenJokbo,
            BaseDamage = finalDamage,
            BaseGold = finalGold,
            FlatDamageBonus = 0,
            FlatGoldBonus = 0,
            DamageMultiplier = 1.0f,
            GoldMultiplier = 1.0f,
            IsFirstRoll = (diceController.currentRollCount == 1),
            RemainingRolls = diceController.maxRolls - diceController.currentRollCount,
            HealAfterAttack = 0
        };
        GameEvents.RaiseBeforeAttack(attackCtx);
        
        // 이벤트에서 수정된 최종 값 계산
        int eventFinalDamage = attackCtx.CalculateFinalDamage();
        int eventFinalGold = attackCtx.CalculateFinalGold();

        Debug.Log($"광역 공격: [{chosenJokbo.Description}] (최종 데미지: {eventFinalDamage}, 골드: {eventFinalGold})");

        AttackJokbo modifiedJokbo = new AttackJokbo(
            chosenJokbo.Description,
            eventFinalDamage,
            eventFinalGold,
            chosenJokbo.CheckLogic
        );

        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            int damageToTake = enemy.CalculateDamageTaken(modifiedJokbo);
            enemy.TakeDamage(damageToTake, modifiedJokbo);
        }

        // 이벤트에서 이미 계산된 골드를 직접 추가 (중복 계산 방지)
        GameManager.Instance.AddGoldDirect(eventFinalGold);
        
        //이벤트 시스템: 공격 후 이벤트 발생 (회복 등)
        GameEvents.RaiseAfterAttack(attackCtx);

        isWaitingForAttackChoice = false;
        CheckWaveStatus();
    }

    private void CheckWaveStatus()
    {
        //이벤트 시스템: 턴 종료 이벤트
        GameEvents.RaiseTurnEnd();
        
        if (activeEnemies.All(e => e == null || e.isDead))
        {
            int rollsRemaining = diceController.maxRolls - diceController.currentRollCount;
            GameManager.Instance.ProcessWaveClear(true, rollsRemaining);
        }
        else
        {
            if (diceController.currentRollCount >= diceController.maxRolls)
            {
                Debug.Log("굴림 횟수 소진. 웨이브 실패.");
                diceController.SetRollButtonInteractable(false);
                GameManager.Instance.ProcessWaveClear(false, 0);
            }
            else
            {
                Debug.Log("적이 남았습니다. 다시 굴리세요.");
                diceController.SetRollButtonInteractable(true);
            }
        }
    }

    public void PrepareNextWave()
    {
        Debug.Log("StageManager: 다음 웨이브 준비 중...");
        isWaitingForAttackChoice = false;

        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                WaveGenerator.Instance.ReturnToPool(enemy.gameObject);
            }
        }
        activeEnemies.Clear();

        if (WaveGenerator.Instance == null || GameManager.Instance == null)
        {
            Debug.LogError("WaveGenerator 또는 GameManager가 씬에 없습니다!");
            return;
        }
        int currentZone = GameManager.Instance.CurrentZone;
        int currentWave = GameManager.Instance.CurrentWave;

        ZoneData currentZoneData = WaveGenerator.Instance.GetCurrentZoneData(currentZone);

        if (currentWave == 1 && currentZoneData != null && backgroundRenderer != null)
        {
            if (currentZoneData.zoneBackground != null)
            {
                backgroundRenderer.sprite = currentZoneData.zoneBackground;
                Debug.Log($"[StageManager] 배경 변경: {currentZoneData.zoneName}");
            }
            else
            {
                Debug.LogWarning($"[StageManager] {currentZoneData.name}에 'zoneBackground' 스프라이트가 없습니다.");
            }
        }

        List<GameObject> enemiesToSpawn = WaveGenerator.Instance.GenerateWave(currentZone, currentWave);

        if (enemySpawnPoints.Length == 0)
        {
            Debug.LogError("적 스폰 포인트가 1개 이상 필요합니다!");
            return;
        }

        //새 적 스폰
        foreach (GameObject enemyPrefab in enemiesToSpawn)
        {
            if (enemyPrefab == null) continue;
            int spawnIndex = Random.Range(0, enemySpawnPoints.Length);
            Vector3 spawnPos = enemySpawnPoints[spawnIndex].position;
            Vector2 randomOffset = Random.insideUnitCircle * spawnSpreadRadius;
            spawnPos.x += randomOffset.x;
            spawnPos.y += randomOffset.y;
            if (mainCam != null)
            {
                spawnPos.x = Mathf.Clamp(spawnPos.x, minViewBoundary.x, maxViewBoundary.x);
                spawnPos.y = Mathf.Clamp(spawnPos.y, minViewBoundary.y, maxViewBoundary.y);
            }

            GameObject enemyGO = WaveGenerator.Instance.SpawnFromPool(enemyPrefab, spawnPos, Quaternion.identity);
            Enemy newEnemy = enemyGO.GetComponent<Enemy>();

            if (newEnemy != null)
            {
                activeEnemies.Add(newEnemy);
            }
            else
            {
                Debug.LogError($"{enemyPrefab.name} 프리팹에 Enemy 스크립트가 없습니다.");
            }
        }

        foreach (Enemy enemy in activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                enemy.OnWaveStart(activeEnemies);
            }
        }


        UIManager.Instance.UpdateWaveInfoPanel(activeEnemies);

        if (GameManager.Instance != null && diceController != null)
        {
            GameManager.Instance.StartNewWave();
            diceController.PrepareNewTurn();
            GameManager.Instance.ApplyAllRelicEffects(diceController);
            GameManager.Instance.ApplyWaveStartBuffs(diceController);
            diceController.SetDiceDeck(GameManager.Instance.playerDiceDeck);
        }



        //영구강화 효과로 시작데미지 적용시키기
        if (GameManager.Instance != null)
        {
            int startDamage = (int)GameManager.Instance.GetTotalMetaBonus(MetaEffectType.StartDamage);

            if (startDamage > 0)
            {
                Debug.Log($"[메타 강화] 제압 사격 발동! 모든 적에게 {startDamage} 데미지.");

                // 스폰된 모든 적에게 데미지 적용
                foreach (Enemy enemy in activeEnemies.ToList())
                {
                    if (enemy != null && !enemy.isDead)
                    {
                        // 족보 정보 없이 고정 데미지 주는 방식 (null 전달)
                        // Enemy.TakeDamage 함수가 null Jokbo를 처리할 수 있어야 함.
                        // 만약 처리 못한다면 더미 Jokbo를 만들어서 보내야 함.
                        enemy.TakeDamage(startDamage, null);
                    }
                }

                // 데미지로 인해 죽은 적이 있을 수 있으므로 상태 체크 한 번 실행
                CheckWaveStatus();
            }
        }

    }

    // 유물 효과가 적용된 최종 데미지/골드 미리보기 계산
    public (int finalDamage, int finalGold) GetPreviewValues(AttackJokbo jokbo)
    {
        // AttackContext를 통해 이벤트 시스템으로 계산
        AttackContext ctx = new AttackContext
        {
            Jokbo = jokbo,
            BaseDamage = jokbo.BaseDamage,
            BaseGold = jokbo.BaseGold,
            FlatDamageBonus = 0,
            FlatGoldBonus = 0,
            DamageMultiplier = 1.0f,
            GoldMultiplier = 1.0f,
            IsFirstRoll = (diceController.currentRollCount == 1),
            RemainingRolls = diceController.maxRolls - diceController.currentRollCount,
            HealAfterAttack = 0
        };
        
        // 메타 업그레이드 보너스 추가
        if (GameManager.Instance != null)
        {
            ctx.FlatDamageBonus += (int)GameManager.Instance.GetTotalMetaBonus(MetaEffectType.BaseDamage);
            ctx.FlatGoldBonus += (int)GameManager.Instance.GetTotalMetaBonus(MetaEffectType.GoldBonus);
            
            // 포션 버프
            if (GameManager.Instance.buffDuration > 0)
            {
                ctx.FlatDamageBonus += GameManager.Instance.buffDamageValue;
            }
            if (GameManager.Instance.nextZoneBuffs.Contains("DamageBoost"))
            {
                ctx.FlatDamageBonus += 15;
            }
        }
        
        // 이벤트 시스템으로 유물 효과 적용
        GameEvents.RaiseBeforeAttack(ctx);
        
        return (ctx.CalculateFinalDamage(), ctx.CalculateFinalGold());
    }

    public void ShowAttackPreview(AttackJokbo jokbo)
    {
        (int finalBaseDamage, int finalBaseGold) = GetPreviewValues(jokbo);

        AttackJokbo modifiedJokbo = new AttackJokbo(
            jokbo.Description,
            finalBaseDamage,
            finalBaseGold,
            jokbo.CheckLogic
        );

        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            enemy.ShowDamagePreview(modifiedJokbo);
        }
    }

    public void HideAllAttackPreviews()
    {
        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            enemy.HideDamagePreview();
        }
    }

    public void SpawnEnemiesForBoss(GameObject prefab, int count)
    {
        if (prefab == null)
        {
            Debug.LogError("[StageManager] 보스가 소환할 프리팹이 null입니다.");
            return;
        }
        Debug.Log($"[StageManager] 보스 기믹으로 {prefab.name} {count}마리 추가 스폰");
        for (int i = 0; i < count; i++)
        {
            if (enemySpawnPoints.Length == 0)
            {
                Debug.LogError("적 스폰 포인트가 없습니다!");
                return;
            }
            int spawnIndex = Random.Range(0, enemySpawnPoints.Length);
            Vector3 spawnPos = enemySpawnPoints[spawnIndex].position;
            Vector2 randomOffset = Random.insideUnitCircle * spawnSpreadRadius;
            spawnPos.x += randomOffset.x;
            spawnPos.y += randomOffset.y;
            if (mainCam != null)
            {
                spawnPos.x = Mathf.Clamp(spawnPos.x, minViewBoundary.x, maxViewBoundary.x);
                spawnPos.y = Mathf.Clamp(spawnPos.y, minViewBoundary.y, maxViewBoundary.y);
            }
            GameObject enemyGO = WaveGenerator.Instance.SpawnFromPool(prefab, spawnPos, Quaternion.identity);
            Enemy newEnemy = enemyGO.GetComponent<Enemy>();
            if (newEnemy != null)
            {
                activeEnemies.Add(newEnemy);
            }
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateWaveInfoPanel(activeEnemies);
        }
    }
}