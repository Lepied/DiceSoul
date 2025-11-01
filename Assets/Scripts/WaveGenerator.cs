using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Where, OrderBy 등 Linq를 사용하기 위해 필요

/// <summary>
/// [수정] SpawnFromPool 함수가 등록되지 않은 프리팹(예: 주사위)도 처리할 수 있도록 보강
/// </summary>
public class WaveGenerator : MonoBehaviour
{
    public static WaveGenerator Instance { get; private set; }

    [Header("적 프리팹 풀")]
    [Tooltip("게임에 등장할 '모든' 적 프리팹(Goblin, Skeleton 등)을 여기에 등록합니다.")]
    public List<GameObject> enemyPrefabPool;

    // [수정] 오브젝트 풀링용 딕셔너리 (Key: 프리팹의 이름(string))
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    
    // [추가] 프리팹 원본을 이름으로 찾기 위한 딕셔너리
    private Dictionary<string, GameObject> prefabDictionary;

    private List<Enemy> enemyDataCache = new List<Enemy>();

    [Header("난이도 설정")]
    public int baseBudgetPerZone = 15;
    public int budgetBonusPerWave = 3;

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

    /// <summary>
    /// [수정] 프리팹 딕셔너리도 초기화합니다.
    /// </summary>
    private void CacheEnemyData()
    {
        enemyDataCache.Clear();
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabDictionary = new Dictionary<string, GameObject>(); // [추가]

        foreach (GameObject prefab in enemyPrefabPool)
        {
            if (prefab == null) continue;

            string key = prefab.name;
            
            // [추가] 프리팹 딕셔너리에 원본 등록
            if (!prefabDictionary.ContainsKey(key))
            {
                prefabDictionary.Add(key, prefab);
            }

            // [추가] 풀 딕셔너리에 큐 생성
            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary.Add(key, new Queue<GameObject>());
            }
            
            Enemy enemy = prefab.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemyDataCache.Add(enemy);
            }
            else
            {
                Debug.LogWarning($"[WaveGenerator] {key} 프리팹에 Enemy 스크립트가 없습니다.");
            }
        }
    }

    // ... (CalculateBudget, GenerateWave 함수는 이전과 동일) ...
    private int CalculateBudget(int currentZone, int currentWave)
    {
        return (currentZone * baseBudgetPerZone) + (currentWave * budgetBonusPerWave);
    }
    public List<GameObject> GenerateWave(int currentZone, int currentWave)
    {
        int budget = CalculateBudget(currentZone, currentWave);
        List<GameObject> enemiesToSpawn = new List<GameObject>();

        List<Enemy> availableEnemies = enemyDataCache
            .Where(e => e.minZoneLevel <= currentZone)
            .OrderBy(e => e.difficultyCost) 
            .ToList();

        if (availableEnemies.Count == 0)
        {
            Debug.LogError($"[WaveGenerator] Zone {currentZone}에서 스폰 가능한 적이 없습니다! 적 프리팹의 minZoneLevel을 확인하세요.");
            return enemiesToSpawn;
        }

        int safetyNet = 100; 

        while (budget > 0 && safetyNet > 0)
        {
            List<Enemy> purchasableEnemies = availableEnemies
                .Where(e => e.difficultyCost <= budget)
                .ToList();

            if (purchasableEnemies.Count == 0)
            {
                break; 
            }

            Enemy chosenEnemyData = purchasableEnemies[Random.Range(0, purchasableEnemies.Count)];
            enemiesToSpawn.Add(chosenEnemyData.gameObject); 
            budget -= chosenEnemyData.difficultyCost;
            safetyNet--;
        }
        
        if (safetyNet <= 0) {
             Debug.LogWarning("[WaveGenerator] 웨이브 생성 중 무한 루프 방지 장치가 발동했습니다.");
        }

        Debug.Log($"[WaveGenerator] 웨이브 생성 완료 (Zone {currentZone}, Wave {currentWave}). 예산: {CalculateBudget(currentZone, currentWave)} / 스폰: {enemiesToSpawn.Count}마리 / 남은 예산: {budget}");
        return enemiesToSpawn;
    }


    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// 풀에 없는 프리팹(예: 주사위)도 스폰할 수 있도록 수정합니다.
    /// </summary>
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[WaveGenerator] 스폰하려는 Prefab이 null입니다.");
            return null;
        }

        string key = prefab.name; 

        // 1. [수정] 풀 딕셔너리에 이 키가 등록되어 있는지 확인
        if (!poolDictionary.ContainsKey(key))
        {
            // 등록이 안 되어있다면 (예: 주사위 프리팹)
            Debug.LogWarning($"[WaveGenerator] 풀에 {key} 키가 없습니다. 동적으로 새 풀을 생성합니다.");
            poolDictionary.Add(key, new Queue<GameObject>());
            
            // 프리팹 원본도 딕셔너리에 등록 (Instantiate용)
            if (!prefabDictionary.ContainsKey(key))
            {
                prefabDictionary.Add(key, prefab);
            }
        }

        // 2. 큐에 오브젝트가 남아있는지 확인
        if (poolDictionary[key].Count > 0)
        {
            GameObject objFromPool = poolDictionary[key].Dequeue(); // 큐에서 꺼냄
            
            objFromPool.transform.position = position;
            objFromPool.transform.rotation = rotation;
            objFromPool.SetActive(true); 
            
            return objFromPool;
        }

        // 3. 풀에 없으면 새로 Instantiate (원본 프리팹 딕셔너리에서 찾음)
        if (prefabDictionary.ContainsKey(key))
        {
            GameObject newObj = Instantiate(prefabDictionary[key], position, rotation);
            newObj.name = key; // (중요) 오브젝트의 이름을 프리팹 이름(Key)과 동일하게 설정
            return newObj;
        }
        
        Debug.LogError($"[WaveGenerator] {key} 프리팹 원본을 찾을 수 없어 스폰에 실패했습니다.");
        return null;
    }

    /// <summary>
    /// 사용이 끝난 오브젝트를 풀에 반환합니다.
    /// </summary>
    public void ReturnToPool(GameObject objectToReturn)
    {
        if (objectToReturn == null) return;
        
        string key = objectToReturn.name; 

        if (!poolDictionary.ContainsKey(key))
        {
            // (오류 상황이지만, 만일을 대비해 새 큐를 생성)
            Debug.LogWarning($"[WaveGenerator] 풀에 {key} 키가 없습니다. 새로 추가합니다.");
            poolDictionary.Add(key, new Queue<GameObject>());
        }

        objectToReturn.SetActive(false); 
        poolDictionary[key].Enqueue(objectToReturn); // 큐에 다시 넣음
    }
}

