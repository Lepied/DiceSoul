using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // 런 상태
    public int currentHealth;
    public int maxHealth;
    public int currentGold;
    public int currentZone;
    public int currentWave;

    // 인벤토리
    public List<string> myDeck = new List<string>(); 
    public List<string> myRelicIDs = new List<string>(); 
}