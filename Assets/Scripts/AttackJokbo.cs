using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq를 사용하기 위해

/// <summary>
/// '공격 족보'의 데이터와 계산 로직을 담는 클래스입니다.
/// [수정] StageManager가 CheckLogic에 접근 가능하도록 public { get; }
/// [수정] 복사 생성자 추가 (AttackDB가 버그 없이 족보를 복제하기 위함)
/// </summary>
public class AttackJokbo
{
    // 기본 정보 (UI 표시용)
    public string Description { get; private set; }
    public int BaseDamage { get; private set; }
    public int BaseScore { get; private set; }

    // 족보 달성 여부를 검사하는 '로직' (람다식)
    public System.Func<List<int>, bool> CheckLogic { get; private set; }
    
    // 가변 데미지/점수 계산 로직 (예: "총합")
    private System.Func<List<int>, int> damageCalculationLogic;
    private System.Func<List<int>, int> scoreCalculationLogic;

    /// <summary>
    /// [기본 생성자] 고정 데미지/점수 족보용 (예: 트리플, 스트레이트)
    /// </summary>
    public AttackJokbo(string description, int baseDamage, int baseScore, System.Func<List<int>, bool> checkLogic)
    {
        this.Description = description;
        this.BaseDamage = baseDamage;
        this.BaseScore = baseScore;
        this.CheckLogic = checkLogic;
    }
    
    /// <summary>
    /// [가변 생성자] 가변 데미지/점수 족보용 (예: "총합")
    /// </summary>
    public AttackJokbo(string description, System.Func<List<int>, int> damageCalc, System.Func<List<int>, int> scoreCalc, System.Func<List<int>, bool> checkLogic)
    {
        this.Description = description;
        this.CheckLogic = checkLogic;
        this.damageCalculationLogic = damageCalc;
        this.scoreCalculationLogic = scoreCalc;

        // (가변 족보는 BaseDamage/Score를 0으로 초기화)
        this.BaseDamage = 0;
        this.BaseScore = 0;
    }

    /// <summary>
    /// [신규] 복사 생성자
    /// (AttackDB가 달성된 족보의 '복사본'을 만들 때 사용)
    /// </summary>
    public AttackJokbo(AttackJokbo original)
    {
        this.Description = original.Description;
        this.BaseDamage = original.BaseDamage;
        this.BaseScore = original.BaseScore;
        this.CheckLogic = original.CheckLogic;
        this.damageCalculationLogic = original.damageCalculationLogic;
        this.scoreCalculationLogic = original.scoreCalculationLogic;
    }

    /// <summary>
    /// 족보가 달성되었는지 확인 후, (필요시) 데미지와 점수를 '계산'합니다.
    /// (이 함수는 '원본' 족보의 BaseDamage/Score를 변경시킵니다)
    /// </summary>
    public bool CheckAndCalculate(List<int> diceValues)
    {
        if (CheckLogic(diceValues))
        {
            // 이 족보가 '가변' 족보인지 확인
            if (damageCalculationLogic != null)
            {
                // (예: "총합" 족보)
                this.BaseDamage = damageCalculationLogic(diceValues);
                this.BaseScore = scoreCalculationLogic(diceValues);
            }
            // (가변 족보가 아니면 BaseDamage, BaseScore는 고정값이 유지됨)
            return true;
        }
        return false;
    }
}