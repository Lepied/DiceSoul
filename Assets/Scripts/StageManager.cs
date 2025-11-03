using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [수정] PrepareNextWave 함수가 '보스 웨이브' 여부를 판단하여
/// WaveGenerator.GenerateWave 함수에 (isBossWave) bool 값을 전달합니다.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("연결")]
    public DiceController diceController;

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
        // ... (연결 로직 동일) ...
        if (diceController == null)
        {
            diceController = FindObjectOfType<DiceController>();
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

    // ... (OnRollFinished, ProcessAttack, CheckWaveStatus 함수는 모두 동일) ...
    public void OnRollFinished(List<int> currentDiceValues)
    {
        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            enemy.OnPlayerRoll(currentDiceValues);
        }
        List<int> modifiedValues = GameManager.Instance.ApplyDiceModificationRelics(currentDiceValues);
        if (AttackDB.Instance == null)
        {
            Debug.LogError("AttackDB 씬에 없습니다!");
            return;
        }
        List<AttackJokbo> achievableJokbos = AttackDB.Instance.GetAchievableJokbos(modifiedValues);
        if (achievableJokbos.Count > 0)
        {
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
            if (diceController.currentRollCount >= diceController.maxRolls) // (오타 수정: maxRolls)
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
        int baseDamage = chosenJokbo.BaseDamage;
        int baseScore = chosenJokbo.BaseScore;
        int bonusDamage = GameManager.Instance.GetAttackDamageBonus();
        int finalBaseDamage = baseDamage + bonusDamage;
        Debug.Log($"광역 공격: [{chosenJokbo.Description}] (기본: {baseDamage}, 보너스: {bonusDamage}, 총: {finalBaseDamage})");
        AttackJokbo modifiedJokbo = new AttackJokbo(
            chosenJokbo.Description,
            finalBaseDamage, 
            baseScore,       
            chosenJokbo.CheckLogic
        );
        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            int damageToTake = enemy.CalculateDamageTaken(modifiedJokbo);
            enemy.TakeDamage(damageToTake, modifiedJokbo);
        }
        GameManager.Instance.AddScore(baseScore);
        isWaitingForAttackChoice = false;
        CheckWaveStatus();
    }
    private void CheckWaveStatus()
    {
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
    /// [!!! 핵심 수정 !!!]
    /// 'isBossWave' 변수를 계산하여 WaveGenerator에게 전달합니다.
    /// </summary>
    public void PrepareNextWave()
    {
        Debug.Log("StageManager: 다음 웨이브 준비 중...");
        isWaitingForAttackChoice = false; 
        
        // 1. 이전 웨이브 적들 정리
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                WaveGenerator.Instance.ReturnToPool(enemy.gameObject);
            }
        }
        activeEnemies.Clear();

        // 2. [변경] WaveGenerator에게 전달할 정보 수집
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
        
        // [!!!] 'wavesPerZone' 변수(GameManager에 있어야 함)를 기준으로 보스 웨이브 여부 판단
        bool isBossWave = (currentWave == GameManager.Instance.wavesPerZone); 
        
        // [변경] GenerateWave 함수에 3번째 인자로 'isBossWave' 전달
        List<GameObject> enemiesToSpawn = WaveGenerator.Instance.GenerateWave(currentZone, currentWave, isBossWave);

        if (enemySpawnPoints.Length == 0)
        {
            Debug.LogError("적 스폰 포인트가 1개 이상 필요합니다!");
            return;
        }
        
        // 3. 새 적 스폰
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
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
            GameObject enemyGO = WaveGenerator.Instance.SpawnFromPool(enemiesToSpawn[i], spawnPos,Quaternion.identity);
            Enemy newEnemy = enemyGO.GetComponent<Enemy>();
            if (newEnemy != null)
            {
                activeEnemies.Add(newEnemy);
            }
        }
        
        // 4. 스폰된 모든 적에게 OnWaveStart 이벤트 전달
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.OnWaveStart(activeEnemies);
            }
        }

        // 5. UIManager에게 적 정보 업데이트 알림
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateWaveInfoPanel(activeEnemies);
        }
        
        // 6. DiceController 초기화 (순서 중요)
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
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClearScoreText();
        }
    }
}

