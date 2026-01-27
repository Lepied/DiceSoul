using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("설정 키")]
    private const string KEY_BGM_VOLUME = "Settings_BGM_Volume";
    private const string KEY_SFX_VOLUME = "Settings_SFX_Volume";
    private const string KEY_FULLSCREEN = "Settings_Fullscreen";
    private const string KEY_RESOLUTION_WIDTH = "Settings_Resolution_Width";
    private const string KEY_RESOLUTION_HEIGHT = "Settings_Resolution_Height";

    [Header("기본값")]
    public float defaultBGMVolume = 0.5f;
    public float defaultSFXVolume = 0.5f;
    public bool defaultFullscreen = true;

    // 현재 설정 값
    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public bool IsFullscreen { get; private set; }
    public int ResolutionWidth { get; private set; }
    public int ResolutionHeight { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //저장된 설정 불러오기
    public void LoadSettings()
    {
        BGMVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, defaultBGMVolume);
        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, defaultSFXVolume);
        IsFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, defaultFullscreen ? 1 : 0) == 1;
        
        // 해상도
        ResolutionWidth = PlayerPrefs.GetInt(KEY_RESOLUTION_WIDTH, Screen.currentResolution.width);
        ResolutionHeight = PlayerPrefs.GetInt(KEY_RESOLUTION_HEIGHT, Screen.currentResolution.height);

        ApplySettings();
    }

    // 현재 설정 저장
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(KEY_BGM_VOLUME, BGMVolume);
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, SFXVolume);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, IsFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RESOLUTION_WIDTH, ResolutionWidth);
        PlayerPrefs.SetInt(KEY_RESOLUTION_HEIGHT, ResolutionHeight);
        PlayerPrefs.Save();
    }

    // 설정 적용 (SoundManager, 화면 설정 등)
    public void ApplySettings()
    {
        // BGM/SFX 볼륨 적용
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(BGMVolume);
            SoundManager.Instance.SetSFXVolume(SFXVolume);
        }

        // 전체화면 적용
        Screen.fullScreen = IsFullscreen;

        // 해상도 적용 (전체화면이 아닐 때만)
        if (!IsFullscreen)
        {
            Screen.SetResolution(ResolutionWidth, ResolutionHeight, false);
        }

    }

    // 개별 설정
    public void SetBGMVolume(float volume)
    {
        BGMVolume = Mathf.Clamp01(volume);
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(BGMVolume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(SFXVolume);
        }
    }

    public void SetFullscreen(bool fullscreen)
    {
        IsFullscreen = fullscreen;
        Screen.fullScreen = IsFullscreen;

        // 전체화면 해제 시 저장된 해상도 적용
        if (!IsFullscreen)
        {
            Screen.SetResolution(ResolutionWidth, ResolutionHeight, false);
        }
    }

    public void SetResolution(int width, int height)
    {
        ResolutionWidth = width;
        ResolutionHeight = height;

        if (!IsFullscreen)
        {
            Screen.SetResolution(ResolutionWidth, ResolutionHeight, false);
        }
    }

    // 설정 초기화
    public void ResetToDefaults()
    {
        BGMVolume = defaultBGMVolume;
        SFXVolume = defaultSFXVolume;
        IsFullscreen = defaultFullscreen;
        ResolutionWidth = Screen.currentResolution.width;
        ResolutionHeight = Screen.currentResolution.height;

        ApplySettings();
        SaveSettings();

    }
}
