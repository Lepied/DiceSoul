using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [수정] 몬스터가 화면 밖으로 스폰되지 않도록 위치를 고정(Clamp)합니다.
/// [수정] PrepareNextWave에서 이전 몬스터를 풀(Pool)로 반환하는 로직을 보강합니다.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("연결")]
    public DiceController diceController;

    [Header("웨이브/적 설정")]
    [Tooltip("적이 스폰될 위치 (최소 1개)")]
    public Transform[] enemySpawnPoints;

    [Tooltip("스폰 지점 주변에 퍼지는 반경")]
    public float spawnSpreadRadius = 0.5f;

    [Tooltip("화면 가장자리에서 얼마나 안쪽으로 스폰을 제한할지 (Padding)")]
    public float screenEdgePadding = 1.0f; // 1 유닛만큼 안쪽


    private List<Enemy> activeEnemies = new List<Enemy>();
    private bool isWaitingForAttackChoice = false;

    // [추가] 카메라 경계
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
            diceController = FindObjectOfType<DiceController>();
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없습니다!");
            return;
        }

        // [추가] 카메라 경계 계산
        mainCam = Camera.main;
        if (mainCam != null)
        {
            minViewBoundary = mainCam.ViewportToWorldPoint(new Vector2(0, 0));
            maxViewBoundary = mainCam.ViewportToWorldPoint(new Vector2(1, 1));

            // 패딩 적용
            minViewBoundary.x += screenEdgePadding;
            minViewBoundary.y += screenEdgePadding;
            maxViewBoundary.x -= screenEdgePadding;
            maxViewBoundary.y -= screenEdgePadding;
        }
        else
        {
            Debug.LogError("Main Camera가 없습니다! 스폰 위치가 제한되지 않습니다.");
        }


        // 씬 시작 시 첫 웨이브 준비
        PrepareNextWave();
    }

    public void OnRollFinished(List<int> currentDiceValues)
    {
        // 굴림 시, 모든 적에게 이벤트 전달
        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            enemy.OnPlayerRoll(currentDiceValues);
        }

        // --- (이하 족보 판정 로직은 동일) ---
        if (AttackDB.Instance == null)
        {
            Debug.LogError("AttackDB가 씬에 없습니다!");
            return;
        }

        List<AttackJokbo> achievableJokbos = AttackDB.Instance.GetAchievableJokbos(currentDiceValues);

        if (achievableJokbos.Count > 0)
        {
            Debug.Log($"달성한 족보: {string.Join(", ", achievableJokbos.Select(j => j.Description))}");
            diceController.SetRollButtonInteractable(false);
            isWaitingForAttackChoice = true;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAttackOptions(achievableJokbos);
            }
            else
            {
                Debug.LogError("UIManager가 없습니다! 임시 자동 공격을 실행합니다.");
                ProcessAttack(achievableJokbos[0]);
            }
        }
        else
        {
            Debug.Log("달성한 족보가 없습니다.");
            if (diceController.currentRollCount >= diceController.maxRolls)
            {
                Debug.Log("굴림 횟수 소진. 웨이브 실패.");
                diceController.SetRollButtonInteractable(false);
                GameManager.Instance.ProcessWaveClear(false);
            }
            else
            {
                Debug.Log("다시 굴리세요.");
                diceController.SetRollButtonInteractable(true);
            }
        }
    }

    public void ProcessAttack(AttackJokbo chosenJokbo)
    {
        if (!isWaitingForAttackChoice) return;

        Debug.Log($"광역 공격: [{chosenJokbo.Description}] (데미지: {chosenJokbo.BaseDamage})");

        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            int damageToTake = enemy.CalculateDamageTaken(chosenJokbo);

            // [수정] TakeDamage에 족보 정보 전달
            enemy.TakeDamage(damageToTake, chosenJokbo);
        }

        GameManager.Instance.AddScore(chosenJokbo.BaseScore);
        isWaitingForAttackChoice = false;
        CheckWaveStatus();
    }

    private void CheckWaveStatus()
    {
        // [수정] 모든 적이 (null이 아니고) 죽었는지 확인
        if (activeEnemies.All(e => e == null || e.isDead))
        {
            GameManager.Instance.ProcessWaveClear(true);
        }
        else
        {
            if (diceController.currentRollCount >= diceController.maxRolls)
            {
                Debug.Log("굴림 횟수 소진. 웨이브 실패.");
                diceController.SetRollButtonInteractable(false);
                GameManager.Instance.ProcessWaveClear(false);
            }
            else
            {
                Debug.Log("적이 남았습니다. 다시 굴리세요.");
                diceController.SetRollButtonInteractable(true);
            }
        }
    }


    /// <summary>
    /// [수정] 스폰 위치 고정(Clamp) 및 이전 웨이브 정리 로직
    /// </summary>
    public void PrepareNextWave()
    {
        Debug.Log("StageManager: 다음 웨이브 준비 중...");

        // 1. [문제 1 해결] 이전 웨이브 적들 정리 (풀로 반환)
        // (죽었든 살았든 모든 적을 풀로 돌려보내 화면을 정리합니다)
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                WaveGenerator.Instance.ReturnToPool(enemy.gameObject);
            }
        }
        activeEnemies.Clear();

        // 2. WaveGenerator에게 현재 레벨에 맞는 적 리스트 요청
        if (WaveGenerator.Instance == null)
        {
            Debug.LogError("WaveGenerator가 씬에 없습니다!");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없습니다!");
            return;
        }

        int currentZone = GameManager.Instance.CurrentZone;
        int currentWave = GameManager.Instance.CurrentWave;
        List<GameObject> enemiesToSpawn = WaveGenerator.Instance.GenerateWave(currentZone, currentWave);

        if (enemySpawnPoints.Length == 0)
        {
            Debug.LogError("적 스폰 포인트가 1개 이상 필요합니다!");
            return;
        }

        // 3. 받아온 적 리스트를 스폰 포인트에 배치
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            // [수정] 스폰 위치를 랜덤으로 선택
            int spawnIndex = Random.Range(0, enemySpawnPoints.Length);
            Vector3 spawnPos = enemySpawnPoints[spawnIndex].position;

            // [수정] 랜덤 오프셋 적용
            Vector2 randomOffset = Random.insideUnitCircle * spawnSpreadRadius;
            spawnPos.x += randomOffset.x;
            spawnPos.y += randomOffset.y;

            // [!!! 문제 2 해결 !!!]
            // 카메라 경계가 설정되었다면, 스폰 위치를 화면 안으로 고정(Clamp)
            if (mainCam != null)
            {
                spawnPos.x = Mathf.Clamp(spawnPos.x, minViewBoundary.x, maxViewBoundary.x);
                spawnPos.y = Mathf.Clamp(spawnPos.y, minViewBoundary.y, maxViewBoundary.y);
            }

            // 4. 풀(Pool)에서 스폰
            GameObject enemyGO = WaveGenerator.Instance.SpawnFromPool(enemiesToSpawn[i], spawnPos, Quaternion.identity);
            Enemy newEnemy = enemyGO.GetComponent<Enemy>();
            if (newEnemy != null)
            {
                activeEnemies.Add(newEnemy);
            }
        }

        // 5. [추가] 스폰된 모든 적에게 OnWaveStart 이벤트 전달
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.OnWaveStart(activeEnemies);
            }
        }

        // 6. GameManager/DiceController/유물 효과 적용 (기존과 동일)
        GameManager.Instance.StartNewWave();

        diceController.PrepareNewTurn();

        GameManager.Instance.ApplyAllRelicEffects(diceController);
        
        diceController.SetDiceDeck(GameManager.Instance.playerDiceDeck);
    }
}

