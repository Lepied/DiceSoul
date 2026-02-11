using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// 언어 선택 UI 컴포넌트
public class LanguageSelector : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown languageDropdown;
    public Button applyButton;
    
    void Start()
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogError("[LanguageSelector] LocalizationManager가 없습니다!");
            gameObject.SetActive(false);
            return;
        }
        
        SetupDropdown();
        
        // Apply 버튼이 있으면 이벤트 연결
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(OnApplyClicked);
        }
        else
        {
            // Apply 버튼이 없으면 드롭다운 변경 시 즉시 적용
            languageDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
    }
    
    private void SetupDropdown()
    {
        if (languageDropdown == null)
        {
            Debug.LogError("[LanguageSelector] Dropdown이 할당되지 않았습니다!");
            return;
        }
        
        languageDropdown.ClearOptions();
        
        // 사용 가능한 언어 목록 추가
        Language[] languages = LocalizationManager.Instance.GetAvailableLanguages();
        var options = languages.Select(lang => new TMP_Dropdown.OptionData(lang.ToString())).ToList();
        languageDropdown.AddOptions(options);
        
        // 현재 언어를 선택 상태로
        Language currentLang = LocalizationManager.Instance.CurrentLanguage;
        int currentIndex = System.Array.IndexOf(languages, currentLang);
        if (currentIndex >= 0)
        {
            languageDropdown.value = currentIndex;
        }
    }
    
    private void OnDropdownChanged(int index)
    {
        Language[] languages = LocalizationManager.Instance.GetAvailableLanguages();
        if (index >= 0 && index < languages.Length)
        {
            LocalizationManager.Instance.ChangeLanguage(languages[index]);
        }
    }
    
    private void OnApplyClicked()
    {
        OnDropdownChanged(languageDropdown.value);
    }
}
