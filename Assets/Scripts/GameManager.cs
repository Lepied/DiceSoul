using UnityEngine;
using System.Collections.Generic;
// using UnityEngine.SceneManagement; // 나중에 씬 이동 시 필요

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 상태")]
    public int PlayerHealth;
    public int MaxPlayerHealth = 10;
    public int MetaCurrency; // 영구 재화 (영혼의 파편)


    [Header("런(Run) 진행 상태")]
    public int CurrentZone = 1; // Balatro의 Ante
    public int CurrentStage = 1; // 스몰/빅 블라인드

    // [변경] SO가 아닌, 'Goal' 클래스 객체를 직접 저장
    public Goal currentStageGoal { get; private set; }
    public List<Relic> activeRelics = new List<Relic>();


    // [변경] 유물 리스트 (SO 대신 유물 클래스/스크립트 필요)
    // public List<RelicSO> activeRelics = new List<RelicSO>(); // (추후 유물 시스템 구현 시)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 새 게임 런을 시작합니다.
    /// </summary>
    public void StartNewRun()
    {
        PlayerHealth = MaxPlayerHealth;
        MetaCurrency = 0; // (또는 저장된 값 불러오기)
        CurrentZone = 1;
        CurrentStage = 1;

        // 게임 씬으로 이동
        // SceneManager.LoadScene("GameScene"); 
        // (씬 로드 후) StageManager가 StartNewStage()를 호출해줄 것임

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
        }


    }

    /// <summary>
    /// 새 스테이지(라운드)를 시작합니다. (StageManager가 호출)
    /// </summary>
    public void StartNewStage()
    {

        if (StageGoalDB.Instance == null)
        {
            Debug.LogError("씬에 StageGoalDB가 없습니다!");
            return;
        }

        // 예시: 3스테이지마다 보스
        if (CurrentStage % 3 == 0 && CurrentStage != 0)
        {
            currentStageGoal = StageGoalDB.Instance.GetRandomBossGoal();
        }
        else
        {
            currentStageGoal = StageGoalDB.Instance.GetRandomNormalGoal();
        }

        if (currentStageGoal == null)
        {
            Debug.LogError("가져올 목표가 없습니다! (StageGoalDatabase 확인 필요)");
            return;
        }
        Debug.Log($"[Stage {CurrentStage}] 새 목표: {currentStageGoal.Description}");

        UIManager.Instance.UpdateGoalText(currentStageGoal.Description);
    }

    /// <param name="isSuccess">성공 여부</param>
    /// <param name="rollsRemaining">성공 시 남은 굴림 횟수</param>
    public void ProcessStageClear(bool isSuccess, int rollsRemaining)
    {
        if (isSuccess)
        {
            Debug.Log($"목표 달성! 보너스 (남은 굴림): {rollsRemaining}");
            CurrentStage++; // 다음 스테이지로

            // [TODO] 남은 횟수 보너스 로직 추가
            // 예: UIManager.Instance.ShowBonusScore(rollsRemaining * 10);

            // 보상 화면 표시
            ShowRewardScreen();
        }
        else
        {
            // (이 로직은 굴림 횟수를 다 썼을 때만 호출됨)
            Debug.Log("목표 실패. 체력이 1 감소합니다.");
            PlayerHealth--;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(PlayerHealth, MaxPlayerHealth);
            }

            if (PlayerHealth <= 0)
            {
                Debug.Log("게임 오버");
                // UIManager.Instance.ShowGameOverScreen();
                return;
            }

            // [수정] 실패 시 '즉시' 다음 턴 준비
            if (StageManager.Instance != null)
            {
                StageManager.Instance.PrepareNextTurn();
            }
        }
    }

    private void ShowRewardScreen()
    {
        // 1. RelicDatabase에서 유물 3개를 뽑습니다.
        if (RelicDB.Instance == null)
        {
            Debug.LogError("RelicDatabase가 씬에 없습니다!");
            return;
        }
        List<Relic> rewardOptions = RelicDB.Instance.GetRandomRelics(3);

        // 2. UIManager에게 이 3개를 보여달라고 요청합니다.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowRewardScreen(rewardOptions);
        }
    }

    /// <summary>
    /// [새 함수] 플레이어가 유물을 '선택'했을 때 UIManager가 호출할 함수
    /// </summary>
    public void OnRelicSelected(Relic chosenRelic)
    {
        // 1. 선택한 유물을 내 리스트에 추가
        activeRelics.Add(chosenRelic);
        Debug.Log($"유물 획득: {chosenRelic.Name}");

        // 2. 보상 화면을 닫고 (UIManager가 스스로 닫음)

        // 3. '이제' 다음 턴을 준비시킵니다.
        if (StageManager.Instance != null)
        {
            StageManager.Instance.PrepareNextTurn();
        }
    }
    public void ApplyAllRelicEffects(DiceController diceController)
    {
        Debug.Log("보유한 유물 효과를 모두 적용합니다...");
        
        foreach (Relic relic in activeRelics)
        {
            // 나중에 유물 효과가 많아지면 switch-case 사용
            switch (relic.EffectType)
            {
                case RelicEffectType.AddMaxRolls:
                    diceController.ApplyRollBonus(relic.EffectIntValue);
                    break;
                
                // (다른 유물 효과들...)
                case RelicEffectType.AddDice:
                    // TODO: 주사위 추가 로직
                    break;
                case RelicEffectType.ModifyDiceValue:
                    // TODO: 주사위 값 변경 로직 (판정 시점에 필요)
                    break;
            }
        }

        // [추가] 모든 유물 효과가 적용된 '최종' maxRolls 값으로 UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRollCount(0, diceController.maxRolls);
        }
    }
}