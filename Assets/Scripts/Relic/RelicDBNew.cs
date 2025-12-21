using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 유물 데이터베이스 (리팩토링 버전)
/// - ScriptableObject 기반 유물 로드
/// - 기존 Relic 클래스와 호환 유지
/// - 획득 경로(DropPool)별 필터링 지원
/// 
/// ※ 나중에 기존 RelicDB를 대체할 때:
///    1. 기존 RelicDB.cs 삭제
///    2. 이 클래스 이름을 RelicDB로 변경
/// </summary>
public class RelicDBNew : MonoBehaviour
{
    public static RelicDBNew Instance { get; private set; }

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
        // 1. ScriptableObject에서 로드
        LoadFromScriptableObjects();
        
        // 2. 기존 하드코딩 유물 로드 (ScriptableObject에 없는 것만)
        InitializeLegacyRelics();
        
        Debug.Log($"[RelicDB] 총 {allRelics.Count}개 유물 로드됨 (SO: {allRelicData.Count}개)");
    }

    /// <summary>
    /// ScriptableObject에서 유물 로드
    /// </summary>
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

    /// <summary>
    /// RelicData(SO) → Relic(기존 클래스) 변환
    /// </summary>
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

    /// <summary>
    /// 기존 하드코딩 유물 (ScriptableObject 없는 것만)
    /// TODO: 모든 유물이 SO로 이전되면 이 메서드 삭제
    /// </summary>
    private void InitializeLegacyRelics()
    {
        // --- 1. 스탯 유물 ---
        AddLegacyRelic(new Relic(
            "RLC_CLOVER", "네잎클로버", "최대 굴림 횟수가 1 증가합니다.",
            LoadRelicIcon("RLC_CLOVER"),
            RelicEffectType.AddMaxRolls,
            intValue: 1,
            maxCount: 3
        ));

        AddLegacyRelic(new Relic(
            "RLC_WHETSTONE", "숫돌", "모든 족보의 기본 데미지가 5 증가합니다.",
            LoadRelicIcon("RLC_WHETSTONE"),
            RelicEffectType.AddBaseDamage,
            intValue: 5,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_GOLD_DICE", "황금 주사위", "획득하는 점수가 1.5배가 됩니다.",
            LoadRelicIcon("RLC_GOLD_DICE"),
            RelicEffectType.AddGoldMultiplier,
            floatValue: 1.5f,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_RUSTY_GEAR", "녹슨 톱니", "모든 족보의 기본 점수가 +3 증가합니다.",
            LoadRelicIcon("RLC_RUSTY_GEAR"),
            RelicEffectType.JokboGoldAdd,
            stringValue: "ALL",
            intValue: 3,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_GLASS_CANNON", "유리 대포", "모든 족보의 기본 데미지가 +10 증가하지만, 최대 체력이 2 감소합니다.",
            LoadRelicIcon("RLC_GLASS_CANNON"),
            RelicEffectType.AddBaseDamage,
            intValue: 10,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_PLUTOCRACY", "금권 정치", "현재 보유한 '점수' 100점당 모든 족보의 기본 데미지가 +1 증가합니다. (최대 +50)",
            LoadRelicIcon("RLC_PLUTOCRACY"),
            RelicEffectType.DynamicDamage_Gold,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_BLOODLUST", "피의 갈증", "플레이어의 잃은 체력 1당 모든 족보의 기본 데미지가 +1 증가합니다.",
            LoadRelicIcon("RLC_BLOODLUST"),
            RelicEffectType.DynamicDamage_LostHealth,
            maxCount: 1
        ));

        // --- 2. 덱 변경 유물 ---
        AddLegacyRelic(new Relic(
            "RLC_EXTRA_DICE", "여분의 주사위", "덱에 'D6' 주사위를 영구적으로 1개 추가합니다.",
            LoadRelicIcon("RLC_EXTRA_DICE"),
            RelicEffectType.AddDice,
            stringValue: "D6",
            maxCount: 3
        ));

        AddLegacyRelic(new Relic(
            "RLC_TINY_DICE", "작은 주사위", "덱에 'D4' 주사위를 영구적으로 1개 추가합니다.",
            LoadRelicIcon("RLC_TINY_DICE"),
            RelicEffectType.AddDice,
            stringValue: "D4",
            maxCount: 3
        ));

        AddLegacyRelic(new Relic(
            "RLC_FOCUS", "집중", "모든 족보의 기본 데미지 +10. 획득 시 덱에서 D6 주사위 1개를 '영구히' 제거합니다.",
            LoadRelicIcon("RLC_FOCUS"),
            RelicEffectType.AddBaseDamage,
            intValue: 10,
            maxCount: 1
        ));

        // --- 3. 주사위 값 변경 유물 ---
        AddLegacyRelic(new Relic(
            "RLC_ALCHEMY", "연금술사의 돌", "'1'이 나온 주사위는 '7'로 취급됩니다.",
            LoadRelicIcon("RLC_ALCHEMY"),
            RelicEffectType.ModifyDiceValue,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_LODESTONE", "자철석", "주사위를 굴린 후, 홀수가 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_LODESTONE"),
            RelicEffectType.RerollOdds,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_TANZANITE", "탄자나이트", "주사위를 굴린 후, 짝수가 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_TANZANITE"),
            RelicEffectType.RerollEvens,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_FEATHER", "가벼운 깃털", "주사위를 굴린 후, '6'이 나온 주사위를 한 번 다시 굴립니다.",
            LoadRelicIcon("RLC_FEATHER"),
            RelicEffectType.RerollSixes,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_COUNTERWEIGHT", "균형추", "주사위를 굴린 후, 'D20' 주사위 값이 10 이하면 +10을 더합니다.",
            LoadRelicIcon("RLC_COUNTERWEIGHT"),
            RelicEffectType.BonusOnLowD20,
            maxCount: 1
        ));

        // --- 4. 족보 강화 유물 ---
        AddLegacyRelic(new Relic(
            "RLC_PERFECTIONIST", "완벽주의자", "'스트레이트 (4연속)'을 비활성화하고, '스트레이트 (5연속)'의 데미지를 +50 증가시킵니다.",
            LoadRelicIcon("RLC_PERFECTIONIST"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "스트레이트 (5연속)",
            intValue: 50,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_SWORD_BOOK", "검술 교본", "모든 '스트레이트' 족보의 기본 데미지가 30 증가합니다.",
            LoadRelicIcon("RLC_SWORD_BOOK"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "스트레이트",
            intValue: 30,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_SPIKE_GLOVE", "가시 돋친 장갑", "모든 '투 페어' 족보의 기본 데미지가 +15 증가합니다.",
            LoadRelicIcon("RLC_SPIKE_GLOVE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "투 페어",
            intValue: 15,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_FOUNDATION", "기틀", "'총합' 족보의 기본 데미지가 10 증가합니다.",
            LoadRelicIcon("RLC_FOUNDATION"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "총합",
            intValue: 10,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_WHITEBOOK", "백마법서", "모든 '모두 짝수' 족보의 획득 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_WHITEBOOK"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "모두 짝수",
            floatValue: 2.0f,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_DARKBOOK", "흑마법서", "모든 '모두 홀수' 족보의 획득 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_DARKBOOK"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "모두 홀수",
            floatValue: 2.0f,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_GEM_CROWN", "보석 박힌 왕관", "'야찌' 족보의 획득 점수가 3배가 됩니다.",
            LoadRelicIcon("RLC_GEM_CROWN"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "야찌",
            floatValue: 3.0f,
            maxCount: 1
        ));

        AddLegacyRelic(new Relic(
            "RLC_SQUARE_PEG", "네모난 못", "'포카드' 족보의 기본 데미지가 +40 증가합니다.",
            LoadRelicIcon("RLC_SQUARE_PEG"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "포카드",
            intValue: 40,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_TRIPOD", "삼각대", "'트리플' 족보의 획득 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_TRIPOD"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "트리플",
            floatValue: 2.0f,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_HOUSE", "집 모형", "'풀 하우스' 족보의 기본 데미지가 +25 증가합니다.",
            LoadRelicIcon("RLC_HOUSE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "풀 하우스",
            intValue: 25,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_ROYAL_SEAL", "왕실의 인장", "'풀 하우스' 족보의 획득 점수가 3배가 됩니다.",
            LoadRelicIcon("RLC_ROYAL_SEAL"),
            RelicEffectType.JokboGoldMultiplier,
            stringValue: "풀 하우스",
            floatValue: 3.0f,
            maxCount: 0
        ));

        AddLegacyRelic(new Relic(
            "RLC_HEAVY_DICE", "무거운 주사위", "모든 '총합' 족보의 데미지 +30. 대신 최대 굴림 횟수 -1.",
            LoadRelicIcon("RLC_HEAVY_DICE"),
            RelicEffectType.JokboDamageAdd,
            stringValue: "총합",
            intValue: 30,
            maxCount: 1
        ));

        // --- 5. 특수 유물 ---
        AddLegacyRelic(new Relic(
            "RLC_BUSINESS_CARD", "명함", "첫 번째 굴림으로 공격할 경우, 해당 족보의 데미지와 점수가 2배가 됩니다.",
            LoadRelicIcon("RLC_BUSINESS_CARD"),
            RelicEffectType.RollCountBonus,
            floatValue: 2.0f,
            maxCount: 1
        ));
    }

    /// <summary>
    /// ScriptableObject에 없는 유물만 추가
    /// </summary>
    private void AddLegacyRelic(Relic relic)
    {
        if (!allRelics.ContainsKey(relic.RelicID))
        {
            allRelics[relic.RelicID] = relic;
        }
    }

    private Sprite LoadRelicIcon(string relicID)
    {
        return Resources.Load<Sprite>("RelicIcons/" + relicID);
    }

    // ===== 조회 메서드 =====

    /// <summary>
    /// ID로 유물 조회 (기존 호환)
    /// </summary>
    public Relic GetRelicByID(string relicID)
    {
        return allRelics.TryGetValue(relicID, out var relic) ? relic : null;
    }

    /// <summary>
    /// ID로 RelicData 조회
    /// </summary>
    public RelicData GetRelicDataByID(string relicID)
    {
        return allRelicData.TryGetValue(relicID, out var data) ? data : null;
    }

    /// <summary>
    /// 랜덤 유물 획득 (기존 호환)
    /// </summary>
    public List<Relic> GetRandomRelics(int count)
    {
        return GetRandomRelics(count, null);
    }

    /// <summary>
    /// 획득 경로별 랜덤 유물 획득
    /// </summary>
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

    /// <summary>
    /// 등급별 가중치를 적용한 랜덤 유물 획득
    /// </summary>
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

    /// <summary>
    /// 모든 유물 목록 (디버그용)
    /// </summary>
    public List<Relic> GetAllRelics()
    {
        return allRelics.Values.ToList();
    }

    /// <summary>
    /// 특정 획득 경로의 모든 유물
    /// </summary>
    public List<RelicData> GetRelicsByDropPool(RelicDropPool dropPool)
    {
        return allRelicData.Values.Where(r => r.dropPool == dropPool).ToList();
    }
}
