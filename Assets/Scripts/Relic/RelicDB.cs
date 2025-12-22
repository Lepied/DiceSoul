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

    private Sprite LoadRelicIcon(string relicID)
    {
        return Resources.Load<Sprite>("RelicIcons/" + relicID);
    }

    // ID로 유물 조회 (기존 호환)
    public Relic GetRelicByID(string relicID)
    {
        return allRelics.TryGetValue(relicID, out var relic) ? relic : null;
    }

    // ID로 RelicData 조회
    public RelicData GetRelicDataByID(string relicID)
    {
        return allRelicData.TryGetValue(relicID, out var data) ? data : null;
    }

    //랜덤 유물 획득 (기존 호환)
    public List<Relic> GetRandomRelics(int count)
    {
        return GetRandomRelics(count, null);
    }

    // 획득 경로별 랜덤 유물 획득
    public List<Relic> GetRandomRelics(int count, RelicDropPool? dropPool)
    {
        if (allRelics.Count == 0 || GameManager.Instance == null) 
            return new List<Relic>();

        List<Relic> playerRelics = GameManager.Instance.activeRelics;

        // 플레이어 보유 유물 카운트
        Dictionary<string, int> playerRelicCounts = new Dictionary<string, int>();
        foreach (Relic relic in playerRelics)
        {
            if (playerRelicCounts.ContainsKey(relic.RelicID))
                playerRelicCounts[relic.RelicID]++;
            else
                playerRelicCounts[relic.RelicID] = 1;
        }

        // 사용 가능한 유물 필터링
        List<Relic> availablePool = new List<Relic>();
        foreach (Relic masterRelic in allRelics.Values)
        {
            // 해금 체크
            bool isUnlocked = masterRelic.IsUnLocked;
            if (!isUnlocked && PlayerPrefs.GetInt($"Unlock_{masterRelic.RelicID}", 0) == 1)
                isUnlocked = true;
            if (!isUnlocked) continue;

            // 획득 경로 체크 (RelicData가 있는 경우만)
            if (dropPool.HasValue && allRelicData.TryGetValue(masterRelic.RelicID, out var relicData))
            {
                if (relicData.dropPool != dropPool.Value) continue;
            }

            // 최대 보유 개수 체크
            if (masterRelic.MaxCount > 0)
            {
                playerRelicCounts.TryGetValue(masterRelic.RelicID, out int currentCount);
                if (currentCount >= masterRelic.MaxCount) continue;
            }

            availablePool.Add(masterRelic);
        }

        // 랜덤 셔플 후 반환
        return availablePool.OrderBy(x => Random.value).Take(count).ToList();
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

        // 가용 유물 중 가중치 적용
        var basePool = GetRandomRelics(count * 3, dropPool); // 여유있게 가져오기
        List<Relic> result = new List<Relic>();

        foreach (var relic in basePool)
        {
            if (result.Count >= count) break;

            if (allRelicData.TryGetValue(relic.RelicID, out var data))
            {
                float weight = weights.TryGetValue(data.rarity, out var w) ? w : 0.25f;
                if (Random.value < weight)
                    result.Add(relic);
            }
            else
            {
                // RelicData 없으면 Common 취급
                if (Random.value < weights[RelicRarity.Common])
                    result.Add(relic);
            }
        }

        // 부족하면 남은 것에서 채우기
        if (result.Count < count)
        {
            var remaining = basePool.Except(result).Take(count - result.Count);
            result.AddRange(remaining);
        }

        return result.Take(count).ToList();
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
