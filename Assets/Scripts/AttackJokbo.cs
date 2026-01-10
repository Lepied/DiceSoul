using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

// 공격 타겟 타입
public enum AttackTargetType
{
    AoE,        // 전체 공격
    Single,     // 지정 공격 (플레이어가 타겟 선택)
    Random,     // 랜덤 타겟 공격
    Hybrid,     // 복합 공격 (주공격 + 부가공격)
    Defense     // 수비 (실드얻음)
}

public class AttackJokbo
{
    // 기본 정보 (UI 표시용)
    public string Description { get; private set; }
    public int BaseDamage { get; private set; }
    public int BaseGold { get; private set; }

    // 공격 타입 정보
    public AttackTargetType TargetType { get; private set; }
    public int RequiredTargetCount { get; private set; }  // 선택해야 할 타겟 수 (Single/Hybrid용)
    public int RandomTargetCount { get; private set; }    // 랜덤 공격 타겟 수 (Random용, 기본 1)
    
    // 복합 공격용 (Hybrid)
    public AttackTargetType SubTargetType { get; private set; }
    public int SubDamage { get; private set; }  // 부가 공격 데미지
    public int SubRandomTargetCount { get; private set; } // 부가 공격의 랜덤 타겟 수

    // 사용된 주사위 인덱스 (연쇄 공격용)
    public List<int> UsedDiceIndices { get; private set; } = new List<int>();

    // VFX 설정
    public VFXConfig VfxConfig { get; private set; }

    // 족보 달성 여부를 검사하는 로직
    public System.Func<List<int>, bool> CheckLogic { get; private set; }
    
    // 사용된 주사위 인덱스를 계산하는 로직
    private System.Func<List<int>, List<int>> usedIndicesLogic;
    
    // 가변 데미지/점수 계산 로직 (예: "총합")
    private System.Func<List<int>, int> damageCalculationLogic;
    private System.Func<List<int>, int> goldCalculationLogic;


    // 고정 데미지/점수 족보용
    public AttackJokbo(
        string description, 
        int baseDamage, 
        int baseGold, 
        System.Func<List<int>, bool> checkLogic,
        System.Func<List<int>, List<int>> usedIndicesLogic,
        AttackTargetType targetType = AttackTargetType.AoE,
        int requiredTargetCount = 1,
        int randomTargetCount = 1,
        AttackTargetType subTargetType = AttackTargetType.AoE,
        int subDamage = 0,
        int subRandomTargetCount = 1,
        VFXConfig vfxConfig = null)
    {
        this.Description = description;
        this.BaseDamage = baseDamage;
        this.BaseGold = baseGold;
        this.CheckLogic = checkLogic;
        this.usedIndicesLogic = usedIndicesLogic;
        this.TargetType = targetType;
        this.RequiredTargetCount = requiredTargetCount;
        this.RandomTargetCount = randomTargetCount;
        this.SubTargetType = subTargetType;
        this.SubDamage = subDamage;
        this.SubRandomTargetCount = subRandomTargetCount;
        this.VfxConfig = vfxConfig;
    }
    
    //가변 데미지/점수 족보용 (예: "총합")
    public AttackJokbo(
        string description, 
        System.Func<List<int>, int> damageCalc, 
        System.Func<List<int>, int> goldCalc, 
        System.Func<List<int>, bool> checkLogic,
        System.Func<List<int>, List<int>> usedIndicesLogic,
        AttackTargetType targetType = AttackTargetType.Random,
        int requiredTargetCount = 0,
        int randomTargetCount = 1,
        VFXConfig vfxConfig = null)
    {
        this.Description = description;
        this.CheckLogic = checkLogic;
        this.damageCalculationLogic = damageCalc;
        this.goldCalculationLogic = goldCalc;
        this.usedIndicesLogic = usedIndicesLogic;
        this.TargetType = targetType;
        this.RequiredTargetCount = requiredTargetCount;
        this.RandomTargetCount = randomTargetCount;
        this.SubTargetType = AttackTargetType.AoE;  // 기본값
        this.SubDamage = 0;
        this.SubRandomTargetCount = 1;
        this.VfxConfig = vfxConfig;

        // (가변 족보는 BaseDamage/Gold를 0으로 초기화)
        this.BaseDamage = 0;
        this.BaseGold = 0;
    }


    // 복사 생성자
    //(AttackDB가 달성된 족보의 '복사본'을 만들 때 사용)
    public AttackJokbo(AttackJokbo original)
    {
        this.Description = original.Description;
        this.BaseDamage = original.BaseDamage;
        this.BaseGold = original.BaseGold;
        this.CheckLogic = original.CheckLogic;
        this.damageCalculationLogic = original.damageCalculationLogic;
        this.goldCalculationLogic = original.goldCalculationLogic;
        this.usedIndicesLogic = original.usedIndicesLogic;
        this.TargetType = original.TargetType;
        this.RequiredTargetCount = original.RequiredTargetCount;
        this.RandomTargetCount = original.RandomTargetCount;
        this.SubTargetType = original.SubTargetType;
        this.SubDamage = original.SubDamage;
        this.SubRandomTargetCount = original.SubRandomTargetCount;
        this.VfxConfig = original.VfxConfig;
        this.UsedDiceIndices = new List<int>(original.UsedDiceIndices);
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
            
            // 사용된 주사위 인덱스 계산 (람다 호출)
            if (usedIndicesLogic != null)
            {
                UsedDiceIndices = usedIndicesLogic(diceValues);
            }
            
            return true;
        }
        return false;
    }
}