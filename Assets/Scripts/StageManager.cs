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
    public SpriteRenderer backgroundRenderer;

    [Header("웨이브/적 설정")]
    public Transform[] enemySpawnPoints;

    public float spawnSpreadRadius = 0.5f;
    public float screenEdgePadding = 1.0f;
    public float minEnemySpacing = 1.5f;  // 적들 간 최소 거리
    public int maxSpawnAttempts = 20;     // 스폰 위치 재시도 횟수

    public List<Enemy> activeEnemies = new List<Enemy>();
    private bool isAttackChoice = false;

    // 족보 선택 진행 중인지 확인
    public bool IsAttackChoice => isAttackChoice;

    // 튜토리얼 콜백
    public System.Action onHandSelectedCallback;

    // 연쇄 공격 시스템 변수
    private AttackHand currentSelectedHand = null;      // 현재 선택된 족보
    private List<Enemy> currentSelectedTargets = new List<Enemy>();  // 선택된 타겟들
    private bool isWaitingForTargetSelection = false;     // 타겟 선택 대기 중
    private int requiredTargetCount = 0;                  // 선택해야 할 타겟 수
    private int currentChainCount = 0;                    // 현재 연쇄 공격 횟수 (광택 구슬용)

    private Camera mainCam;
    private Vector2 minViewBoundary;
    private Vector2 maxViewBoundary;

    // 주공격용 타겟
    private List<Enemy> _cachedMainTargets = new List<Enemy>();
    // 부가공격용 타겟
    private List<Enemy> _cachedSubTargets = new List<Enemy>();
    // 랜덤 결과
    private List<Enemy> _cachedRandomTargets = new List<Enemy>();
    // 랜덤 풀
    private List<Enemy> _cachedAvailableEnemies = new List<Enemy>();
    // 배열 변환용
    private Vector3[] _cachedPositionArray = new Vector3[30];
    private Transform[] _cachedTransformArray = new Transform[30];


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

    void OnDestroy()
    {

        if (Instance == this)
        {
            Instance = null;

        }
    }
    void Start()
    {
        if (diceController == null)
        {
            diceController = FindFirstObjectByType<DiceController>();
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

        // 현재 Zone에 맞는 배경 설정 (이어하기 대응)
        SetBackgroundForCurrentZone();

        if (GameManager.Instance.hasInitializedRun)
        {
            PrepareNextWave();
        }
        else
        {
            StartCoroutine(WaitForGameInitialization());
        }
    }

    private IEnumerator WaitForGameInitialization()
    {
        //초기화끝날때까지 대기시키기
        while (GameManager.Instance == null || !GameManager.Instance.hasInitializedRun)
        {
            yield return null;
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
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                Enemy enemy = activeEnemies[i];
                if (enemy != null && !enemy.isDead)
                {
                    enemy.OnPlayerRoll(initialValues);
                }
            }
        }


        CheckHand(initialValues);

        yield break;
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
    private void CheckHand(List<int> finalValues)
    {
        if (AttackDB.Instance == null) return;

        // Locked 주사위 제외한 사용 가능한 값들로 족보 계산
        List<int> availableValues = diceController.GetAvailableValues();
        List<AttackHand> achievableHands = AttackDB.Instance.GetAchievableHands(availableValues);

        if (achievableHands.Count > 0)
        {
            // 이벤트 시스템: 족보 완성 이벤트
            HandContext handCtx = new HandContext
            {
                achievedHands = achievableHands,
                DiceValuesList = finalValues,
                BonusDamage = 0,
                BonusGold = 0
            };
            GameEvents.RaiseHandComplete(handCtx);

            diceController.SetRollButtonInteractable(false);
            isAttackChoice = true;
            if (UIManager.Instance != null)
            {
                List<AttackHand> previewHands = new List<AttackHand>();
                foreach (var hand in achievableHands)
                {
                    (int finalBaseDamage, int finalBaseGold) = GetPreviewValues(hand);
                    // 복사 생성자 사용하여 모든 로직 유지
                    var preview = new AttackHand(hand);
                    // 프리뷰용 데미지/골드는 수동으로 업데이트 필요 (참조 타입 문제로 private set이므로 불가)
                    previewHands.Add(hand); // 원본 그대로 사용 (프리뷰 값은 GetPreviewValues로 처리됨)
                }
                UIManager.Instance.ShowAttackOptions(previewHands);
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
    public void ProcessAttack(AttackHand chosenHand)
    {
        if (!isAttackChoice) return;

        currentSelectedHand = chosenHand;

        // 튜토리얼 콜백 호출
        onHandSelectedCallback?.Invoke();

        // 공격 타입에 따라 처리 분기
        switch (chosenHand.TargetType)
        {
            case AttackTargetType.AoE:
                ExecuteAoEAttack(chosenHand);
                break;

            case AttackTargetType.Single:
                // 타겟 선택 대기
                StartTargetSelection(chosenHand);
                break;

            case AttackTargetType.Random:
                ExecuteRandomAttack(chosenHand);
                break;

            case AttackTargetType.Hybrid:
                // 복합 공격은 먼저 주공격 타겟 선택
                StartTargetSelection(chosenHand);
                break;

            case AttackTargetType.Defense:
                ExecuteDefense(chosenHand);
                break;
        }
    }

    // 수비 (실드 얻기)
    private void ExecuteDefense(AttackHand hand)
    {
        (int shieldAmount, _) = GetPreviewValues(hand);

        diceController.RemoveDiceByIndices(hand.UsedDiceIndices);

        GameManager.Instance.AddShield(shieldAmount);

        FinishAttackAndCheckChain();
    }

    //전체 공격 (AoE) 실행
    private void ExecuteAoEAttack(AttackHand hand)
    {
        // 최종 데미지 계산
        (int finalDamage, int finalGold) = GetPreviewValues(hand);
        int bonusDamage = finalDamage - hand.BaseDamage;

        // 실제 공격용 AttackContext 생성
        AttackContext attackCtx = CreateAttackContext(hand, finalDamage, finalGold);

        _cachedMainTargets.Clear();
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && !activeEnemies[i].isDead)
            {
                _cachedMainTargets.Add(activeEnemies[i]);
            }
        }
        if (_cachedMainTargets.Count == 0)
        {
            FinishAttackAndCheckChain();
            return;
        }

        // VFX 통합 버전
        if (VFXManager.Instance != null && hand.VfxConfig != null)
        {
            // 주사위 위치들
            Vector3[] dicePos = diceController.GetDicePositions(hand.UsedDiceIndices);

            // 적 위치들
            Vector3[] targetPos = GetPositionsArray(_cachedMainTargets);

            // 주사위 제거 시작 (VFX와 동시)
            diceController.RemoveDiceByIndices(hand.UsedDiceIndices);

            // VFX 재생
            VFXManager.Instance.PlayAoEAttack(
                config: hand.VfxConfig,
                dicePositions: dicePos,
                targetPositions: targetPos,
                onImpact: (int targetIndex) =>
                {
                    // 각 타겟에 데미지 적용
                    if (targetIndex < _cachedMainTargets.Count)
                    {
                        Enemy enemy = _cachedMainTargets[targetIndex];
                        int damageToTake = enemy.CalculateDamageTaken(hand) + bonusDamage;
                        enemy.TakeDamage(damageToTake, hand, isSplash: false, isCritical: attackCtx.IsCritical);
                    }
                },
                onComplete: () =>
                {
                    // VFX 완료 후
                    GameManager.Instance.AddGold(finalGold, GoldSource.Combat);
                    GameEvents.RaiseAfterAttack(attackCtx);
                    FinishAttackAndCheckChain();
                }
            );
        }
    }

    // 랜덤 공격 실행
    private void ExecuteRandomAttack(AttackHand hand)
    {
        _cachedMainTargets.Clear();
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && !activeEnemies[i].isDead)
            {
                _cachedMainTargets.Add(activeEnemies[i]);
            }
        }
        if (_cachedMainTargets.Count == 0)
        {
            FinishAttackAndCheckChain();
            return;
        }

        // 런 통계: 족보 사용 기록
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordHandUsage(hand.Description);
        }

        // 최종 데미지 계산 (유물 효과 포함)
        (int finalDamage, int finalGold) = GetPreviewValues(hand);

        // 실제 공격용 AttackContext 생성
        AttackContext attackCtx = CreateAttackContext(hand, finalDamage, finalGold);

        // RandomTargetCount 계산 (총합의 경우 최종 데미지/10)
        int randomTargetCount = hand.RandomTargetCount;
        if (randomTargetCount == 0)  // 총합의 경우면
        {
            randomTargetCount = Mathf.Max(1, finalDamage / 10);  // 최소 1명
        }

        // 생존 적 수보다 많으면 조정
        randomTargetCount = Mathf.Min(randomTargetCount, _cachedMainTargets.Count);

        _cachedRandomTargets.Clear();
        _cachedAvailableEnemies.Clear();
        _cachedAvailableEnemies.AddRange(_cachedMainTargets);

        for (int i = 0; i < randomTargetCount; i++)
        {
            if (_cachedAvailableEnemies.Count == 0) break;

            int randomIndex = Random.Range(0, _cachedAvailableEnemies.Count);
            Enemy target = _cachedAvailableEnemies[randomIndex];
            _cachedRandomTargets.Add(target);
            _cachedAvailableEnemies.RemoveAt(randomIndex);  // 중복 방지
        }

        int bonusDamage = finalDamage - hand.BaseDamage;

        // VFX 통합 버전
        if (VFXManager.Instance != null && hand.VfxConfig != null)
        {
            Vector3[] dicePos = diceController.GetDicePositions(hand.UsedDiceIndices);
            Vector3 centerPos = dicePos.Length > 0 ? CalculateCenterPosition(dicePos) : Vector3.zero;

            Vector3[] targetPos = GetPositionsArray(_cachedRandomTargets);
            Transform[] targetTransforms = GetTransformsArray(_cachedRandomTargets);

            // 주사위 제거 시작
            diceController.RemoveDiceByIndices(hand.UsedDiceIndices);

            // VFX 재생 (다중 투사체)
            VFXManager.Instance.PlayMultiProjectileAttack(
                config: hand.VfxConfig,
                fromPosition: centerPos,
                toPositions: targetPos,
                targets: targetTransforms,
                onEachReach: (int targetIndex) =>
                {
                    // 각 투사체가 도착할 때 데미지
                    if (targetIndex < randomTargets.Count)
                    {
                        Enemy target = _cachedRandomTargets[targetIndex];
                        int damageToTake = target.CalculateDamageTaken(hand) + bonusDamage;
                        target.TakeDamage(damageToTake, hand, isSplash: false, isCritical: attackCtx.IsCritical);

                    }
                },
                onComplete: () =>
                {
                    GameManager.Instance.AddGold(finalGold, GoldSource.Combat);
                    FinishAttackAndCheckChain();
                }
            );
        }
    }

    // 타겟 선택 모드 시작 (Single/Hybrid 공격용)
    private void StartTargetSelection(AttackHand hand)
    {
        requiredTargetCount = hand.RequiredTargetCount;

        int aliveEnemyCount = 0;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && !activeEnemies[i].isDead)
            {
                aliveEnemyCount++;
            }
        }
        
        if (aliveEnemyCount <= requiredTargetCount)
        {
            currentSelectedTargets.Clear();
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                if (activeEnemies[i] != null && !activeEnemies[i].isDead)
                {
                    currentSelectedTargets.Add(activeEnemies[i]);
                }
            }

            // 바로 공격 실행
            if (hand.TargetType == AttackTargetType.Single)
            {
                ExecuteMultiTargetAttack(hand, currentSelectedTargets);
            }
            else if (hand.TargetType == AttackTargetType.Hybrid)
            {
                ExecuteHybridAttack(hand, currentSelectedTargets);
            }
            return;
        }

        // 적이 충분하면 타겟 선택 모드 진입
        isWaitingForTargetSelection = true;
        currentSelectedHand = hand;
        currentSelectedTargets.Clear();

        // UI에 타겟 선택 표시
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTargetSelectionMode(requiredTargetCount, 0);
        }
    }

    // 타겟이 선택되었을 때 호출 (Enemy 클릭 시)
    public void OnEnemySelected(Enemy selectedEnemy)
    {
        if (!isWaitingForTargetSelection || currentSelectedHand == null) return;
        if (selectedEnemy == null || selectedEnemy.isDead) return;

        // 이미 선택된 적이면 선택 해제
        if (currentSelectedTargets.Contains(selectedEnemy))
        {
            currentSelectedTargets.Remove(selectedEnemy);

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
            return;
        }

        // 타겟 추가
        currentSelectedTargets.Add(selectedEnemy);
        // UI 업데이트 (확인 버튼 활성화 상태도 업데이트)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTargetSelectionMode(requiredTargetCount, currentSelectedTargets.Count);
        }
    }

    // 타겟 선택 확인 (확인 버튼 클릭 시)
    public void ConfirmTargetSelection()
    {
        if (!isWaitingForTargetSelection || currentSelectedHand == null) return;

        int aliveEnemyCount = 0;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && !activeEnemies[i].isDead)
            {
                aliveEnemyCount++;
            }
        }
        
        if (aliveEnemyCount < requiredTargetCount)
        {
            currentSelectedTargets.Clear();
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                if (activeEnemies[i] != null && !activeEnemies[i].isDead)
                {
                    currentSelectedTargets.Add(activeEnemies[i]);
                }
            }
        }
        else if (currentSelectedTargets.Count < requiredTargetCount)
        {
            return;
        }

        isWaitingForTargetSelection = false;

        // UI 타겟 선택 모드 종료
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideTargetSelectionMode();
        }

        if (currentSelectedHand.TargetType == AttackTargetType.Single)
        {
            ExecuteMultiTargetAttack(currentSelectedHand, currentSelectedTargets);
        }
        else if (currentSelectedHand.TargetType == AttackTargetType.Hybrid)
        {
            ExecuteHybridAttack(currentSelectedHand, currentSelectedTargets);
        }
    }

    // 타겟 선택 취소 (취소 버튼 클릭 시)
    public void CancelTargetSelection()
    {
        if (!isWaitingForTargetSelection) return;

        isWaitingForTargetSelection = false;
        AttackHand cancelledHand = currentSelectedHand;
        currentSelectedHand = null;
        currentSelectedTargets.Clear();
        requiredTargetCount = 0;


        // UI 타겟 선택 모드 종료
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideTargetSelectionMode();
        }

        // 족보 선택 메뉴로 돌아가기
        isAttackChoice = true;

        // 현재 주사위로 가능한 족보 다시 표시 (Locked 제외)
        List<int> availableValues = diceController.GetAvailableValues();
        List<AttackHand> achievableHands = AttackDB.Instance.GetAchievableHands(availableValues);

        if (achievableHands.Count > 0 && UIManager.Instance != null)
        {
            UIManager.Instance.ShowAttackOptions(achievableHands);
        }
    }

    // 복수 타겟 공격 실행 (Single 타입의 복수 타겟)
    private void ExecuteMultiTargetAttack(AttackHand hand, List<Enemy> targets)
    {
        // 최종 데미지 계산
        (int finalDamage, int finalGold) = GetPreviewValues(hand);
        int bonusDamage = finalDamage - hand.BaseDamage;

        // 실제 공격용 AttackContext 생성
        AttackContext attackCtx = CreateAttackContext(hand, finalDamage, finalGold);

        // VFX 통합 버전
        if (VFXManager.Instance != null && hand.VfxConfig != null)
        {
            // 주사위 위치들
            Vector3[] dicePos = diceController.GetDicePositions(hand.UsedDiceIndices);
            Vector3 centerPos = dicePos.Length > 0 ? CalculateCenterPosition(dicePos) : Vector3.zero;

            _cachedMainTargets.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && !targets[i].isDead)
                {
                    _cachedMainTargets.Add(targets[i]);
                }
            }
            
            Vector3[] targetPos = GetPositionsArray(_cachedMainTargets);
            Transform[] targetTransforms = GetTransformsArray(_cachedMainTargets);


            diceController.RemoveDiceByIndices(hand.UsedDiceIndices);

            // VFX 재생
            VFXManager.Instance.PlayMultiProjectileAttack(
                config: hand.VfxConfig,
                fromPosition: centerPos,
                toPositions: targetPos,
                targets: targetTransforms,
                onEachReach: (int targetIndex) =>
                {
                    // 각 타겟에 데미지 적용
                    if (targetIndex < _cachedMainTargets.Count)
                    {
                        Enemy enemy = _cachedMainTargets[targetIndex];
                        int damageToTake = enemy.CalculateDamageTaken(hand) + bonusDamage;
                        enemy.TakeDamage(damageToTake, hand, isSplash: false, isCritical: attackCtx.IsCritical);
                    }
                },
                onComplete: () =>
                {
                    // VFX 완료 후
                    GameManager.Instance.AddGold(finalGold, GoldSource.Combat);
                    GameEvents.RaiseAfterAttack(attackCtx);
                    FinishAttackAndCheckChain();
                }
            );
        }
    }

    // 복합 공격 실행 (주공격 + 부가공격)
    private void ExecuteHybridAttack(AttackHand hand, List<Enemy> mainTargets)
    {
        // 주공격 실행 - 최종 데미지 계산 (유물 효과 포함)
        (int finalDamage, int finalGold) = GetPreviewValues(hand);
        int bonusDamage = finalDamage - hand.BaseDamage;

        // 실제 공격용 AttackContext 생성
        AttackContext attackCtx = CreateAttackContext(hand, finalDamage, finalGold);

        // VFX
        if (VFXManager.Instance != null && hand.VfxConfig != null)
        {
            Vector3[] dicePos = diceController.GetDicePositions(hand.UsedDiceIndices);
            Vector3 centerPos = dicePos.Length > 0 ? CalculateCenterPosition(dicePos) : Vector3.zero;

            _cachedMainTargets.Clear();
            for (int i = 0; i < mainTargets.Count; i++)
            {
                if (mainTargets[i] != null && !mainTargets[i].isDead)
                {
                    _cachedMainTargets.Add(mainTargets[i]);
                }
            }
            
            Vector3[] mainTargetPos = GetPositionsArray(_cachedMainTargets);
            Transform[] mainTargetTransforms = GetTransformsArray(_cachedMainTargets);

            diceController.RemoveDiceByIndices(hand.UsedDiceIndices);
            VFXManager.Instance.PlayMultiProjectileAttack(
                config: hand.VfxConfig,
                fromPosition: centerPos,
                toPositions: mainTargetPos,
                targets: mainTargetTransforms,
                onEachReach: (int targetIndex) =>
                {
                    if (targetIndex < _cachedMainTargets.Count)
                    {
                        Enemy mainTarget = _cachedMainTargets[targetIndex];
                        int mainDamageToTake = mainTarget.CalculateDamageTaken(hand) + bonusDamage;
                        mainTarget.TakeDamage(mainDamageToTake, hand, isSplash: false, isCritical: attackCtx.IsCritical);

                    }
                },
                onComplete: () =>
                {
                    GameManager.Instance.AddGold(finalGold, GoldSource.Combat);
                    GameEvents.RaiseAfterAttack(attackCtx);

                    ExecuteSubAttack(hand, mainTargets, onSubComplete: () =>
                    {
                        FinishAttackAndCheckChain();
                    });
                }
            );
        }
    }

    // 복합 공격의 부가 공격 실행
    private void ExecuteSubAttack(AttackHand hand, List<Enemy> mainTargets, Action onSubComplete = null)
    {
        int subDamage = hand.SubDamage;
        if (subDamage <= 0)
        {
            onSubComplete?.Invoke();
            return;
        }
        var subHand = hand;

        switch (hand.SubTargetType)
        {
            case AttackTargetType.AoE:
                // 전체 공격 (주공격 대상 제외)
                _cachedSubTargets.Clear();
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    Enemy e = activeEnemies[i];
                    if (e != null && !e.isDead && !mainTargets.Contains(e))
                    {
                        _cachedSubTargets.Add(e);
                    }
                }

                if (VFXManager.Instance != null && hand.SubVfxConfig != null && _cachedSubTargets.Count > 0)
                {
                    // 부가공격 VFX 있으면 사용
                    Vector3[] targetPos = GetPositionsArray(_cachedSubTargets);

                    VFXManager.Instance.PlayAoEAttack(
                        config: hand.SubVfxConfig,
                        dicePositions: new Vector3[0],
                        targetPositions: targetPos,
                        onImpact: (int targetIndex) =>
                        {
                            if (targetIndex < _cachedSubTargets.Count)
                            {
                                Enemy enemy = _cachedSubTargets[targetIndex];
                                int damageToTake = enemy.CalculateDamageTaken(subHand);
                                enemy.TakeDamage(damageToTake, subHand, isSplash: false, isCritical: false);
                            }
                        },
                        onComplete: onSubComplete
                    );
                }
                break;

            case AttackTargetType.Random:
                // 랜덤 타겟
                _cachedSubTargets.Clear();
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    Enemy e = activeEnemies[i];
                    if (e != null && !e.isDead && !mainTargets.Contains(e))
                    {
                        _cachedSubTargets.Add(e);
                    }
                }
                
                if (_cachedSubTargets.Count == 0)
                {
                    for (int i = 0; i < activeEnemies.Count; i++)
                    {
                        Enemy e = activeEnemies[i];
                        if (e != null && !e.isDead)
                        {
                            _cachedSubTargets.Add(e);
                        }
                    }
                }

                int subRandomCount = Mathf.Min(hand.SubRandomTargetCount, _cachedSubTargets.Count);
                _cachedRandomTargets.Clear();
                _cachedAvailableEnemies.Clear();
                _cachedAvailableEnemies.AddRange(_cachedSubTargets);

                for (int i = 0; i < subRandomCount; i++)
                {
                    if (_cachedAvailableEnemies.Count == 0) break;

                    int randomIndex = Random.Range(0, _cachedAvailableEnemies.Count);
                    Enemy randomTarget = _cachedAvailableEnemies[randomIndex];
                    _cachedRandomTargets.Add(randomTarget);
                    _cachedAvailableEnemies.RemoveAt(randomIndex);  // 중복 방지
                }

                if (VFXManager.Instance != null && hand.SubVfxConfig != null && _cachedRandomTargets.Count > 0)
                {
                    // 부가공격 VFX 있으면 사용
                    Vector3[] targetPos = GetPositionsArray(_cachedRandomTargets);
                    Transform[] targetTransforms = GetTransformsArray(_cachedRandomTargets);

                    VFXManager.Instance.PlayMultiProjectileAttack(
                        config: hand.SubVfxConfig,
                        fromPosition: Vector3.zero,
                        toPositions: targetPos,
                        targets: targetTransforms,
                        onEachReach: (int targetIndex) =>
                        {
                            if (targetIndex < _cachedRandomTargets.Count)
                            {
                                Enemy randomTarget = _cachedRandomTargets[targetIndex];
                                int damageToTake = randomTarget.CalculateDamageTaken(subHand);
                                randomTarget.TakeDamage(damageToTake, subHand, isSplash: false, isCritical: false);

                            }
                        },
                        onComplete: onSubComplete
                    );
                }
                break;
        }
    }

    // AttackContext 생성 헬퍼 메서드
    private AttackContext CreateAttackContext(AttackHand hand, int baseDamage, int baseGold)
    {
        return new AttackContext
        {
            hand = hand,
            BaseDamage = baseDamage,
            BaseGold = baseGold,
            FlatDamageBonus = 0,
            FlatGoldBonus = 0,
            DamageMultiplier = 1.0f,
            GoldMultiplier = 1.0f,
            IsFirstRoll = (diceController.currentRollCount == 1),
            RemainingRolls = diceController.maxRolls - diceController.currentRollCount,
            HealAfterAttack = 0,
            IsFirstAttackThisWave = (currentChainCount == 0)
        };
    }

    // 공격 완료 후 연쇄 공격 체크
    private void FinishAttackAndCheckChain()
    {
        // 주사위는 이미 Execute 메서드에서 제거되었음 (이중 제거 방지)

        isAttackChoice = false;
        currentSelectedHand = null;
        currentSelectedTargets.Clear();

        // 연쇄 공격 카운터 증가
        currentChainCount++;

        // 런 통계: 연쇄 공격 기록
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordChainCount(currentChainCount);
        }

        // 적이 모두 죽었는지 확인
        int aliveEnemyCount = 0;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && !activeEnemies[i].isDead)
            {
                aliveEnemyCount++;
            }
        }
        if (aliveEnemyCount == 0)
        {
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
            CheckWaveStatus();
            return;
        }

        // 사용 가능한 주사위가 없으면 잠금 체크 후 턴 종료
        List<int> availableValues = diceController.GetAvailableValues();
        if (availableValues.Count == 0)
        {
            // 잠긴 주사위가 있는지 확인하고 있으면 잠금풀고 자동공격나가게 
            bool hasLockedDice = false;
            for (int i = 0; i < diceController.activeDice.Count; i++)
            {
                if (diceController.activeDice[i].State == DiceState.Locked)
                {
                    hasLockedDice = true;
                    break;
                }
            }

            if (hasLockedDice)
            {
                diceController.DecreaseLockDurations();

                // 잠금 해제 후 다시 체크
                availableValues = diceController.GetAvailableValues();

                // 여전히 사용 가능한 주사위가 없으면 턴 종료
                if (availableValues.Count == 0)
                {
                    CheckWaveStatus();
                    return;
                }
            }
            else
            {
                CheckWaveStatus();
                return;
            }
        }

        // 사용 가능한 주사위 1개면 자동으로 총합 랜덤 공격
        if (availableValues.Count == 1)
        {
            List<AttackHand> autoAttackHands = AttackDB.Instance.GetAchievableHands(availableValues);

            // 총합 족보 찾기
            AttackHand sumHand = null;
            for (int i = 0; i < autoAttackHands.Count; i++)
            {
                if (autoAttackHands[i].Description.Contains("총합"))
                {
                    sumHand = autoAttackHands[i];
                    break;
                }
            }
            if (sumHand != null)
            {
                sumHand.CheckAndCalculate(availableValues);
                ExecuteRandomAttack(sumHand);
            }
            else
            {
                CheckWaveStatus();
            }
            return;
        }

        // 남은 주사위로 만들 수 있는 족보 확인
        List<int> chainAvailableValues = diceController.GetAvailableValues();
        List<AttackHand> chainHands = AttackDB.Instance.GetAchievableHands(chainAvailableValues);

        if (chainHands.Count > 0)
        {
            // 족보 선택 UI 표시 (원본 그대로 사용)
            isAttackChoice = true;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAttackOptions(chainHands);
            }
        }
        else
        {
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

        bool allEnemiesDead = true;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && !activeEnemies[i].isDead)
            {
                allEnemiesDead = false;
                break;
            }
        }
        
        if (allEnemiesDead)
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
                diceController.SetRollButtonInteractable(false);
                GameManager.Instance.ProcessWaveClear(false, 0);
            }
            else
            {
                // 주사위 전부 소진되었으면 다시 생성
                if (diceController.GetRemainingDiceCount() <= 0)
                {
                    diceController.SetDiceDeck(GameManager.Instance.playerDiceDeck);
                }

                diceController.SetRollButtonInteractable(true);
            }
        }
    }

    public void PrepareNextWave()
    {
        isAttackChoice = false;

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

        int currentZone = GameManager.Instance.CurrentZone;
        int currentWave = GameManager.Instance.CurrentWave;

        ZoneData currentZoneData = WaveGenerator.Instance.GetCurrentZoneData(currentZone);

        // 존의 배경 및 BGM 변경 (존 전환 시)
        if (currentWave == 1 && currentZoneData != null && backgroundRenderer != null)
        {
            if (currentZoneData.zoneBackground != null)
            {
                backgroundRenderer.sprite = currentZoneData.zoneBackground;
            }

            // BGM 변경
            if (currentZoneData.zoneBGM != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBGMConfig(currentZoneData.zoneBGM);
            }
        }

        List<GameObject> enemiesToSpawn = WaveGenerator.Instance.GenerateWave(currentZone, currentWave);

        if (enemySpawnPoints.Length == 0)
        {
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

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];
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

                // 스폰된 모든 적에게 데미지 적용
                for (int i = 0; i < activeEnemies.Count; i++)
                {
                    Enemy enemy = activeEnemies[i];
                    if (enemy != null && !enemy.isDead)
                    {
                        // 족보 정보 없이 고정 데미지 주는 방식 (null 전달)
                        // Enemy.TakeDamage 함수가 null Hand를 처리할 수 있어야 함.
                        // 만약 처리 못한다면 더미 Hand를 만들어서 보내야 함.
                        enemy.TakeDamage(startDamage, null, isSplash: false, isCritical: false);
                    }
                }

                // 데미지로 인해 죽은 적이 있을 수 있으므로 상태 체크 한 번 실행
                CheckWaveStatus();
            }
        }

    }

    // 유물 효과가 적용된 최종 데미지/골드 미리보기 계산
    public (int finalDamage, int finalGold) GetPreviewValues(AttackHand hand)
    {
        // AttackContext를 통해 이벤트 시스템으로 계산
        AttackContext ctx = new AttackContext
        {
            hand = hand,
            BaseDamage = hand.BaseDamage,
            BaseGold = hand.BaseGold,
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
            if (fourDiceBonus > 0 && hand != null)
            {
                int usedDiceCount = hand.GetUsedDiceCount();
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

    public void ShowAttackPreview(AttackHand hand)
    {
        (int finalBaseDamage, int finalBaseGold) = GetPreviewValues(hand);

        // 원본 Hand 사용
        var modifiedHand = hand;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.isDead)
            {
                enemy.ShowDamagePreview(modifiedHand);
            }
        }
    }

    public void HideAllAttackPreviews()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.isDead)
            {
                enemy.HideDamagePreview();
            }
        }
    }

    public void SpawnEnemiesForBoss(GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (enemySpawnPoints.Length == 0)
            {
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

    // 연쇄 공격 카운터
    public int GetCurrentChainCount()
    {
        return currentChainCount;
    }

    // 여러 위치의 중심점 계산 (VFX용)
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

    // 현재 Zone에 맞는 배경 설정
    private void SetBackgroundForCurrentZone()
    {
        if (WaveGenerator.Instance == null || GameManager.Instance == null || backgroundRenderer == null)
        {
            return;
        }

        int currentZone = GameManager.Instance.CurrentZone;
        ZoneData currentZoneData = WaveGenerator.Instance.GetCurrentZoneData(currentZone);

        if (currentZoneData != null)
        {

            backgroundRenderer.sprite = currentZoneData.zoneBackground;

            // BGM 설정 (이어하기 대응)
            if (currentZoneData.zoneBGM != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBGMConfig(currentZoneData.zoneBGM);
            }
        }
    }

    private void StartWave2Tutorial()
    {
        TutorialWave2Controller wave2Tutorial = FindFirstObjectByType<TutorialWave2Controller>();
        if (wave2Tutorial != null)
        {
            wave2Tutorial.StartWave2Tutorial();
        }
    }

    private Vector3[] GetPositionsArray(List<Enemy> enemies)
    {
        int count = enemies.Count;
        
        if (_cachedPositionArray.Length < count)
            _cachedPositionArray = new Vector3[count * 2];
        
        for (int i = 0; i < count; i++)
        {
            _cachedPositionArray[i] = enemies[i].transform.position;
        }
        
        if (_cachedPositionArray.Length == count)
            return _cachedPositionArray;
        
        Vector3[] result = new Vector3[count];
        Array.Copy(_cachedPositionArray, result, count);
        return result;
    }

    private Transform[] GetTransformsArray(List<Enemy> enemies)
    {
        int count = enemies.Count;
        
        if (_cachedTransformArray.Length < count)
            _cachedTransformArray = new Transform[count * 2];
        
        for (int i = 0; i < count; i++)
        {
            _cachedTransformArray[i] = enemies[i].transform;
        }
        
        if (_cachedTransformArray.Length == count)
            return _cachedTransformArray;
        
        Transform[] result = new Transform[count];
        Array.Copy(_cachedTransformArray, result, count);
        return result;
    }
}