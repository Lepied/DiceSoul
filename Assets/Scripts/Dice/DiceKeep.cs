using UnityEngine;

// 이 스크립트를 붙이면 SpriteRenderer와 BoxCollider2D가 자동으로 추가됨
[RequireComponent(typeof(BoxCollider2D))]
public class DiceKeep : MonoBehaviour
{

    private int diceIndex;
    private DiceController diceController;

    [Header("시각 효과 (선택 사항)")]
    [Tooltip("킵 했을 때 켤/끌 이펙트 (예: 빛나는 테두리)")]
    public GameObject keepEffect; 

    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        // 자신의 컴포넌트는 미리 찾아둠
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// DiceController가 생성 시 호출해주는 초기화 함수
    /// </summary>
    /// <param name="controller">자신을 관리할 컨트롤러</param>
    /// <param name="index">자신의 인덱스 (0~4)</param>
    public void Initialize(DiceController controller, int index)
    {
        this.diceController = controller;
        this.diceIndex = index;
    }

    void Start()
    {
        // 시작할 때 시각 효과 끄기
        UpdateVisual(false);
    }

    private void OnMouseDown()
    {
        // 컨트롤러가 아직 할당되지 않았으면 무시
        if (diceController == null) return;

        // 굴리는 중이거나, 첫 굴림 전(0회)이거나, 마지막 굴림 후(maxRolls)에는 킵 불가
        if (diceController.isRolling) return;
        if (diceController.currentRollCount <= 0 || diceController.currentRollCount >= diceController.maxRolls)
        {
            return;
        }

        // 1. 킵 상태 뒤집기 (토글)
        // (리스트의 크기를 벗어나는 인덱스인지 안전 확인)
        if (diceIndex < 0 || diceIndex >= diceController.isKept.Count) return;
        
        bool newState = !diceController.isKept[diceIndex];
        diceController.isKept[diceIndex] = newState;

        // 2. 시각 효과 업데이트
        UpdateVisual(newState);
    }

    /// <summary>
    /// 킵 상태에 따라 시각 효과(이펙트, 색상)를 업데이트합니다.
    /// (DiceController가 새 턴 시작 시 호출할 수도 있음)
    /// </summary>
    public void UpdateVisual(bool isKept)
    {
        if (keepEffect != null)
        {
            keepEffect.SetActive(isKept);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = isKept ? Color.gray : Color.white;
        }
    }
}