using UnityEngine;
using System.Collections;

public class MeteorFall : MonoBehaviour
{
    [Header("낙하 설정")]
    [Tooltip("시작 오프셋 (대각선 왼쪽 위)")]
    public Vector3 startOffset = new Vector3(-2f, 5f, 0);
    
    [Tooltip("낙하 시간")]
    public float fallDuration = 1f;
    
    void Start()
    {
        Vector3 targetPos = transform.position;
        transform.position = targetPos + startOffset;
        StartCoroutine(FallDown(targetPos));
    }
    
    IEnumerator FallDown(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            
            float easeT = t * t;
            
            transform.position = Vector3.Lerp(startPos, targetPos, easeT);
            yield return null;
        }
        
        transform.position = targetPos;
        
        Transform coreTransform = transform.Find("Core");
        if (coreTransform != null)
        {
            ParticleSystem coreParticle = coreTransform.GetComponent<ParticleSystem>();
            if (coreParticle != null)
            {
                coreParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
    }
}
