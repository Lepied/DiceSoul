using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New DeckData", menuName = "DiceSoul/Deck Data")]
public class DeckData : ScriptableObject
{
    [Header("기본 정보")]
    public string deckKey;        // GameManager가 식별할 키
    public string deckName;       // UI에 표시될 이름
    [TextArea]
    public string description;    // 덱 설명

    [Header("해금 조건")]
    public int unlockCost;        // 비용
    public string unlockKey;      // 저장될 키
    
    [Header("미리보기용 정보")]
    public List<string> displayDice; // UI에 보여줄 주사위 목록
    public string displayRelicID;    // UI에 보여줄 시작 유물
    
    //현재 언어에 맞는 덱 이름 반환
    public string GetLocalizedName()
    {
        if (LocalizationManager.Instance != null)
        {
            string translatedKey = TranslateDeckKey(deckKey);
            string key = $"DECK_{translatedKey}_NAME";
            return LocalizationManager.Instance.GetText(key);
        }
        return deckName;
    }
    
    // 현재 언어에 맞는 덱 설명 반환
    public string GetLocalizedDescription()
    {
        if (LocalizationManager.Instance != null)
        {
            string translatedKey = TranslateDeckKey(deckKey);
            string key = $"DECK_{translatedKey}_DESC";
            return LocalizationManager.Instance.GetText(key);
        }
        return description;
    }
    
    // 한글 deckKey를 영어로 변환
    private string TranslateDeckKey(string key)
    {
        var mapping = new System.Collections.Generic.Dictionary<string, string>
        {
            { "기본", "DEFAULT" },
            { "Default", "DEFAULT" },
            { "단골", "REGULAR" },
            { "Regular", "REGULAR" },
            { "도박사", "GAMBLER" },
            { "Gambler", "GAMBLER" },
            { "마법사", "WIZARD" },
            { "Wizard", "WIZARD" }
        };
        
        if (mapping.TryGetValue(key, out string result))
            return result;
        
        return key.ToUpper();
    }
}