using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 덱 목록의 각 아이템(슬롯)을 제어하는 스크립트입니다.
/// DeckData를 주입받아 UI를 갱신합니다.
/// </summary>
public class DeckListItem : MonoBehaviour
{
    public DeckData Data { get; private set; }

    [Header("UI 표시용")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Image deckImage;
    public GameObject lockedOverlay;
    public TextMeshProUGUI costText; 

    [Header("연출용")]
    public float focusScale = 1.2f;
    public float normalScale = 0.9f;
    public float animDuration = 0.2f;

    public void Setup(DeckData data, MainMenuManager manager)
    {
        Data = data;
        if(nameText) nameText.text = data.deckName;
        if(descText) descText.text = data.description;
        
        // 가격 텍스트 설정
        if(costText) 
        {
            // 가격이 0원이면 텍스트를 끄거나 "무료"로 표시
            if (data.unlockCost > 0)
                costText.text = $"ㅁ {data.unlockCost}";
            else
                costText.text = "Free";
        }

        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        bool isUnlocked = (Data.unlockCost == 0) || (PlayerPrefs.GetInt(Data.unlockKey, 0) == 1);
        
        // 잠겨있을 때만 Overlay
        if(lockedOverlay) lockedOverlay.SetActive(!isUnlocked);
    }

    public void SetFocusScale(bool isFocused)
    {
        float targetScale = isFocused ? focusScale : normalScale;
        
        // DOTween을 사용하여 부드럽게 크기 변경
        transform.DOScale(targetScale, animDuration).SetEase(Ease.OutBack);

        // 포커스 안 된 애들은 좀 어둡게
        if (deckImage != null) 
           deckImage.DOFade(isFocused ? 1.0f : 0.5f, animDuration);
    }
}