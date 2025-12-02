using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// 덱 목록의 각 아이템(슬롯)을 제어하는 스크립트
/// DeckData를 주입받아 UI를 갱신
public class DeckListItem : MonoBehaviour
{
    public DeckData Data { get; private set; }
    private MainMenuManager menuManager;

    [Header("UI 표시용")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Image deckImage;
    public GameObject lockedOverlay;
    public TextMeshProUGUI costText;

    [Header("아이콘 설정")]

    public GameObject iconPrefab;
    public Transform diceContainer;
    public Transform relicContainer;

    [System.Serializable]
    public struct DiceIconMapping
    {
        public string diceType; // 예: "D6", "D20"
        public Sprite icon;     // 해당 주사위 이미지
    }

    [Tooltip("주사위 이름(Key)과 보여줄 아이콘(Sprite) 매핑")]
    public List<DiceIconMapping> diceIconMappings;


    [Header("연출용")]
    public float focusScale = 1.2f;
    public float normalScale = 0.9f;
    public float animDuration = 0.2f;

    public void Setup(DeckData data, MainMenuManager manager)
    {
        Data = data;
        this.menuManager = manager;

        if (nameText) nameText.text = data.deckName;
        if (descText) descText.text = data.description;

        // 가격 텍스트 설정
        if (costText)
        {
            // 가격이 0원이면 텍스트를 끄거나 "무료"로 표시
            if (data.unlockCost > 0)
                costText.text = $"ㅁ {data.unlockCost}";
            else
                costText.text = "Free";
        }

        RefreshVisuals();
        GenerateAllIcons();
    }

    public void RefreshVisuals()
    {
        bool isUnlocked = (Data.unlockCost == 0) || (PlayerPrefs.GetInt(Data.unlockKey, 0) == 1);

        // 잠겨있을 때만 Overlay
        if (lockedOverlay) lockedOverlay.SetActive(!isUnlocked);
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

    private void GenerateAllIcons()
    {
        // (1) 기존 아이콘들 싹싹이
        if (diceContainer != null) foreach (Transform child in diceContainer) Destroy(child.gameObject);
        if (relicContainer != null) foreach (Transform child in relicContainer) Destroy(child.gameObject);

        if (iconPrefab == null) return;

        // (2) 유물 아이콘 생성
        if (!string.IsNullOrEmpty(Data.displayRelicID) && RelicDB.Instance != null)
        {
            Relic relic = RelicDB.Instance.GetRelicByID(Data.displayRelicID);
            if (relic != null)
            {
                // 유물은 제목이랑 설명
                CreateIcon(relicContainer, relic.Icon, relic.Name, relic.Description);
            }
        }

        // (3) 주사위 아이콘 생성
        if (Data.displayDice != null)
        {
            foreach (string diceType in Data.displayDice)
            {
                Sprite s = GetDiceSprite(diceType);
                if (s != null)
                {
                    // 주사위는 설명 없이 이름만
                    CreateIcon(diceContainer, s, diceType, "");
                }
            }
        }
    }
    private void CreateIcon(Transform parentContainer, Sprite sprite, string title, string description)
    {
        if (parentContainer == null || sprite == null) return;

        // 1. 프리팹 생성
        GameObject iconObj = Instantiate(iconPrefab, parentContainer);

        // 2. 이미지 교체
        Image img = iconObj.GetComponent<Image>();
        if (img != null) img.sprite = sprite;

        // 3. 툴팁 이벤트 연결
        EventTrigger trigger = iconObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = iconObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        // 마우스 올림
        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) =>
        {

            Debug.Log($"[1] 마우스 감지됨! (제목: {title})");
            if (menuManager == null)
            {
                Debug.LogError("[2] 앗! menuManager가 연결되지 않았습니다 (null 상태).");
            }
            else
            {
                Debug.Log("[2] 매니저에게 팝업 요청 보냄.");
                menuManager.ShowInfoPopup(title, description, iconObj.GetComponent<RectTransform>());
            }
        });
        trigger.triggers.Add(entryEnter);

        // 마우스 나감
        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) =>
        {
            if (menuManager != null)
                menuManager.HideInfoPopup();
        });
        trigger.triggers.Add(entryExit);
    }
    private Sprite GetDiceSprite(string diceType)
    {
        foreach (var mapping in diceIconMappings)
        {
            if (mapping.diceType == diceType)
                return mapping.icon;
        }
        return null; // 매핑된 이미지가 없으면 null 반환
    }
}