using UnityEngine;

// 사운드 설정 클래스
[System.Serializable]
public class SoundConfig
{
    [Tooltip("메인 사운드")]
    public AudioClip primarySound;
    
    [Tooltip("레이어 사운드들 (동시 재생)")]
    public AudioClip[] layeredSounds;
    
    [Tooltip("볼륨")]
    [Range(0f, 1f)]
    public float volume = 1.0f;
    
    [Tooltip("랜덤 피치")]
    public bool useRandomPitch = false;
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.1f;

    public bool HasSound()
    {
        return primarySound != null || (layeredSounds != null && layeredSounds.Length > 0);
    }
}
