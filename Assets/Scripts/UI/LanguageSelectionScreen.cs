using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 게임 최초 실행 시 언어 선택 화면
/// </summary>
public class LanguageSelectionScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject languageSelectionPanel;
    public Button koreanButton;
    public Button englishButton;
    
    [Header("Text")]
    public string koreanButtonText = "한국어";
    public string englishButtonText = "English";
    
    [Header("Settings")]
    public bool showOnFirstLaunchOnly = true;
    public string nextSceneName = "MainMenu"; // 언어 선택 후 이동할 씬
    
    private void Start()
    {
        // 이미 언어를 선택했는지 확인
        bool hasSelectedLanguage = PlayerPrefs.GetInt("LanguageSelected", 0) == 1;
        
        if (showOnFirstLaunchOnly && hasSelectedLanguage)
        {
            // 이미 선택했으면 바로 다음 단계로
            HideLanguageSelection();
            LoadNextScene();
            return;
        }
        
        // 언어 선택 화면 표시
        ShowLanguageSelection();
    }
    
    private void ShowLanguageSelection()
    {
        if (languageSelectionPanel != null)
        {
            languageSelectionPanel.SetActive(true);
        }
        
        // 버튼 텍스트 설정
        if (koreanButton != null)
        {
            var koreanText = koreanButton.GetComponentInChildren<TextMeshProUGUI>();
            if (koreanText != null) koreanText.text = koreanButtonText;
            koreanButton.onClick.AddListener(() => OnLanguageSelected(Language.Korean));
        }
        
        if (englishButton != null)
        {
            var englishText = englishButton.GetComponentInChildren<TextMeshProUGUI>();
            if (englishText != null) englishText.text = englishButtonText;
            englishButton.onClick.AddListener(() => OnLanguageSelected(Language.English));
        }
    }
    
    private void OnLanguageSelected(Language selectedLanguage)
    {
        // LocalizationManager에 언어 설정
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ChangeLanguage(selectedLanguage);
        }
        
        // PlayerPrefs에 선택 완료 기록
        PlayerPrefs.SetInt("LanguageSelected", 1);
        PlayerPrefs.Save();
        
        Debug.Log($"[LanguageSelection] 언어 선택: {selectedLanguage}");
        
        // 언어 선택 화면 숨기기
        HideLanguageSelection();
        
        // 다음 씬으로 이동
        StartCoroutine(LoadNextSceneDelayed(0.3f));
    }
    
    private void HideLanguageSelection()
    {
        if (languageSelectionPanel != null)
        {
            languageSelectionPanel.SetActive(false);
        }
    }
    
    private IEnumerator LoadNextSceneDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadNextScene();
    }
    
    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[LanguageSelection] nextSceneName이 설정되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 언어 선택 초기화 (디버그용)
    /// </summary>
    public void ResetLanguageSelection()
    {
        PlayerPrefs.DeleteKey("LanguageSelected");
        PlayerPrefs.Save();
        Debug.Log("[LanguageSelection] 언어 선택 초기화됨");
    }
}
