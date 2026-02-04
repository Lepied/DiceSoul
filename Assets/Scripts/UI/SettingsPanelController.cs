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

    [Header("인게임 메뉴 버튼")]
    public Button resumeButton;
    public Button returnToMainButton;
    public Button quitGameButton;

    [Header("확인 팝업")]
    public GameObject confirmPopup;
    public TextMeshProUGUI confirmMessage;
    public Button confirmYesButton;
    public Button confirmNoButton;

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
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveButton);
        }
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButton);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(CancelAndClose);
        }
        if (returnToMainButton != null)
        {
            returnToMainButton.onClick.AddListener(OnReturnToMainMenu);
        }
        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(OnQuitGame);
        }
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.AddListener(OnConfirmYes);
        }
        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.AddListener(OnConfirmNo);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CancelAndClose);
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
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
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

        // 버튼 활성화/비활성화
        bool isRunActive = IsRunActive();
    
        resumeButton.gameObject.SetActive(isRunActive);
        returnToMainButton.gameObject.SetActive(isRunActive);
        quitGameButton.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(!isRunActive);
        // 런 진행 중이면 일시정지
        if (isRunActive)
        {
            Time.timeScale = 0f;
        }
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

    // 변경사항 취소하고 닫기
    private void CancelAndClose()
    {
        // 저장 안 하고 닫기
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.LoadSettings();
        }
        CloseSettings();
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

    // 런이 진행 중인지 확인
    private bool IsRunActive()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game" 
            && GameManager.Instance != null;
    }

    // 확인 팝업이 활성화되어 있는지
    public bool IsConfirmPopupActive()
    {
        return confirmPopup != null && confirmPopup.activeSelf;
    }

    // 메인으로 돌아가 (포기)
    private void OnReturnToMainMenu()
    {
        ShowConfirmPopup("출정을 포기하고\n영지로 돌아가시겠습니까?", ConfirmAction.ReturnToMain);
    }

    // 게임 종료
    private void OnQuitGame()
    {
        ShowConfirmPopup("게임을\n종료하시겠습니까?", ConfirmAction.QuitGame);
    }

    private enum ConfirmAction
    {
        ReturnToMain,
        QuitGame
    }

    private ConfirmAction currentConfirmAction;

    // 확인 팝업 표시
    private void ShowConfirmPopup(string message, ConfirmAction action)
    {
        if (confirmPopup != null && confirmMessage != null)
        {
            confirmMessage.text = message;
            confirmPopup.SetActive(true);
            currentConfirmAction = action;
        }
    }

    // 확인 팝업 - 예
    private void OnConfirmYes()
    {
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
        }

        switch (currentConfirmAction)
        {
            case ConfirmAction.ReturnToMain:
                OnConfirmReturnToMain();
                break;
            case ConfirmAction.QuitGame:
                OnConfirmQuit();
                break;
        }
    }

    // 확인 팝업 - 아니오
    private void OnConfirmNo()
    {
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
        }
    }

    // 포기 확정
    private void OnConfirmReturnToMain()
    {
        // 설정 패널과 확인 팝업 비활성화 (게임오버 연출이 보이도록)
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
        }
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        Time.timeScale = 1f; // 일시정지 해제
        
        // 게임오버 절차 밟기 (영구 재화 계산, 게임오버 연출)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ProcessAbandonRun();
        }
        else
        {
            // GameManager가 없으면 그냥 메인 메뉴로
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadMainMenuWithFade();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
    }

    // 게임 종료 확정
    private void OnConfirmQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
