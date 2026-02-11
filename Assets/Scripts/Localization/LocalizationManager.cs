using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum Language
{
    Korean,
    English
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    
    [Header("Settings")]
    public Language defaultLanguage = Language.Korean;
    
    [Header("CSV Files")]
    public TextAsset[] localizationCSVFiles;
    
    public Language CurrentLanguage { get; private set; }
    
    // Key → (Language → Text)
    private Dictionary<string, Dictionary<Language, string>> textData = new Dictionary<string, Dictionary<Language, string>>();
    
    // 언어 변경 이벤트
    public event System.Action OnLanguageChanged;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Initialize()
    {
        // PlayerPrefs에서 저장된 언어 불러오기
        string savedLang = PlayerPrefs.GetString("Language", defaultLanguage.ToString());
        if (System.Enum.TryParse(savedLang, out Language lang))
        {
            CurrentLanguage = lang;
        }
        else
        {
            CurrentLanguage = defaultLanguage;
        }
        
        // CSV 파일들 로드
        LoadAllCSVFiles();
    }
    
    private void LoadAllCSVFiles()
    {
        textData.Clear();
        
        if (localizationCSVFiles == null || localizationCSVFiles.Length == 0)
        {
            return;
        }
        
        foreach (var csvFile in localizationCSVFiles)
        {
            if (csvFile == null) continue;
            LoadCSV(csvFile);
        }
    }
    
    private void LoadCSV(TextAsset csvFile)
    {
        string[] lines = csvFile.text.Split('\n');
        
        if (lines.Length < 2)
        {
            Debug.LogWarning($"[Localization] CSV 파일이 비어있음: {csvFile.name}");
            return;
        }
        
        // 헤더 (Key,Korean,English)
        string[] headers = lines[0].Trim().Split(',');
        
        // 헤더에서 언어찾기
        Dictionary<Language, int> languageColumns = new Dictionary<Language, int>();
        for (int i = 1; i < headers.Length; i++)
        {
            string header = headers[i].Trim();
            if (System.Enum.TryParse<Language>(header, true, out Language lang))
            {
                languageColumns[lang] = i;
            }
        }
        
        if (languageColumns.Count == 0)
        {
            Debug.LogWarning($"[Localization] 언어 컬럼을 찾을 수 없음: {csvFile.name}");
            return;
        }
        
        // 데이터파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] columns = ParseCSVLine(line);
            if (columns.Length < 2) continue;
            
            string key = columns[0].Trim();
            if (string.IsNullOrEmpty(key)) continue;
            
            if (!textData.ContainsKey(key))
            {
                textData[key] = new Dictionary<Language, string>();
            }
            
            // 각 언어별 텍스트 저장
            foreach (var kvp in languageColumns)
            {
                Language lang = kvp.Key;
                int colIndex = kvp.Value;
                
                if (colIndex < columns.Length)
                {
                    string text = columns[colIndex].Trim();
                    textData[key][lang] = text;
                }
            }
        }
        
        Debug.Log($"[Localization] CSV 로드 완료: {csvFile.name}");
    }
    
    // CSV 라인 파싱 (쉼표 처리)
    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        
        result.Add(current);
        return result.ToArray();
    }
    
    // 키에 해당하는 현재 언어의 텍스트 반환
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "";
        
        if (textData.TryGetValue(key, out var languages))
        {
            if (languages.TryGetValue(CurrentLanguage, out string text))
            {
                return text;
            }
            
            // 현재 언어에 없으면 기본 언어(Korean) 시도
            if (CurrentLanguage != Language.Korean && languages.TryGetValue(Language.Korean, out string fallback))
            {
                return fallback;
            }
        }
        
        Debug.LogWarning($"[Localization] 키를 찾을 수 없음: {key}");
        return $"[{key}]";
    }
    
    // 언어 변경
    public void ChangeLanguage(Language newLanguage)
    {
        if (CurrentLanguage == newLanguage)
            return;
        
        CurrentLanguage = newLanguage;
        PlayerPrefs.SetString("Language", newLanguage.ToString());
        PlayerPrefs.Save();
        
        Debug.Log($"[Localization] 언어 변경: {newLanguage}");
        
        // 모든 UI 갱신
        OnLanguageChanged?.Invoke();
    }
    
    // 사용 가능한 모든 언어 목록
    public Language[] GetAvailableLanguages()
    {
        return (Language[])System.Enum.GetValues(typeof(Language));
    }
    
    // 특정 키가 존재하는지 확인
    public bool HasKey(string key)
    {
        return textData.ContainsKey(key);
    }
}
