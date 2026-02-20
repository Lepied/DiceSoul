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
    public int attackDamage = 5;
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

    [Tooltip("보스 표시")]
    public Image bossIcon;

    private bool isInitialized = false;

    // 공통 상태 변수
    public int currentHP { get; protected set; }
    public bool isDead { get; protected set; } = false;

    // 애니메이션 관리를 위한 Tween 변수
    private Tween blinkTween;
    private Image damagePreviewFillImage;

    // 타격감이랑 효과용
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Tween flashTween;
    private Tween knockbackTween;

    private Material materialInstance;
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");

    void Awake() // 또는 Start
    {
        // 게임 시작 시 프리팹에 설정된 타입을 원본으로 저장
        if (!isInitialized)
        {
            originalType = enemyType;
            baseHP = maxHP;
            isInitialized = true;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            materialInstance = new Material(spriteRenderer.sharedMaterial);
            spriteRenderer.material = materialInstance;
            materialInstance.SetFloat(FlashAmountID, 0f);
        }
        else
        {
            originalColor = Color.white; // 기본값
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
        if (bossIcon != null) bossIcon.gameObject.SetActive(isBoss);

        blinkTween?.Kill();
        flashTween?.Kill();
        knockbackTween?.Kill();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

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
        
        // 튜토리얼 모드일 때 적 약해지게
        if (GameManager.Instance != null && GameManager.Instance.isTutorialMode)
        {
            scaledHP = Mathf.Max(1, Mathf.RoundToInt(scaledHP * 0.3f));
            attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * 0.5f));
        }
        
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

    void OnDestroy()
    {
        // Tween 정리
        flashTween?.Kill();
        knockbackTween?.Kill();
        
        // Material 인스턴스 정리
        if (materialInstance != null)
        {
            Destroy(materialInstance);
            materialInstance = null;
        }
    }

    public virtual string GetGimmickDescription()
    {
        // 서브클래스에서 override하여 고유 기믹을 표시
        // 기본적으로는 빈 문자열 반환
        return "";
    }
    
    public virtual string GetLocalizedName()
    {
        // 클래스 이름을 기반으로 키 생성
        string className = GetType().Name.ToUpper();
        string key = $"ENEMY_{className}";
        
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.HasKey(key))
        {
            return LocalizationManager.Instance.GetText(key);
        }
        
        // Fallback: enemyName 사용
        return enemyName;
    }

    public virtual int CalculateDamageTaken(AttackHand hand)
    {
        if (hand == null) 
        {
            return 0; // 족보가 없으면 추가 계산 없이 0 리턴 (외부에서 계산된 고정 데미지 사용)
        }
        int finalDamage = hand.BaseDamage;
        string desc = hand.Description;

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
                    string text = LocalizationManager.Instance?.GetText("COMBAT_RESIST") ?? "저항";
                    EffectManager.Instance.ShowText(transform, text, Color.grey);
                }
                break;
        }

        return finalDamage;
    }

    public bool TakeDamage(int finalDamage, AttackHand attackerHand, bool isSplash = false, bool isCritical = false)
    {
        if (isDead) return true;
        if (finalDamage > 0)

        
        {
            // 크리티컬 데미지 표시
            EffectManager.Instance.ShowDamage(transform, finalDamage, isCritical);
        }
        else
        {
            // 일단 0 이면 면역 띄우기. 아마 몹별로 따로따로 해야할거같은데? ex) 면역이면 면역, 방어면 방어 등등
            string text = LocalizationManager.Instance?.GetText("COMBAT_IMMUNE") ?? "면역";
            EffectManager.Instance.ShowText(transform, text, Color.gray);
        }

        currentHP -= finalDamage;
        Debug.Log($"{enemyName} 피격! 데미지: {finalDamage}. 남은 체력: {currentHP}/{maxHP}");
        
        // 런 통계: 데미지 기록
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RecordDamage(finalDamage);
        }

        OnDamageTaken(finalDamage, attackerHand);
        
        // 스플래시 데미지/  Single 타겟 공격만발동하게
        if (!isSplash && attackerHand != null && attackerHand.TargetType == AttackTargetType.Single && GameManager.Instance != null)
        {
            float splashPercent = GameManager.Instance.GetTotalMetaBonus(MetaEffectType.SplashDamage);
            if (splashPercent > 0)
            {
                int splashDamage = Mathf.RoundToInt(finalDamage * splashPercent / 100f);
                if (splashDamage > 0 && StageManager.Instance != null)
                {
                    foreach (Enemy nearby in StageManager.Instance.activeEnemies)
                    {
                        if (nearby != this && nearby != null && !nearby.isDead)
                        {
                            nearby.TakeDamage(splashDamage, null, isSplash: true, isCritical: false);
                        }
                    }
                }
            }
        }

        if (currentHP <= 0)
        {
            currentHP = 0;
            isDead = true;
            
            // 런 통계: 적 처치 기록
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecordKill(isBoss);
            }
            
            OnDeath();
        }

        UpdateUI();
        return isDead;
    }

    protected void UpdateUI()
    {
        blinkTween?.Kill();

        // 보스 아이콘 표시
        if (bossIcon != null)
        {
            bossIcon.gameObject.SetActive(isBoss);
        }

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
        flashTween?.Kill();
        knockbackTween?.Kill();
        gameObject.SetActive(false);
    }

    //플래시 효과
    public void PlayHitFlash(Color flashColor, float duration)
    {
        flashTween?.Kill();

        // 플래시 색상 설정
        materialInstance.SetColor(FlashColorID, flashColor);

        // FlashAmount 애니메이션
        flashTween = DOTween.Sequence()
            .Append(
                DOTween.To(
                    () => materialInstance.GetFloat(FlashAmountID),
                    x => materialInstance.SetFloat(FlashAmountID, x),
                    1f,
                    duration * 0.3f
                ).SetUpdate(true)
            )
            .Append(
                DOTween.To(
                    () => materialInstance.GetFloat(FlashAmountID),
                    x => materialInstance.SetFloat(FlashAmountID, x),
                    0f,
                    duration * 0.7f
                ).SetUpdate(true)
            );
    }

    //넉백 효과
    public void PlayKnockback(Vector3 hitDirection, float distance, float duration)
    {
        if (distance <= 0) return;

        knockbackTween?.Kill();
        Vector3 originalPos = transform.position;
        Vector3 knockbackPos = originalPos + hitDirection.normalized * distance;

        knockbackTween = transform.DOMove(knockbackPos, duration * 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOMove(originalPos, duration * 0.7f).SetEase(Ease.InOutQuad);
            });
    }

    public virtual void OnWaveStart(List<Enemy> allies) { }
    public virtual void OnPlayerRoll(List<int> diceValues) { }
    public virtual void OnDamageTaken(int damageTaken, AttackHand hand) { }

    public virtual void ShowDamagePreview(AttackHand hand)
    {
        if (isDead || hpSlider == null || damagePreviewSlider == null) return;

        blinkTween?.Kill();

        float hpPercent = (maxHP > 0) ? (float)currentHP / maxHP : 0;
        damagePreviewSlider.value = hpPercent;

        int damageToTake = CalculateDamageTaken(hand);
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
        if (isDead) return;
        
        // 타겟 선택 모드일 때만 반응
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnEnemySelected(this);
        }
    }
}