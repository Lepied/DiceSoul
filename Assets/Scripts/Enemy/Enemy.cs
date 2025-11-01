using UnityEngine;
using TMPro; // TextMeshPro (UI)
using UnityEngine.UI; // Slider (UI)
using System.Collections.Generic; // List (OnPlayerRoll)

// 적의 기본 타입을 정의 (Boss는 isBoss 플래그로 분리)
public enum EnemyType { Biological, Spirit, Undead, Armored }

/// <summary>
/// [수정] 오브젝트 풀링을 위해 OnEnable에서 스탯을 리셋하도록 변경
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("기본 스탯 (자식 클래스가 덮어씀)")]
    public string enemyName = "Enemy";
    public int maxHP = 10;
    public EnemyType enemyType = EnemyType.Biological;
    
    [Tooltip("이 적이 보스인지 여부 (True/False)")]
    public bool isBoss = false; 

    [Header("웨이브 생성용 데이터 (자식 클래스가 덮어씀)")]
    [Tooltip("이 적을 스폰하는 데 필요한 '난이도 비용'")]
    public int difficultyCost = 1; 
    [Tooltip("이 적이 등장하기 시작하는 최소 존(Zone) 레벨")]
    public int minZoneLevel = 1;

    [Header("UI 연결 (공통)")]
    public TextMeshProUGUI hpText;
    public Slider hpSlider;

    // 공통 상태 변수
    public int currentHP { get; protected set; } 
    public bool isDead { get; protected set; } = false;

    // --- 1. 초기화 로직 (Awake -> SetupStats -> Start) ---
    void Awake()
    {
        // 1. 자식 클래스가 정의한 고유 스탯을 먼저 설정
        SetupStats(); 
        // 2. [수정] 체력 초기화는 OnEnable에서 수행
        // currentHP = maxHP; 
        
        if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>();
        if (hpText == null) hpText = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// [신규] 오브젝트 풀에서 활성화될 때마다 호출됩니다.
    /// 스탯을 리셋합니다.
    /// </summary>
    void OnEnable()
    {
        currentHP = maxHP;
        isDead = false;
        UpdateUI();
    }

    void Start()
    {
        // Start는 오브젝트 생성 시 '한 번'만 호출됩니다.
        // OnEnable이 UI 업데이트를 처리하므로 Start에서 UI를 부를 필요가 없습니다.
        // UpdateUI(); 
    }

    /// <summary>
    /// [핵심 1] 자식 클래스(Goblin, Skeleton)가 이 함수를 'override'(재정의)하여
    /// 자신만의 고유 스탯을 설정합니다.
    /// </summary>
    protected virtual void SetupStats()
    {
        this.enemyName = "Default Enemy";
        this.maxHP = 10;
        this.enemyType = EnemyType.Biological;
        this.isBoss = false;
        this.difficultyCost = 1;
        this.minZoneLevel = 1;
    }

    // --- 2. 데미지 계산 로직 ---

    /// <summary>
    /// [핵심 2] 데미지 계산 로직.
    /// 자식 클래스가 이 함수를 'override'하여 내성/약점을 구현합니다.
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

        // [효과 발동] 데미지를 입었을 때 발동하는 효과 (예: 격노)
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

    // --- 3. UI 및 사망 로직 (공통) ---

    /// <summary>
    /// 체력 바와 텍스트를 업데이트합니다.
    /// </summary>
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

    /// <summary>
    /// 적이 사망했을 때 호출됩니다.
    /// </summary>
    protected virtual void OnDeath()
    {
        Debug.Log($"{enemyName}이(가) 처치되었습니다!");
        // [수정] Destroy 대신 풀에 반환하도록 StageManager가 관리.
        // 여기서는 비활성화만.
        // (StageManager가 ReturnToPool을 호출하면 어차피 비활성화됨)
        // gameObject.SetActive(false); // -> ReturnToPool이 처리
    }

    // --- 4. 특수 기믹/효과용 빈 가상 함수들 ---

    /// <summary>
    /// [효과 함수 1] 웨이브가 시작될 때 (적이 스폰될 때) 한 번 호출됩니다.
    /// </summary>
    public virtual void OnWaveStart(List<Enemy> allies)
    {
        // (보스 등이 이 함수를 재정의하여 사용)
    }

    /// <summary>
    /// [효과 함수 2] 플레이어가 주사위를 '굴릴 때마다' 호출됩니다.
    /// </summary>
    public virtual void OnPlayerRoll(List<int> diceValues)
    {
        // (보스 등이 이 함수를 재정의하여 사용)
    }

    /// <summary>
    /// [효과 함수 3] 이 적이 데미지를 '입은 직후' 호출됩니다.
    /// </summary>
    public virtual void OnDamageTaken(int damageTaken, AttackJokbo jokbo)
    {
        // (보스 등이 이 함수를 재정의하여 사용)
    }
}

