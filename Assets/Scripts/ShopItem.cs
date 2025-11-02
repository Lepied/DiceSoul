using UnityEngine;
using System.Collections.Generic; // List<T>

// [신규] 상점에서 판매하는 아이템의 종류
public enum ShopItemType
{
    Heal,       // 체력 회복
    Relic,      // 유물
    DiceAdd,    // 주사위 추가
    DiceUpgrade // 주사위 업그레이드 (D6->D8)
}

/// <summary>
/// [수정] 모든 프로퍼티와 생성자가 public인지 확인
/// </summary>
public class ShopItem
{
    // [수정] ShopManager가 접근할 수 있도록 모두 public { get; }
    public ShopItemType Type { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Price { get; private set; }

    // [삭제] Data_StringValue, Data_DiceIndex 등 복잡한 데이터 제거
    // [추가] 이 아이템이 '실행할 코드' 그 자체
    public System.Action ExecuteEffect { get; private set; }

    /// <summary>
    /// [수정] 아이템 생성 시 '실행할 효과(Action)'를 함께 받습니다.
    /// </summary>
    public ShopItem(string name, string description, int price, System.Action effectAction)
    {
        // Type은 이제 구별용으로만 사용 (선택 사항)
        // this.Type = type; 
        this.Name = name;
        this.Description = description;
        this.Price = price;
        this.ExecuteEffect = effectAction;
    }
}

