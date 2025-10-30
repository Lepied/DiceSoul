using UnityEngine;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    public DiceController diceController;
    private bool isStageCleared = false;  

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
        // 씬에서 DiceController를 자동으로 찾아 연결
        if (diceController == null)
        {
            diceController = FindObjectOfType<DiceController>();
            if (diceController == null)
            {
                Debug.LogError("씬에 DiceController2D가 없습니다!");
            }
        }

        // GameManager가 준비되었는지 확인
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없습니다!");
            return;
        }

        // 씬이 시작되면 GameManager에게 새 스테이지(라운드)를 시작하라고 요청
        PrepareNextTurn();
    }

    /// <summary>
    /// DiceController가 굴림을 모두 마쳤을 때 호출할 함수
    /// </summary>
public void OnRollFinished(List<int> currentDiceValues)
    {
        // 이미 이번 스테이지를 클리어했다면, 더 이상 판정하지 않음
        if (isStageCleared) return;

        if (GameManager.Instance == null || GameManager.Instance.currentStageGoal == null) return;

        // 1. 매번 성공 여부를 즉시 체크
        bool isSuccess = GameManager.Instance.currentStageGoal.CheckGoal(currentDiceValues);

        if (isSuccess)
        {
            // 2. [성공!]
            isStageCleared = true; // 클리어 상태로 변경
            int rollsRemaining = diceController.maxRolls - diceController.currentRollCount;
            
            Debug.Log($"목표 달성! 남은 굴림 횟수: {rollsRemaining}");

            // 굴림 버튼 즉시 비활성화 (더 못 굴리게)
            diceController.SetRollButtonInteractable(false);
            
            // GameManager에게 "성공"과 "남은 횟수"를 보고
            GameManager.Instance.ProcessStageClear(true, rollsRemaining);
        }
        else
        {
            // 3. [아직 실패]
            // 굴림 횟수를 다 썼는지 확인
            if (diceController.currentRollCount >= diceController.maxRolls)
            {
                // [최종 실패] 굴림 횟수 소진
                Debug.Log("굴림 횟수 소진. 최종 실패.");
                diceController.SetRollButtonInteractable(false); // (이미 비활성화됐겠지만 확인차)
                
                // GameManager에게 "실패" 보고
                GameManager.Instance.ProcessStageClear(false, 0);
            }
            else
            {
                // [아직 기회 있음]
                Debug.Log("아직 실패. 다시 굴리세요.");
                // 굴림 버튼을 다시 활성화 (다음 굴림 허용)
                diceController.SetRollButtonInteractable(true);
            }
        }
    }

    /// <summary>
    /// (GameManager가 호출) 보상 선택이 끝나면 다음 턴을 준비시킴
    /// </summary>
    public void PrepareNextTurn()
    {
        Debug.Log("StageManager: 다음 턴 준비 중...");
        isStageCleared = false;

        // 1. GameManager에게 새 스테이지 목표를 받아오라고 함
        GameManager.Instance.StartNewStage();

        // 2. DiceController에게 주사위와 굴림 횟수를 리셋하라고 함
        if (diceController != null)
        {
            diceController.PrepareNewTurn();
        }
        if (GameManager.Instance != null && diceController != null)
        {
            GameManager.Instance.ApplyAllRelicEffects(diceController);
        }
    }
}