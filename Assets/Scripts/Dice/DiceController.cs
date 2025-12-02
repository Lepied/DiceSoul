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

    [Header("프리팹 및 UI 설정")]
    [Tooltip("BoxCollider2D와 DiceKeep.cs가 포함된 주사위 프리팹")]
    public GameObject dicePrefab;

    [Tooltip("생성된 주사위들이 배치될 부모 오브젝트 (Hierarchy 정리용)")]
    public Transform diceContainer;

    [Tooltip("UI 캔버스의 굴림 버튼")]
    public Button rollButtonUI;

    [Header("스프라이트 설정")]

    public List<DiceSpriteData> diceSpriteSettings;
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

    [Header("사운드 설정")]
    public AudioClip rollSound;

    [Header("게임 로직 설정")]
    [Tooltip("기본 최대 굴림 횟수")]
    public int baseMaxRolls = 3;
    public int maxRolls { get; private set; }

    [Header("배치 설정")]
    public float horizontalSpacing = 2.0f;
    public float yPosition = 0f;
    [Tooltip("스폰될 주사위의 크기 (기본값 0.4)")]
    public float diceScale = 0.4f;


    private List<SpriteRenderer> diceRenderers = new List<SpriteRenderer>();
    private List<DiceKeep> diceKeepScripts = new List<DiceKeep>();
    private List<string> playerDiceDeck = new List<string>();

    public int currentRollCount { get; private set; }
    public bool isRolling { get; private set; } = false;
    public List<int> currentValues { get; private set; } = new List<int>();
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
                if(pair.value <= maxSide)
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
        if (diceLookupMap.TryGetValue(diceType, out Dictionary<int, Sprite> valueMap))
        {
            if (valueMap.TryGetValue(value, out Sprite s))
            {
                return s;
            }

        }
        Debug.LogWarning($"스프라이트 누락: 타입 {diceType}의 값 {value}에 대한 이미지가 없습니다!");
        return null;
    }

    public void SetDiceDeck(List<string> deckFromGameManager)
    {
        if (dicePrefab == null)
        {
            Debug.LogError("Dice Prefab이 연결되지 않았습니다!");
            return;
        }

        // 1. 덱 정보 복사
        this.playerDiceDeck = new List<string>(deckFromGameManager);

        currentValues = new List<int>(new int[playerDiceDeck.Count]);
        isKept = new List<bool>(new bool[playerDiceDeck.Count]);

        // 2. 스폰 위치 계산
        float totalWidth = (playerDiceDeck.Count - 1) * horizontalSpacing;
        float startX = -totalWidth / 2.0f;

        // 3. 새 주사위 스폰
        for (int i = 0; i < playerDiceDeck.Count; i++)
        {
            float posX = startX + (i * horizontalSpacing);
            Vector3 spawnLocalPosition = new Vector3(posX, yPosition, 0);

            GameObject diceGO = WaveGenerator.Instance.SpawnFromPool(dicePrefab, Vector3.zero, Quaternion.identity);

            diceGO.transform.SetParent(diceContainer);
            diceGO.transform.localPosition = spawnLocalPosition;
            diceGO.transform.localScale = new Vector3(diceScale, diceScale, diceScale);

            SpriteRenderer sr = diceGO.GetComponent<SpriteRenderer>();
            DiceKeep keepScript = diceGO.GetComponent<DiceKeep>();

            if (sr != null && keepScript != null)
            {
                string myType = playerDiceDeck[i];
                Sprite initailSprite = GetDiceSprite(myType, 1); //초기값

                if (initailSprite != null)
                {
                    sr.sprite = initailSprite;
                }

                keepScript.Initialize(this, i);
                keepScript.UpdateVisual(false);

                diceRenderers.Add(sr);
                diceKeepScripts.Add(keepScript);
            }
            else
            {
                Debug.LogError("Dice Prefab에 SpriteRenderer나 DiceKeep 스크립트가 없습니다!");
            }
        }

        // 4. 새 덱 개수로 UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRollCount(currentRollCount, maxRolls);
        }
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

        StartCoroutine(RollAnimation());
    }

    public void PrepareNewTurn()
    {
        // 1. 기존 주사위 오브젝트 풀로 반환
        foreach (SpriteRenderer renderer in diceRenderers)
        {
            if (renderer != null)
            {
                WaveGenerator.Instance.ReturnToPool(renderer.gameObject);
            }
        }

        // 2. 모든 리스트 초기화
        diceRenderers.Clear();
        diceKeepScripts.Clear();
        currentValues.Clear();
        isKept.Clear();
        playerDiceDeck.Clear();

        // 3. 턴 상태 초기화
        currentRollCount = 0;
        isRolling = false;
        SetRollButtonInteractable(true);
        maxRolls = baseMaxRolls;
    }

    private IEnumerator RollAnimation()
    {
        isRolling = true;
        SetRollButtonInteractable(false);

        float rollDuration = 0.5f;
        float timer = 0f;

        while (timer < rollDuration)
        {
            for (int i = 0; i < diceRenderers.Count; i++)
            {
                if (!isKept[i])
                {
                    string myType = playerDiceDeck[i];

                    if (diceAnimationList.TryGetValue(myType, out List<Sprite> animSprites) && animSprites.Count > 0)
                    {
                        diceRenderers[i].sprite = animSprites[Random.Range(0, animSprites.Count)];
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < diceRenderers.Count; i++)
        {
            if (!isKept[i])
            {
                if (i >= playerDiceDeck.Count)
                {
                    Debug.LogError($"playerDiceDeck 인덱스 오류! i:{i}, 덱 크기:{playerDiceDeck.Count}");
                    continue;
                }

                string diceType = playerDiceDeck[i];
                int finalValue;

                switch (diceType)
                {
                    case "D4":
                        finalValue = Random.Range(1, 5); // 1~4
                        break;
                    case "D8":
                        finalValue = Random.Range(1, 9); // 1~8
                        break;
                    case "D20":
                        finalValue = Random.Range(1, 21); // 1~20
                        break;
                    case "D6":
                    default:
                        finalValue = Random.Range(1, 7); // 1~6
                        break;
                }

                UpdateSingleDiceVisual(i, finalValue);
            }
        }

        isRolling = false;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnRollFinished(currentValues);
        }
        else
        {
            Debug.LogError("StageManager.Instance가 없습니다!");
        }
    }

    public void SetRollButtonInteractable(bool interactable)
    {
        if (rollButtonUI != null)
        {
            rollButtonUI.interactable = interactable;
        }
    }

    public void UpdateSingleDiceVisual(int index, int value)
    {
        if (index < 0 || index >= diceRenderers.Count) return;

        string diceType = playerDiceDeck[index];
        Sprite s = GetDiceSprite(diceType, value);

        if (s != null)
        {
            diceRenderers[index].sprite = s;
            currentValues[index] = value;
        }
    }

    public void ApplyRollBonus(int amount)
    {
        maxRolls += amount;
        Debug.Log($"유물 효과 적용: 최대 굴림 +{amount}. (현재: {maxRolls})");
    }

    public void ForceKeepRandomDice()
    {
        // 1. 킵(Keep)이 '가능한' 주사위 인덱스 리스트를 찾습니다.
        List<int> unkeptIndices = new List<int>();
        for (int i = 0; i < isKept.Count; i++)
        {
            if (!isKept[i])
            {
                unkeptIndices.Add(i);
            }
        }

        // 2. 킵 가능한 주사위가 있으면, 그 중 하나를 랜덤 선택
        if (unkeptIndices.Count > 0)
        {
            int randomIndex = unkeptIndices[Random.Range(0, unkeptIndices.Count)];

            // 3. 강제 킵
            isKept[randomIndex] = true;

            // 4. 시각 효과 업데이트
            if (randomIndex < diceKeepScripts.Count && diceKeepScripts[randomIndex] != null)
            {
                diceKeepScripts[randomIndex].UpdateVisual(true);
                Debug.Log($"슬라임 기믹: {randomIndex}번 주사위 강제 킵!");
            }
        }
        else
        {
            Debug.Log("슬라임 기믹: 모든 주사위가 이미 킵되어 있습니다.");
        }
    }
}

