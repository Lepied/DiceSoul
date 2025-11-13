using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 'generalEnemies' 리스트가 'EnemyData' (SO) 대신,
/// 'Enemy' 프리팹 (GameObject)을 직접 연결하도록 변경했습니다.
/// </summary>
[CreateAssetMenu(fileName = "Zone 1 - Plains", menuName = "Dice Rogue/Zone Data")]
public class ZoneData : ScriptableObject
{
    [Header("존 정보")]
    public string zoneName = "평원";
    public int zoneTier = 1; // 난이도 티어
    
    [Header("웨이브 구성 (순서대로)")]
    [Tooltip("이 존을 구성하는 웨이브 리스트 (WaveData.asset 5개)")]
    public List<WaveData> waves; 

    [Header("랜덤 스폰 풀 ('추가 예산'용)")]
    [Tooltip("이 존의 '추가 예산'으로 스폰될 수 있는 일반 적 프리팹 리스트")]
    // [수정] EnemyData -> GameObject
    public List<GameObject> generalEnemies;
}