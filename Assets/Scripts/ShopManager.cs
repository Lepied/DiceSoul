using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Tooltip("상점에 한 번에 표시될 아이템 개수")]
    public int shopItemCount = 4;

    [Header("리롤 관련")]
    public int baseRerollCost = 100;
    public int currentRerollCost { get; private set; }
    public List<ShopItem> currentShopItems = new List<ShopItem>();

    public Sprite fullHealIcon;   //회복
    public Sprite maxHealthIcon;  //최대체력증가

    //포션
    public Sprite emptyBottleIcon; //  빈 병
    public Sprite liquidIcon;      // 내용물 < 이거 색바꿔서 다른 포션인척하기
    public Sprite unknownDiceIcon;// 랜덤 주사위

    public Sprite relicBagIcon;   // 랜덤 유물 (이미지 로딩 실패 시 대비)
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

    public void ResetShop()
    {
        currentRerollCost = baseRerollCost;
        GenerateShopItems();
    }

    public void GenerateShopItems()
    {
        currentShopItems.Clear();
        if (GameManager.Instance == null) return;

        //1. 강력한 아이템 (주사위 or 유물) : 2개
        for (int i = 0; i < 2; i++)
        {
            // 40% 유물, 60% 주사위
            if (Random.value < 0.4f && RelicDB.Instance != null)
                AddRandomRelic();
            else
                AddRandomDice();
        }

        //2. 필수 생존 아이템 (회복/체력) : 2개
        // (A) 체력 회복
        if (GameManager.Instance.PlayerHealth < GameManager.Instance.MaxPlayerHealth)
        {
            currentShopItems.Add(new ShopItem(
                "완전 회복", "체력을 모두 회복합니다.", 300, fullHealIcon,
                () =>
                {
                    int heal = GameManager.Instance.MaxPlayerHealth - GameManager.Instance.PlayerHealth;
                    GameManager.Instance.HealPlayer(heal);
                }
            ));
        }
        else
        {
            // 체력이 꽉 찼으면 작은 최대 체력 증가로 대체
            currentShopItems.Add(new ShopItem("활력의 정수", "최대 체력 +2", 300, maxHealthIcon,
                () => GameManager.Instance.ModifyMaxHealth(2)));
        }

        // (B) 최대 체력 증가
        currentShopItems.Add(new ShopItem("생명의 그릇", "최대 체력 +5", 500, maxHealthIcon,
            () => GameManager.Instance.ModifyMaxHealth(5)));


        // 3. 랜덤 포션 (다음 존 버프) : 2개
        for (int i = 0; i < 2; i++)
        {
            AddRandomPotion();
        }
    }
    private void AddRandomDice()
    {
        // 랜덤한 주사위 타입 선택 (D4, D6, D8, D12, D20)
        string[] diceTypes = { "D4", "D6", "D8", "D12", "D20" };
        string selectedType = diceTypes[Random.Range(0, diceTypes.Length)];

        int price = 0;
        switch (selectedType)
        {
            case "D4": price = 400; break;
            case "D6": price = 500; break;
            case "D8": price = 600; break;
            case "D12": price = 900; break;
            case "D20": price = 1500; break;
        }

        Sprite icon = unknownDiceIcon;
        if (DiceController.Instance != null)
        {
            // 대표 이미지로 '가장 높은 숫자' 혹은 '1'
            Sprite s = DiceController.Instance.GetDiceSprite(selectedType, 1);
            if (s != null) icon = s;
        }

        currentShopItems.Add(new ShopItem(
            $"주사위 ({selectedType})",
            $"덱에 {selectedType} 주사위를 1개 추가합니다.",
            price,
            icon,
            () => { GameManager.Instance.AddDiceToDeck(selectedType); }
        ));
    }

    private void AddRandomRelic()
    {
        List<Relic> randomRelics = RelicDB.Instance.GetRandomRelics(1);
        if (randomRelics.Count > 0)
        {
            Relic relic = randomRelics[0];
            currentShopItems.Add(new ShopItem(
                $"[유물] {relic.Name}",
                relic.Description,
                600, // 가격 <- 나중에 유물 등급생기면 등급별로 다르게?
                relic.Icon,
                () => { GameManager.Instance.AddRelic(relic); }
            ));
        }
        else
        {
            // 유물이 동났으면 주사위로 대체
            AddRandomDice();
        }
    }

    private void AddRandomPotion()
    {
        int type = Random.Range(0, 3); // 이거늘려서 종류 늘리기
        string pName = "";
        string pDesc = "";
        string buffKey = "";
        Color pColor = Color.white;
        int price = 150;

        switch (type)
        {
            case 0: // 신속 (파랑)
                pName = "신속의 물약";
                pDesc = "다음 존(Zone) 동안 [최대 굴림 횟수 +1]";
                buffKey = "ExtraRoll";
                pColor = new Color(0.2f, 0.6f, 1f); // 밝은 파랑
                break;
            case 1: // 힘 (빨강)
                pName = "힘의 물약";
                pDesc = "다음 존(Zone) 동안 [모든 데미지 +15]";
                buffKey = "DamageBoost";
                pColor = new Color(1f, 0.3f, 0.3f); // 밝은 빨강
                break;
            case 2: // 탐욕 (노랑)
                pName = "탐욕의 물약";
                pDesc = "다음 존(Zone) 동안 [점수 획득량 1.5배]";
                buffKey = "ScoreBoost";
                pColor = new Color(1f, 0.8f, 0.2f); // 골드
                break;
        }

        // 포션 아이템 생성 
        currentShopItems.Add(new ShopItem(
            pName, pDesc, price,
            emptyBottleIcon, //병
            liquidIcon,      //내용물
            pColor,          //색
            () => GameManager.Instance.AddNextZoneBuff(buffKey)
        ));
    }

    public void BuyItem(ShopItem item)
    {
        if (GameManager.Instance.SubtractScore(item.Price))
        {
            item.ExecuteEffect();
            currentShopItems.Remove(item);
            UIManager.Instance.UpdateShopUI(currentShopItems, currentRerollCost);
        }
    }

    public void RerollShop()
    {
        if (GameManager.Instance.SubtractScore(currentRerollCost))
        {
            currentRerollCost += 100; // 리롤할수록 비싸짐
            GenerateShopItems();
            UIManager.Instance.UpdateShopUI(currentShopItems, currentRerollCost);
        }
    }
}

