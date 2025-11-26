using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("프리팹 연결")]
    public GameObject popupPrefab; //데미지 팝업 프리팹

    void Awake() { Instance = this; }

    public void ShowPopup(Vector3 position, string text, Color color, bool isCritical = false)
    {
        // 적의 머리 위쪽으로 약간 띄워서 생성
        Vector3 spawnPos = position + new Vector3(0, 1.0f, 0);

        GameObject go = Instantiate(popupPrefab, spawnPos, Quaternion.identity);
        DamagePopup popup = go.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Setup(text, color, isCritical);
        }
    }

    public void ShowDamage(Vector3 pos, int damage, bool isCritical = false)
    {
        ShowPopup(pos, damage.ToString(), isCritical ? Color.red : Color.white, isCritical);
    }

    public void ShowHeal(Vector3 pos, int amount)
    {
        ShowPopup(pos, $"+{amount}", Color.green);
    }

    public void ShowText(Vector3 pos, string message, Color color)
    {
        ShowPopup(pos, message, color); //텍스트로 보여줄거 "면역!", "방어!" 등등등
    }
}