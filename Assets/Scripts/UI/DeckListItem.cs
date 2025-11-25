using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// (신규) 덱 목록의 각 아이템(슬롯)을 제어하는 스크립트입니다.
/// DeckData를 주입받아 UI를 갱신합니다.
/// </summary>
public class DeckListItem : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descText;
    
    public Button actionButton;      // 선택 또는 해금 버튼
    public GameObject lockedOverlay; // 잠김 상태일 때 덮을 패널
    public GameObject selectedBorder; // 선택됨 표시 테두리
    public TextMeshProUGUI buttonText;

    private DeckData data;
    private MainMenuManager manager;
    private bool isUnlocked;

    /// <summary>
    /// MainMenuManager가 생성 직후 호출하여 데이터를 세팅합니다.
    /// </summary>
    public void Setup(DeckData deckData, MainMenuManager mainManager)
    {
        data = deckData;
        manager = mainManager;

        // 1. 텍스트 설정
        if (nameText != null) nameText.text = data.deckName;
        if (descText != null) descText.text = data.description;

        // 2. 해금 여부 확인 (비용이 0이거나, 저장된 값이 1이면 해금됨)
        isUnlocked = (data.unlockCost == 0) || (PlayerPrefs.GetInt(data.unlockKey, 0) == 1);

        RefreshState();
    }

    /// <summary>
    /// 현재 선택된 덱인지 확인하고 UI 상태를 갱신합니다.
    /// </summary>
    public void RefreshState()
    {
        string currentSelected = PlayerPrefs.GetString("SelectedDeck", "Default");
        bool isSelected = (data.deckKey == currentSelected);

        if (isUnlocked)
        {
            // [해금됨 상태]
            if (lockedOverlay != null) lockedOverlay.SetActive(false);
            if (selectedBorder != null) selectedBorder.SetActive(isSelected);

            // 버튼: 이미 선택된 상태면 비활성, 아니면 '선택' 가능
            actionButton.interactable = !isSelected;
            buttonText.text = isSelected ? "선택됨" : "선택";
            
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => manager.SelectDeck(data.deckKey));
        }
        else
        {
            // [잠김 상태]
            if (lockedOverlay != null) lockedOverlay.SetActive(true);
            if (selectedBorder != null) selectedBorder.SetActive(false);

            // 버튼: 돈이 충분하면 활성, 아니면 비활성
            int currentMoney = PlayerPrefs.GetInt(manager.metaCurrencyKey, 0);
            bool canAfford = currentMoney >= data.unlockCost;

            actionButton.interactable = canAfford;
            buttonText.text = $"{data.unlockCost} 파편";

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => manager.UnlockDeck(data));
        }
    }
}