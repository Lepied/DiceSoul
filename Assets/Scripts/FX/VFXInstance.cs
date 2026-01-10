using UnityEngine;
using System.Collections;
using UnityEngine.VFX;

public class VFXInstance : MonoBehaviour
{
    private ParticleSystem[] particleSystems;
    private VisualEffect visualEffect;

    private bool isInitialized = false;
    private VFXInstanceType instanceType = VFXInstanceType.Unknown;

    private void Awake()
    {
        Initialize();
    }

    //초기화랑 타입감지
    private void Initialize()
    {
        if (isInitialized) return;

        // Particle System
        particleSystems = GetComponentsInChildren<ParticleSystem>();
        if (particleSystems.Length > 0)
        {
            instanceType = VFXInstanceType.ParticleSystem;
            isInitialized = true;
            return;
        }

        // VFX Graph
        visualEffect = GetComponent<VisualEffect>();
        if (visualEffect != null)
        {
            instanceType = VFXInstanceType.VFXGraph;
            isInitialized = true;
            return;
        }

        instanceType = VFXInstanceType.Unknown;
        isInitialized = true;
    }

    // VFX 재생
    public void Play()
    {
        Initialize();

        switch (instanceType)
        {
            case VFXInstanceType.ParticleSystem:
                foreach (var ps in particleSystems)
                {
                    ps.Play();
                }
                break;

            case VFXInstanceType.VFXGraph:
                if (visualEffect != null)
                {
                    visualEffect.Play();
                }
                break;
        }
    }

    // VFX 정지
    public void Stop(bool withChildren = true, ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting)
    {
        Initialize();

        switch (instanceType)
        {
            case VFXInstanceType.ParticleSystem:
                foreach (var ps in particleSystems)
                {
                    ps.Stop(withChildren, stopBehavior);
                }
                break;

            case VFXInstanceType.VFXGraph:
                if (visualEffect != null)
                {
                    visualEffect.Stop();
                }
                break;
        }
    }

    // VFX 즉시 제거
    public void StopAndClear()
    {
        Initialize();

        switch (instanceType)
        {
            case VFXInstanceType.ParticleSystem:
                foreach (var ps in particleSystems)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                }
                break;

            case VFXInstanceType.VFXGraph:
                if (visualEffect != null)
                {
                    visualEffect.Stop();
                    visualEffect.Reinit();
                }
                break;
        }
    }

    // VFX아직 잇는지 확인
    public bool IsAlive()
    {
        Initialize();

        switch (instanceType)
        {
            case VFXInstanceType.ParticleSystem:
                foreach (var ps in particleSystems)
                {
                    if (ps.IsAlive(true)) // withChildren = true
                        return true;
                }
                return false;

            case VFXInstanceType.VFXGraph:
                if (visualEffect != null)
                {
                    return visualEffect.aliveParticleCount > 0;
                }
                return false;

            default:
                return false;
        }
    }

    // VFX 지속 시간 계산
    public float GetDuration()
    {
        Initialize();

        switch (instanceType)
        {
            case VFXInstanceType.ParticleSystem:
                float maxDuration = 0f;
                foreach (var ps in particleSystems)
                {
                    float duration = ps.main.duration;
                    if (!ps.main.loop)
                    {
                        duration += ps.main.startLifetime.constantMax;
                    }
                    maxDuration = Mathf.Max(maxDuration, duration);
                }
                return maxDuration;

            case VFXInstanceType.VFXGraph:
                return 2f;

            default:
                return 1f;
        }
    }

    // 파라미터 설정 VFX Graph 용
    public void SetVector3(string name, Vector3 value)
    {
        if (instanceType == VFXInstanceType.VFXGraph && visualEffect != null)
        {
            if (visualEffect.HasVector3(name))
                visualEffect.SetVector3(name, value);
        }
    }

    public void SetFloat(string name, float value)
    {
        if (instanceType == VFXInstanceType.VFXGraph && visualEffect != null)
        {
            if (visualEffect.HasFloat(name))
                visualEffect.SetFloat(name, value);
        }
    }

    public void SetInt(string name, int value)
    {
        if (instanceType == VFXInstanceType.VFXGraph && visualEffect != null)
        {
            if (visualEffect.HasInt(name))
                visualEffect.SetInt(name, value);
        }
    }

    // 색상 변경 Particle System 용
    public void SetColor(Color color)
    {
        if (instanceType == VFXInstanceType.ParticleSystem)
        {
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.startColor = color;
            }
        }
    }

    // 크기 배율 적용 Particle System 용
    public void SetSizeMultiplier(float multiplier)
    {
        if (instanceType == VFXInstanceType.ParticleSystem)
        {
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.startSizeMultiplier = multiplier;
            }
        }
    }

    // VFX 대기
    public IEnumerator WaitForComplete()
    {
        Play();

        // 최소 1프레임 대기
        yield return null;

        // 살아있는 동안 대기
        while (IsAlive())
        {
            yield return null;
        }
    }

    public VFXInstanceType GetInstanceType()
    {
        Initialize();
        return instanceType;
    }
}

// VFX 인스턴스 타입
public enum VFXInstanceType
{
    Unknown,
    ParticleSystem,
    VFXGraph
}
