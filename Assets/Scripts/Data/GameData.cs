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
    
    // 런 존 순서
    public List<string> runZoneOrder = new List<string>();
    
    // 런 통계
    public float playTime = 0f;
    public int totalKills = 0;
    public int totalGoldEarned = 0;
    public int maxDamageDealt = 0;
    public int maxChainCount = 0;
    public int bossesDefeated = 0;
    public int perfectWaves = 0;
    
    // 족보 사용 횟수 직접저장 -> 왜? 딕셔너리로 관리하고있는 족보별 사용횟수를 저장할 때 직렬화할 수 없어서.
    public List<string> handUsageKeys = new List<string>();
    public List<int> handUsageValues = new List<int>();
}