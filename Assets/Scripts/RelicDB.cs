using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Find()

/// <summary>
/// 게임에 존재하는 모든 'Relic'을 생성하고 관리하는 데이터베이스입니다.
/// 씬(Scene)에 빈 GameObject를 만들고 이 스크립트를 붙여야 합니다.
/// </summary>
public class RelicDB : MonoBehaviour
{
    public static RelicDB Instance { get; private set; }

    // 게임에 존재하는 모든 유물 원본
    private List<Relic> allRelics = new List<Relic>();
    // (성능을 위해 ID로 빠르게 찾을 수 있게 딕셔너리 사용)
    private Dictionary<string, Relic> relicDictionary = new Dictionary<string, Relic>();

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

    /// <summary>
    /// 여기에 모든 유물을 코드로 정의합니다.
    /// </summary>
    private void InitializeRelics()
    {
        // --- (예시) 굴림/주사위 관련 유물 ---
        AddRelicToDB(new Relic(
            "RELIC_CLOVER", // ID
            "네잎클로버", 
            "최대 굴림 횟수가 1 증가합니다.", 
            RelicEffectType.AddMaxRolls,
            intValue: 1
        ));

        // --- (예시) 점수 관련 유물 ---
        AddRelicToDB(new Relic(
            "RELIC_GOLD_DICE", // ID
            "황금 주사위", 
            "획득하는 모든 점수에 1.5배의 배율이 적용됩니다.", 
            RelicEffectType.AddScoreMultiplier,
            floatValue: 1.5f
        ));
        
        // --- (예시) 데미지 관련 유물 ---
        AddRelicToDB(new Relic(
            "RELIC_WHETSTONE", // ID
            "숫돌", 
            "모든 족보의 기본 데미지가 5 증가합니다.", 
            RelicEffectType.AddBaseDamage,
            intValue: 5
        ));

        // (TODO: 게임에 등장할 다양한 유물 10~20개 추가)
    }

    // 데이터베이스에 유물을 추가하는 헬퍼 함수
    private void AddRelicToDB(Relic relic)
    {
        allRelics.Add(relic);
        relicDictionary[relic.ID] = relic;
    }

    /// <summary>
    /// ID를 기반으로 유물 원본을 찾아 반환합니다.
    /// </summary>
    public Relic GetRelic(string id)
    {
        if (relicDictionary.ContainsKey(id))
        {
            return relicDictionary[id];
        }
        Debug.LogError($"[RelicDatabase] ID가 '{id}'인 Relic을 찾을 수 없습니다!");
        return null;
    }

    /// <summary>
    /// 모든 유물 리스트에서 랜덤하게 'count'개 만큼 뽑아서 반환합니다.
    /// </summary>
    public List<Relic> GetRandomRelics(int count)
    {
        if (allRelics.Count == 0)
        {
            Debug.LogError("RelicDatabase에 유물이 없습니다!");
            return new List<Relic>();
        }

        // 중복 없이 뽑기 위해 리스트를 복사하고 섞음
        List<Relic> shuffledRelics = allRelics.OrderBy(x => Random.value).ToList();
        return shuffledRelics.Take(count).ToList();
    }
}
