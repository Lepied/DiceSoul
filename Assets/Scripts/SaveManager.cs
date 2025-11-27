using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // 게임 시작 시 "이어하기"를 눌렀는지 확인
    public static bool shouldLoadSave = false; 

    private string saveFilePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 저장 경로 설정
            saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame(GameData data)
    {
        string json = JsonUtility.ToJson(data, true); 
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[SaveManager] 게임 저장 완료: {saveFilePath}");
    }

    public GameData LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("[SaveManager] 저장 파일이 없습니다.");
            return null;
        }

        string json = File.ReadAllText(saveFilePath);
        GameData data = JsonUtility.FromJson<GameData>(json);
        Debug.Log("[SaveManager] 게임 로드 완료");
        return data;
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("[SaveManager] 저장 파일 삭제됨 (게임 오버/클리어)");
        }
    }

    public bool HasSaveFile()
    {
        return File.Exists(saveFilePath);
    }
}