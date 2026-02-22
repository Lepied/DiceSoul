#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


// 언어 변경 테스트용
public class LanguageTestToggle : MonoBehaviour
{

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
        {
            ToggleLanguage();
        }
    }
    
    public void ToggleLanguage()
    {
        if (LocalizationManager.Instance == null)
        {
            return;
        }
        
        Language current = LocalizationManager.Instance.CurrentLanguage;
        Language newLang = current == Language.Korean ? Language.English : Language.Korean;
        
        LocalizationManager.Instance.ChangeLanguage(newLang);
        
    }
    
}
#endif
