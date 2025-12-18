using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class AttackJokbo
{
    // 기본 정보 (UI 표시용)
    public string Description { get; private set; }
    public int BaseDamage { get; private set; }
    public int BaseGold { get; private set; }

    // 족보 달성 여부를 검사하는 '로직' (람다식)
    public System.Func<List<int>, bool> CheckLogic { get; private set; }
    
    // 가변 데미지/점수 계산 로직 (예: "총합")
    private System.Func<List<int>, int> damageCalculationLogic;
    private System.Func<List<int>, int> goldCalculationLogic;


    // 고정 데미지/점수 족보용 (예: 트리플, 스트레이트)
    public AttackJokbo(string description, int baseDamage, int baseGold, System.Func<List<int>, bool> checkLogic)
    {
        this.Description = description;
        this.BaseDamage = baseDamage;
        this.BaseGold = baseGold;
        this.CheckLogic = checkLogic;
    }
    
    //가변 데미지/점수 족보용 (예: "총합")
    public AttackJokbo(string description, System.Func<List<int>, int> damageCalc, System.Func<List<int>, int> goldCalc, System.Func<List<int>, bool> checkLogic)
    {
        this.Description = description;
        this.CheckLogic = checkLogic;
        this.damageCalculationLogic = damageCalc;
        this.goldCalculationLogic = goldCalc;

        // (가변 족보는 BaseDamage/Gold를 0으로 초기화)
        this.BaseDamage = 0;
        this.BaseGold = 0;
    }


    /// 복사 생성자
    /// (AttackDB가 달성된 족보의 '복사본'을 만들 때 사용)
    public AttackJokbo(AttackJokbo original)
    {
        this.Description = original.Description;
        this.BaseDamage = original.BaseDamage;
        this.BaseGold = original.BaseGold;
        this.CheckLogic = original.CheckLogic;
        this.damageCalculationLogic = original.damageCalculationLogic;
        this.goldCalculationLogic = original.goldCalculationLogic;
    }

    // 족보달성 검사하고 게산
    public bool CheckAndCalculate(List<int> diceValues)
    {
        if (CheckLogic(diceValues))
        {
            // 이 족보가 '가변' 족보인지 확인
            if (damageCalculationLogic != null)
            {
                // (예: "총합" 족보)
                this.BaseDamage = damageCalculationLogic(diceValues);
                this.BaseGold = goldCalculationLogic(diceValues);
            }
            // (가변 족보가 아니면 BaseDamage, BaseGold는 고정값이 유지됨)
            return true;
        }
        return false;
    }
}