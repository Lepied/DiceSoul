using UnityEngine;
using DG.Tweening;

public class Dice : MonoBehaviour
{
    [Header("연결")]
    public SpriteRenderer spriteRenderer;
    public GameObject keepEffectObj;

    // 상태
    public string Type { get; private set; } 
    public int Value { get; private set; }
    public bool IsKept { get; private set; }

    // 초기화
    public void Initialize(string type)
    {
        Type = type;
        IsKept = false;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (keepEffectObj != null) keepEffectObj.SetActive(false);
        
        // 초기 이미지(1) 설정
        UpdateKeepVisual();
        UpdateVisual(1);
    }

    // 이미지 갱신
    public void UpdateVisual(int newValue)
    {
        Value = newValue;
        Sprite s = DiceController.Instance.GetDiceSprite(Type, newValue);
        if (s != null) spriteRenderer.sprite = s;
    }

    // --- 행동 (Actions) ---
    private void OnMouseDown()
    {
        // 1. 굴리는 중이거나 횟수 제한 확인
        if (DiceController.Instance == null) return;
        if (DiceController.Instance.isRolling) return;
        
        int rolls = DiceController.Instance.currentRollCount;
        int max = DiceController.Instance.maxRolls;
        
        // 첫 굴림 전이거나, 굴림 기회를 다 썼으면 조작 불가
        if (rolls <= 0 || rolls >= max) return;

        // 2. 킵 상태 토글
        ToggleKeep();
    }
    //킵 토글
    public void ToggleKeep()
    {
        SetKeep(!IsKept);
    }

    public void SetKeep(bool state)
    {
        IsKept = state;
        UpdateKeepVisual();
    }

    //굴러가는 중 랜덤 이미지 보여주기
    public void ShowRandomFace()
    {
        if (IsKept) return;
        
        // Controller에게 "나(Type)한테 맞는 랜덤 이미지 하나 줘" 요청
        Sprite s = DiceController.Instance.GetRandomAnimationSprite(Type);
        if (s != null) spriteRenderer.sprite = s;
    }

    //킵킵
    private void UpdateKeepVisual()
    {
        // 킵 효과 오브젝트 끄고 켜기 (나중에하나 넣기)
        if (keepEffectObj != null) keepEffectObj.SetActive(IsKept);
        
        // 색변경
        if (spriteRenderer != null) 
            spriteRenderer.color = IsKept ? Color.gray : Color.white;
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