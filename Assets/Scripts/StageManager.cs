using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// [수정] 
/// 1. ProcessAttack이 GameManager의 GetAttackDamageBonus()를 호출
/// 2. 수정된(modified) 족보를 생성할 때 AttackJokbo.CheckLogic을 사용
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

    // 카메라 경계
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

        // 카메라 경계 계산
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

    /// <summary>
    /// 굴림이 끝나면 호출됨 (적 기믹 발동)
    /// </summary>
    public void OnRollFinished(List<int> currentDiceValues)
    {
        // 1. 굴림 시, 모든 적에게 이벤트 전달
        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            enemy.OnPlayerRoll(currentDiceValues);
        }

        // 2. 족보 판정
        if (AttackDB.Instance == null)
        {
            Debug.LogError("AttackDatabase가 씬에 없습니다!");
            return;
        }
        List<AttackJokbo> achievableJokbos = AttackDB.Instance.GetAchievableJokbos(currentDiceValues);

        if (achievableJokbos.Count > 0)
        {
            // 3. 공격 가능: UI 표시
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
            // 4. 공격 불가능: 턴 확인
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
    
    /// <summary>
    /// [!!! 여기가 핵심 수정 !!!]
    /// UIManager가 공격 족보를 선택하면 호출됨 (광역 공격)
    /// </summary>
    public void ProcessAttack(AttackJokbo chosenJokbo)
    {
        if (!isWaitingForAttackChoice) return;

        // 1. 기본 데미지와 점수를 가져옵니다.
        int baseDamage = chosenJokbo.BaseDamage;
        int baseScore = chosenJokbo.BaseScore;

        // 2. [신규] GameManager에게 유물로 인한 '추가 데미지'를 물어봅니다.
        int bonusDamage = GameManager.Instance.GetAttackDamageBonus();
        int finalBaseDamage = baseDamage + bonusDamage;

        Debug.Log($"광역 공격: [{chosenJokbo.Description}] (기본: {baseDamage}, 보너스: {bonusDamage}, 총: {finalBaseDamage})");

        // 3. [신규] 계산된 최종 데미지로 새 '임시' 족보 객체를 만듭니다.
        // (Enemy가 CalculateDamageTaken에서 올바른 데미지를 참조하도록)
        AttackJokbo modifiedJokbo = new AttackJokbo(
            chosenJokbo.Description,
            finalBaseDamage, // <-- 수정된 데미지
            baseScore,       // <-- 점수는 그대로
            chosenJokbo.CheckLogic // <-- [오류 수정] public이 된 CheckLogic 참조
        );

        // 4. 모든 적에게 '수정된 족보'로 데미지를 계산하고 적용합니다.
        foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
        {
            int damageToTake = enemy.CalculateDamageTaken(modifiedJokbo);
            enemy.TakeDamage(damageToTake, modifiedJokbo);
        }

        // 5. 점수는 '원본 족보' 기준으로 추가합니다 (데미지 유물이 점수를 올리진 않음)
        GameManager.Instance.AddScore(baseScore);
        isWaitingForAttackChoice = false;
        
        // 6. 웨이브 상태 확인
        CheckWaveStatus();
    }

    /// <summary>
    /// 적들이 모두 죽었는지, 굴림 횟수가 남았는지 확인
    /// </summary>
    private void CheckWaveStatus()
    {
        // 1. 모든 적이 죽었는지 확인
        if (activeEnemies.All(e => e == null || e.isDead))
        {
            GameManager.Instance.ProcessWaveClear(true); // 성공
        }
        else // 2. 아직 적이 남음
        {
            if (diceController.currentRollCount >= diceController.maxRolls)
            {
                // 굴림 횟수 없음 = 실패
                Debug.Log("굴림 횟수 소진. 웨이브 실패.");
                diceController.SetRollButtonInteractable(false);
                GameManager.Instance.ProcessWaveClear(false);
            }
            else
            {
                // 굴림 횟수 남음 = 턴 계속
                Debug.Log("적이 남았습니다. 다시 굴리세요.");
                diceController.SetRollButtonInteractable(true); 
            }
        }
    }

    /// <summary>
    /// 다음 웨이브를 준비하고, DiceController를 올바른 순서로 초기화합니다.
    /// </summary>
    public void PrepareNextWave()
    {
        Debug.Log("StageManager: 다음 웨이브 준비 중...");
        isWaitingForAttackChoice = false; // 공격 대기 상태 리셋
        
        // 1. 이전 웨이브 적들 정리 (풀로 반환)
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                WaveGenerator.Instance.ReturnToPool(enemy.gameObject);
            }
        }
        activeEnemies.Clear();

        // 2. WaveGenerator에게 새 적 리스트 요청
        if (WaveGenerator.Instance == null)
        {
            Debug.LogError("WaveGenerator가 씬에 없습니다!");
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
        
        // 3. 새 적 스폰 (위치 고정 포함)
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
        
        // 4. 스폰된 모든 적에게 OnWaveStart 이벤트 전달
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.OnWaveStart(activeEnemies);
            }
        }
        
        // --- 5. DiceController 초기화 (순서 중요) ---
        
        // 5-1. GameManager에게 새 웨이브 시작 알림 (UI 업데이트 등)
        GameManager.Instance.StartNewWave();
        
        // 5-2. DiceController 리셋 (기존 주사위 풀 반환, maxRolls 리셋)
        if (diceController != null)
        {
            diceController.PrepareNewTurn();
        }

        // 5-3. GameManager에게 유물 효과 적용 요청 (maxRolls 등 변경)
        if (GameManager.Instance != null && diceController != null)
        {
            GameManager.Instance.ApplyAllRelicEffects(diceController);
        }

        // 5-4. 변경된 덱 정보(playerDiceDeck)로 새 주사위를 '스폰'하라고 명령
        if (GameManager.Instance != null && diceController != null)
        {
            diceController.SetDiceDeck(GameManager.Instance.playerDiceDeck);
        }
        
    }
}

