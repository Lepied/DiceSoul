using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

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
    public float minEnemySpacing = 1.5f;  // 적들 간 최소 거리
    public int maxSpawnAttempts = 10;     // 스폰 위치 재시도 횟수

    public List<Enemy> activeEnemies = new List<Enemy>();
    private bool isWaitingForAttackChoice = false;

    // 튜토리얼 콜백
    public System.Action onJokboSelectedCallback;

    // 연쇄 공격 시스템 변수
    private AttackJokbo currentSelectedJokbo = null;      // 현재 선택된 족보
    private List<Enemy> currentSelectedTargets = new List<Enemy>();  // 선택된 타겟들
    private bool isWaitingForTargetSelection = false;     // 타겟 선택 대기 중
    private int requiredTargetCount = 0;                  // 선택해야 할 타겟 수
    private int currentChainCount = 0;                    // 현재 연쇄 공격 횟수 (광택 구슬용)

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

    public void OnRollFinished(List<int> currentDiceValues, bool isManualRoll = true)
    {
        StartCoroutine(ProcessDiceResults(currentDiceValues, isManualRoll));
    }

    //유물 효과 적용 및 연출 처리 코루틴
    private IEnumerator ProcessDiceResults(List<int> initialValues, bool isManualRoll = true)
    {
        // 1. 적 턴 반응 / 기믹 처리 (수동 리롤 버튼을 누른 경우만)
        if (isManualRoll)
        {
            foreach (Enemy enemy in activeEnemies.Where(e => e != null && !e.isDead))
            {
                enemy.OnPlayerRoll(initialValues);
            }
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

        // Locked 주사위 제외한 사용 가능한 값들로 족보 계산
        List<int> availableValues = diceController.GetAvailableValues();
        List<AttackJokbo> achievableJokbos = AttackDB.Instance.GetAchievableJokbos(availableValues);

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
                    // 복사 생성자 사용하여 모든 로직 유지
                    var preview = new AttackJokbo(jokbo);
                    // 프리뷰용 데미지/골드는 수동으로 업데이트 필요 (참조 타입 문제로 private set이므로 불가)
                    previewJokbos.Add(jokbo); // 원본 그대로 사용 (프리뷰 값은 GetPreviewValues로 처리됨)
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

        currentSelectedJokbo = chosenJokbo;
        
        // 튜토리얼 콜백 호출
        onJokboSelectedCallback?.Invoke();

        // 공격 타입에 따라 처리 분기
        switch (chosenJokbo.TargetType)
        {
            case AttackTargetType.AoE:
                ExecuteAoEAttack(chosenJokbo);
                break;

            case AttackTargetType.Single:
                // 타겟 선택 대기
                StartTargetSelection(chosenJokbo);
                break;

            case AttackTargetType.Random:
                ExecuteRandomAttack(chosenJokbo);
                break;

            case AttackTargetType.Hybrid:
                // 복합 공격은 먼저 주공격 타겟 선택
                StartTargetSelection(chosenJokbo);
                break;
                
            case AttackTargetType.Defense:
                ExecuteDefense(chosenJokbo);
                break;
        }
    }
    
    // 수비 (실드 얻기)
    private void ExecuteDefense(AttackJokbo jokbo)
    {
        (int shieldAmount, _) = GetPreviewValues(jokbo);
        
        Debug.Log($"[수비] {jokbo.Description} - 실드 {shieldAmount} 획득");

        diceController.RemoveDiceByIndices(jokbo.UsedDiceIndices);
        
        GameManager.Instance.AddShield(shieldAmount);
        
        CheckWaveStatus();
    }

    //전체 공격 (AoE) 실행
    private void ExecuteAoEAttack(AttackJokbo jokbo)
    {
        // 최종 데미지 계산
        (int finalDamage, int finalGold) = GetPreviewValues(jokbo);
        int bonusDamage = finalDamage - jokbo.BaseDamage;
        
        // 실제 공격용 AttackContext 생성
        AttackContext attackCtx = CreateAttackContext(jokbo, finalDamage, finalGold);

        Debug.Log($"[전체 공격] {jokbo.Description} - 데미지: {finalDamage}");

        List<Enemy> targets = activeEnemies.Where(e => e != null && !e.isDead).ToList();
        if (targets.Count == 0)
        {
            FinishAttackAndCheckChain(jokbo);
            return;
        }

        // VFX 통합 버전
        if (VFXManager.Instance != null && jokbo.VfxConfig != null)
        {
            // 주사위 위치들
            Vector3[] dicePos = diceController.GetDicePositions(jokbo.UsedDiceIndices);
            
            // 적 위치들
            Vector3[] targetPos = targets.Select(e => e.transform.position).ToArray();

            // 주사위 제거 시작 (VFX와 동시)
            diceController.RemoveDiceByIndices(jokbo.UsedDiceIndices);

            // VFX 재생
            VFXManager.Instance.PlayAoEAttack(
                config: jokbo.VfxConfig,
                dicePositions: dicePos,
                targetPositions: targetPos,
                onImpact: (int targetIndex) =>
                {
                    // 각 타겟에 데미지 적용
                    if (targetIndex < targets.Count)
                    {
                        Enemy enemy = targets[targetIndex];
                        int damageToTake = enemy.CalculateDamageTaken(jokbo) + bonusDamage;
                        enemy.TakeDamage(damageToTake, jokbo);
                        Debug.Log($"  → {enemy.name} - 데미지: {damageToTake}");
                    }
                },
                onComplete: () =>
                {
                    // VFX 완료 후
                    GameManager.Instance.AddGoldDirect(finalGold);
                    GameEvents.RaiseAfterAttack(attackCtx);
                    FinishAttackAndCheckChain(jokbo);
                }
            );
        }
        else
        {
            // VFX 없으면 즉시 데미지
            foreach (Enemy enemy in targets)
            {
                int damageToTake = enemy.CalculateDamageTaken(jokbo) + bonusDamage;
                enemy.TakeDamage(damageToTake, jokbo);
                Debug.Log($"  → {enemy.name} - 데미지: {damageToTake}");
            }

            GameManager.Instance.AddGoldDirect(finalGold);
            GameEvents.RaiseAfterAttack(attackCtx);
            FinishAttackAndCheckChain(jokbo);
        }
    }

    // 랜덤 공격 실행
    private void ExecuteRandomAttack(AttackJokbo jokbo)
    {
        List<Enemy> aliveEnemies = activeEnemies.Where(e => e != null && !e.isDead).ToList();
        if (aliveEnemies.Count == 0)
        {
            FinishAttackAndCheckChain(jokbo);
            return;
        }
        
        // 런 통계: 족보 사용 기록
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordJokboUsage(jokbo.Description);
        }

        // 최종 데미지 계산 (유물 효과 포함)
        (int finalDamage, int finalGold) = GetPreviewValues(jokbo);
        
        // RandomTargetCount 계산 (총합의 경우 최종 데미지/10)
        int randomTargetCount = jokbo.RandomTargetCount;
        if (randomTargetCount == 0)  // 총합의 경우면
        {
            randomTargetCount = Mathf.Max(1, finalDamage / 10);  // 최소 1명
        }

        // 생존 적 수보다 많으면 조정
        randomTargetCount = Mathf.Min(randomTargetCount, aliveEnemies.Count);

        Debug.Log($"[랜덤 공격] {jokbo.Description} → {randomTargetCount}명의 적 - 데미지: {finalDamage}");

        // 랜덤 타겟 선택
        List<Enemy> randomTargets = new List<Enemy>();
        List<Enemy> availableEnemies = new List<Enemy>(aliveEnemies);
        
        for (int i = 0; i < randomTargetCount; i++)
        {
            if (availableEnemies.Count == 0) break;
            
            int randomIndex = Random.Range(0, availableEnemies.Count);
            Enemy target = availableEnemies[randomIndex];
            randomTargets.Add(target);
            availableEnemies.RemoveAt(randomIndex);  // 중복 방지
        }

        int bonusDamage = finalDamage - jokbo.BaseDamage;

        // VFX 통합 버전
        if (VFXManager.Instance != null && jokbo.VfxConfig != null)
        {
            // 주사위 위치
            Vector3[] dicePos = diceController.GetDicePositions(jokbo.UsedDiceIndices);
            Vector3 centerPos = dicePos.Length > 0 ? CalculateCenterPosition(dicePos) : Vector3.zero;

            // 타겟 위치들
            Vector3[] targetPos = randomTargets.Select(e => e.transform.position).ToArray();
            Transform[] targetTransforms = randomTargets.Select(e => e.transform).ToArray();

            // 주사위 제거 시작
            diceController.RemoveDiceByIndices(jokbo.UsedDiceIndices);

            // VFX 재생 (다중 투사체)
            VFXManager.Instance.PlayMultiProjectileAttack(
                config: jokbo.VfxConfig,
                fromPosition: centerPos,
                toPositions: targetPos,
                targets: targetTransforms,
                onEachReach: (int targetIndex) =>
                {
                    // 각 투사체가 도착할 때 데미지
                    if (targetIndex < randomTargets.Count)
                    {
                        Enemy target = randomTargets[targetIndex];
                        int damageToTake = target.CalculateDamageTaken(jokbo) + bonusDamage;
                        target.TakeDamage(damageToTake, jokbo);
                        Debug.Log($"  → {target.name} - 데미지: {damageToTake}");
                    }
                },
                onComplete: () =>
                {
                    GameManager.Instance.AddGoldDirect(finalGold);
                    FinishAttackAndCheckChain(jokbo);
                }
            );
        }
        else
        {
            // VFX 없으면 즉시 데미지
            foreach (Enemy target in randomTargets)
            {
                int damageToTake = target.CalculateDamageTaken(jokbo) + bonusDamage;
                target.TakeDamage(damageToTake, jokbo);
                Debug.Log($"  → {target.name} - 데미지: {damageToTake}");
            }

            GameManager.Instance.AddGoldDirect(finalGold);
            FinishAttackAndCheckChain(jokbo);
        }
    }

    // 타겟 선택 모드 시작 (Single/Hybrid 공격용)
    private void StartTargetSelection(AttackJokbo jokbo)
    {
        requiredTargetCount = jokbo.RequiredTargetCount;
        
        // 적의 수가 요구 타겟 수보다 적으면 자동으로 모든 적 선택 후 바로 공격
        int aliveEnemyCount = activeEnemies.Count(e => e != null && !e.isDead);
        if (aliveEnemyCount <= requiredTargetCount)
        {
            Debug.Log($"[자동 타겟 선택] 적({aliveEnemyCount}명)이 요구 타겟 수({requiredTargetCount}명) 이하 - 모든 적 자동 선택");
            currentSelectedTargets.Clear();
            currentSelectedTargets.AddRange(activeEnemies.Where(e => e != null && !e.isDead));
            
            // 바로 공격 실행
            if (jokbo.TargetType == AttackTargetType.Single)
            {
                ExecuteMultiTargetAttack(jokbo, currentSelectedTargets);
            }
            else if (jokbo.TargetType == AttackTargetType.Hybrid)
            {
                ExecuteHybridAttack(jokbo, currentSelectedTargets);
            }
            return;
        }
        
        // 적이 충분하면 타겟 선택 모드 진입
        isWaitingForTargetSelection = true;
        currentSelectedJokbo = jokbo;
        currentSelectedTargets.Clear();

        Debug.Log($"[타겟 선택] {jokbo.Description} - {requiredTargetCount}명의 적을 선택하세요");
        
        // UI에 타겟 선택 표시
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTargetSelectionMode(requiredTargetCount, 0);
        }
    }

    // 타겟이 선택되었을 때 호출 (Enemy 클릭 시)
    public void OnEnemySelected(Enemy selectedEnemy)
    {
        if (!isWaitingForTargetSelection || currentSelectedJokbo == null) return;
        if (selectedEnemy == null || selectedEnemy.isDead) return;

        // 이미 선택된 적이면 선택 해제
        if (currentSelectedTargets.Contains(selectedEnemy))
        {
            currentSelectedTargets.Remove(selectedEnemy);
            Debug.Log($"[타겟 선택 해제] {selectedEnemy.name} - 남은 선택: {requiredTargetCount - currentSelectedTargets.Count}명");
            
            // UI 업데이트 (확인 버튼 활성화 상태도 업데이트)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowTargetSelectionMode(requiredTargetCount, currentSelectedTargets.Count);
            }
            return;
        }

        // 필요한 수만큼만 선택 가능
        if (currentSelectedTargets.Count >= requiredTargetCount)
        {
            Debug.Log($"[타겟 선택 제한] 이미 {requiredTargetCount}명을 선택했습니다");
            return;
        }

        // 타겟 추가
        currentSelectedTargets.Add(selectedEnemy);
        Debug.Log($"[타겟 선택] {selectedEnemy.name} - 남은 선택: {requiredTargetCount - currentSelectedTargets.Count}명");
        
        // UI 업데이트 (확인 버튼 활성화 상태도 업데이트)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTargetSelectionMode(requiredTargetCount, currentSelectedTargets.Count);
        }

        // 필요한 수만큼 선택되면 확인 버튼 활성화
        if (currentSelectedTargets.Count >= requiredTargetCount)
        {
            Debug.Log($"[타겟 선택 완료] {requiredTargetCount}명 선택 완료 - 확인 버튼을 누르세요");
        }
    }

    // 타겟 선택 확인 (확인 버튼 클릭 시)
    public void ConfirmTargetSelection()
    {
        if (!isWaitingForTargetSelection || currentSelectedJokbo == null) return;
        
        // 적의 수가 요구 타겟 수보다 적으면 모든 적을 자동 선택
        int aliveEnemyCount = activeEnemies.Count(e => e != null && !e.isDead);
        if (aliveEnemyCount < requiredTargetCount)
        {
            Debug.Log($"[자동 타겟 선택] 적({aliveEnemyCount}명)이 요구 타겟 수({requiredTargetCount}명)보다 적어 모든 적을 선택합니다");
            currentSelectedTargets.Clear();
            currentSelectedTargets.AddRange(activeEnemies.Where(e => e != null && !e.isDead));
        }
        else if (currentSelectedTargets.Count < requiredTargetCount)
        {
            Debug.Log($"[확인 실패] {requiredTargetCount}명을 모두 선택해주세요");
            return;
        }

        isWaitingForTargetSelection = false;
        
        // UI 타겟 선택 모드 종료
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideTargetSelectionMode();
        }

        if (currentSelectedJokbo.TargetType == AttackTargetType.Single)
        {
            ExecuteMultiTargetAttack(currentSelectedJokbo, currentSelectedTargets);
        }
        else if (currentSelectedJokbo.TargetType == AttackTargetType.Hybrid)
        {
            ExecuteHybridAttack(currentSelectedJokbo, currentSelectedTargets);
        }
    }

    // 타겟 선택 취소 (취소 버튼 클릭 시)
    public void CancelTargetSelection()
    {
        if (!isWaitingForTargetSelection) return;

        isWaitingForTargetSelection = false;
        AttackJokbo cancelledJokbo = currentSelectedJokbo;
        currentSelectedJokbo = null;
        currentSelectedTargets.Clear();
        requiredTargetCount = 0;

        Debug.Log("[타겟 선택 취소]");
        
        // UI 타겟 선택 모드 종료
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideTargetSelectionMode();
        }
        
        // 족보 선택 메뉴로 돌아가기
        isWaitingForAttackChoice = true;
        
        // 현재 주사위로 가능한 족보 다시 표시 (Locked 제외)
        List<int> availableValues = diceController.GetAvailableValues();
        List<AttackJokbo> achievableJokbos = AttackDB.Instance.GetAchievableJokbos(availableValues);
        
        if (achievableJokbos.Count > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowAttackOptions(achievableJokbos);
            Debug.Log($"[족보 재표시] {achievableJokbos.Count}개의 족보를 다시 표시합니다");
        }
    }

    // 복수 타겟 공격 실행 (Single 타입의 복수 타겟)
    private void ExecuteMultiTargetAttack(AttackJokbo jokbo, List<Enemy> targets)
    {
        // 최종 데미지 계산
        (int finalDamage, int finalGold) = GetPreviewValues(jokbo);
        int bonusDamage = finalDamage - jokbo.BaseDamage;
        
        // 실제 공격용 AttackContext 생성
        AttackContext attackCtx = CreateAttackContext(jokbo, finalDamage, finalGold);

        Debug.Log($"[복수 타겟 공격] {jokbo.Description} → {targets.Count}명의 적 - 데미지: {finalDamage}");

        // VFX 통합 버전
        if (VFXManager.Instance != null && jokbo.VfxConfig != null)
        {
            // 주사위 위치들
            Vector3[] dicePos = diceController.GetDicePositions(jokbo.UsedDiceIndices);
            Vector3 centerPos = dicePos.Length > 0 ? CalculateCenterPosition(dicePos) : Vector3.zero;
            
            // 살아있는 적만 필터링
            List<Enemy> aliveTargets = targets.Where(t => t != null && !t.isDead).ToList();
            Vector3[] targetPos = aliveTargets.Select(t => t.transform.position).ToArray();
            Transform[] targetTransforms = aliveTargets.Select(t => t.transform).ToArray();


            diceController.RemoveDiceByIndices(jokbo.UsedDiceIndices);

            // VFX 재생
            VFXManager.Instance.PlayMultiProjectileAttack(
                config: jokbo.VfxConfig,
                fromPosition: centerPos,
                toPositions: targetPos,
                targets: targetTransforms,
                onEachReach: (int targetIndex) =>
                {
                    // 각 타겟에 데미지 적용
                    if (targetIndex < aliveTargets.Count)
                    {
                        Enemy enemy = aliveTargets[targetIndex];
                        int damageToTake = enemy.CalculateDamageTaken(jokbo) + bonusDamage;
                        enemy.TakeDamage(damageToTake, jokbo);
                        Debug.Log($"  → {enemy.name} - 데미지: {damageToTake}");
                    }
                },
                onComplete: () =>
                {
                    // VFX 완료 후
                    GameManager.Instance.AddGoldDirect(finalGold);
                    GameEvents.RaiseAfterAttack(attackCtx);
                    FinishAttackAndCheckChain(jokbo);
                }
            );
        }
        else
        {
            // VFX 없으면 즉시 데미지
            foreach (Enemy target in targets)
            {
                if (target != null && !target.isDead)
                {
                    int damageToTake = target.CalculateDamageTaken(jokbo) + bonusDamage;
                    target.TakeDamage(damageToTake, jokbo);
                    Debug.Log($"  → {target.name} - 데미지: {damageToTake}");
                }
            }

            GameManager.Instance.AddGoldDirect(finalGold);
            GameEvents.RaiseAfterAttack(attackCtx);
            FinishAttackAndCheckChain(jokbo);
        }
    }

    // 복합 공격 실행 (주공격 + 부가공격)
    private void ExecuteHybridAttack(AttackJokbo jokbo, List<Enemy> mainTargets)
    {
        // 주공격 실행 - 최종 데미지 계산 (유물 효과 포함)
        (int finalDamage, int finalGold) = GetPreviewValues(jokbo);
        int bonusDamage = finalDamage - jokbo.BaseDamage;
        
        // 실제 공격용 AttackContext 생성
        AttackContext attackCtx = CreateAttackContext(jokbo, finalDamage, finalGold);

        Debug.Log($"[복합 공격] {jokbo.Description} → 주공격: {mainTargets.Count}명 - 데미지: {finalDamage}");

        // VFX
        if (VFXManager.Instance != null && jokbo.VfxConfig != null)
        {
            Vector3[] dicePos = diceController.GetDicePositions(jokbo.UsedDiceIndices);
            Vector3 centerPos = dicePos.Length > 0 ? CalculateCenterPosition(dicePos) : Vector3.zero;
            
            List<Enemy> aliveMainTargets = mainTargets.Where(t => t != null && !t.isDead).ToList();
            Vector3[] mainTargetPos = aliveMainTargets.Select(t => t.transform.position).ToArray();
            Transform[] mainTargetTransforms = aliveMainTargets.Select(t => t.transform).ToArray();

            diceController.RemoveDiceByIndices(jokbo.UsedDiceIndices);
            VFXManager.Instance.PlayMultiProjectileAttack(
                config: jokbo.VfxConfig,
                fromPosition: centerPos,
                toPositions: mainTargetPos,
                targets: mainTargetTransforms,
                onEachReach: (int targetIndex) =>
                {
                    if (targetIndex < aliveMainTargets.Count)
                    {
                        Enemy mainTarget = aliveMainTargets[targetIndex];
                        int mainDamageToTake = mainTarget.CalculateDamageTaken(jokbo) + bonusDamage;
                        mainTarget.TakeDamage(mainDamageToTake, jokbo);
                        Debug.Log($"  → {mainTarget.name} - 주공격 데미지: {mainDamageToTake}");
                    }
                },
                onComplete: () =>
                {
                    GameManager.Instance.AddGoldDirect(finalGold);
                    GameEvents.RaiseAfterAttack(attackCtx);
                    
                    ExecuteSubAttack(jokbo, mainTargets, onSubComplete: () =>
                    {
                        FinishAttackAndCheckChain(jokbo);
                    });
                }
            );
        }
        else
        {
            // VFX 없으면 즉시 데미지
            foreach (Enemy mainTarget in mainTargets)
            {
                if (mainTarget != null && !mainTarget.isDead)
                {
                    int mainDamageToTake = mainTarget.CalculateDamageTaken(jokbo) + bonusDamage;
                    mainTarget.TakeDamage(mainDamageToTake, jokbo);
                    Debug.Log($"  → {mainTarget.name} - 데미지: {mainDamageToTake}");
                }
            }

            GameManager.Instance.AddGoldDirect(finalGold);
            GameEvents.RaiseAfterAttack(attackCtx);
            
            // 부가 공격 실행
            ExecuteSubAttack(jokbo, mainTargets, onSubComplete: () =>
            {
                FinishAttackAndCheckChain(jokbo);
            });
        }
    }

    // 복합 공격의 부가 공격 실행
    private void ExecuteSubAttack(AttackJokbo jokbo, List<Enemy> mainTargets, Action onSubComplete = null)
    {
        int subDamage = jokbo.SubDamage;
        if (subDamage <= 0)
        {
            onSubComplete?.Invoke();
            return;
        }

        Debug.Log($"[복합 - 부가공격] 타입: {jokbo.SubTargetType}, 데미지: {subDamage}");

        var subJokbo = jokbo;

        switch (jokbo.SubTargetType)
        {
            case AttackTargetType.AoE:
                // 전체 공격
                List<Enemy> allTargets = activeEnemies.Where(e => e != null && !e.isDead).ToList();
                
                if (VFXManager.Instance != null && jokbo.SubVfxConfig != null && allTargets.Count > 0)
                {
                    // 부가공격 VFX 있으면 사용
                    Vector3[] targetPos = allTargets.Select(e => e.transform.position).ToArray();
                    
                    VFXManager.Instance.PlayAoEAttack(
                        config: jokbo.SubVfxConfig,
                        dicePositions: new Vector3[0],
                        targetPositions: targetPos,
                        onImpact: (int targetIndex) =>
                        {
                            if (targetIndex < allTargets.Count)
                            {
                                Enemy enemy = allTargets[targetIndex];
                                int damageToTake = enemy.CalculateDamageTaken(subJokbo);
                                enemy.TakeDamage(damageToTake, subJokbo);
                                Debug.Log($"  → {enemy.name} - 부가공격 데미지: {damageToTake}");
                            }
                        },
                        onComplete: onSubComplete
                    );
                }
                else
                {
                    // VFX 없으면 즉시 데미지
                    foreach (Enemy enemy in allTargets)
                    {
                        int damageToTake = enemy.CalculateDamageTaken(subJokbo);
                        enemy.TakeDamage(damageToTake, subJokbo);
                    }
                    onSubComplete?.Invoke();
                }
                break;

            case AttackTargetType.Random:
                // 랜덤 타겟
                List<Enemy> otherEnemies = activeEnemies.Where(e => e != null && !e.isDead && !mainTargets.Contains(e)).ToList();
                if (otherEnemies.Count == 0)
                {
                    otherEnemies = activeEnemies.Where(e => e != null && !e.isDead).ToList();
                }
                
                int subRandomCount = Mathf.Min(jokbo.SubRandomTargetCount, otherEnemies.Count);
                List<Enemy> randomTargets = new List<Enemy>();
                List<Enemy> availableEnemies = new List<Enemy>(otherEnemies);
                
                for (int i = 0; i < subRandomCount; i++)
                {
                    if (availableEnemies.Count == 0) break;
                    
                    int randomIndex = Random.Range(0, availableEnemies.Count);
                    Enemy randomTarget = availableEnemies[randomIndex];
                    randomTargets.Add(randomTarget);
                    availableEnemies.RemoveAt(randomIndex);  // 중복 방지
                }
                
                if (VFXManager.Instance != null && jokbo.SubVfxConfig != null && randomTargets.Count > 0)
                {
                    // 부가공격 VFX 있으면 사용
                    Vector3[] targetPos = randomTargets.Select(e => e.transform.position).ToArray();
                    Transform[] targetTransforms = randomTargets.Select(e => e.transform).ToArray();
                    
                    VFXManager.Instance.PlayMultiProjectileAttack(
                        config: jokbo.SubVfxConfig,
                        fromPosition: Vector3.zero,
                        toPositions: targetPos,
                        targets: targetTransforms,
                        onEachReach: (int targetIndex) =>
                        {
                            if (targetIndex < randomTargets.Count)
                            {
                                Enemy randomTarget = randomTargets[targetIndex];
                                int damageToTake = randomTarget.CalculateDamageTaken(subJokbo);
                                randomTarget.TakeDamage(damageToTake, subJokbo);
                                Debug.Log($"  → 랜덤 타겟: {randomTarget.name} - 부가공격 데미지: {damageToTake}");
                            }
                        },
                        onComplete: onSubComplete
                    );
                }
                else
                {
                    // VFX 없으면 즉시 데미지
                    foreach (Enemy randomTarget in randomTargets)
                    {
                        int damageToTake = randomTarget.CalculateDamageTaken(subJokbo);
                        randomTarget.TakeDamage(damageToTake, subJokbo);
                        Debug.Log($"  → 랜덤 타겟: {randomTarget.name} - 데미지: {damageToTake}");
                    }
                    onSubComplete?.Invoke();
                }
                break;
        }
    }

    // AttackContext 생성 헬퍼 메서드
    private AttackContext CreateAttackContext(AttackJokbo jokbo, int baseDamage, int baseGold)
    {
        return new AttackContext
        {
            Jokbo = jokbo,
            BaseDamage = baseDamage,
            BaseGold = baseGold,
            FlatDamageBonus = 0,
            FlatGoldBonus = 0,
            DamageMultiplier = 1.0f,
            GoldMultiplier = 1.0f,
            IsFirstRoll = (diceController.currentRollCount == 1),
            RemainingRolls = diceController.maxRolls - diceController.currentRollCount,
            HealAfterAttack = 0
        };
    }

    // 공격 완료 후 연쇄 공격 체크
    private void FinishAttackAndCheckChain(AttackJokbo usedJokbo)
    {
        // 주사위는 이미 Execute 메서드에서 제거되었음 (이중 제거 방지)
        
        isWaitingForAttackChoice = false;
        currentSelectedJokbo = null;
        currentSelectedTargets.Clear();
        
        // 연쇄 공격 카운터 증가
        currentChainCount++;
        Debug.Log($"[연쇄 공격] 현재 연쇄 횟수: {currentChainCount}");
        
        // 런 통계: 연쇄 공격 기록
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordChainCount(currentChainCount);
        }

        // 적이 모두 죽었는지 확인
        int aliveEnemyCount = activeEnemies.Count(e => e != null && !e.isDead);
        if (aliveEnemyCount == 0)
        {
            Debug.Log("[연쇄 종료] 모든 적 처치 완료");
            CheckWaveStatus();
            return;
        }

        // 연쇄 공격 체크
        CheckForChainAttack();
    }

    // 연쇄 공격 가능 여부 체크
    private void CheckForChainAttack()
    {
        int remainingDice = diceController.GetRemainingDiceCount();

        if (remainingDice <= 0)
        {
            // 주사위 소진 → 턴 종료
            Debug.Log("[연쇄 종료] 주사위 소진");
            CheckWaveStatus();
            return;
        }
        
        // 사용 가능한 주사위가 없으면 턴 종료
        List<int> availableValues = diceController.GetAvailableValues();
        if (availableValues.Count == 0)
        {
            CheckWaveStatus();
            return;
        }

        // 사용 가능한 주사위 1개면 자동으로 총합 랜덤 공격
        if (availableValues.Count == 1)
        {
            Debug.Log("[자동 공격] 사용 가능한 주사위 1개 - 총합 랜덤 공격 실행");
            
            List<AttackJokbo> autoAttackJokbos = AttackDB.Instance.GetAchievableJokbos(availableValues);
            
            // 총합 족보 찾기
            AttackJokbo sumJokbo = autoAttackJokbos.FirstOrDefault(j => j.Description.Contains("총합"));
            if (sumJokbo != null)
            {
                sumJokbo.CheckAndCalculate(availableValues);
                ExecuteRandomAttack(sumJokbo);
            }
            else
            {
                Debug.LogWarning("[자동 공격 실패] 총합 족보를 찾을 수 없습니다");
                CheckWaveStatus();
            }
            return;
        }

        // 남은 주사위로 만들 수 있는 족보 확인
        List<int> chainAvailableValues = diceController.GetAvailableValues();
        List<AttackJokbo> chainJokbos = AttackDB.Instance.GetAchievableJokbos(chainAvailableValues);

        if (chainJokbos.Count > 0)
        {
            Debug.Log($"[연쇄 가능] 남은 주사위: {remainingDice}개, 가능한 족보: {chainJokbos.Count}개");
            
            // 족보 선택 UI 표시 (원본 그대로 사용)
            isWaitingForAttackChoice = true;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAttackOptions(chainJokbos);
            }
        }
        else
        {
            // 만들 수 있는 족보 없음 → 턴 종료
            Debug.Log("[연쇄 종료] 가능한 족보 없음");
            CheckWaveStatus();
        }
    }

    private void CheckWaveStatus()
    {
        //이벤트 시스템: 턴 종료 이벤트
        GameEvents.RaiseTurnEnd();
        
        // 주사위 잠금 지속시간 감소
        if (diceController != null)
        {
            diceController.DecreaseLockDurations();
        }
        
        if (activeEnemies.All(e => e == null || e.isDead))
        {
            // 런 통계: 웨이브 완료
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWaveComplete();
            }
            
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
                
                // 주사위 전부 소진되었으면 다시 생성
                if (diceController.GetRemainingDiceCount() <= 0)
                {
                    Debug.Log("[주사위 재생성] 모든 주사위가 소진되어 덱을 다시 생성합니다.");
                    diceController.SetDiceDeck(GameManager.Instance.playerDiceDeck);
                }
                
                diceController.SetRollButtonInteractable(true);
            }
        }
    }

    public void PrepareNextWave()
    {
        Debug.Log("StageManager: 다음 웨이브 준비 중...");
        isWaitingForAttackChoice = false;
        
        // 연쇄 공격 카운터 초기화
        currentChainCount = 0;

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
            
            Vector3 spawnPos = GetSpawnPosition();

            GameObject enemyGO = WaveGenerator.Instance.SpawnFromPool(enemyPrefab, spawnPos, Quaternion.identity);
            Enemy newEnemy = enemyGO.GetComponent<Enemy>();

            if (newEnemy != null)
            {
                // 스케일링 시스템 적용
                newEnemy.InitializeWithScaling(currentZone, currentWave);
                activeEnemies.Add(newEnemy);
            }
            else
            {
                Debug.LogError($"{enemyPrefab.name} 프리팹에 Enemy 스크립트가 없습니다.");
            }
        }

        // 튜토리얼 모드이고 Wave 2라면 적 정보 튜토리얼 시작
        if (GameManager.Instance.isTutorialMode && currentZone == 1 && currentWave == 2)
        {
            TutorialWave2Controller wave2Tutorial = FindFirstObjectByType<TutorialWave2Controller>();
            if (wave2Tutorial != null)
            {
                Invoke(nameof(StartWave2Tutorial), 1f);
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
            
            // 런 통계: 웨이브 시작
            GameManager.Instance.OnWaveStart();
            
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
        
        // 치명타 판정
        ctx.IsCritical = RollCritical();
        ctx.CritMultiplier = GetCritMultiplier();
        
        // 메타 업그레이드 보너스 추가
        if (GameManager.Instance != null)
        {
            ctx.FlatDamageBonus += (int)GameManager.Instance.GetTotalMetaBonus(MetaEffectType.BaseDamage);
            ctx.FlatGoldBonus += (int)GameManager.Instance.GetTotalMetaBonus(MetaEffectType.GoldBonus);
            
            // 4A: 황금의 손 - 골드 배율
            float goldMult = GameManager.Instance.GetTotalMetaBonus(MetaEffectType.GoldMultiplier);
            ctx.GoldMultiplier *= (1 + goldMult / 100f);
            
            // 리롤할때마다  데미지 보너스
            float rerollBonus = GameManager.Instance.GetTotalMetaBonus(MetaEffectType.RerollDamageBonus);
            if (rerollBonus > 0)
            {
                int rerollCount = diceController.currentRollCount - 1;
                ctx.FlatDamageBonus += (int)(rerollBonus * rerollCount);
            }
            
            // 주사위 4개 이상 사용하는 족보 데미지 보너스
            float fourDiceBonus = GameManager.Instance.GetTotalMetaBonus(MetaEffectType.FourDiceDamageBonus);
            if (fourDiceBonus > 0 && jokbo != null)
            {
                int usedDiceCount = jokbo.GetUsedDiceCount();
                if (usedDiceCount >= 4)
                {
                    ctx.DamageMultiplier *= (1 + fourDiceBonus / 100f);
                }
            }
            
            // 콤보 보너스
            float comboBonus = GameManager.Instance.GetTotalMetaBonus(MetaEffectType.ComboBonus);
            if (comboBonus > 0)
            {
                int chainCount = GetCurrentChainCount();
                if (chainCount > 1)
                {
                    ctx.DamageMultiplier += (comboBonus / 100f);
                }
            }
            
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
    
    // 치명타 판정
    private bool RollCritical()
    {
        if (GameManager.Instance == null) return false;
        
        float critChance = GameManager.Instance.GetTotalMetaBonus(MetaEffectType.CritChance);
        return UnityEngine.Random.value < (critChance / 100f);
    }
    
    // 치명타 배율 가져오기
    private float GetCritMultiplier()
    {
        if (GameManager.Instance == null) return 1.5f;
        
        float critMult = GameManager.Instance.GetTotalMetaBonus(MetaEffectType.CritMultiplier);
        return critMult > 0 ? 2.0f : 1.5f;
    }

    public void ShowAttackPreview(AttackJokbo jokbo)
    {
        (int finalBaseDamage, int finalBaseGold) = GetPreviewValues(jokbo);

        // 원본 jokbo 사용
        var modifiedJokbo = jokbo;

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
                // 보스 소환한 적도 스케일링 적용
                newEnemy.InitializeWithScaling(GameManager.Instance.CurrentZone, GameManager.Instance.CurrentWave);
                activeEnemies.Add(newEnemy);
            }
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateWaveInfoPanel(activeEnemies);
        }
    }

    // 겹치지 않는 스폰 위치 찾기
    private Vector3 GetSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // 랜덤 스폰 포인트 선택
            int spawnIndex = Random.Range(0, enemySpawnPoints.Length);
            Vector3 spawnPos = enemySpawnPoints[spawnIndex].position;
            
            // 랜덤 오프셋 적용
            Vector2 randomOffset = Random.insideUnitCircle * spawnSpreadRadius;
            spawnPos.x += randomOffset.x;
            spawnPos.y += randomOffset.y;
            
            // 화면 경계 제한
            if (mainCam != null)
            {
                spawnPos.x = Mathf.Clamp(spawnPos.x, minViewBoundary.x, maxViewBoundary.x);
                spawnPos.y = Mathf.Clamp(spawnPos.y, minViewBoundary.y, maxViewBoundary.y);
            }

            // 다른 적들과 겹치는지 확인
            bool isOverlapping = false;
            foreach (Enemy existingEnemy in activeEnemies)
            {
                if (existingEnemy != null)
                {
                    float distance = Vector3.Distance(spawnPos, existingEnemy.transform.position);
                    if (distance < minEnemySpacing)
                    {
                        isOverlapping = true;
                        break;
                    }
                }
            }

            // 겹치지 않으면 해당 위치 반환
            if (!isOverlapping)
            {
                return spawnPos;
            }
        }

        // 최대 시도 후에도 실패하면 마지막 시도한 위치 반환
        Debug.LogWarning($"[스폰 경고] {maxSpawnAttempts}번 시도 후에도 겹치지 않는 위치를 찾지 못했습니다.");
        int finalIndex = Random.Range(0, enemySpawnPoints.Length);
        Vector3 finalPos = enemySpawnPoints[finalIndex].position;
        Vector2 finalOffset = Random.insideUnitCircle * spawnSpreadRadius;
        finalPos.x += finalOffset.x;
        finalPos.y += finalOffset.y;
        
        if (mainCam != null)
        {
            finalPos.x = Mathf.Clamp(finalPos.x, minViewBoundary.x, maxViewBoundary.x);
            finalPos.y = Mathf.Clamp(finalPos.y, minViewBoundary.y, maxViewBoundary.y);
        }
        
        return finalPos;
    }
    
    // 연쇄 공격 카운터 getter
    public int GetCurrentChainCount()
    {
        return currentChainCount;
    }

    /// <summary>
    /// 여러 위치의 중심점 계산 (VFX용)
    /// </summary>
    private Vector3 CalculateCenterPosition(Vector3[] positions)
    {
        if (positions == null || positions.Length == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Length;
    }
    
    private void StartWave2Tutorial()
    {
        TutorialWave2Controller wave2Tutorial = FindFirstObjectByType<TutorialWave2Controller>();
        if (wave2Tutorial != null)
        {
            wave2Tutorial.StartWave2Tutorial();
        }
    }
}