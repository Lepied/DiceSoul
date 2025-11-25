using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 덱의 정보(이름, 가격, 구성 등)를 담는 데이터 에셋입니다.
/// </summary>
[CreateAssetMenu(fileName = "New DeckData", menuName = "Dice Rogue/Deck Data")]
public class DeckData : ScriptableObject
{
    [Header("기본 정보")]
    public string deckKey;        // GameManager가 식별할 키 (예: "단골", "도박사")
    public string deckName;       // UI에 표시될 이름
    [TextArea]
    public string description;    // 덱 설명

    [Header("해금 조건")]
    public int unlockCost;        // 비용 (0이면 기본 해금)
    public string unlockKey;      // 저장될 키 (예: "Unlocked_Regular")
    
    [Header("미리보기용 정보")]
    public List<string> displayDice; // UI에 보여줄 주사위 목록 (실제 로직은 GameManager에 있음)
    public string displayRelicID;    // UI에 보여줄 시작 유물
}