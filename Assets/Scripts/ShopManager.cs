using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Button 처리를 위해

/// <summary>
/// 상점에서 판매할 아이템을 생성하고,
/// 구매 요청을 처리하는 싱글톤 매니저입니다.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    // (나중에 유물처럼 상점 아이템도 데이터베이스화 할 수 있습니다)
    // public List<ShopItem> availableUpgrades; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// GameManager가 호출. 현재 존(Zone) 레벨에 맞는 상점 아이템 리스트를 생성합니다.
    /// </summary>
    public List<ShopItem> GenerateShopItems(int currentZone)
    {
        List<ShopItem> items = new List<ShopItem>();

        // 1. 유물 1개 추가 (예: 가격 = 200 * 존 레벨)
        Relic randomRelic = RelicDB.Instance.GetRandomRelics(1)[0];
        items.Add(new ShopItem(
            randomRelic.Name, 
            randomRelic.Description, 
            200 * currentZone, 
            ShopItemType.Relic, 
            randomRelic
        ));

        // 2. 체력 회복 추가 (예: 100점)
        items.Add(new ShopItem(
            "체력 10 회복", 
            "플레이어의 체력을 10 회복합니다.", 
            100, 
            ShopItemType.Heal
        ));

        // 3. 주사위 업그레이드 추가 (예: 400점)
        // (TODO: 이미 D8이면 D10이 나오게 하는 등 확장 필요)
        items.Add(new ShopItem(
            "주사위 업그레이드 (D8)", 
            "기본 D6 주사위 1개를 D8로 바꿉니다.", 
            400, 
            ShopItemType.DiceUpgrade
        ));

        return items;
    }

    /// <summary>
    /// UIManager가 호출. 아이템 구매를 시도합니다.
    /// </summary>
    public void BuyItem(ShopItem item, Button clickedButton)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // 1. 점수(재화)가 충분한지 확인
        if (gm.CurrentScore >= item.Price)
        {
            // 2. 점수 차감
            gm.SpendScore(item.Price);
            Debug.Log($"{item.ItemName} 구매 완료! (가격: {item.Price})");

            // 3. 아이템 효과 적용
            ApplyItemEffect(item);

            // 4. 구매한 아이템 버튼 비활성화 (매진 처리)
            if (clickedButton != null)
            {
                clickedButton.interactable = false; 
            }
        }
        else
        {
            Debug.Log($"구매 실패: 점수가 부족합니다. (현재: {gm.CurrentScore} / 필요: {item.Price})");
            // (TODO: 점수 부족 UI 피드백)
        }
    }

    /// <summary>
    /// 구매한 아이템의 효과를 GameManager에 적용
    /// </summary>
    private void ApplyItemEffect(ShopItem item)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        switch (item.ItemType)
        {
            case ShopItemType.Relic:
                gm.AddRelic(item.RelicData); // (이 함수는 새로 만들어야 함)
                break;
            case ShopItemType.DiceUpgrade:
                gm.UpgradeDice("D8"); // (이 함수는 새로 만들어야 함)
                break;
            case ShopItemType.DiceAdd:
                // (TODO)
                break;
            case ShopItemType.Heal:
                gm.HealPlayer(10); // (이 함수는 새로 만들어야 함)
                break;
            case ShopItemType.RerollShop:
                // (TODO)
                break;
        }
    }
}
