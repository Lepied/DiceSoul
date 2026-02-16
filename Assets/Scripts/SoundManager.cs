using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("오디오 소스 연결")]
    public AudioSource bgmSource; // 배경음악용
    public AudioSource sfxSource; // 효과음용

    [Header("볼륨 설정")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f; 
    [Range(0f, 1f)] public float sfxVolume = 1.0f; 

    [Header("AudioSource 풀링")]
    [SerializeField] private int poolSize = 20; // 풀 크기 (여러 레이어 사운드 동시 재생 대비)
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private int currentPoolIndex = 0;

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
            InitializeAudioPool();
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

    // AudioSource 풀 초기화
    private void InitializeAudioPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject($"PooledAudioSource_{i}");
            go.transform.SetParent(transform);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSourcePool.Add(source);
        }
    }

    // 풀에서 사용 가능한 AudioSource 가져오기
    private AudioSource GetPooledAudioSource()
    {
        // 현재 재생 중이지 않은 AudioSource 찾기
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            int index = (currentPoolIndex + i) % audioSourcePool.Count;
            if (!audioSourcePool[index].isPlaying)
            {
                currentPoolIndex = (index + 1) % audioSourcePool.Count;
                return audioSourcePool[index];
            }
        }

        // 모두 사용 중이면 순환해서 다음 것 사용
        AudioSource source = audioSourcePool[currentPoolIndex];
        currentPoolIndex = (currentPoolIndex + 1) % audioSourcePool.Count;
        return source;
    }

    // BGM 
    public void PlayBGM(AudioClip clip, float volume = 0.5f)
    {
        if (clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = volume * bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }
    
    // BGM SoundConfig 재생
    public void PlayBGMConfig(SoundConfig config)
    {
        if (config == null || !config.HasSound()) return;
        
        AudioClip clip = config.primarySound;
        if (clip == null) return;
        
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        
        bgmSource.clip = clip;
        bgmSource.volume = config.volume * bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // BGM 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource.isPlaying)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    // SFX 볼륨 설정
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    //SFX 이름재생
    public void PlaySFX(string soundName, float volume = 1.0f)
    {
        if (soundDict.ContainsKey(soundName))
        {
            sfxSource.PlayOneShot(soundDict[soundName], volume * sfxVolume);
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
            sfxSource.PlayOneShot(clip, volume * sfxVolume);
        }
    }
    
    // 랜덤 피치로 SFX 재생
    public void PlayRandomPitchSFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip != null)
        {
            AudioSource source = GetPooledAudioSource();
            
            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.pitch = Random.Range(0.9f, 1.1f);
            source.loop = false;
            source.Play();
        }
    }

    // SoundConfig 재생 (완벽한 시스템)
    public void PlaySoundConfig(SoundConfig config)
    {
        if (config == null || !config.HasSound()) return;

        // 메인 사운드 재생
        if (config.primarySound != null)
        {
            if (config.useRandomPitch)
            {
                AudioSource source = GetPooledAudioSource();
                source.clip = config.primarySound;
                source.volume = config.volume * sfxVolume;
                source.pitch = 1.0f + Random.Range(-config.pitchVariation, config.pitchVariation);
                source.loop = false;
                source.Play();
            }
            else
            {
                sfxSource.PlayOneShot(config.primarySound, config.volume * sfxVolume);
            }
        }

        // 레이어 사운드들 동시 재생
        if (config.layeredSounds != null)
        {
            foreach (var sound in config.layeredSounds)
            {
                if (sound != null)
                {
                    if (config.useRandomPitch)
                    {
                        AudioSource source = GetPooledAudioSource();
                        source.clip = sound;
                        source.volume = config.volume * sfxVolume;
                        source.pitch = 1.0f + Random.Range(-config.pitchVariation, config.pitchVariation);
                        source.loop = false;
                        source.Play();
                    }
                    else
                    {
                        sfxSource.PlayOneShot(sound, config.volume * sfxVolume);
                    }
                }
            }
        }
    }
}