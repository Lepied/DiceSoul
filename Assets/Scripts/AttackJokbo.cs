using System.Collections.Generic;
using System.Linq;

/// <summary>
/// '공격 족보'의 데이터 틀 (기존 Goal.cs 대체)
/// 족보 이름, 기본 데미지, 기본 점수, 달성 조건, 특수 계산 로직을 가짐
/// </summary>
public class AttackJokbo
{
    public string Description { get; private set; }
    public int BaseDamage { get; private set; }
    public int BaseScore { get; private set; }

    // 족보 달성 조건 (예: 트리플인가?)
    private System.Func<List<int>, bool> conditionLogic;

    // [추가] 족보의 데미지를 계산하는 특수 로직 (예: 총합)
    private System.Func<List<int>, int> damageCalculationLogic;
    // [추가] 족보의 점수를 계산하는 특수 로직 (예: 총합)
    private System.Func<List<int>, int> scoreCalculationLogic;

    /// <summary>
    /// 기본 생성자 (고정 데미지/점수 족보)
    /// </summary>
    public AttackJokbo(string description, int baseDamage, int baseScore, System.Func<List<int>, bool> condition)
    {
        this.Description = description;
        this.BaseDamage = baseDamage;
        this.BaseScore = baseScore;
        this.conditionLogic = condition;
        
        // 특수 로직이 없으면 기본값을 사용
        this.damageCalculationLogic = (diceValues) => this.BaseDamage;
        this.scoreCalculationLogic = (diceValues) => this.BaseScore;
    }

    /// <summary>
    /// [추가] 특수 계산 로직 생성자 (예: "총합" 족보)
    /// </summary>
    public AttackJokbo(string description, int baseDamage, int baseScore, 
                       System.Func<List<int>, bool> condition, 
                       System.Func<List<int>, int> damageLogic, 
                       System.Func<List<int>, int> scoreLogic)
    {
        this.Description = description;
        this.BaseDamage = baseDamage; // (기본값, 특수 로직이 덮어씀)
        this.BaseScore = baseScore;   // (기본값, 특수 로직이 덮어씀)
        this.conditionLogic = condition;
        this.damageCalculationLogic = damageLogic;
        this.scoreCalculationLogic = scoreLogic;
    }

    /// <summary>
    /// 이 족보의 달성 조건을 확인
    /// </summary>
    public bool CheckCondition(List<int> diceValues)
    {
        return conditionLogic(diceValues);
    }

    /// <summary>
    /// [추가] "총합" 족보 등이 실제 값을 계산하도록 함
    /// </summary>
    public void CalculateValues(List<int> diceValues)
    {
        // 특수 로직을 실행하여 BaseDamage와 BaseScore를 덮어씀
        this.BaseDamage = damageCalculationLogic(diceValues);
        this.BaseScore = scoreCalculationLogic(diceValues);
    }
}

