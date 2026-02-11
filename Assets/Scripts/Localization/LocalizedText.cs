using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Header("Localization Key")]
    [Tooltip("Localization CSV에서 정의한 Key")]
    public string key;
    
    
    private TextMeshProUGUI textComponent;
    
    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        LocalizationManager.Instance.OnLanguageChanged += UpdateText;
        UpdateText();


    }
    
    void OnDestroy()
    {
        LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }
    
    private void UpdateText()
    {
        if (textComponent == null)
            return;
        
        if (string.IsNullOrEmpty(key))
        {
            return;
        }
        
        string localizedText = LocalizationManager.Instance.GetText(key);
        textComponent.text = localizedText;
    }
    
    public void SetKey(string newKey)
    {
        key = newKey;
        UpdateText();
    }
    
    public void Refresh()
    {
        UpdateText();
    }
}
