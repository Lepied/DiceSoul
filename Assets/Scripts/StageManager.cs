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

        // 2. 유물 효과 계산 적용하기
        List<int> modifiedValues = GameManager.Instance.ApplyDiceModificationRelics(initialValues);

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

        // 4. 변화가 있었다면, 연출이 끝날 때까지 잠시 대기
        if (anyChange)
        {
            yield return new WaitForSeconds(0.6f);
        }
        //최종값으로 족보 계산
        CheckJokbo(modifiedValues);
    }

    //족보 계산 및 UI 표시
    private void CheckJokbo(List<int> finalValues)
    {
        if (AttackDB.Instance == null) return;

        List<AttackJokbo> achievableJokbos = AttackDB.Instance.GetAchievableJokbos(finalValues);

        if (achievableJokbos.Count > 0)
        {
            diceController.SetRollButtonInteractable(false);
            isWaitingForAttackChoice = true;
            if (UIManager.Instance != null)
            {
                List<AttackJokbo> previewJokbos = new List<AttackJokbo>();
                foreach (var jokbo in achievableJokbos)
                {
                    (int finalBaseDamage, int finalBaseScore) = GetPreviewValues(jokbo);
                    previewJokbos.Add(new AttackJokbo(
                        jokbo.Description,
                        finalBaseDamage,
                        finalBaseScore,
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

        (int finalDamage, int finalScore) = GetPreviewValues(chosenJokbo);

        Debug.Log($"광역 공격: [{chosenJokbo.Description}] (최종 데미지: {finalDamage})");

        AttackJokbo modifiedJokbo = new AttackJokbo(
            chosenJokbo.Description,
            finalDamage,
            finalScore,
            chosenJokbo.CheckLogic
        );

        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            int damageToTake = enemy.CalculateDamageTaken(modifiedJokbo);
            enemy.TakeDamage(damageToTake, modifiedJokbo);
        }

        GameManager.Instance.AddScore(finalScore, chosenJokbo);

        isWaitingForAttackChoice = false;
        CheckWaveStatus();
    }

    private void CheckWaveStatus()
    {
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

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// 1. 'isBossWave' bool 변수 삭제
    /// 2. WaveGenerator.GetCurrentZoneData() (public 함수) 호출
    /// 3. [!!!] backgroundUI.sprite -> backgroundRenderer.sprite로 변경
    /// 4. WaveGenerator.GenerateWave(zone, wave) (인자 2개) 호출
    /// </summary>
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

        // [!!!] 'GetCurrentZoneData' (public 함수) 호출
        ZoneData currentZoneData = WaveGenerator.Instance.GetCurrentZoneData(currentZone);

        // [!!! 배경 교체 로직 수정 !!!]
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

        // 4. 새 적 스폰 (이하 동일)
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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateWaveInfoPanel(activeEnemies);
        }
        GameManager.Instance.StartNewWave();
        if (diceController != null)
        {
            diceController.PrepareNewTurn();
        }
        if (GameManager.Instance != null && diceController != null)
        {
            GameManager.Instance.ApplyAllRelicEffects(diceController);
        }
        if (GameManager.Instance != null && diceController != null)
        {
            diceController.SetDiceDeck(GameManager.Instance.playerDiceDeck);
        }
    }

    public (int finalDamage, int finalScore) GetPreviewValues(AttackJokbo jokbo)
    {
        int baseDamage = jokbo.BaseDamage;
        int baseScore = jokbo.BaseScore;

        int bonusDamage = GameManager.Instance.GetAttackDamageModifiers(jokbo);
        int bonusScore = GameManager.Instance.GetAttackScoreBonus(jokbo);

        int finalBaseDamage = baseDamage + bonusDamage;
        int finalBaseScore = baseScore + bonusScore;

        var (rollDamageMult, rollScoreMult) = GameManager.Instance.GetRollCountBonuses(diceController.currentRollCount);

        if (rollDamageMult > 1.0f || rollScoreMult > 1.0f)
        {
            finalBaseDamage = (int)(finalBaseDamage * rollDamageMult);
            finalBaseScore = (int)(finalBaseScore * rollScoreMult);
        }

        return (finalBaseDamage, finalBaseScore);
    }

    public void ShowAttackPreview(AttackJokbo jokbo)
    {
        (int finalBaseDamage, int finalBaseScore) = GetPreviewValues(jokbo);

        AttackJokbo modifiedJokbo = new AttackJokbo(
            jokbo.Description,
            finalBaseDamage,
            finalBaseScore,
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