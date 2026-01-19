using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public enum DiceState
{
    Normal,
    Locked,
    Preserved
}

public class Dice : MonoBehaviour, IPointerClickHandler
{
    [Header("연결")]
    public SpriteRenderer spriteRenderer;
    public GameObject lockEffectObj;      // 잠금 이펙트
    public GameObject preserveEffectObj;  // 보존 이펙트


    // 상태
    public string Type { get; private set; } 
    public int Value { get; private set; }
    public DiceState State { get; private set; } = DiceState.Normal;
    public int lockDuration = 0; // 잠금 지속 턴 수

    // 초기화
    public void Initialize(string type)
    {
        Type = type;
        State = DiceState.Normal;
        lockDuration = 0;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (lockEffectObj != null) lockEffectObj.SetActive(false);
        if (preserveEffectObj != null) preserveEffectObj.SetActive(false);
        
        // 초기 이미지(1) 설정
        UpdateStateVisual();
        UpdateVisual(1);
    }

    // 이미지랑 값 갱신
    public void UpdateVisual(int newValue)
    {
        Value = newValue;
        Sprite s = DiceController.Instance.GetDiceSprite(Type, newValue);
        if (s != null) spriteRenderer.sprite = s;
    }

    // --- 행동 (Actions) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (DiceController.Instance == null) return;
        
        if (State == DiceState.Locked) return;
        
        int myIndex = DiceController.Instance.activeDice.IndexOf(this);
        if (myIndex < 0) return;
        
        // 이중 주사위 선택 모드 확인
        bool handled = DiceController.Instance.TryUseDoubleDiceOn(myIndex);
        if (handled) return;
        
        // 보존 기회가 있고 Normal 상태면 바로 보존
        if (State == DiceState.Normal && RelicEffectHandler.Instance != null)
        {
            if (RelicEffectHandler.Instance.CanUsePreserve())
            {
                bool success = RelicEffectHandler.Instance.UsePreserveCharge(myIndex);
                if (success)
                {
                    // 유물 패널 업데이트
                    if (UIManager.Instance != null && GameManager.Instance != null)
                    {
                        UIManager.Instance.UpdateRelicPanel(GameManager.Instance.activeRelics);
                    }
                }
            }
        }
    }

    //굴러가는 중 랜덤 이미지 보여주기
    public void ShowRandomFace()
    {
        // Locked나 Preserved 상태면 굴러가지 않음
        if (State != DiceState.Normal) return;
        
        // Controller에게 "나(Type)한테 맞는 랜덤 이미지 하나 줘" 요청
        Sprite s = DiceController.Instance.GetRandomAnimationSprite(Type);
        if (s != null) spriteRenderer.sprite = s;
    }

    // 상태별 시각 효과
    private void UpdateStateVisual()
    {
        if (spriteRenderer == null) return;
        
        switch (State)
        {
            case DiceState.Normal:
                spriteRenderer.color = Color.white;
                if (lockEffectObj != null) lockEffectObj.SetActive(false);
                if (preserveEffectObj != null) preserveEffectObj.SetActive(false);
                break;
                
            case DiceState.Locked:
                spriteRenderer.color = Color.gray; // 회색
                if (lockEffectObj != null) lockEffectObj.SetActive(true);
                if (preserveEffectObj != null) preserveEffectObj.SetActive(false);
                break;
                
            case DiceState.Preserved:
                spriteRenderer.color = new Color(1f, 0.9f, 0.5f); // 황금빛
                if (lockEffectObj != null) lockEffectObj.SetActive(false);
                if (preserveEffectObj != null) preserveEffectObj.SetActive(true);
                break;
        }
    }
    
    // 상태 설정
    public void SetState(DiceState newState)
    {
        State = newState;
        UpdateStateVisual();
    }
    
    // 잠금 애니메이션
    public void PlayLockAnimation()
    {
        transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
    }
    
    // 해제 애니메이션
    public void PlayUnlockAnimation()
    {
        transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 8, 0.8f);
    }
    
    // 보존 애니메이션
    public void PlayPreserveAnimation()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(DiceController.Instance.diceScale * 1.3f, 0.2f));
        seq.Append(transform.DOScale(DiceController.Instance.diceScale, 0.2f));
    }

    //뾰로롱
    public void PlayMagicAnimation(int newValue)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1.5f, 0.25f).SetEase(Ease.OutBack)); // 커짐
        seq.Join(spriteRenderer.DOColor(new Color(0.5f, 1f, 1f), 0.25f).SetLoops(2, LoopType.Yoyo)); // 반짝

        seq.OnComplete(() =>
        {
            UpdateVisual(newValue); // 값/이미지 교체
            transform.DOScale(DiceController.Instance.diceScale, 0.15f); // 원래 크기로
            spriteRenderer.color = Color.white;
        });
    }

    //휘리릭
    public void PlayRerollAnimation(int newValue)
    {
        transform.DORotate(new Vector3(0, 0, 360), 0.4f, RotateMode.FastBeyond360).SetRelative(true);
        // (회전 중 랜덤 이미지는 코루틴 등 추가 구현 가능, 여기선 간단히 회전 후 값 변경)
        
        DOVirtual.DelayedCall(0.4f, () => {
            UpdateVisual(newValue);
        });
    }
}