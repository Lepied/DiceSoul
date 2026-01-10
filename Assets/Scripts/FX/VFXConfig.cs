using UnityEngine;

// VFX 설정 ScriptableObject
[CreateAssetMenu(fileName = "VFXConfig_", menuName = "DiceSoul/VFX Config")]
public class VFXConfig : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("VFX 식별자")]
    public string vfxName;
    
    [Tooltip("VFX 타입")]
    public VFXType type = VFXType.Projectile;

    [Header("프리팹 (Particle System 또는 VFX Graph)")]
    [Tooltip("주사위 수집/모이는 VFX (선택사항)")]
    public GameObject gatherPrefab;
    
    [Tooltip("투사체/이동 VFX (선택사항)")]
    public GameObject projectilePrefab;
    
    [Tooltip("타격/임팩트 VFX (필수)")]
    public GameObject impactPrefab;
    
    [Tooltip("버프/지속 효과 VFX (선택사항)")]
    public GameObject buffPrefab;

    [Header("타이밍 설정")]
    [Tooltip("주사위 수집 애니메이션 시간")]
    public float gatherDuration = 0.3f;
    
    [Tooltip("투사체 이동 시간 (0이면 속도 기반)")]
    public float travelDuration = 0.5f;
    
    [Tooltip("투사체 속도 (travelDuration이 0일 때 사용)")]
    public float travelSpeed = 10f;
    
    [Tooltip("임팩트 지속 시간")]
    public float impactDuration = 0.2f;

    [Header("이동 설정")]
    [Tooltip("투사체 이동 커브")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Tooltip("포물선 높이 (0이면 직선)")]
    public float arcHeight = 0f;

    [Header("사운드")]
    public AudioClip launchSound;
    public AudioClip impactSound;
    
    [Header("카메라 쉐이크")]
    [Range(0f, 1f)]
    public float shakeIntensity = 0f;

    // 총 VFX 진행 시간 계산
    public float GetTotalDuration()
    {
        float total = 0f;
        
        if (gatherPrefab != null)
            total += gatherDuration;
            
        if (projectilePrefab != null)
            total += travelDuration;
            
        if (impactPrefab != null)
            total += impactDuration;
            
        return total;
    }
}

// VFX 타입 정의
public enum VFXType
{
    None,              
    Projectile,        //단일 투사체 (주사위 → 타겟)
    MultiProjectile,   //다중 투사체 (주사위 → 여러 타겟)
    AreaBurst,         //범위 폭발
    OnTarget,          //타겟 위치에서
    OnAllTargets,      //모든 타겟에서 한번에 
    Buff,              //버프/실드
    Beam               //비이이이이이이임
}
