using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.EventSystems;

public enum EnemyType { Biological, Spirit, Undead, Armored }

public class Enemy : MonoBehaviour, IPointerClickHandler
{
    // [인스펙터 설정]
    [Header("스탯")]
    public string enemyName = "Enemy";
    public int maxHP = 10;
    public int attackDamage = 5;  // 적의 공격력
    public EnemyType enemyType = EnemyType.Biological;
    private EnemyType originalType;

    //스케일링을 위한 원본 HP 
    private int baseHP = 0;

    [Header("기본 설정")]
    public bool isBoss = false;
    public int difficultyCost = 1;
    public int minZoneLevel = 1;

    [Header("UI 연결")]
    public TextMeshProUGUI hpText;

    [Tooltip("현재 체력")]
    public Slider hpSlider;

    [Tooltip("예상 데미지")]
    public Slider damagePreviewSlider;

    private bool isInitialized = false;

    // 공통 상태 변수
    public int currentHP { get; protected set; }
    public bool isDead { get; protected set; } = false;

    // 애니메이션 관리를 위한 Tween 변수
    private Tween blinkTween;
    private Image damagePreviewFillImage;

    void Awake() // 또는 Start
    {
        // 게임 시작 시 프리팹에 설정된 타입을 원본으로 저장
        if (!isInitialized)
        {
            originalType = enemyType;
            baseHP = maxHP;
            isInitialized = true;
        }        
        Debug.Log($"[Enemy Awake] {enemyName} 생성됨, Collider: {GetComponent<Collider2D>() != null}");    }
    void OnEnable()
    {
        // 기본값 초기화
        currentHP = maxHP;
        isDead = false;
        if (isInitialized)
        {
            enemyType = originalType;
        }

        if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>();
        if (hpText == null) hpText = GetComponentInChildren<TextMeshProUGUI>();

        blinkTween?.Kill();
        UpdateUI();
    }
    
    // 스케일링 시스템을 적용하여 적을 초기화
    public void InitializeWithScaling(int zone, int wave)
    {
        //baseHP가 설정되지 않았다면 현재 maxHP를 baseHP로 사용
        if (baseHP == 0)
        {
            baseHP = maxHP;
        }
        
        // EnemyScaling 시스템으로 최종 HP 계산
        int scaledHP = EnemyScaling.GetScaledHP(baseHP, zone, wave, isBoss);
        
        // 최종 HP 적용
        maxHP = scaledHP;
        currentHP = scaledHP;
        
        // UI 갱신
        UpdateUI();
        
    }
    void OnDisable()
    {
        EffectManager.Instance.RemoveQueue(this.transform);
    }

    public virtual string GetGimmickDescription()
    {
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

    public virtual int CalculateDamageTaken(AttackJokbo jokbo)
    {
        if (jokbo == null) 
        {
            return 0; // 족보가 없으면 추가 계산 없이 0 리턴 (외부에서 계산된 고정 데미지 사용)
        }
        int finalDamage = jokbo.BaseDamage;
        string desc = jokbo.Description;

        // 1. 타입별 공통 데미지 공식 
        switch (enemyType)
        {
            case EnemyType.Biological:
                // 생체: 100% (변동 없음)
                break;

            case EnemyType.Undead:
                // 언데드: 고급 족보에 약함 (150%), 기본 족보에 강함 (50%)
                if (desc.Contains("트리플") || desc.Contains("포카드") || desc.Contains("풀 하우스") || desc.Contains("야찌") || desc.Contains("스트레이트"))
                {
                    finalDamage = (int)(finalDamage * 1.5f);
                    // (약점 텍스트 띄우고 싶으면 여기서 EffectManager 호출 가능)
                }
                else
                {
                    finalDamage = (int)(finalDamage * 0.5f);
                }
                break;

            case EnemyType.Spirit:
                // 영혼: 물리(총합) 면역, 마법(홀/짝) 약점
                if (desc.Contains("총합")) return 0; // 0 데미지
                if (desc.Contains("모두")) finalDamage = (int)(finalDamage * 1.5f);
                break;

            case EnemyType.Armored:
                // 장갑: 단순 족보(총합, 트리플) 반감
                if (desc.Contains("총합") || desc.Contains("트리플"))
                {
                    finalDamage = (int)(finalDamage * 0.5f);
                    EffectManager.Instance.ShowText(transform, "저항", Color.grey);
                }
                break;
        }

        return finalDamage;
    }

    public bool TakeDamage(int finalDamage, AttackJokbo attackerJokbo)
    {
        if (isDead) return true;
        if (finalDamage > 0)

        
        {
            // 크리티컬 판정 만약 나중에 생기면 여기에 넣어야
            EffectManager.Instance.ShowDamage(transform, finalDamage);
        }
        else
        {
            // 일단 0 이면 면역 띄우기. 아마 몹별로 따로따로 해야할거같은데? ex) 면역이면 면역, 방어면 방어 등등
            EffectManager.Instance.ShowText(transform, "면역", Color.gray);
        }

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
        blinkTween?.Kill();

        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {maxHP}";
        }

        float hpPercent = (maxHP > 0) ? (float)currentHP / maxHP : 0;

        if (hpSlider != null)
        {
            hpSlider.value = hpPercent;
        }

        if (damagePreviewSlider != null)
        {
            damagePreviewSlider.value = hpPercent;
        }

        if (damagePreviewFillImage == null && damagePreviewSlider != null && damagePreviewSlider.fillRect != null)
        {
            damagePreviewFillImage = damagePreviewSlider.fillRect.GetComponent<Image>();
        }

        if (damagePreviewFillImage != null)
        {
            Color c = damagePreviewFillImage.color;
            c.a = 1f;
            damagePreviewFillImage.color = c;
        }
    }

    protected virtual void OnDeath()
    {
        Debug.Log($"{enemyName}이(가) 처치되었습니다!");
        blinkTween?.Kill();
        gameObject.SetActive(false);
    }

    public virtual void OnWaveStart(List<Enemy> allies) { }
    public virtual void OnPlayerRoll(List<int> diceValues) { }
    public virtual void OnDamageTaken(int damageTaken, AttackJokbo jokbo) { }

    public virtual void ShowDamagePreview(AttackJokbo jokbo)
    {
        if (isDead || hpSlider == null || damagePreviewSlider == null) return;

        blinkTween?.Kill();

        float hpPercent = (maxHP > 0) ? (float)currentHP / maxHP : 0;
        damagePreviewSlider.value = hpPercent;

        int damageToTake = CalculateDamageTaken(jokbo);
        int previewHP = Mathf.Max(0, currentHP - damageToTake);
        float previewPercent = (maxHP > 0) ? (float)previewHP / maxHP : 0;

        hpSlider.value = previewPercent;

        if (damagePreviewFillImage == null && damagePreviewSlider.fillRect != null)
        {
            damagePreviewFillImage = damagePreviewSlider.fillRect.GetComponent<Image>();
        }

        if (damagePreviewFillImage != null)
        {

            Color c = damagePreviewFillImage.color;
            c.a = 1f;
            damagePreviewFillImage.color = c;

            blinkTween = damagePreviewFillImage.DOFade(0.1f, 1f)
                                              .SetLoops(-1, LoopType.Yoyo)
                                              .SetEase(Ease.Linear);
        }
    }

    public virtual void HideDamagePreview()
    {
        if (isDead || hpSlider == null) return;

        blinkTween?.Kill();

        float hpPercent = (maxHP > 0) ? (float)currentHP / maxHP : 0;

        hpSlider.value = hpPercent;

        if (damagePreviewFillImage != null)
        {
            Color c = damagePreviewFillImage.color;
            c.a = 1f;
            damagePreviewFillImage.color = c;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[Enemy OnPointerClick] {enemyName} 클릭 감지! isDead={isDead}");
        
        if (isDead) return;
        
        // 타겟 선택 모드일 때만 반응
        if (StageManager.Instance != null)
        {
            Debug.Log($"[Enemy] StageManager.OnEnemySelected({enemyName}) 호출");
            StageManager.Instance.OnEnemySelected(this);
        }
        else
        {
            Debug.LogWarning("[Enemy] StageManager.Instance가 null입니다!");
        }
    }
}