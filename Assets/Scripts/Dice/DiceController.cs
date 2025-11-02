using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // List<T>

/// <summary>
/// [수정] 
/// 1. SetDiceDeck이 덱을 참조하지 않고 '새로 복사(Copy)'하도록 변경
/// 2. PrepareNewTurn이 GameManager의 덱을 비우지 않도록 playerDiceDeck.Clear() 제거
/// </summary>
public class DiceController : MonoBehaviour
{
    [Header("프리팹 및 UI 설정")]
    [Tooltip("BoxCollider2D와 DiceKeep.cs가 포함된 주사위 프리팹")]
    public GameObject dicePrefab; 

    [Tooltip("생성된 주사위들이 배치될 부모 오브젝트 (Hierarchy 정리용)")]
    public Transform diceContainer; 

    [Tooltip("UI 캔버스의 굴림 버튼")]
    public Button rollButtonUI; 

    [Header("스프라이트 에셋")]
    [Tooltip("주사위 1~6 윗면 스프라이트 (6개 할당)")]
    public Sprite[] diceFaceSprites; 

    [Header("게임 로직 설정")]
    [Tooltip("기본 최대 굴림 횟수")]
    public int baseMaxRolls = 3; 
    public int maxRolls { get; private set; } 

    [Header("배치 설정")]
    [Tooltip("주사위 사이의 가로 간격 (예: 2.0)")]
    public float horizontalSpacing = 2.0f;
    [Tooltip("주사위가 생성될 Y 위치")]
    public float yPosition = 0f;
    [Tooltip("스폰될 주사위의 크기 (기본값 0.4)")]
    public float diceScale = 0.4f;

    
    private List<SpriteRenderer> diceRenderers = new List<SpriteRenderer>();
    private List<DiceKeep> diceKeepScripts = new List<DiceKeep>();
    
    // 이 컨트롤러가 '이번 턴'에 굴릴 주사위 덱 (GameManager 덱의 '복사본')
    private List<string> playerDiceDeck = new List<string>(); 

    public int currentRollCount { get; private set; } 
    public bool isRolling { get; private set; } = false;
    public List<int> currentValues { get; private set; } = new List<int>();
    public List<bool> isKept { get; set; } = new List<bool>();

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
    
    /// <summary>
    /// [수정] GameManager로부터 덱 정보를 '복사(Copy)'하여 주사위를 스폰합니다.
    /// </summary>
    public void SetDiceDeck(List<string> deckFromGameManager)
    {
        if (dicePrefab == null)
        {
            Debug.LogError("Dice Prefab이 연결되지 않았습니다!");
            return;
        }
        
        // 1. [!!! 핵심 수정 !!!]
        // 덱을 참조(this.playerDiceDeck = deck)하는 대신,
        // '새 리스트'로 '복사'합니다.
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
                if (diceFaceSprites != null && diceFaceSprites.Length > 0)
                {
                    sr.sprite = diceFaceSprites[0]; 
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
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRollCount(currentRollCount, maxRolls);
        }
        
        StartCoroutine(RollAnimation());
    }

    /// <summary>
    /// [수정] 턴 시작 시, playerDiceDeck.Clear()를 제거합니다.
    /// </summary>
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
        
        // [!!! 핵심 수정 !!!]
        // 이 리스트를 여기서 Clear하면 GameManager의 덱을 참조할 때 문제가 됩니다.
        // 어차피 SetDiceDeck에서 새로 덮어쓰므로 Clear할 필요가 없습니다.
        // playerDiceDeck.Clear(); 

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
                    if (diceFaceSprites != null && diceFaceSprites.Length > 0)
                    {
                        int randomFace = Random.Range(0, diceFaceSprites.Length); 
                        diceRenderers[i].sprite = diceFaceSprites[randomFace];
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
                
                if (i >= currentValues.Count)
                {
                    Debug.LogError($"currentValues 인덱스 오류! i:{i}, 밸류 크기:{currentValues.Count}");
                    continue;
                }

                currentValues[i] = finalValue;
                int spriteIndex = Mathf.Clamp(finalValue - 1, 0, diceFaceSprites.Length - 1);
                
                if (diceFaceSprites.Length > spriteIndex)
                {
                    diceRenderers[i].sprite = diceFaceSprites[spriteIndex];
                }
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
    
    public void ApplyRollBonus(int amount)
    {
        maxRolls += amount;
        Debug.Log($"유물 효과 적용: 최대 굴림 +{amount}. (현재: {maxRolls})");
    }
}

