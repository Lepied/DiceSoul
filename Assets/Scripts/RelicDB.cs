using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RelicDB: MonoBehaviour
{
    public static RelicDB Instance { get; private set; }

    // 게임에 존재하는 모든 유물 리스트
    private List<Relic> allRelics = new List<Relic>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeRelics(); // 게임 시작 시 모든 유물 생성
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
        // --- (예시) 주사위/굴림 관련 유물 ---
        allRelics.Add(new Relic(
            "네잎클로버", 
            "최대 굴림 횟수가 1 증가합니다.", 
            RelicEffectType.AddMaxRolls,
            effectIntValue: 1
        ));

        allRelics.Add(new Relic(
            "무거운 돌", 
            "주사위를 하나 더 굴립니다. (총 6개)", 
            RelicEffectType.AddDice,
            effectIntValue: 1
        ));

        // --- (예시) 점수 관련 유물 ---
        allRelics.Add(new Relic(
            "황금 주사위", 
            "최종 점수 1.5배.", 
            RelicEffectType.AddScoreMultiplier,
            effectValue: 1.5f
        ));

        allRelics.Add(new Relic(
            "연금술사의 돌", 
            "'1'이 나온 주사위는 '7'로 취급됩니다.", 
            RelicEffectType.ModifyDiceValue
            // (이 유물은 값 대신 EffectType만으로 특별한 로직을 처리)
        ));

        // TODO: 게임에 등장할 다양한 유물 10~20개 추가
    }

    /// <summary>
    /// 모든 유물 리스트에서 랜덤하게 'count'개 만큼 뽑아서 반환합니다.
    /// (중복 없이 뽑음)
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

        // 요청한 개수(count)만큼 잘라서 반환
        return shuffledRelics.Take(count).ToList();
    }
}