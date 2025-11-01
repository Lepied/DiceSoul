using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// [수정] Instantiate/Destroy 대신 WaveGenerator의 오브젝트 풀링을 사용합니다.
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
    
    // '현재' 최대 굴림 횟수 (유물 효과 적용됨)
    public int maxRolls { get; private set; } 

    [Header("배치 설정")]
    [Tooltip("주사위 사이의 가로 간격 (예: 2.0)")]
    public float horizontalSpacing = 2.0f;
    [Tooltip("주사위가 생성될 Y 위치")]
    public float yPosition = 0f;

    
    private List<SpriteRenderer> diceRenderers = new List<SpriteRenderer>();
    private List<DiceKeep> diceKeepScripts = new List<DiceKeep>();

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
    /// [새 함수] GameManager로부터 덱 정보를 받아 주사위를 스폰합니다.
    /// </summary>
    public void SetDiceDeck(List<string> deck)
    {
        if (dicePrefab == null)
        {
            Debug.LogError("Dice Prefab이 연결되지 않았습니다!");
            return;
        }
        
        // 1. 덱 정보 저장 및 리스트 초기화
        this.playerDiceDeck = deck;
        currentValues = new List<int>(new int[deck.Count]);
        isKept = new List<bool>(new bool[deck.Count]);

        // 2. 스폰 위치 계산
        float totalWidth = (deck.Count - 1) * horizontalSpacing;
        float startX = -totalWidth / 2.0f;

        // 3. 새 주사위 스폰
        for (int i = 0; i < deck.Count; i++)
        {
            float posX = startX + (i * horizontalSpacing);
            Vector3 spawnLocalPosition = new Vector3(posX, yPosition, 0);
            
            // [!!! 핵심 수정 1 !!!]
            // Instantiate -> SpawnFromPool
            if (WaveGenerator.Instance == null)
            {
                Debug.LogError("WaveGenerator가 씬에 없습니다!");
                return;
            }
            GameObject diceGO = WaveGenerator.Instance.SpawnFromPool(dicePrefab, spawnLocalPosition, Quaternion.identity);
            diceGO.transform.SetParent(diceContainer); // 부모 설정
            diceGO.transform.localPosition = spawnLocalPosition; 
            diceGO.name = "Dice_" + i;

            SpriteRenderer sr = diceGO.GetComponent<SpriteRenderer>();
            DiceKeep keepScript = diceGO.GetComponent<DiceKeep>();

            if (sr != null && keepScript != null)
            {
                keepScript.Initialize(this, i);
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
    /// [수정] 턴 시작 시, 기존에 스폰된 주사위들을 모두 '풀(Pool)로 반환'합니다.
    /// </summary>
    public void PrepareNewTurn()
    {
        // 1. 기존 주사위 오브젝트를 풀로 반환
        foreach (SpriteRenderer renderer in diceRenderers)
        {
            if (renderer != null && renderer.gameObject.activeSelf)
            {
                // [!!! 핵심 수정 2 !!!]
                // Destroy -> ReturnToPool
                if (WaveGenerator.Instance != null)
                {
                    WaveGenerator.Instance.ReturnToPool(renderer.gameObject);
                }
                else
                {
                    // (폴백) 웨이브 제너레이터가 없으면 그냥 파괴
                    Destroy(renderer.gameObject);
                }
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

    // ... (RollAnimation 함수는 이전과 동일) ...
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

                currentValues[i] = finalValue;
                
                int spriteIndex = Mathf.Clamp(finalValue - 1, 0, diceFaceSprites.Length - 1);
                diceRenderers[i].sprite = diceFaceSprites[spriteIndex];
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

