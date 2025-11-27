using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("오디오 소스 연결")]
    public AudioSource bgmSource; // 배경음악용
    public AudioSource sfxSource; // 효과음용

    [Header("사운드 라이브러리")]
    public List<SoundData> soundLibrary; 
    private Dictionary<string, AudioClip> soundDict = new Dictionary<string, AudioClip>();

    [System.Serializable]
    public struct SoundData
    {
        public string name;
        public AudioClip clip;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLibrary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLibrary()
    {
        foreach (var data in soundLibrary)
        {
            if (!soundDict.ContainsKey(data.name))
            {
                soundDict.Add(data.name, data.clip);
            }
        }
    }

    // BGM 
    public void PlayBGM(AudioClip clip, float volume = 0.5f)
    {
        if (clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    //SFX 이름재생
    public void PlaySFX(string soundName, float volume = 1.0f)
    {
        if (soundDict.ContainsKey(soundName))
        {
            sfxSource.PlayOneShot(soundDict[soundName], volume);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] 사운드 '{soundName}'를 찾을 수 없습니다!");
        }
    }

    //SFX 클립재생
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip != null)
        {

            sfxSource.PlayOneShot(clip, volume);
        }
    }
    
    // 랜덤 피치로 SFX 재생시키기 
    public void PlayRandomPitchSFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip != null)
        {
            sfxSource.pitch = Random.Range(0.9f, 1.1f); 
            sfxSource.PlayOneShot(clip, volume);
            sfxSource.pitch = 1.0f; 
        }
    }
}