using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // List<T>
using System.Linq;
using DG.Tweening;
using UnityEditor.SettingsManagement;
using UnityEngine.InputSystem;

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

#if UNITY_EDITOR
    void Update()
    {
        if (Keyboard.current == null) return;
        
        // F5: 야찌
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            for (int i = 0; i < activeDice.Count && i < 5; i++)
            {
                activeDice[i].UpdateVisual(6);
            }
            StageManager.Instance?.OnRollFinished(currentValues, false);
        }
        
        // F6: 포카드
        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            for (int i = 0; i < activeDice.Count && i < 4; i++)
            {
                activeDice[i].UpdateVisual(5);
            }
            if (activeDice.Count >= 5) activeDice[4].UpdateVisual(2);
            StageManager.Instance?.OnRollFinished(currentValues, false);
        }
        
        // F7: 풀하우스
        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            for (int i = 0; i < 3 && i < activeDice.Count; i++)
            {
                activeDice[i].UpdateVisual(4);
            }
            for (int i = 3; i < 5 && i < activeDice.Count; i++)
            {
                activeDice[i].UpdateVisual(3);
            }
            StageManager.Instance?.OnRollFinished(currentValues, false);
        }
        
        // F8: 스트레이트
        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            for (int i = 0; i < activeDice.Count && i < 5; i++)
            {
                activeDice[i].UpdateVisual(i + 1); // 1,2,3,4,5
            }
            StageManager.Instance?.OnRollFinished(currentValues, false);
        }
    }
#endif

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
        if (diceContainer != null) diceContainer.gameObject.SetActive(true);

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
        
        // 굴림 횟수가 maxRolls에 도달했을 때 날쌘 손놀림 체크
        if (currentRollCount >= maxRolls)
        {
            if (RelicEffectHandler.Instance != null && GameManager.Instance != null)
            {
                bool freeRollGranted = RelicEffectHandler.Instance.CheckFreeRollAtZero(GameManager.Instance.CurrentWave);
                if (freeRollGranted)
                {
                    // 무료 굴림있음
                    Debug.Log("[날쌘 손놀림] 무료 굴림");
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        // 리롤 해도 실드 제거
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearShield();
        }

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

        //이벤트 시스템: 주사위 굴림 완료 이벤트 발생
        RollContext rollCtx = new RollContext
        {
            DiceValues = activeDice.Select(d => d.Value).ToArray(),
            DiceTypes = activeDice.Select(d => d.Type).ToArray(),
            IsFirstRoll = (currentRollCount == 1),
            RerollIndices = new List<int>()
        };
        GameEvents.RaiseDiceRolled(rollCtx);

        // ★ 유물이 주사위 값을 변환했으면 UI에 반영
        for (int i = 0; i < activeDice.Count && i < rollCtx.DiceValues.Length; i++)
        {
            if (activeDice[i].Value != rollCtx.DiceValues[i])
            {
                // 값이 바뀌었으면 마법 연출로 업데이트
                activeDice[i].PlayMagicAnimation(rollCtx.DiceValues[i]);
            }
        }

        // ★ 유물이 재굴림을 요청했으면 처리
        if (rollCtx.RerollIndices != null && rollCtx.RerollIndices.Count > 0)
        {
            yield return new WaitForSeconds(0.3f); // 연출 대기
            
            foreach (int idx in rollCtx.RerollIndices.Distinct())
            {
                if (idx >= 0 && idx < activeDice.Count && !activeDice[idx].IsKept)
                {
                    int maxSide = GetMaxSideFromType(activeDice[idx].Type);
                    int newValue = Random.Range(1, maxSide + 1);
                    activeDice[idx].PlayRerollAnimation(newValue);
                }
            }
            
            yield return new WaitForSeconds(0.4f); // 재굴림 연출 대기
            
            // 재굴림 후 이벤트 (값 변환 유물이 다시 적용될 수 있음)
            rollCtx.DiceValues = activeDice.Select(d => d.Value).ToArray();
            GameEvents.RaiseDiceRerolled(rollCtx);
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
        
        // 유물 효과로 굴림 횟수 보너스 재적용
        if (GameManager.Instance != null)
        {
            foreach (var relic in GameManager.Instance.activeRelics)
            {
                if (relic.EffectType == RelicEffectType.AddMaxRolls || 
                    relic.EffectType == RelicEffectType.ModifyMaxRolls)
                {
                    maxRolls += relic.IntValue;
                }
            }
        }
        
        isRolling = false;
        SetRollButtonInteractable(true);
        
        //이벤트 시스템: 턴 시작 이벤트
        GameEvents.RaiseTurnStart();
        
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

    public void HideAllDice()
    {
        // 주사위 숨기기
        if (diceContainer != null)
        {
            diceContainer.gameObject.SetActive(false);
        }
        
    }

    // <summary>
    // 사용한 주사위를 인덱스 기반으로 제거 (연쇄 공격 시스템용)
    /// <param name="indices">제거할 주사위 인덱스 리스트</param>
    public void RemoveDiceByIndices(List<int> indices)
    {
        if (indices == null || indices.Count == 0 || activeDice.Count == 0) return;

        // 인덱스 정렬 (큰 것부터 제거해야 인덱스 안전)
        var sortedIndices = indices.OrderByDescending(i => i).ToList();

        foreach (int index in sortedIndices)
        {
            if (index >= 0 && index < activeDice.Count)
            {
                Dice diceToRemove = activeDice[index];
                activeDice.RemoveAt(index);
                
                // 페이드 아웃 애니메이션
                if (diceToRemove != null && diceToRemove.gameObject != null)
                {
                    // 기존 DOTween 중단 (중요!)
                    diceToRemove.transform.DOKill();
                    
                    diceToRemove.transform.DOScale(0f, 0.3f).OnComplete(() =>
                    {
                        if (diceToRemove != null && diceToRemove.gameObject != null)
                        {
                            Destroy(diceToRemove.gameObject);
                        }
                    });
                }
            }
        }

        // 남은 주사위 위치 재조정
        RepositionRemainingDice();
    }

    // 남은 주사위들의 위치를 가운데로 재정렬
    private void RepositionRemainingDice()
    {
        if (activeDice.Count == 0) return;

        float startX = -((activeDice.Count - 1) * horizontalSpacing) / 2.0f;

        for (int i = 0; i < activeDice.Count; i++)
        {
            Vector3 targetPos = new Vector3(startX + (i * horizontalSpacing), 0, 0);
            activeDice[i].transform.DOLocalMove(targetPos, 0.4f).SetEase(Ease.OutQuad);
        }
    }

    // 남은 주사위 개수 반환
    public int GetRemainingDiceCount()
    {
        return activeDice.Count;
    }
    // 주사위 타입 목록 가져오기
    public List<string> GetDiceTypes()
    {
        return activeDice.Select(d => d.Type).ToList();
    }

    /// <summary>
    /// 특정 인덱스의 주사위 위치 가져오기 (VFX 시작 위치용)
    /// </summary>
    public Vector3 GetDicePosition(int index)
    {
        if (index >= 0 && index < activeDice.Count)
        {
            return activeDice[index].transform.position;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 여러 인덱스의 주사위 위치들 가져오기
    /// </summary>
    public Vector3[] GetDicePositions(List<int> indices)
    {
        if (indices == null || indices.Count == 0)
            return new Vector3[0];

        List<Vector3> positions = new List<Vector3>();
        foreach (int index in indices)
        {
            if (index >= 0 && index < activeDice.Count)
            {
                positions.Add(activeDice[index].transform.position);
            }
        }
        return positions.ToArray();
    }

    /// <summary>
    /// 모든 주사위 위치 가져오기
    /// </summary>
    public Vector3[] GetAllDicePositions()
    {
        return activeDice.Select(d => d.transform.position).ToArray();
    }
    
    //이중 주사위 유물 관련
    private bool isDoubleDiceSelectionMode = false;
    
    public void StartDoubleDiceSelectionMode()
    {
        isDoubleDiceSelectionMode = true;
        Debug.Log("[DiceController] 이중 주사위 모드 활성화 - 주사위를 클릭하세요");
        
        // TODO: 선택 가능 표시추가하기
    }
    
    public bool TryUseDoubleDiceOn(int diceIndex)
    {
        if (!isDoubleDiceSelectionMode) return false;
        
        OnDiceClickedForDoubleDice(diceIndex);
        return true;
    }
    
    private void OnDiceClickedForDoubleDice(int diceIndex)
    {
        if (!isDoubleDiceSelectionMode) return;
        
        if (diceIndex < 0 || diceIndex >= activeDice.Count)
        {
            Debug.LogWarning($"[DiceController] 잘못된 주사위 인덱스: {diceIndex}");
            return;
        }
        
        Dice selectedDice = activeDice[diceIndex];
        int currentValue = selectedDice.Value;
        
        // RelicEffectHandler를 통해 이중 주사위 사용
        if (RelicEffectHandler.Instance != null)
        {
            int newValue = RelicEffectHandler.Instance.UseDoubleDice(diceIndex, currentValue);
            
            if (newValue > 0)
            {
                // 주사위 값 업데이트
                selectedDice.UpdateVisual(newValue);
                Debug.Log($"[DiceController] 주사위[{diceIndex}] {currentValue} → {newValue}");
                
                // 족보 프리뷰 업데이트
                if (StageManager.Instance != null)
                {
                    StageManager.Instance.OnRollFinished(currentValues, false);
                }
                
                // 유물 패널 업데이트 (회색 처리)
                if (UIManager.Instance != null && GameManager.Instance != null)
                {
                    UIManager.Instance.UpdateRelicPanel(GameManager.Instance.activeRelics);
                }
            }
        }
        
        isDoubleDiceSelectionMode = false;
    }
    
    // 운명의 주사위 유물 관련
    public void ApplyFateDiceValues(int[] newValues)
    {
        if (newValues.Length != activeDice.Count)
        {
            Debug.LogWarning($"[DiceController] 주사위 개수 불일치: {newValues.Length} vs {activeDice.Count}");
            return;
        }
        
        // 모든 주사위 값 업데이트
        for (int i = 0; i < activeDice.Count; i++)
        {
            activeDice[i].UpdateVisual(newValues[i]);
        }
        
        Debug.Log("[DiceController] 운명의 주사위 적용 완료!");
        
        // 족보 프리뷰 업데이트
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnRollFinished(currentValues, false);
        }
    }
}
