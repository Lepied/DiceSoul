using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MetaShopManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI soulCurrencyText;
    public GameObject shopPanel;

    // 가격 설정 (배열 인덱스 = 현재 레벨)
    private int[] healthCosts = { 100, 200, 400, 800, 1500 };
    private int[] scoreCosts = { 100, 200, 400, 800, 1500 };
    private int[] rerollCosts = { 1500, 2500, 4000 };
    private int reviveCost = 1000;

    void OnEnable()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        int currentSouls = PlayerPrefs.GetInt("MetaCurrency", 0);
        soulCurrencyText.text = $"{currentSouls}";
    }

    //버튼에연결하거나 할 메서드들

    public void BuyHealthUpgrade()
    {
        BuyUpgrade("Passive_Health", healthCosts, 5);
    }

    public void BuyScoreUpgrade()
    {
        BuyUpgrade("Passive_Score", scoreCosts, 5);
    }

    public void BuyRerollUpgrade()
    {
        BuyUpgrade("Passive_Reroll", rerollCosts, 3);
    }

    public void BuyReviveToken()
    {
        // 소모품은 1개만 보유 가능
        if (PlayerPrefs.GetInt("Consumable_Revive", 0) == 1)
        {
            Debug.Log("이미 부활권을 가지고 있습니다.");
            return;
        }

        if (TrySpendSouls(reviveCost))
        {
            PlayerPrefs.SetInt("Consumable_Revive", 1);
            Debug.Log("부활권 구매 완료!");
            UpdateUI();
        }
    }

    //내부 로직
    private void BuyUpgrade(string key, int[] costs, int maxLevel)
    {
        int currentLevel = PlayerPrefs.GetInt(key, 0);

        if (currentLevel >= maxLevel)
        {
            Debug.Log("이미 최대 레벨입니다.");
            return;
        }

        int price = costs[currentLevel];
        if (TrySpendSouls(price))
        {
            PlayerPrefs.SetInt(key, currentLevel + 1);
            Debug.Log($"{key} 업그레이드 완료! (Lv.{currentLevel + 1})");
            UpdateUI();
        }
    }

    private bool TrySpendSouls(int amount)
    {
        int currentSouls = PlayerPrefs.GetInt("MetaCurrency", 0);
        if (currentSouls >= amount)
        {
            currentSouls -= amount;
            PlayerPrefs.SetInt("MetaCurrency", currentSouls);
            PlayerPrefs.Save();
            return true;
        }
        else
        {
            Debug.Log("마석이 부족합니다!");
            // 여기에 '돈 부족' 팝업이나 효과음 추가
            return false;
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }
}