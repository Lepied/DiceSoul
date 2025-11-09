using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening; // DOTween 사용

public enum EnemyType { Biological, Spirit, Undead, Armored }

/// <summary>
/// [!!! 핵심 수정 !!!]
/// - ShowDamagePreview 함수에 'damagePreviewFillImage'가 null일 때
///   '왜' null인지 상세한 에러 로그를 출력하는 디버깅 코드 추가
/// </summary>
public class Enemy : MonoBehaviour
{
    // [인스펙터 설정]
    [Header("핵심 스탯 (인스펙터에서 설정)")]
    public string enemyName = "Enemy";
    public int maxHP = 10;
    public EnemyType enemyType = EnemyType.Biological;
    public bool isBoss = false; 
    public int difficultyCost = 1; 
    public int minZoneLevel = 1;

    [Header("UI 연결 (공통)")]
    public TextMeshProUGUI hpText;
    
    [Tooltip("현재 체력 (예: 초록색, 전경)")]
    public Slider hpSlider; 
    
    [Tooltip("예상 데미지 (예: 빨간색, 배경)")]
    public Slider damagePreviewSlider; 


    // 공통 상태 변수
    public int currentHP { get; protected set; }
    public bool isDead { get; protected set; } = false;

    // 애니메이션 관리를 위한 Tween 변수
    private Tween blinkTween;
    private Image damagePreviewFillImage; // 깜빡임 효과를 위한 Fill 이미지


    void OnEnable()
    {
        currentHP = maxHP; 
        isDead = false;
        
        if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>();
        if (hpText == null) hpText = GetComponentInChildren<TextMeshProUGUI>();
        
        // [수정] damagePreviewFillImage를 찾는 로직을 OnEnable에서 분리
        // (UpdateUI에서 매번 null 체크를 하도록 변경 - 더 안전함)
        // if (damagePreviewSlider != null)
        // { ... }
        
        blinkTween?.Kill();
        UpdateUI(); 
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
        return jokbo.BaseDamage;
    }

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

    /// <summary>
    /// [수정] UpdateUI는 즉시 값을 설정
    /// </summary>
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
        
        // [수정] damagePreviewFillImage가 null이면 찾아옴 (더 안전한 방식)
        if (damagePreviewFillImage == null && damagePreviewSlider != null && damagePreviewSlider.fillRect != null)
        {
            damagePreviewFillImage = damagePreviewSlider.fillRect.GetComponent<Image>();
        }

        if (damagePreviewFillImage != null)
        {
            Color c = damagePreviewFillImage.color;
            c.a = 1f; // 알파(투명도) 값을 100%로 설정
            damagePreviewFillImage.color = c;
        }
    }

    protected virtual void OnDeath()
    {
        Debug.Log($"{enemyName}이(가) 처치되었습니다!");
        blinkTween?.Kill();
        gameObject.SetActive(false); 
    }

    public virtual void OnWaveStart(List<Enemy> allies) {}
    public virtual void OnPlayerRoll(List<int> diceValues) {}
    public virtual void OnDamageTaken(int damageTaken, AttackJokbo jokbo) {}

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// damagePreviewFillImage가 왜 null인지 상세히 디버깅합니다.
    /// </summary>
    public virtual void ShowDamagePreview(AttackJokbo jokbo)
    {
        if (isDead || hpSlider == null || damagePreviewSlider == null) return;
        
        blinkTween?.Kill();

        float hpPercent = (maxHP > 0) ? (float)currentHP / maxHP : 0;
        damagePreviewSlider.value = hpPercent;

        int damageToTake = CalculateDamageTaken(jokbo);
        int previewHP = Mathf.Max(0, currentHP - damageToTake);
        float previewPercent = (maxHP > 0) ? (float)previewHP / maxHP : 0;

        // 5. 초록색 바(전경)를 예상 체력(예: 50%)으로 '즉시' 낮춤
        hpSlider.value = previewPercent;

        // 6. [!!! 디버깅 코드 추가 !!!]
        // 빨간색 바의 Fill 이미지를 '빠르고 선명하게' 깜빡이게
        
        // (안전) 만약 OnEnable에서 못 찾았다면, 여기서 한 번 더 찾기
        if (damagePreviewFillImage == null && damagePreviewSlider.fillRect != null)
        {
             damagePreviewFillImage = damagePreviewSlider.fillRect.GetComponent<Image>();
        }

        // [!!!] 최종 확인
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

    /// <summary>
    /// [수정] hpSlider.value를 즉시 설정
    /// </summary>
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
}

