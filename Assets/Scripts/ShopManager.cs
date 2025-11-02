using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용

/// <summary>
/// [수정] 
/// 1. GenerateShopItems에 "유물 판매" 로직 추가
/// 2. RelicDatabase 참조
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Tooltip("상점에 한 번에 표시될 아이템 개수")]
    public int shopItemCount = 3;

    public List<ShopItem> currentShopItems = new List<ShopItem>();

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

    public void GenerateShopItems()
    {
        currentShopItems.Clear();
        if (GameManager.Instance == null || RelicDB.Instance == null)
        {
             Debug.LogError("GameManager 또는 RelicDatabase가 없습니다!");
            return;
        }

        List<ShopItem> itemPool = new List<ShopItem>();

        // --- 아이템 풀(Pool) 생성 ---

        // 1. 체력 회복 아이템
        itemPool.Add(new ShopItem(
            "체력 5 회복", 
            "플레이어 체력을 5 회복합니다.", 
            50,
            () => { GameManager.Instance.HealPlayer(5); } 
        ));

        // 2. 주사위 추가 아이템 (최대 8개 제한)
        if (GameManager.Instance.playerDiceDeck.Count < 8)
        {
            itemPool.Add(new ShopItem(
                "새 주사위 (D6)",
                "주사위 슬롯을 1개 추가합니다. (기본 D6)",
                300, 
                () => { GameManager.Instance.AddDiceToDeck("D6"); } 
            ));
        }

        // 3. 주사위 업그레이드 아이템
        List<int> upgradableDiceIndices = new List<int>();
        for (int i = 0; i < GameManager.Instance.playerDiceDeck.Count; i++)
        {
            if (GameManager.Instance.playerDiceDeck[i] != "D20")
            {
                upgradableDiceIndices.Add(i);
            }
        }
        if (upgradableDiceIndices.Count > 0)
        {
            int diceIndexToUpgrade = upgradableDiceIndices[Random.Range(0, upgradableDiceIndices.Count)];
            string currentType = GameManager.Instance.playerDiceDeck[diceIndexToUpgrade];
            string newType = (currentType == "D6" || currentType == "D4") ? "D8" : "D20";
            int price = (newType == "D8") ? 150 : 250;

            itemPool.Add(new ShopItem(
                $"{diceIndexToUpgrade + 1}번 주사위 업그레이드 ({newType})",
                $"{diceIndexToUpgrade + 1}번 주사위({currentType})를 {newType}(으)로 교체합니다.",
                price,
                () => { GameManager.Instance.UpgradeSingleDice(diceIndexToUpgrade, newType); } 
            ));
        }

        // 4. [!!! 신규 !!!] 유물 판매 아이템
        // (이미 획득한 유물은 제외하고 뽑으면 더 좋지만, 일단 랜덤 1개)
        Relic randomRelic = RelicDB.Instance.GetRandomRelics(1).FirstOrDefault();
        if (randomRelic != null)
        {
            itemPool.Add(new ShopItem(
                $"유물: {randomRelic.Name}",
                randomRelic.Description,
                400, // 유물 고정 가격 (예시)
                () => { GameManager.Instance.AddRelic(randomRelic); } // <-- 이게 효과(Action)입니다.
            ));
        }


        // --- 아이템 풀에서 랜덤 선택 ---
        for (int i = 0; i < shopItemCount; i++)
        {
            if (itemPool.Count == 0) break; 
            ShopItem chosenItem = itemPool[Random.Range(0, itemPool.Count)];
            currentShopItems.Add(chosenItem);
            itemPool.Remove(chosenItem); 
        }
    }

    public void BuyItem(ShopItem item)
    {
        // [수정] GameManager의 SubtractScore 함수를 먼저 호출
        if (GameManager.Instance.SubtractScore(item.Price))
        {
            // 1. 점수 차감 성공
            Debug.Log($"{item.Name} 구매 성공!");

            // 2. 아이템 효과 실행
            item.ExecuteEffect();

            // 3. 구매한 아이템은 상점에서 제거
            currentShopItems.Remove(item);
            
            // 4. UIManager에게 상점 화면을 새로고침하라고 지시
            UIManager.Instance.ShowMaintenanceScreen(currentShopItems);
        }
        else
        {
            Debug.Log("점수가 부족합니다!");
        }
    }
}

