using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용

/// <summary>
/// [!!! 핵심 수정 !!!]
/// 1. GenerateShopItems 함수가 'RelicDatabase'를 호출하여
///    '필터링된' 유물 1개를 상점 목록에 추가합니다.
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

    /// <summary>
    /// [수정] '유물' 아이템을 상점 풀에 추가합니다.
    /// </summary>
    public void GenerateShopItems()
    {
        currentShopItems.Clear();
        if (GameManager.Instance == null) return;

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

        // 4. [!!! 신규 추가 !!!] 유물 아이템 1개 추가
        if (RelicDB.Instance != null)
        {
            // GetRandomRelics는 이제 획득 한도를 초과한 유물을 '알아서' 제외합니다.
            List<Relic> randomRelics = RelicDB.Instance.GetRandomRelics(1);
            if (randomRelics.Count > 0)
            {
                Relic relicToSell = randomRelics[0];
                int relicPrice = 200; // (TODO: 유물 등급별 가격 책정)

                itemPool.Add(new ShopItem(
                    $"[유물] {relicToSell.Name}",
                    relicToSell.Description,
                    relicPrice,
                    // [핵심] 이 아이템의 효과(Effect)는 GameManager에 유물을 '추가'하는 것입니다.
                    () => { GameManager.Instance.AddRelic(relicToSell); }
                ));
            }
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

    /// <summary>
    /// [변경 없음] BuyItem 로직은 이미 Action 기반이라 수정할 필요가 없습니다.
    /// </summary>
    public void BuyItem(ShopItem item)
    {
        if (GameManager.Instance.SubtractScore(item.Price)) // [수정] 점수 차감 로직 변경
        {
            // 1. [변경 없음] 아이템이 가진 '효과'를 그냥 실행!
            item.ExecuteEffect();

            // 2. 구매한 아이템은 상점에서 제거
            currentShopItems.Remove(item);
            
            // 3. UIManager에게 상점 화면을 새로고침하라고 지시
            UIManager.Instance.ShowMaintenanceScreen(currentShopItems);
        }
        else
        {
            Debug.Log("점수가 부족합니다!");
        }
    }
}

