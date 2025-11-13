using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq를 사용하기 위해 필요

/// <summary>
/// [!!! 핵심 수정 (SO + 인스펙터 방식) !!!]
/// 1. 'allGameZones' (List<ZoneData>) 변수를 인스펙터에서 받습니다.
/// 2. 'CacheEnemyData' 함수가 부활: 게임 시작 시 ZoneData/WaveData를 모두 스캔하여,
///    프리팹의 'Enemy.cs' 인스펙터에서 'difficultyCost' 등을 읽어와 '메뉴판'(Cache)을 만듭니다.
/// 3. 'GenerateWave' 함수가 이 '메뉴판'을 기반으로 "필수 스폰 + 추가 예산" 로직을 실행하고,
///    'List<GameObject>' (프리팹 리스트)를 반환합니다.
/// </summary>
public class WaveGenerator : MonoBehaviour
{
    public static WaveGenerator Instance { get; private set; }

    [Header("존(Zone) 데이터")]
    [Tooltip("게임에 등장할 '모든' ZoneData.asset 파일을 여기에 등록합니다.")]
    public List<ZoneData> allGameZones; 

    // [신규] 프리팹(Key)과 '캐시된 데이터'(Value)를 매칭하는 메뉴판
    private class CachedEnemyData
    {
        public GameObject prefab;
        public int cost;
        public int minZoneLevel;
        public bool isBoss;
    }
    private Dictionary<string, CachedEnemyData> enemyDataCache = new Dictionary<string, CachedEnemyData>();
    
    // [신규] '존(Zone)'별 랜덤 스폰풀을 캐시
    private Dictionary<string, List<CachedEnemyData>> zoneGeneralPoolCache = new Dictionary<string, List<CachedEnemyData>>();

    // 오브젝트 풀링용 딕셔너리
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheEnemyData(); // [!!!] 프리팹 스탯을 읽어 메뉴판 생성
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// 'allGameZones' SO를 모두 스캔하여, 프리팹 인스펙터의 'Enemy.cs' 스탯을 읽어와
    /// 'enemyDataCache' (메뉴판)을 생성합니다.
    /// </summary>
    private void CacheEnemyData()
    {
        enemyDataCache.Clear();
        poolDictionary.Clear();
        zoneGeneralPoolCache.Clear();

        if (allGameZones == null) return;

        // 모든 존(Zone) SO 순회
        foreach (ZoneData zone in allGameZones)
        {
            if (zone == null) continue;
            
            // 1. '일반 스폰 풀' 캐시
            List<CachedEnemyData> generalPool = new List<CachedEnemyData>();
            if (zone.generalEnemies != null)
            {
                foreach (GameObject prefab in zone.generalEnemies)
                {
                    CachedEnemyData data = AddOrGetCachedData(prefab);
                    if(data != null) generalPool.Add(data);
                }
            }
            zoneGeneralPoolCache.Add(zone.name, generalPool); // (존 이름으로 캐시)

            // 2. '필수 스폰' 프리팹 캐시
            if (zone.waves != null)
            {
                foreach (WaveData wave in zone.waves)
                {
                    if (wave == null) continue;
                    if (wave.mandatorySpawns != null)
                    {
                        foreach (var mandatory in wave.mandatorySpawns)
                        {
                            if (mandatory != null)
                                AddOrGetCachedData(mandatory.enemyPrefab);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// [신규 헬퍼] 프리팹을 캐시에 등록하고, 풀(Pool)을 초기화하는 함수
    /// </summary>
    private CachedEnemyData AddOrGetCachedData(GameObject prefab)
    {
        if (prefab == null) return null;
        string key = prefab.name;

        // 1. 이미 캐시(메뉴판)에 있으면 바로 반환
        if (enemyDataCache.ContainsKey(key))
        {
            return enemyDataCache[key];
        }

        // 2. 캐시에 없으면, 'GetComponent'로 인스펙터 값 읽기
        Enemy enemyScript = prefab.GetComponent<Enemy>();
        if (enemyScript == null)
        {
            Debug.LogError($"[WaveGenerator] {key} 프리팹에 Enemy 스크립트가 없습니다!");
            return null;
        }

        // 3. '인스펙터 값'으로 '메뉴판' 데이터 생성
        CachedEnemyData newData = new CachedEnemyData
        {
            prefab = prefab,
            cost = enemyScript.difficultyCost,
            minZoneLevel = enemyScript.minZoneLevel,
            isBoss = enemyScript.isBoss
        };
        
        enemyDataCache.Add(key, newData); // 메뉴판(캐시)에 등록
        
        // 4. 오브젝트 풀(Pool) 등록
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
            Debug.Log($"[WaveGenerator] 오브젝트 풀 등록: {key}");
        }
        
        return newData;
    }


    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// SO와 캐시(메뉴판)를 읽어 '필수 스폰 + 추가 예산' 로직을 실행합니다.
    /// </summary>
    public List<GameObject> GenerateWave(int currentZone, int currentWave)
    {
        List<GameObject> enemiesToSpawn = new List<GameObject>();

        // 1. 현재 존(Zone) 데이터 찾기
        int zoneIndex = currentZone - 1;
        if (allGameZones == null || allGameZones.Count <= zoneIndex || allGameZones[zoneIndex] == null)
        {
             Debug.LogError($"[WaveGenerator] Zone {currentZone}에 해당하는 'ZoneData.asset'이 allGameZones 리스트에 등록되지 않았습니다!");
             return enemiesToSpawn;
        }
        ZoneData zoneData = allGameZones[zoneIndex];

        // 2. 현재 웨이브(Wave) 데이터 찾기
        int waveIndex = currentWave - 1;
        if (zoneData.waves == null || zoneData.waves.Count <= waveIndex || zoneData.waves[waveIndex] == null)
        {
             Debug.LogError($"[WaveGenerator] {zoneData.name}에 Wave {currentWave}에 해당하는 'WaveData.asset'이 등록되지 않았습니다!");
             return enemiesToSpawn;
        }
        WaveData waveData = zoneData.waves[waveIndex];

        // 3. '필수 스폰' 목록(MandatorySpawns)을 리스트에 추가
        foreach (var spawnData in waveData.mandatorySpawns)
        {
            if (spawnData == null || spawnData.enemyPrefab == null) continue;
            
            for(int i=0; i < spawnData.count; i++)
            {
                enemiesToSpawn.Add(spawnData.enemyPrefab); // 프리팹을 리스트에 추가
            }
        }
        
        // 4. '추가 예산(BonusBudget)'으로 '랜덤 스폰'
        int budget = waveData.bonusBudget;
        if (budget > 0 && zoneGeneralPoolCache.ContainsKey(zoneData.name))
        {
            // [!!!] '메뉴판(캐시)'에서 이 존의 랜덤 스폰풀을 가져옴
             List<CachedEnemyData> availableEnemies = zoneGeneralPoolCache[zoneData.name]
                .Where(e => e.minZoneLevel <= currentZone && 
                            e.cost > 0 && 
                            !e.isBoss) // (일반몹만)
                .ToList();

            if (availableEnemies.Count > 0)
            {
                int safetyNet = 50;
                while (budget > 0 && safetyNet > 0)
                {
                    List<CachedEnemyData> purchasable = availableEnemies.Where(e => e.cost <= budget).ToList();
                    if (purchasable.Count == 0) break; 
                    
                    CachedEnemyData chosenData = purchasable[Random.Range(0, purchasable.Count)];
                    enemiesToSpawn.Add(chosenData.prefab); // 프리팹을 리스트에 추가
                    budget -= chosenData.cost;
                    safetyNet--;
                }
            }
        }

        Debug.Log($"[WaveGenerator] 웨이브 생성 완료 ({zoneData.name} - Wave {currentWave}). / 총 {enemiesToSpawn.Count}마리");
        return enemiesToSpawn;
    }

    // --- 오브젝트 풀링 함수들 (변경 없음) ---
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name; 
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"[WaveGenerator] 풀에 {key} 키가 없습니다. 동적으로 새 풀을 생성합니다.");
            poolDictionary.Add(key, new Queue<GameObject>());
        }
        if (poolDictionary[key].Count > 0)
        {
            GameObject objFromPool = poolDictionary[key].Dequeue();
            objFromPool.transform.position = position;
            objFromPool.transform.rotation = rotation;
            objFromPool.SetActive(true); 
            return objFromPool;
        }
        GameObject newObj = Instantiate(prefab, position, rotation);
        newObj.name = key; 
        return newObj;
    }

    public void ReturnToPool(GameObject objectToReturn)
    {
        string key = objectToReturn.name; 
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"[WaveGenerator] 풀에 {key} 키가 없습니다. 새로 추가합니다.");
            poolDictionary.Add(key, new Queue<GameObject>());
        }
        objectToReturn.SetActive(false); 
        poolDictionary[key].Enqueue(objectToReturn);
    }
}