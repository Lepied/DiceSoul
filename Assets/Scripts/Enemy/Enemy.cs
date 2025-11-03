using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public enum EnemyType { Biological, Spirit, Undead, Armored }

/// <summary>
/// [!!! 핵심 수정 !!!]
/// "인스펙터 우선" 방식으로 변경합니다.
/// 1. 스탯 변수(maxHP, isBoss 등)가 'public'으로 변경되어 인스펙터에서 보입니다.
/// 2. SetupStats() 함수가 삭제되었습니다.
/// 3. 모든 스탯은 '프리팹의 인스펙터'에서 직접 설정해야 합니다.
/// </summary>
public class Enemy : MonoBehaviour
{
    // [!!! 핵심 수정 1 !!!]
    // 이 변수들은 이제 'public'이므로 인스펙터에서 직접 설정해야 합니다.
    [Header("핵심 스탯 (인스펙터에서 설정)")]
    public string enemyName = "Enemy";
    public int maxHP = 10;
    public EnemyType enemyType = EnemyType.Biological;
    public bool isBoss = false; 
    public int difficultyCost = 1; 
    public int minZoneLevel = 1;

    // [유지] UI 연결 변수
    [Header("UI 연결 (공통)")]
    public TextMeshProUGUI hpText;
    public Slider hpSlider;

    // 공통 상태 변수
    public int currentHP { get; protected set; }
    public bool isDead { get; protected set; } = false;

    
    /// <summary>
    /// [!!! 핵심 수정 2 !!!]
    /// OnEnable()이 더 이상 SetupStats()를 호출하지 않습니다.
    /// 인스펙터의 maxHP 값을 읽어 체력을 초기화합니다.
    /// </summary>
    void OnEnable()
    {
        // 1. 인스펙터에 설정된 maxHP로 현재 체력 초기화
        currentHP = maxHP; 
        isDead = false;
        
        // 2. UI 컴포넌트 찾기
        if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>();
        if (hpText == null) hpText = GetComponentInChildren<TextMeshProUGUI>();
        
        // 3. UI 업데이트
        UpdateUI();
    }
    
    // [!!! 핵심 수정 3 !!!]
    // SetupStats() 함수 삭제
    // protected virtual void SetupStats() { ... }


    /// <summary>
    /// UIManager가 호출할, 이 적의 기믹/내성 설명문.
    /// (자식 클래스가 이 함수를 재정의(override)할 수 있음)
    /// </summary>
    public virtual string GetGimmickDescription()
    {
        // (이 로직은 인스펙터의 enemyType을 기반으로 작동)
        switch (enemyType)
        {
            case EnemyType.Biological:
                return "생체: 모든 족보에 100% 피해를 받습니다.";
            case EnemyType.Spirit:
                return "영혼: '총합'에 면역, '마법' 족보에 150% 피해.";
            case EnemyType.Undead:
                return "언데드: 고급 족보(트리플+) 150% 피해, 그 외 50% 피해.";
            case EnemyType.Armored:
                return "장갑: 단순 족보(총합, 트리플) 50% 피해.";
            default:
                return "특성 없음.";
        }
    }

    /// <summary>
    /// 데미지 계산 로직.
    /// (자식 클래스가 이 함수를 'override'하여 내성/약점을 구현합니다)
    /// </summary>
    public virtual int CalculateDamageTaken(AttackJokbo jokbo)
    {
        // 기본 로직: 생체 (Biological)
        return jokbo.BaseDamage;
    }

    /// <summary>
    /// 계산된 최종 데미지를 받아 체력을 깎습니다. (공통 로직)
    /// </summary>
    public bool TakeDamage(int finalDamage, AttackJokbo attackerJokbo)
    {
        if (isDead) return true;

        currentHP -= finalDamage;
        Debug.Log($"{enemyName} 피격! 데미지: {finalDamage}. 남은 체력: {currentHP}/{maxHP}");

        OnDamageTaken(finalDamage, attackerJokbo);

        if (currentHP <= 0)
        {
            currentHP = 0;
            isDead = true;
            OnDeath();
        }
        
        UpdateUI();
        return isDead;
    }

    protected void UpdateUI()
    {
        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {maxHP}";
        }
        if (hpSlider != null)
        {
            if (maxHP > 0)
            {
                hpSlider.value = (float)currentHP / maxHP;
            }
        }
    }

    protected virtual void OnDeath()
    {
        Debug.Log($"{enemyName}이(가) 처치되었습니다!");
        gameObject.SetActive(false); 
    }

    // --- 특수 기믹/효과용 빈 가상 함수들 (유지) ---
    public virtual void OnWaveStart(List<Enemy> allies) {}
    public virtual void OnPlayerRoll(List<int> diceValues) {}
    public virtual void OnDamageTaken(int damageTaken, AttackJokbo jokbo) {}
}

