using UnityEngine;

/// <summary>
/// (신규 파일)
/// '적'의 모든 원본 데이터를 담는 ScriptableObject입니다.
/// WaveGenerator가 이 파일을 참조하여 '메뉴판'을 만듭니다.
/// </summary>
[CreateAssetMenu(fileName = "New EnemyData", menuName = "Dice Rogue/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("핵심 데이터")]
    [Tooltip("WaveGenerator가 참조할 프리팹")]
    public GameObject enemyPrefab; // (Goblin.cs, Troll.cs 등이 붙어있는 프리팹)

    [Header("스폰 로직")]
    [Tooltip("이 적을 스폰하는 데 필요한 '난이도 비용'")]
    public int difficultyCost = 5; 
    
    [Tooltip("이 적이 '일반 웨이브'의 '추가 예산'으로 스폰될 수 있는지 여부")]
    public bool canSpawnInGeneralWaves = true;
    
    [Tooltip("이 적이 '보스 웨이브'의 '추가 예산'으로 스폰될 수 있는지 여부")]
    public bool canSpawnInBossWaves = false; // (보스는 보통 Mandatory로만 스폰)
    
    [Tooltip("이 적이 등장하기 시작하는 최소 존(Zone) 레벨")]
    public int minZoneLevel = 1;
}