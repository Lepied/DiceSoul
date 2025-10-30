using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DiceController : MonoBehaviour
{

    public GameObject dicePrefab;
    public Transform diceContainer;
    public Button rollButtonUI;
    public Sprite[] diceFaceSprites;

    public int initialDiceCount = 5; // 시작할 때 생성할 주사위 개수
    public int baseMaxRolls = 3; 
    public int maxRolls { get; private set; } 

    // --- 현재 상태 변수 ---
    private List<SpriteRenderer> diceRenderers = new List<SpriteRenderer>();
    private List<DiceKeep> diceKeepScripts = new List<DiceKeep>();

    [Header("배치 설정")]
    [Tooltip("주사위 사이의 가로 간격 (예: 2.0)")]
    public float horizontalSpacing = 2.0f;
    [Tooltip("주사위가 생성될 Y 위치")]
    public float yPosition = 0f;

    public int currentRollCount { get; private set; }
    public bool isRolling { get; private set; } = false;
    public List<int> currentValues { get; private set; }
    public List<bool> isKept { get; set; }

    void Start()
    {
        // 1. 리스트 초기화 (시작 개수 기준)
        currentValues = new List<int>(new int[initialDiceCount]);
        isKept = new List<bool>(new bool[initialDiceCount]);

        // 2. UI 버튼 리스너 연결
        if (rollButtonUI != null)
        {
            rollButtonUI.onClick.AddListener(OnRollButton);
        }
        else
        {
            Debug.LogError("Roll Button UI가 DiceController에 연결되지 않았습니다!");
        }

        // 3. 프리팹으로 주사위 생성
        SpawnDice();

        // 4. 새 턴 준비
        PrepareNewTurn();
    }

    /// <summary>
    /// 설정된 개수만큼 주사위 프리팹을 생성하고 초기화합니다.
    /// </summary>
    private void SpawnDice()
    {
        if (dicePrefab == null)
        {
            Debug.LogError("Dice Prefab이 연결되지 않았습니다!");
            return;
        }

        float totalWidth = (initialDiceCount - 1) * horizontalSpacing;

        float startX = -totalWidth / 2.0f;

        for (int i = 0; i < initialDiceCount; i++)
        {
            float posX = startX + (i * horizontalSpacing);

            Vector3 spawnLocalPosition = new Vector3(posX, yPosition, 0);

            // 프리팹 생성, diceContainer를 부모로 설정
            GameObject diceGO = Instantiate(dicePrefab, diceContainer);
            diceGO.transform.localPosition = spawnLocalPosition;
            diceGO.name = "Dice_" + i;

            // 생성된 프리팹에서 스크립트와 렌더러 가져오기
            SpriteRenderer sr = diceGO.GetComponent<SpriteRenderer>();
            DiceKeep keepScript = diceGO.GetComponent<DiceKeep>();

            if (sr != null && keepScript != null)
            {
                keepScript.Initialize(this, i);

                // 관리 리스트에 추가
                diceRenderers.Add(sr);
                diceKeepScripts.Add(keepScript);
            }
            else
            {
                Debug.LogError("Dice Prefab에 SpriteRenderer나 DiceKeep 스크립트가 없습니다!");
            }
        }
    }

    // --- (OnRollButton, RollAnimation, SetRollButtonInteractable 함수는 이전과 동일) ---
    // (아래에 복사해 두었습니다)

    private void OnRollButton()
    {
        if (isRolling) return;
        if (currentRollCount >= maxRolls) return;

        currentRollCount++;
        UIManager.Instance.UpdateRollCount(currentRollCount, maxRolls);
        StartCoroutine(RollAnimation());
    }

    public void PrepareNewTurn()
    {
        currentRollCount = 0;
        isRolling = false;
        SetRollButtonInteractable(true);

        maxRolls = baseMaxRolls;
        

        // 생성된 주사위 개수(diceRenderers.Count)만큼 반복
        for (int i = 0; i < diceRenderers.Count; i++)
        {
            isKept[i] = false;
            currentValues[i] = 0;

            // DiceKeep 스크립트 리스트에서 가져와서 시각 효과 리셋
            if (diceKeepScripts[i] != null)
            {
                diceKeepScripts[i].UpdateVisual(false);
            }

            if (diceFaceSprites != null && diceFaceSprites.Length > 0)
            {
                diceRenderers[i].sprite = diceFaceSprites[0];
            }
        }
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
                int finalValue = Random.Range(1, 7);
                currentValues[i] = finalValue;

                if (diceFaceSprites != null && diceFaceSprites.Length >= finalValue)
                {
                    diceRenderers[i].sprite = diceFaceSprites[finalValue - 1];
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
    //유물용 함수
    public void ApplyRollBonus(int amount)
    {
        maxRolls += amount;
        Debug.Log($"유물 효과 적용: 최대 굴림 +{amount}. (현재: {maxRolls})");
    }
}