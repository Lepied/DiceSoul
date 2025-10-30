using System.Collections.Generic;
using System.Linq; // Linq는 조건 검사에 매우 유용합니다.

public enum GoalType { Normal, Boss }

// SO 대신 사용할 목표 데이터 클래스
public class Goal
{
    public string Description { get; private set; }
    public GoalType Type { get; private set; }

    // 'Check' 로직을 담을 델리게이트(Delegate) 또는 인터페이스
    // 여기서는 Func<List<int>, bool>를 사용합니다.
    // (입력: 주사위 리스트, 반환: 성공 여부)
    private System.Func<List<int>, bool> checkLogic;

    // 생성자
    public Goal(string description, GoalType type, System.Func<List<int>, bool> logic)
    {
        this.Description = description;
        this.Type = type;
        this.checkLogic = logic;
    }

    // 외부에서 이 함수를 호출해 성공 여부를 검사
    public bool CheckGoal(List<int> diceValues)
    {
        return checkLogic(diceValues);
    }
}