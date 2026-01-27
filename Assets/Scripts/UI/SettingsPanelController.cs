using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanelController : MonoBehaviour
{
    [Header("UI 요소")]
    public GameObject settingsPanel;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public Button closeButton;
    public Button saveButton;
    public Button resetButton;

    [Header("텍스트")]
    public TextMeshProUGUI bgmVolumeText;
    public TextMeshProUGUI sfxVolumeText;

    // 임시 설정 값
    private float tempBGMVolume;
    private float tempSFXVolume;
    private bool tempFullscreen;
    private int tempResolutionIndex;

    // 지원하는 해상도 목록 (16:9 비율)
    private readonly (int width, int height)[] resolutions = new (int, int)[]
    {
        (1920, 1080),   // FHD
        (1600, 900),   // HD+
        (1280, 720)   // HD
    };

    void Start()
    {
        // 버튼 리스너 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButton);
        }
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveButton);
        }
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButton);
        }

        // 슬라이더 리스너 연결
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // 토글 리스너 연결
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        // 해상도 드롭다운 초기화 및 리스너 연결
        if (resolutionDropdown != null)
        {
            RectTransform templateRect = resolutionDropdown.template;
            if (templateRect != null)
            {
                templateRect.sizeDelta = new Vector2(templateRect.sizeDelta.x, 240);
            }

            resolutionDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            foreach (var res in resolutions)
            {
                options.Add($"{res.width} x {res.height}");
            }
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        // 초기 상태는 비활성화
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    //설정 패널 열기
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        // 현재 설정 불러오기
        LoadCurrentSettings();

        // 일시정지
        Time.timeScale = 0f;
    }

    // 설정 패널 닫기
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // 일시정지 해제
        Time.timeScale = 1f;
    }

    // 현재 저장된 설정을 UI에 반영
    private void LoadCurrentSettings()
    {
        if (SettingsManager.Instance == null) return;

        tempBGMVolume = SettingsManager.Instance.BGMVolume;
        tempSFXVolume = SettingsManager.Instance.SFXVolume;
        tempFullscreen = SettingsManager.Instance.IsFullscreen;

        // UI 업데이트
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(tempBGMVolume);
            UpdateBGMVolumeText(tempBGMVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(tempSFXVolume);
            UpdateSFXVolumeText(tempSFXVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(tempFullscreen);
        }

        // 현재 해상도에 맞는 드롭다운 인덱스 찾기
        if (resolutionDropdown != null)
        {
            int currentWidth = SettingsManager.Instance.ResolutionWidth;
            int currentHeight = SettingsManager.Instance.ResolutionHeight;
            tempResolutionIndex = FindClosestResolutionIndex(currentWidth, currentHeight);
            resolutionDropdown.SetValueWithoutNotify(tempResolutionIndex);
            
            // 전체화면이면 해상도 드롭다운 비활성화
            resolutionDropdown.interactable = !tempFullscreen;
        }
    }

    // 현재 해상도와 가장 가까운 옵션 찾기
    private int FindClosestResolutionIndex(int width, int height)
    {
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
            {
                return i;
            }
        }
        return 0;
    }


    private void OnBGMVolumeChanged(float value)
    {
        tempBGMVolume = value;
        UpdateBGMVolumeText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetBGMVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        tempSFXVolume = value;
        UpdateSFXVolumeText(value);

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetSFXVolume(value);
        }

        // 테스트 사운드 재생
        if (SoundManager.Instance != null)
        {
            //ui클릭사운드 여기서 재생시키기
        }
    }

    private void OnFullscreenToggled(bool isOn)
    {
        tempFullscreen = isOn;
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetFullscreen(isOn);
        }

        // 전체화면이면 해상도 드롭다운 비활성화
        if (resolutionDropdown != null)
        {
            resolutionDropdown.interactable = !isOn;
        }
    }

    private void OnResolutionChanged(int index)
    {
        tempResolutionIndex = index;
        var selectedRes = resolutions[index];

        if (SettingsManager.Instance != null && !tempFullscreen)
        {
            SettingsManager.Instance.SetResolution(selectedRes.width, selectedRes.height);
        }
    }

    private void OnSaveButton()
    {
        // 설정 저장
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings();
        }

        CloseSettings();
    }

    private void OnCloseButton()
    {
        // 저장 안 하고 닫기
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.LoadSettings();
        }

        CloseSettings();
    }

    private void OnResetButton()
    {
        // 기본값으로 초기화
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ResetToDefaults();
        }

        // UI 업데이트
        LoadCurrentSettings();
    }


    private void UpdateBGMVolumeText(float value)
    {
        if (bgmVolumeText != null)
        {
            bgmVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void UpdateSFXVolumeText(float value)
    {
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }


    public bool IsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    public void ToggleSettings()
    {
        if (IsOpen())
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }
}
