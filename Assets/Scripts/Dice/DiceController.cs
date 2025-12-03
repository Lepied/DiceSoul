using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // List<T>
using System.Linq;
using DG.Tweening;
using UnityEditor.SettingsManagement;

public class DiceController : MonoBehaviour
{
    public static DiceController Instance { get; private set; }

    [Header("설정")]
    public GameObject dicePrefab;
    public Transform diceContainer;
    public Button rollButtonUI;
    public AudioClip rollSound;

    [Header("게임 로직 설정")]
    public int baseMaxRolls = 3;

    [Header("배치 설정")]
    public float horizontalSpacing = 2.0f;
    public float yPosition = 0f;
    public float diceScale = 0.4f;

    [Header("데이터")]

    public List<DiceSpriteData> diceSpriteSettings;

    public List<Dice> activeDice = new List<Dice>();

    //딕셔너리 DB들
    private Dictionary<string, Dictionary<int, Sprite>> diceLookupMap = new Dictionary<string, Dictionary<int, Sprite>>(); // 딕셔너리
    private Dictionary<string, List<Sprite>> diceAnimationList = new Dictionary<string, List<Sprite>>(); //주사위 애니메이션용

    [System.Serializable]
    public class DiceSpriteData
    {
        public string diceType;
        public List<DiceFacePair> faces;
    }
    [System.Serializable]
    public struct DiceFacePair
    {
        public int value;    // 예: 1, 2, 3... 또는 7(마법), 10(보정)
        public Sprite sprite; // 그 값에 해당하는 이미지
    }

    //상태
    public int maxRolls { get; private set; }
    public int currentRollCount { get; private set; }
    public bool isRolling { get; private set; } = false;
    public List<int> currentValues 
    {
        get 
        {
            if (activeDice == null) return new List<int>();
            return activeDice.Select(d => d.Value).ToList();
        }
    }
    public List<bool> isKept { get; set; } = new List<bool>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InitializeDiceSpriteDict();
    }

    void Start()
    {
        if (rollButtonUI != null)
        {
            rollButtonUI.onClick.AddListener(OnRollButton);
            maxRolls = baseMaxRolls;
        }
        else
        {
            Debug.LogError("Roll Button UI가 DiceController에 연결되지 않았습니다!");
        }
    }

    private void InitializeDiceSpriteDict()
    {
        diceLookupMap.Clear();
        diceAnimationList.Clear();
        foreach (var data in diceSpriteSettings)
        {
            Dictionary<int, Sprite> valueMap = new Dictionary<int, Sprite>(); //검색용 맵
            List<Sprite> spriteList = new List<Sprite>();

            int maxSide = GetMaxSideFromType(data.diceType);

            foreach (var pair in data.faces)
            {
                //검색용 맵에는 모든 이미지 등록시키기
                if (!valueMap.ContainsKey(pair.value))
                {
                    valueMap[pair.value] = pair.sprite;
                }
                // 애니메이션 용 리스트에는 최대값 이하만 등록시키기
                if (pair.value <= maxSide)
                {
                    spriteList.Add(pair.sprite);
                }
            }
            if (!diceLookupMap.ContainsKey(data.diceType))
            {
                diceLookupMap.Add(data.diceType, valueMap);
                diceAnimationList.Add(data.diceType, spriteList);
            }
        }
    }
    //다이스타입에서 최대값뽑는용
    private int GetMaxSideFromType(string diceType)
    {
        // "D"를 떼고 나머지 숫자를 파싱
        if (diceType.StartsWith("D") && int.TryParse(diceType.Substring(1), out int result))
        {
            return result;
        }
        return 6; //기본값 (D6)
    }

    public Sprite GetDiceSprite(string diceType, int value)
    {
        if (diceLookupMap.TryGetValue(diceType, out var map) && map.TryGetValue(value, out var sprite))
        {
            return sprite;
        }
        Debug.LogWarning($"스프라이트 누락: 타입 {diceType}의 값 {value}에 대한 이미지가 없습니다!");
        return null;
    }
    public Sprite GetRandomAnimationSprite(string type)
    {
        if (diceAnimationList.TryGetValue(type, out var list) && list.Count > 0)
            return list[Random.Range(0, list.Count)];
        return null;
    }

    public void SetDiceDeck(List<string> deck)
    {
        // 기존 삭제
        foreach (Transform child in diceContainer) Destroy(child.gameObject);
        activeDice.Clear();

        float startX = -((deck.Count - 1) * horizontalSpacing) / 2.0f;

        for (int i = 0; i < deck.Count; i++)
        {
            GameObject go = Instantiate(dicePrefab, diceContainer);
            go.transform.localPosition = new Vector3(startX + (i * horizontalSpacing), 0, 0);
            go.transform.localScale = Vector3.one * diceScale;

            // Dice 컴포넌트 초기화
            Dice dice = go.GetComponent<Dice>();
            if (dice == null) dice = go.AddComponent<Dice>(); // 없으면 붙여줌

            dice.Initialize(deck[i]); //주사위 타입 설정
            activeDice.Add(dice);
        }

        if (UIManager.Instance != null) UIManager.Instance.UpdateRollCount(currentRollCount, maxRolls);
    }

    private void OnRollButton()
    {
        if (isRolling) return;
        if (currentRollCount >= maxRolls) return;

        currentRollCount++;

        if (SoundManager.Instance != null && rollSound != null)
        {
            SoundManager.Instance.PlayRandomPitchSFX(rollSound);
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRollCount(currentRollCount, maxRolls);
        }

        StartCoroutine(RollSequence());
    }
    private IEnumerator RollSequence()
    {
        isRolling = true;
        SetRollButtonInteractable(false);

        float duration = 0.5f;
        float timer = 0f;

        //주사위 굴리는 애니메이션연출
        while (timer < duration)
        {
            foreach (var dice in activeDice)
            {
                dice.ShowRandomFace();
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 결과
        foreach (var dice in activeDice)
        {
            if (!dice.IsKept)
            {
                int maxSide = GetMaxSideFromType(dice.Type);
                int result = Random.Range(1, maxSide + 1);
                dice.UpdateVisual(result);
            }
        }

        isRolling = false;

        if (StageManager.Instance != null)
        {
            // Dice 리스트에서 값만 뽑아서 전달
            string debugValues = string.Join(", ", currentValues);
            Debug.Log($"[DiceController] 굴림 완료! StageManager로 보내는 값: {debugValues}");
            StageManager.Instance.OnRollFinished(currentValues);
            
        }
    }
    public void PrepareNewTurn()
    {
        currentRollCount = 0;
        maxRolls = baseMaxRolls;
        isRolling = false;
        SetRollButtonInteractable(true);
        // 주사위들은 다음 SetDiceDeck때 파괴되고 재생성됨
    }

    public void SetRollButtonInteractable(bool interactable)
    {
        if (rollButtonUI != null)
        {
            rollButtonUI.interactable = interactable;
        }
    }

    public void UpdateSingleDiceVisual(int index, int newValue)
    {
        if (index >= 0 && index < activeDice.Count)
        {
            activeDice[index].UpdateVisual(newValue);
        }
    }

    public void ApplyRollBonus(int amount)
    {
        maxRolls += amount;
        Debug.Log($"유물 효과 적용: 최대 굴림 +{amount}. (현재: {maxRolls})");
    }

    public void ForceKeepRandomDice()
    {
        var unkept = activeDice.Where(d => !d.IsKept).ToList();
        if (unkept.Count > 0)
        {
            Dice target = unkept[Random.Range(0, unkept.Count)];
            target.SetKeep(true);
            Debug.Log("강제 킵!");
        }
    }

    //유물 연출(뾰로롱)
    public void PlayMagicChangeVisual(int index, int newValue) // (StageManager가 호출하기 쉽게 void나 Coroutine으로)
    {
        if (index >= 0 && index < activeDice.Count)
        {
            activeDice[index].PlayMagicAnimation(newValue);
        }
    }

    //유물 연출 (휘리릭)
    public void PlayRerollVisual(int index, int newValue)
    {
        if (index >= 0 && index < activeDice.Count)
        {
            activeDice[index].PlayRerollAnimation(newValue);
        }
    }
}

