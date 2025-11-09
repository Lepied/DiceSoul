using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. CheckWaveStatus 함수가 '남은 굴림 횟수(rollsRemaining)'를 계산
/// 2. GameManager.ProcessWaveClear(isSuccess, rollsRemaining)을 호출 (인자 2개)
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
                List<AttackJokbo> previewJokbos = new List<AttackJokbo>();
                foreach (var jokbo in achievableJokbos)
                {
                    int baseDamage = jokbo.BaseDamage;
                    int bonusDamage = GameManager.Instance.GetAttackDamageModifiers(jokbo);
                    int finalBaseDamage = baseDamage + bonusDamage;

                    int baseScore = jokbo.BaseScore;
                    int bonusScore = GameManager.Instance.GetAttackScoreBonus(jokbo); 
                    int finalBaseScore = baseScore + bonusScore;

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
                // [수정] 실패 시 0 전달
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
        
        int baseDamage = chosenJokbo.BaseDamage;
        int baseScore = chosenJokbo.BaseScore; 
        int finalBaseDamage = baseDamage;

        Debug.Log($"광역 공격: [{chosenJokbo.Description}] (최종 데미지: {finalBaseDamage})");
        
        AttackJokbo modifiedJokbo = chosenJokbo;

        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            int damageToTake = enemy.CalculateDamageTaken(modifiedJokbo);
            enemy.TakeDamage(damageToTake, modifiedJokbo);
        }

        GameManager.Instance.AddScore(baseScore, chosenJokbo);
        
        isWaitingForAttackChoice = false;
        CheckWaveStatus();
    }

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// 남은 굴림 횟수를 계산하여 GameManager에게 전달합니다.
    /// </summary>
    private void CheckWaveStatus()
    {
        // 1. 모든 적이 죽었는지 확인
        if (activeEnemies.All(e => e == null || e.isDead))
        {
            // [수정] 남은 굴림 횟수 계산
            int rollsRemaining = diceController.maxRolls - diceController.currentRollCount;
            GameManager.Instance.ProcessWaveClear(true, rollsRemaining); // 성공 + 남은 횟수 전달
        }
        else // 2. 아직 적이 남음
        {
            if (diceController.currentRollCount >= diceController.maxRolls)
            {
                // [수정] 굴림 횟수 없음 = 실패
                Debug.Log("굴림 횟수 소진. 웨이브 실패.");
                diceController.SetRollButtonInteractable(false);
                GameManager.Instance.ProcessWaveClear(false, 0); // 실패 + 0 전달
            }
            else
            {
                // 굴림 횟수 남음 = 턴 계속
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

        if (WaveGenerator.Instance == null)
        {
            Debug.LogError("WaveGenerator가 씬에 없습니다!");
            return;
        }
        int currentZone = GameManager.Instance.CurrentZone;
        int currentWave = GameManager.Instance.CurrentWave;
        bool isBossWave = (currentWave == GameManager.Instance.wavesPerZone); 
        List<GameObject> enemiesToSpawn = WaveGenerator.Instance.GenerateWave(currentZone, currentWave, isBossWave);
        if (enemySpawnPoints.Length == 0)
        {
            Debug.LogError("적 스폰 포인트가 1개 이상 필요합니다!");
            return;
        }
        
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
            GameObject enemyGO = WaveGenerator.Instance.SpawnFromPool(enemiesToSpawn[i], spawnPos, Quaternion.identity);
            Enemy newEnemy = enemyGO.GetComponent<Enemy>();
            if (newEnemy != null)
            {
                activeEnemies.Add(newEnemy);
            }
        }
        
        foreach (Enemy enemy in activeEnemies)
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
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClearScoreText();
        }
    }

    
    public void ShowAttackPreview(AttackJokbo jokbo)
    {
        int baseDamage = jokbo.BaseDamage;
        int baseScore = jokbo.BaseScore;
        int bonusDamage = GameManager.Instance.GetAttackDamageModifiers(jokbo);
        int bonusScore = GameManager.Instance.GetAttackScoreBonus(jokbo); 
        
        int finalBaseDamage = baseDamage + bonusDamage;
        int finalBaseScore = baseScore + bonusScore; 

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