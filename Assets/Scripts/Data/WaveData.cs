using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 'EnemyData' (SO) 대신, 'Enemy' 프리팹 (GameObject)을
/// 직접 연결하도록 MandatorySpawn 구조체를 변경했습니다.
/// </summary>
[System.Serializable]
public class MandatorySpawn
{
    [Tooltip("Goblin_Prefab, Troll_Prefab 등 'Enemy' 프리팹을 직접 연결")]
    public GameObject enemyPrefab; 
    public int count = 1;
}

[CreateAssetMenu(fileName = "Wave 1-1", menuName = "Dice Rogue/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("웨이브 설계")]
    [Tooltip("이 웨이브에 '필수'로 스폰될 적 프리팹 리스트")]
    public List<MandatorySpawn> mandatorySpawns;
    
    [Tooltip("필수 스폰 외에, '추가 예산'으로 몇 점까지 랜덤 스폰할지")]
    public int bonusBudget = 0;
}