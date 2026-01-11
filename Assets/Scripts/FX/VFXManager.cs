using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("기본 VFX 설정")]
    [Tooltip("기본 AoE VFX")]
    public VFXConfig defaultAoEConfig;

    [Tooltip("기본 투사체 VFX")]
    public VFXConfig defaultProjectileConfig;

    [Tooltip("기본 임팩트 VFX")]
    public VFXConfig defaultImpactConfig;

    [Tooltip("기본 버프 VFX")]
    public VFXConfig defaultBuffConfig;


    public Transform vfxContainer;
    public bool showDebugLogs = true;

    private Dictionary<GameObject, Queue<VFXInstance>> vfxPools = new Dictionary<GameObject, Queue<VFXInstance>>();

    // 히트스탑 관리
    private bool isHitStopActive = false;
    private Coroutine hitStopCoroutine = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (vfxContainer == null)
        {
            GameObject container = new GameObject("VFX_Container");
            container.transform.SetParent(transform);
            vfxContainer = container.transform;
        }
    }

    // VFX 인스턴스 
    private VFXInstance SpawnVFX(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        GameObject vfxObj = Instantiate(prefab, position, rotation, vfxContainer);

        VFXInstance instance = vfxObj.GetComponent<VFXInstance>();
        if (instance == null)
        {
            instance = vfxObj.AddComponent<VFXInstance>();
        }

        return instance;
    }

    // 간단한 이펙트 재생 (에셋이나 프리팹같은거)
    public void PlayEffect(GameObject prefab, Vector3 position, Vector3 forward = default)
    {
        if (prefab == null) return;

        VFXInstance vfx = SpawnVFX(prefab, position, Quaternion.identity);

        if (forward != Vector3.zero)
            vfx.transform.forward = forward;

        vfx.Play();

        // 자동 파괴
        float duration = vfx.GetDuration();
        Destroy(vfx.gameObject, duration + 0.5f);
    }

    // AoE 공격 VFX (startPoint에 따라 자동 분기)
    public void PlayAoEAttack(
        VFXConfig config,
        Vector3[] dicePositions,
        Vector3[] targetPositions,
        Action<int> onImpact,
        Action onComplete)
    {
        // startPoint에 따라 분기
        if (config != null && config.startPoint == VFXStartPoint.EachDice)
        {
            // 각 주사위 위치에서 발사
            StartCoroutine(MultiSourceAoESequence(config, dicePositions, targetPositions, onImpact, onComplete));
        }
        else
        {
            // 중심점에서 발사 (기본)
            StartCoroutine(AoEAttackSequence(config, dicePositions, targetPositions, onImpact, onComplete));
        }
    }

    private IEnumerator AoEAttackSequence(
        VFXConfig config,
        Vector3[] dicePositions,
        Vector3[] targetPositions,
        Action<int> onImpact,
        Action onComplete)
    {
        if (config == null) config = defaultAoEConfig;
        if (config == null)
        {
            Debug.LogWarning("[VFXManager] AoE VFX Config가 없습니다!");
            onComplete?.Invoke();
            yield break;
        }

        Vector3 centerPos = CalculateCenterPosition(targetPositions);

        // 1. 주사위 수집 VFX
        if (config.gatherPrefab != null)
        {
            VFXInstance gather = SpawnVFX(config.gatherPrefab, centerPos, Quaternion.identity);
            gather.Play();

            yield return new WaitForSeconds(config.gatherDuration);

            Destroy(gather.gameObject, 1f);
        }

        // 2. 임팩트 VFX
        if (config.impactPrefab != null)
        {
            VFXInstance impact = SpawnVFX(config.impactPrefab, centerPos, Quaternion.identity);
            impact.Play();

            // 사운드 재생
            if (config.impactSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(config.impactSound);
            }

            // 임팩트 시작 후 잠시 대기 후 데미지 적용
            yield return new WaitForSeconds(config.impactDuration * 0.5f);

            // 카메라 쉐이크
            if (config.shakeIntensity > 0 && CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(config.shakeIntensity, 0.15f);
            }

            // 모든 타겟에 임팩트 콜백
            for (int i = 0; i < targetPositions.Length; i++)
            {
                onImpact?.Invoke(i);
            }

            yield return new WaitForSeconds(config.impactDuration * 0.5f);

            Destroy(impact.gameObject, 1f);
        }
        else
        {
            // VFX 없으면 즉시 데미지
            for (int i = 0; i < targetPositions.Length; i++)
            {
                onImpact?.Invoke(i);
            }
        }

        onComplete?.Invoke();
    }

    // 투사체 공격 VFX
    public void PlayProjectileAttack(
        VFXConfig config,
        Vector3 fromPosition,
        Vector3 toPosition,
        Transform target,
        Action onReach,
        Action onComplete)
    {
        StartCoroutine(ProjectileAttackSequence(config, fromPosition, toPosition, target, onReach, onComplete));
    }

    private IEnumerator ProjectileAttackSequence(
        VFXConfig config,
        Vector3 from,
        Vector3 to,
        Transform target,
        Action onReach,
        Action onComplete)
    {
        if (config == null) config = defaultProjectileConfig;
        if (config == null)
        {
            Debug.LogWarning("[VFXManager] Projectile VFX Config가 없습니다!");
            onReach?.Invoke();
            onComplete?.Invoke();
            yield break;
        }

        // 1. 투사체 발사
        if (config.projectilePrefab != null)
        {
            VFXInstance projectile = SpawnVFX(config.projectilePrefab, from, Quaternion.identity);
            Vector3 dir = to - from;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            projectile.transform.rotation = Quaternion.AngleAxis(angle+90, Vector3.forward);
            projectile.Play();

            // 사운드
            if (config.launchSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(config.launchSound);
            }

            // DOTween으로 이동
            float distance = Vector3.Distance(from, to);
            float duration = config.travelDuration > 0 ? config.travelDuration : distance / config.travelSpeed;

            Tween moveTween;
            if (config.arcHeight > 0)
            {
                // 포물선 이동
                moveTween = projectile.transform.DOJump(to, config.arcHeight, 1, duration)
                    .SetEase(config.moveCurve);
            }
            else
            {
                // 직선 이동
                moveTween = projectile.transform.DOMove(to, duration)
                    .SetEase(config.moveCurve);
            }

            // 도착 완료
            moveTween.OnComplete(() =>
            {
                if (projectile != null && projectile.gameObject != null)
                {
                    projectile.transform.position = to;
                    projectile.StopAndClear();
                    projectile.gameObject.SetActive(false);
                    Destroy(projectile.gameObject);
                }
            });

            yield return new WaitForSeconds(duration);
        }

        // 2. 타겟 도착 콜백
        onReach?.Invoke();

        // 카메라 쉐이크
        if (config.shakeIntensity > 0 && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(config.shakeIntensity);
        }

        //플래시, 넉백
        if (target != null)
        {
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 플래시 효과
                if (config.enableHitFlash)
                {
                    enemy.PlayHitFlash(config.flashColor, config.flashDuration);
                }

                // 넉백 효과
                if (config.knockbackDistance > 0)
                {
                    Vector3 hitDirection = (to - from).normalized;
                    enemy.PlayKnockback(hitDirection, config.knockbackDistance, config.knockbackDuration);
                }
            }
        }

        //히트스톱
        if (config.hitStopDuration > 0 && !isHitStopActive)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }
            hitStopCoroutine = StartCoroutine(PlayHitStop(config.hitStopDuration));
        }
        // 3. 임팩트 VFX
        if (config.impactPrefab != null)
        {
            VFXInstance impact = SpawnVFX(config.impactPrefab, to, Quaternion.identity);
            impact.Play();

            if (config.impactSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(config.impactSound);
            }

            yield return new WaitForSeconds(config.impactDuration);

            Destroy(impact.gameObject, 1f);
        }

        onComplete?.Invoke();
    }

    // 다중 투사체 (랜덤 공격)
    public void PlayMultiProjectileAttack(
        VFXConfig config,
        Vector3 fromPosition,
        Vector3[] toPositions,
        Transform[] targets,
        Action<int> onEachReach,
        Action onComplete)
    {
        StartCoroutine(MultiProjectileSequence(config, fromPosition, toPositions, targets, onEachReach, onComplete));
    }

    private IEnumerator MultiProjectileSequence(
        VFXConfig config,
        Vector3 from,
        Vector3[] targetPositions,
        Transform[] targets,
        Action<int> onEachReach,
        Action onComplete)
    {
        if (config == null) config = defaultProjectileConfig;

        List<Coroutine> projectiles = new List<Coroutine>();

        // 모든 투사체 동시 발사
        for (int i = 0; i < targetPositions.Length; i++)
        {
            int index = i; // 클로저 캡처
            Transform target = (targets != null && index < targets.Length) ? targets[index] : null;
            Coroutine co = StartCoroutine(ProjectileAttackSequence(
                config, from, targetPositions[index], target,
                onReach: () => onEachReach?.Invoke(index),
                onComplete: null
            ));
            projectiles.Add(co);
        }

        // 모든 투사체 완료 대기
        float maxDuration = config.GetTotalDuration();
        yield return new WaitForSeconds(maxDuration);

        onComplete?.Invoke();
    }

    // 다중 발사점 → 다중 타겟 (PlayOnTarget 기반으로 간소화)
    private IEnumerator MultiSourceAoESequence(
        VFXConfig config,
        Vector3[] fromPositions,
        Vector3[] toPositions,
        Action<int> onEachImpact,
        Action onComplete)
    {
        if (config == null) config = defaultAoEConfig;
        if (config == null || fromPositions == null || fromPositions.Length == 0)
        {
            Debug.LogWarning("[VFXManager] MultiSourceAoE Config 또는 발사 위치 없음!");
            onComplete?.Invoke();
            yield break;
        }

        // 1단계: 각 주사위 위치에서 발사 이펙트 (gatherPrefab)
        if (config.gatherPrefab != null)
        {
            foreach (Vector3 pos in fromPositions)
            {
                PlayOnTarget(config.gatherPrefab, pos, config.gatherDuration);
            }
            yield return new WaitForSeconds(config.gatherDuration);
        }

        // 2단계: 각 적 위치에 임팩트 이펙트
        if (config.impactPrefab != null && toPositions != null)
        {
            // 사운드 재생 (첫 번째만)
            if (config.impactSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(config.impactSound);
            }

            // 모든 타겟에 임팩트 VFX
            for (int i = 0; i < toPositions.Length; i++)
            {
                PlayOnTarget(config.impactPrefab, toPositions[i], config.impactDuration);
            }

            // 임팩트 시작 후 잠시 대기 후 데미지 적용
            yield return new WaitForSeconds(config.impactDuration * 0.5f);

            // 카메라 쉐이크
            if (config.shakeIntensity > 0 && CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(config.shakeIntensity, 0.15f);
            }

            // 모든 타겟에 데미지 콜백
            for (int i = 0; i < toPositions.Length; i++)
            {
                onEachImpact?.Invoke(i);
            }

            yield return new WaitForSeconds(config.impactDuration * 0.5f);
        }
        else
        {
            // VFX 없으면 즉시 데미지
            for (int i = 0; i < (toPositions?.Length ?? 0); i++)
            {
                onEachImpact?.Invoke(i);
            }
        }

        onComplete?.Invoke();
    }

    //타겟 위치에서 직접 발동
    public void PlayOnTarget(
        GameObject prefab,
        Vector3 targetPosition,
        float duration,
        Action onComplete = null)
    {
        StartCoroutine(OnTargetDirectSequence(prefab, targetPosition, duration, onComplete));
    }



    private IEnumerator OnTargetDirectSequence(GameObject prefab, Vector3 position, float duration, Action onComplete)
    {
        if (prefab == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        VFXInstance vfx = SpawnVFX(prefab, position, Quaternion.identity);
        vfx.Play();

        yield return new WaitForSeconds(duration);

        Destroy(vfx.gameObject, 1f);
        onComplete?.Invoke();
    }

    // 버프/실드 VFX 
    public void PlayBuff(Transform target, VFXConfig config, float duration)
    {
        if (config == null) config = defaultBuffConfig;
        if (config == null || config.buffPrefab == null) return;

        VFXInstance buff = SpawnVFX(config.buffPrefab, target.position, Quaternion.identity);
        buff.transform.SetParent(target);
        buff.Play();

        Destroy(buff.gameObject, duration);
    }

    // 임팩트만 간단히 재생
    public void PlayImpact(Vector3 position, string impactType = "default")
    {
        if (defaultImpactConfig != null && defaultImpactConfig.impactPrefab != null)
        {
            PlayEffect(defaultImpactConfig.impactPrefab, position);
        }
    }

    // 중심점 계산
    private Vector3 CalculateCenterPosition(Vector3[] positions)
    {
        if (positions == null || positions.Length == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Length;
    }

    // 히트스톱 재생
    private IEnumerator PlayHitStop(float duration)
    {
        if (isHitStopActive) yield break;

        isHitStopActive = true;

        // 원래 타임스케일 저장
        float originalTimeScale = 1.0f;

        // 시간 느리게
        Time.timeScale = 0.05f;

        // 실제 시간으로 대기
        yield return new WaitForSecondsRealtime(duration);

        //복원
        Time.timeScale = originalTimeScale;

        isHitStopActive = false;
        hitStopCoroutine = null;
    }

    // VFX다 날리기
    public void ClearAllVFX()
    {
        foreach (Transform child in vfxContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
