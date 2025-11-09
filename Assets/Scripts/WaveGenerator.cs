using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Where, OrderBy 등 Linq를 사용하기 위해 필요

/// <summary>
/// [수정] GenerateWave 함수가 'isBossWave' bool 값을 받아,
/// 보스 웨이브에는 보스만, 일반 웨이브에는 일반 적만 스폰하도록 수정
/// </summary>
public class WaveGenerator : MonoBehaviour
{
    public static WaveGenerator Instance { get; private set; }

    [Header("적 프리팹 풀")]
    [Tooltip("게임에 등장할 '모든' 적 프리팹(Goblin, Skeleton, Troll 등)을 여기에 등록합니다.")]
    public List<GameObject> enemyPrefabPool;

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private List<Enemy> enemyDataCache = new List<Enemy>();

    [Header("난이도 설정")]
    public int baseBudgetPerZone = 40;
    public int budgetBonusPerWave = 10;

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

    private void CacheEnemyData()
    {
        enemyDataCache.Clear();
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (GameObject prefab in enemyPrefabPool)
        {
            Enemy enemy = prefab.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemyDataCache.Add(enemy);
                string key = prefab.name;
                if (!poolDictionary.ContainsKey(key))
                {
                    poolDictionary.Add(key, new Queue<GameObject>());
                }
            }
            else
            {
                Debug.LogWarning($"[WaveGenerator] {prefab.name} 프리팹에 Enemy 스크립트가 없습니다.");
            }
        }
    }

    private int CalculateBudget(int currentZone, int currentWave)
    {
        return (currentZone * baseBudgetPerZone) + (currentWave * budgetBonusPerWave);
    }

    /// <summary>
    /// [!!! 핵심 수정 !!!]
    /// (int currentZone, int currentWave, bool isBossWave) 3개의 인자를 받습니다.
    /// </summary>
    public List<GameObject> GenerateWave(int currentZone, int currentWave, bool isBossWave)
    {
        int budget = CalculateBudget(currentZone, currentWave);
        List<GameObject> enemiesToSpawn = new List<GameObject>();

        // 1. [!!! 핵심 수정 !!!]
        // 필터링: 최소 존 레벨을 만족하고, 'isBoss' 상태가 요청된 상태와 일치하는 적만
        List<Enemy> availableEnemies = enemyDataCache
            .Where(e => e.minZoneLevel <= currentZone && e.isBoss == isBossWave)
            .OrderBy(e => e.difficultyCost)
            .ToList();

        if (availableEnemies.Count == 0)
        {
            string waveType = isBossWave ? "보스" : "일반";
            Debug.LogError($"[WaveGenerator] Zone {currentZone}에서 스폰 가능한 '{waveType}' 적이 없습니다! (minZoneLevel 또는 isBoss 설정을 확인하세요)");
            return enemiesToSpawn; // 빈 리스트 반환
        }

        int safetyNet = 100;

        while (budget > 0 && safetyNet > 0)
        {
            // 3. 이 예산으로 '살 수 있는' 적들만 다시 필터링
            List<Enemy> purchasableEnemies = availableEnemies
                .Where(e => e.difficultyCost <= budget)
                .ToList();

            if (purchasableEnemies.Count == 0)
            {
                // (보스 웨이브인데 예산이 부족할 경우)
                if (isBossWave && enemiesToSpawn.Count == 0)
                {
                    Debug.LogWarning($"[WaveGenerator] 보스 웨이브 예산({budget})이 부족하여 가장 저렴한 보스({availableEnemies[0].enemyName})를 강제 스폰합니다.");
                    enemiesToSpawn.Add(availableEnemies[0].gameObject);
                    budget -= availableEnemies[0].difficultyCost;
                }
                break; // 살 수 있는 적이 없으면 종료
            }

            // 4. 랜덤으로 적 하나 선택
            Enemy chosenEnemyData = purchasableEnemies[Random.Range(0, purchasableEnemies.Count)];
            
            enemiesToSpawn.Add(chosenEnemyData.gameObject); 
            budget -= chosenEnemyData.difficultyCost;
            safetyNet--;

            // [추가] 보스 웨이브는 보스 1마리만 스폰
            if (isBossWave)
            {
                break;
            }
        }
        
        if (safetyNet <= 0) {
             Debug.LogWarning("[WaveGenerator] 웨이브 생성 중 무한 루프 방지 장치가 발동했습니다.");
        }

        Debug.Log($"[WaveGenerator] 웨이브 생성 완료 (Zone {currentZone}, Wave {currentWave}, Boss: {isBossWave}). 예산: {CalculateBudget(currentZone, currentWave)} / 스폰: {enemiesToSpawn.Count}마리");
        return enemiesToSpawn;
    }

    // --- 오브젝트 풀링 함수들 (변경 없음) ---

    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string key = prefab.name; 

        // [수정] 풀이 동적으로 생성될 때 경고 로그
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

