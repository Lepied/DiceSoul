using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [!!! CS7036 오류 수정 !!!]
/// 1. 'GetPreviewValues(AttackJokbo jokbo)' 헬퍼 함수를 'public'으로 새로 추가
///    (UIManager.ShowAttackOptions가 호출할 수 있도록)
/// 2. [버그 수정] ProcessAttack이 GetPreviewValues를 재사용하여
///    데미지/점수 계산이 '명함' 유물과 일관되게 작동하도록 수정
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

    /// <summary>
    /// [수정] UIManager.ShowAttackOptions가 '원본 족보' 리스트를 받도록 수정
    /// </summary>
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
                // [!!!] UIManager에게 '원본' 족보 리스트를 전달
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
    
    /// <summary>
    /// [수정] '원본 족보'를 받아 '최종 데미지/점수'를 계산
    /// </summary>
    public void ProcessAttack(AttackJokbo chosenJokbo)
    {
        if (!isWaitingForAttackChoice) return;
        
        // 1. [!!!] UIManager가 '원본 족보'를 전달
        // 2. [!!!] GetPreviewValues를 '재사용'하여 최종 데미지/점수 계산
        (int finalDamage, int finalScore) = GetPreviewValues(chosenJokbo);
        
        Debug.Log($"광역 공격: [{chosenJokbo.Description}] (최종 데미지: {finalDamage})");

        // 3. 계산된 최종 데미지로 '수정된 족보' 생성
        AttackJokbo modifiedJokbo = new AttackJokbo(
            chosenJokbo.Description,
            finalDamage, 
            finalScore, // (점수도 최종 점수를 전달)     
            chosenJokbo.CheckLogic
        );
        
        // 4. 적 공격
        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            int damageToTake = enemy.CalculateDamageTaken(modifiedJokbo);
            enemy.TakeDamage(damageToTake, modifiedJokbo);
        }

        // 5. GameManager에게 '최종 점수'와 '원본 족보' 전달
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

    
    /// <summary>
    /// UIManager.ShowAttackOptions가 '최종 값'을 표시하기 위해 호출하는 헬퍼 함수.
    /// '명함' (첫 굴림 2배) 등 모든 보너스를 계산하여 (데미지, 점수)를 반환합니다.
    /// </summary>
    public (int finalDamage, int finalScore) GetPreviewValues(AttackJokbo jokbo)
    {
        // 1. 족보의 '원본' 데미지/점수
        int baseDamage = jokbo.BaseDamage;
        int baseScore = jokbo.BaseScore;

        // 2. GameManager에게 '보너스' 데미지/점수 요청
        int bonusDamage = GameManager.Instance.GetAttackDamageModifiers(jokbo);
        int bonusScore = GameManager.Instance.GetAttackScoreBonus(jokbo); 
        
        int finalBaseDamage = baseDamage + bonusDamage;
        int finalBaseScore = baseScore + bonusScore; 
        
        // 3. [!!! "명함" 효과 적용 !!!]
        var (rollDamageMult, rollScoreMult) = GameManager.Instance.GetRollCountBonuses(diceController.currentRollCount);

        if (rollDamageMult > 1.0f || rollScoreMult > 1.0f)
        {
            finalBaseDamage = (int)(finalBaseDamage * rollDamageMult);
            finalBaseScore = (int)(finalBaseScore * rollScoreMult); 
        }
        
        return (finalBaseDamage, finalBaseScore);
    }
    
    /// <summary>
    /// 족보 미리보기를 표시합니다. (GetPreviewValues 로직을 사용)
    /// </summary>
    public void ShowAttackPreview(AttackJokbo jokbo)
    {
        // 1. [!!!] GetPreviewValues 헬퍼 함수를 사용하여 '최종 값' 계산
        (int finalBaseDamage, int finalBaseScore) = GetPreviewValues(jokbo);

        // 2. 계산된 최종 데미지/점수로 '임시' 족보 객체를 만듭니다.
        AttackJokbo modifiedJokbo = new AttackJokbo(
            jokbo.Description,
            finalBaseDamage, 
            finalBaseScore, 
            jokbo.CheckLogic
        );

        // 3. 모든 적에게 '수정된 족보'로 프리뷰를 표시하라고 지시
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