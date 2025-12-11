using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MetaShopSlot : MonoBehaviour
{
    [Header("데이터 연결")]
    public MetaUpgradeData data;

    [Header("UI 컴포넌트")]
    public Image iconImage;
    public Button clickButton;

    [Header("원형 레벨 표시 설정")]
    public Transform pipsContainer;
    public GameObject pipPrefab;      // 레벨 표시용
    public float pipRadius = 90f;     // 반지름
    public float startAngle = 90f;    // 시작 각도


    private List<MetaPip> pips = new List<MetaPip>();
    private MetaShopManager manager;

    public void Setup(MetaShopManager shopManager)
    {
        this.manager = shopManager;

        if (data != null && iconImage != null)
            iconImage.sprite = data.icon;

        // 버튼 클릭 시: 매니저 호출 + 카메라 이동 요청
        if (clickButton != null)
        {
            clickButton.onClick.RemoveAllListeners();
            clickButton.onClick.AddListener(() =>
            {
                manager.OnSlotClicked(this);
                if (SKillTreeController.Instance != null)
                    SKillTreeController.Instance.FocusOnSlot(GetComponent<RectTransform>());
            });
        }

        CreateCircularPips();
        RefreshUI();
    }

    private void CreateCircularPips()
    {
        foreach (Transform child in pipsContainer) Destroy(child.gameObject);
        pips.Clear();

        if (data == null || pipPrefab == null) return;

        // 360도 전체에 균등 배치
        float angleStep = 360f / Mathf.Max(1, data.maxLevel);

        for (int i = 0; i < data.maxLevel; i++)
        {
            GameObject pip = Instantiate(pipPrefab, pipsContainer);

            // 각도 계산 
            float angleRad = (startAngle - (i * angleStep)) * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * pipRadius;
            float y = Mathf.Sin(angleRad) * pipRadius;

            pip.transform.localPosition = new Vector3(x, y, 0);
            MetaPip pipScript = pip.GetComponent<MetaPip>();
            if (pipScript != null)
            {
                pips.Add(pipScript);
            }
        }
    }

    public void RefreshUI()
    {
        if (data == null) return;
        int currentLevel = PlayerPrefs.GetInt(data.id, 0);

        if (currentLevel == 0)
        {
            if (pipsContainer != null) pipsContainer.gameObject.SetActive(false);
        }
        else
        {
            // 레벨이 1 이상이면 켜고 상태 갱신
            if (pipsContainer != null) pipsContainer.gameObject.SetActive(true);

            for (int i = 0; i < pips.Count; i++)
            {
                bool isUnlocked = (i < currentLevel);

                // (Pip 스크립트에 상태 전달)
                if (pips[i] != null) pips[i].SetStatus(isUnlocked);
            }
        }
    }
}