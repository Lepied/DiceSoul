using UnityEngine;
using System.Collections.Generic;


[DefaultExecutionOrder(-100)] //Awake같은거 먼저 실행시키기
public class WaveGenerator : MonoBehaviour
{
    public static WaveGenerator Instance { get; private set; }

    [Header("존(Zone) 데이터")]
    public List<ZoneData> allGameZones;

    private class CachedEnemyData
    {
        public GameObject prefab;
        public int cost;
        public int minZoneLevel;
        public bool isBoss;
    }

    private Transform enemyContainer;

    private Dictionary<string, CachedEnemyData> enemyDataCache = new Dictionary<string, CachedEnemyData>();
    private Dictionary<string, List<CachedEnemyData>> zoneGeneralPoolCache = new Dictionary<string, List<CachedEnemyData>>();

    // 오브젝트 풀링용 딕셔너리
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    //이번 런에서 플레이할 존 순서 (티어별로 모든 존을 셔플해서 구성)
    private List<ZoneData> currentRunZoneOrder = new List<ZoneData>();
    
    // GenerateWave 캐시
    private List<GameObject> _cachedEnemiesToSpawn = new List<GameObject>();
    private List<CachedEnemyData> _cachedAvailableEnemies = new List<CachedEnemyData>();
    private List<CachedEnemyData> _cachedPurchasable = new List<CachedEnemyData>();

    void Start()
    {
        if (enemyContainer == null)
        {
            GameObject containerObj = GameObject.Find("EnemyContainer");
            if (containerObj != null)
                enemyContainer = containerObj.transform;
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheEnemyData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void BuildRunZoneOrder()
    {
        currentRunZoneOrder.Clear();
        if (allGameZones == null || allGameZones.Count == 0)
        {
            Debug.LogError("[WaveGenerator] 'allGameZones' 리스트가비어쓰");
            return;
        }

        Dictionary<int, List<ZoneData>> zonesByTier = new Dictionary<int, List<ZoneData>>();
        for (int i = 0; i < allGameZones.Count; i++)
        {
            if (allGameZones[i] != null)
            {
                int tier = allGameZones[i].zoneTier;
                if (!zonesByTier.ContainsKey(tier))
                {
                    zonesByTier[tier] = new List<ZoneData>();
                }
                zonesByTier[tier].Add(allGameZones[i]);
            }
        }
        
        List<int> sortedTiers = new List<int>(zonesByTier.Keys);
        sortedTiers.Sort();

        foreach (int tier in sortedTiers)
        {
            List<ZoneData> zones = new List<ZoneData>(zonesByTier[tier]);
            
            ShuffleList(zones);
        
            currentRunZoneOrder.AddRange(zones);
        }
    }

    // Fisher-Yates 셔플 알고리즘
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }


    private void CacheEnemyData()
    {
        enemyDataCache.Clear();
        poolDictionary.Clear();
        zoneGeneralPoolCache.Clear();
        if (allGameZones == null) return;
        foreach (ZoneData zone in allGameZones)
        {
            if (zone == null) continue;
            List<CachedEnemyData> generalPool = new List<CachedEnemyData>();
            if (zone.generalEnemies != null)
            {
                foreach (GameObject prefab in zone.generalEnemies)
                {
                    CachedEnemyData data = AddOrGetCachedData(prefab);
                    if (data != null) generalPool.Add(data);
                }
            }
            zoneGeneralPoolCache.Add(zone.name, generalPool);
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

    private CachedEnemyData AddOrGetCachedData(GameObject prefab)
    {
        if (prefab == null) return null;
        string key = prefab.name;
        if (enemyDataCache.ContainsKey(key))
        {
            return enemyDataCache[key];
        }
        Enemy enemyScript = prefab.GetComponent<Enemy>();

        CachedEnemyData newData = new CachedEnemyData
        {
            prefab = prefab,
            cost = enemyScript.difficultyCost,
            minZoneLevel = enemyScript.minZoneLevel,
            isBoss = enemyScript.isBoss
        };
        enemyDataCache.Add(key, newData);
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
        }
        return newData;
    }

    public ZoneData GetCurrentZoneData(int currentZone)
    {
        int zoneIndex = currentZone - 1; 

        if (currentRunZoneOrder == null || currentRunZoneOrder.Count <= zoneIndex || currentRunZoneOrder[zoneIndex] == null)
        {
            return null;
        }

        return currentRunZoneOrder[zoneIndex];
    }

    public List<GameObject> GenerateWave(int currentZone, int currentWave)
    {
        _cachedEnemiesToSpawn.Clear();

        //  '셔플된' 런 순서에서 현재 존(Zone) 데이터 찾기
        ZoneData zoneData = GetCurrentZoneData(currentZone);
        if (zoneData == null)
        {
            Debug.LogError($"[WaveGenerator] Zone {currentZone}에 해당하는 'ZoneData'를 찾을 수 없습니다! (InitializeNewRun() 또는 allGameZones 인스펙터를 확인하세요)");
            return _cachedEnemiesToSpawn;
        }

        // 2. 현재 웨이브(Wave) 데이터 찾기
        int waveIndex = currentWave - 1;
        if (zoneData.waves == null || zoneData.waves.Count <= waveIndex || zoneData.waves[waveIndex] == null)
        {
            Debug.LogError($"[WaveGenerator] {zoneData.name}에 Wave {currentWave}에 해당하는 'WaveData.asset'이 등록되지 않았습니다!");
            return _cachedEnemiesToSpawn;
        }
        WaveData waveData = zoneData.waves[waveIndex];

        // 3. '필수 스폰' 목록(MandatorySpawns)을 리스트에 추가
        foreach (var spawnData in waveData.mandatorySpawns)
        {
            if (spawnData == null || spawnData.enemyPrefab == null) continue;

            for (int i = 0; i < spawnData.count; i++)
            {
                _cachedEnemiesToSpawn.Add(spawnData.enemyPrefab);
            }
        }

        // 4. '추가 예산(BonusBudget)'으로 '랜덤 스폰'
        int budget = waveData.bonusBudget;
        if (budget > 0 && zoneGeneralPoolCache.ContainsKey(zoneData.name))
        {
            _cachedAvailableEnemies.Clear();
            List<CachedEnemyData> cachedList = zoneGeneralPoolCache[zoneData.name];
            
            for (int i = 0; i < cachedList.Count; i++)
            {
                CachedEnemyData e = cachedList[i];
                if (e.minZoneLevel <= currentZone && e.cost > 0 && !e.isBoss)
                {
                    _cachedAvailableEnemies.Add(e);
                }
            }

            if (_cachedAvailableEnemies.Count > 0)
            {
                int safetyNet = 50;
                while (budget > 0 && safetyNet > 0)
                {
                    _cachedPurchasable.Clear();
                    for (int i = 0; i < _cachedAvailableEnemies.Count; i++)
                    {
                        if (_cachedAvailableEnemies[i].cost <= budget)
                        {
                            _cachedPurchasable.Add(_cachedAvailableEnemies[i]);
                        }
                    }
                    
                    if (_cachedPurchasable.Count == 0) break;

                    CachedEnemyData chosenData = _cachedPurchasable[Random.Range(0, _cachedPurchasable.Count)];
                    _cachedEnemiesToSpawn.Add(chosenData.prefab);
                    budget -= chosenData.cost;
                    safetyNet--;
                }
            }
        }

        Debug.Log($"[WaveGenerator] 웨이브 생성 완료 ({zoneData.name} - Wave {currentWave}). / 총 {_cachedEnemiesToSpawn.Count}마리");
        return _cachedEnemiesToSpawn;
    }

    // --- 오브젝트 풀링 함수들 ---
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (enemyContainer == null)
        {
            GameObject containerObj = GameObject.Find("EnemyContainer");
            enemyContainer = containerObj.transform;
        }

        string key = prefab.name;
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
        }
        if (poolDictionary[key].Count > 0)
        {
            GameObject objFromPool = poolDictionary[key].Dequeue();
            objFromPool.transform.position = position;
            objFromPool.transform.rotation = rotation;
            objFromPool.transform.SetParent(enemyContainer);
            objFromPool.SetActive(true);
            return objFromPool;
        }
        GameObject newObj = Instantiate(prefab, position, rotation);
        newObj.transform.SetParent(enemyContainer);
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

    //저장/로드
    // 현재 런의 존 순서를 문자열 리스트로 반환
    public List<string> GetRunZoneOrderAsStrings()
    {
        List<string> result = new List<string>();
        foreach (var zone in currentRunZoneOrder)
        {
            if (zone != null)
            {
                result.Add(zone.name);
            }
        }
        return result;
    }
    //저장된 존 순서 복원
    public void RestoreRunZoneOrder(List<string> zoneNames)
    {
        currentRunZoneOrder.Clear();

        if (zoneNames == null || zoneNames.Count == 0)
        {
            return;
        }

        foreach (string zoneName in zoneNames)
        {
            ZoneData zone = allGameZones.Find(z => z != null && z.name == zoneName);
            if (zone != null)
            {
                currentRunZoneOrder.Add(zone);
            }
        }
    }

    // 이번 런에서 플레이할 총 존 개수
    public int GetTotalZoneCount()
    {
        return currentRunZoneOrder.Count;
    }

    // 현재 존이 마지막 존인지
    public bool IsLastZone(int currentZone)
    {
        return currentZone >= currentRunZoneOrder.Count;
    }
}