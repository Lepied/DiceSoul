using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [!!! 핵심 수정 (SO + 인스펙터 방식) !!!]
/// 1. WaveGenerator가 'List<GameObject>' (프리팹)를 반환합니다.
/// 2. 스폰 시 'Initialize(data)' 함수를 호출할 필요가 없습니다.
///    (스탯은 이미 프리팹의 인스펙터에 저장되어 있습니다.)
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
            Debug.LogError("AttackDB가 씬에 없습니다!");
            return;
        }
        List<AttackJokbo> achievableJokbos = AttackDB.Instance.GetAchievableJokbos(modifiedValues);

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
            else
            {
                Debug.LogError("UIManager가 없습니다! 임시 자동 공격을 실행합니다.");
                ProcessAttack(achievableJokbos[0]);
            }
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
                Debug.Log("다시 굴리세요.");
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
    /// 1. WaveGenerator가 'List<GameObject>' (프리팹)를 반환
    /// 2. 스폰 시 'Initialize(data)' 함수를 호출할 필요가 없음
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

        // 2. WaveGenerator에게 새 적 리스트 요청
        if (WaveGenerator.Instance == null || GameManager.Instance == null)
        {
            Debug.LogError("WaveGenerator 또는 GameManager가 씬에 없습니다!");
            return;
        }
        int currentZone = GameManager.Instance.CurrentZone;
        int currentWave = GameManager.Instance.CurrentWave;
        
        // [!!!] 'List<GameObject>' (프리팹 리스트)를 받음
        List<GameObject> enemiesToSpawn = WaveGenerator.Instance.GenerateWave(currentZone, currentWave);

        if (enemySpawnPoints.Length == 0)
        {
            Debug.LogError("적 스폰 포인트가 1개 이상 필요합니다!");
            return;
        }
        
        // 3. 새 적 스폰 (위치 고정 포함)
        foreach (GameObject enemyPrefab in enemiesToSpawn) // [!!!] 프리팹 리스트를 순회
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("[StageManager] 스폰할 프리팹이 null입니다.");
                continue;
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
            
            // 4. [!!!] 스폰
            GameObject enemyGO = WaveGenerator.Instance.SpawnFromPool(enemyPrefab, spawnPos, Quaternion.identity);
            Enemy newEnemy = enemyGO.GetComponent<Enemy>();
            
            // 5. [!!!] 스탯 주입(Initialize) 로직 삭제
            // (스탯은 프리팹의 인스펙터에 이미 저장되어 있음)
            
            if (newEnemy != null)
            {
                activeEnemies.Add(newEnemy);
            }
            else
            {
                Debug.LogError($"{enemyPrefab.name} 프리팹에 Enemy 스크립트가 없습니다.");
            }
        }
        
        // 6. 스폰된 모든 적에게 OnWaveStart 이벤트 전달
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.OnWaveStart(activeEnemies);
            }
        }

        // 7. UI 및 DiceController 초기화
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
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClearScoreText();
        }
    }

    // ... (GetPreviewValues, ShowAttackPreview, HideAllAttackPreviews 함수는 모두 동일) ...
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
}