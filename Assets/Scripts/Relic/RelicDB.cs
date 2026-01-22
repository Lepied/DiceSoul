using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 유물 데이터베이스 (ScriptableObject 기반)
// - Resources/Relics 폴더에서 자동 로드
// - 기존 Relic 클래스와 호환 유지
// - 획득 경로(DropPool)별 필터링 지원
public class RelicDB : MonoBehaviour
{
    public static RelicDB Instance { get; private set; }

    [Header("ScriptableObject 유물 목록")]
    [Tooltip("Resources/Relics 폴더에서 자동 로드하거나, 여기에 수동 할당")]
    public List<RelicData> relicDataAssets = new List<RelicData>();
    
    [Header("설정")]
    [Tooltip("Resources 폴더에서 자동 로드")]
    public bool autoLoadFromResources = true;
    public string resourcesPath = "Relics";

    // 내부 저장소
    private Dictionary<string, RelicData> allRelicData = new Dictionary<string, RelicData>();
    private Dictionary<string, Relic> allRelics = new Dictionary<string, Relic>();  // 기존 호환용

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeRelics();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeRelics()
    {
        // ScriptableObject에서 로드
        LoadFromScriptableObjects();
        
        Debug.Log($"[RelicDB] 총 {allRelics.Count}개 유물 로드됨 (SO: {allRelicData.Count}개)");
    }

    // ScriptableObject에서 유물 로드
    private void LoadFromScriptableObjects()
    {
        // Resources 폴더에서 자동 로드
        if (autoLoadFromResources)
        {
            RelicData[] loadedAssets = Resources.LoadAll<RelicData>(resourcesPath);
            foreach (var asset in loadedAssets)
            {
                if (!relicDataAssets.Contains(asset))
                    relicDataAssets.Add(asset);
            }
        }
        
        // RelicData → Relic 변환 및 등록
        foreach (var relicData in relicDataAssets)
        {
            if (relicData == null) continue;
            
            allRelicData[relicData.relicID] = relicData;
            
            // 기존 Relic 클래스로 변환 (하위 호환)
            Relic relic = ConvertToLegacyRelic(relicData);
            allRelics[relicData.relicID] = relic;
        }
    }

    // RelicData(SO) → Relic(기존 클래스) 변환
    private Relic ConvertToLegacyRelic(RelicData data)
    {
        return new Relic(
            data.relicID,
            data.relicName,
            data.description,
            data.icon,
            data.effectType,
            data.stringValue,
            data.intValue,
            data.floatValue,
            data.maxCount,
            data.IsUnlocked()
        );
    }

    // ID로 유물 조회
    public Relic GetRelicByID(string relicID)
    {
        return allRelics.TryGetValue(relicID, out var relic) ? relic : null;
    }


    // 획득 가능한 유물만
    public List<Relic> GetAcquirableRelics(int count)
    {
        return GetAcquirableRelics(count, null);
    }

    // 획득 가능한 유물만 필터링
    public List<Relic> GetAcquirableRelics(int count, RelicDropPool? dropPool)
    {
        if (allRelics.Count == 0 || GameManager.Instance == null) 
            return new List<Relic>();

        // 획득 가능한 유물 필터링
        List<Relic> availablePool = new List<Relic>();

        //되는지 안되는지 다 체크하기
        foreach (Relic masterRelic in allRelics.Values)
        {
            bool isUnlocked = masterRelic.IsUnLocked;
            if (!isUnlocked && PlayerPrefs.GetInt($"Unlock_{masterRelic.RelicID}", 0) == 1)
                isUnlocked = true;
            if (!isUnlocked) continue;
            if (dropPool.HasValue && allRelicData.TryGetValue(masterRelic.RelicID, out var relicData))
            {
                if (relicData.dropPool != dropPool.Value) continue;
            }
            if (!GameManager.Instance.CanAcquireRelic(masterRelic)) continue;

            availablePool.Add(masterRelic);
        }

        if (availablePool.Count == 0)
        {
            Debug.LogWarning("[RelicDB] 획듍 가능한 유물이 없습니다!");
            return new List<Relic>();
        }
        int actualCount = Mathf.Min(count, availablePool.Count);
        return availablePool.OrderBy(x => Random.value).Take(actualCount).ToList();
    }

    // 등급별 가중치를 적용한 랜덤 유물 획득
    public List<Relic> GetWeightedRandomRelics(int count, RelicDropPool dropPool)
    {
        // 획득 경로별 등급 가중치
        Dictionary<RelicRarity, float> weights = dropPool switch
        {
            RelicDropPool.WaveReward => new Dictionary<RelicRarity, float>
            {
                { RelicRarity.Common, 0.70f },
                { RelicRarity.Uncommon, 0.25f },
                { RelicRarity.Rare, 0.05f },
                { RelicRarity.Epic, 0.00f }
            },
            RelicDropPool.ShopOnly => new Dictionary<RelicRarity, float>
            {
                { RelicRarity.Common, 0.15f },
                { RelicRarity.Uncommon, 0.30f },
                { RelicRarity.Rare, 0.45f },
                { RelicRarity.Epic, 0.10f }
            },
            RelicDropPool.MaintenanceReward => new Dictionary<RelicRarity, float>
            {
                { RelicRarity.Common, 0.10f },
                { RelicRarity.Uncommon, 0.40f },
                { RelicRarity.Rare, 0.40f },
                { RelicRarity.Epic, 0.10f }
            },
            _ => new Dictionary<RelicRarity, float>
            {
                { RelicRarity.Common, 0.25f },
                { RelicRarity.Uncommon, 0.25f },
                { RelicRarity.Rare, 0.25f },
                { RelicRarity.Epic, 0.25f }
            }
        };

        // 획득 가능한 유물만 가져오기)
        var basePool = GetAcquirableRelics(count * 3, dropPool);
        
        if (basePool.Count == 0)
        {
            Debug.LogWarning($"[RelicDB] {dropPool} 경로에서 획득 가능한 유물이 없습니다!");
            return new List<Relic>();
        }

        // 등급별로 그룹화하여 가중치 풀 생성
        List<Relic> weightedPool = new List<Relic>();
        
        foreach (var relic in basePool)
        {
            // RelicData에서 등급 확인
            RelicRarity rarity = RelicRarity.Common; 
            if (allRelicData.TryGetValue(relic.RelicID, out var data))
            {
                rarity = data.rarity;
            }

            // 가중치에 따라 복제 추가
            float weight = weights.TryGetValue(rarity, out var w) ? w : 0.25f;
            int copies = Mathf.RoundToInt(weight * 100);
            
            for (int i = 0; i < copies; i++)
            {
                weightedPool.Add(relic);
            }
        }

        // 가중치 풀에서 랜덤 선택
        var selected = weightedPool.OrderBy(x => Random.value)
                                   .Distinct()
                                   .Take(count)
                                   .ToList();
        
        // 부족하면 남은 유물에서 채우기
        if (selected.Count < count)
        {
            var remaining = basePool.Except(selected)
                                   .OrderBy(x => Random.value)
                                   .Take(count - selected.Count);
            selected.AddRange(remaining);
        }

        return selected;
    }

    // 모든 유물 목록 (디버그용)
    public List<Relic> GetAllRelics()
    {
        return allRelics.Values.ToList();
    }

    // 특정 획득 경로의 모든 유물
    public List<RelicData> GetRelicsByDropPool(RelicDropPool dropPool)
    {
        return allRelicData.Values.Where(r => r.dropPool == dropPool).ToList();
    }
}
