using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DeckSnapScroller : MonoBehaviour, IEndDragHandler
{
    [Header("설정")]
    public ScrollRect scrollRect;
    public HorizontalLayoutGroup layoutGroup;
    public float snapSpeed = 10f;
    
    [Header("연결")]
    public MainMenuManager mainManager; // 포커스 변경 시 알림용

    private RectTransform contentPanel;
    private List<DeckListItem> items = new List<DeckListItem>();
    private bool isSnapping;
    private Vector2 targetPosition;
    private int currentFocusedIndex = -1;

    void Awake()
    {
        contentPanel = scrollRect.content;
    }

    // MainMenuManager가 아이템 생성 직후 호출해줘야 함
    public void Initialize(List<DeckListItem> spawnedItems)
    {
        if (contentPanel == null && scrollRect != null)
        {
            contentPanel = scrollRect.content;
        }
        
        items = spawnedItems;
        // 초기 위치 잡기 (첫 번째 아이템)
        if(items.Count > 0)
        {
            SnapToItem(0);
        }
    }

    void Update()
    {
        // 드래그 중이 아니고, 스냅해야 할 때 위치 보정
        if (isSnapping && contentPanel != null)
        {
            contentPanel.anchoredPosition = Vector2.Lerp(
                contentPanel.anchoredPosition, 
                targetPosition, 
                Time.deltaTime * snapSpeed
            );

            // 목표에 거의 도달하면 스냅 종료
            if (Vector2.Distance(contentPanel.anchoredPosition, targetPosition) < 1f)
            {
                contentPanel.anchoredPosition = targetPosition;
                isSnapping = false;
            }
        }
    }

    // 사용자가 드래그를 끝냈을 때 호출됨
    public void OnEndDrag(PointerEventData eventData)
    {
        int nearestIndex = FindNearestItemIndex();
        SnapToItem(nearestIndex);
    }

    private int FindNearestItemIndex()
    {
        float centerLine = -contentPanel.anchoredPosition.x; // 현재 컨텐츠의 중앙 위치(로컬)
        float minDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < items.Count; i++)
        {
            // 각 아이템의 중심 좌표 계산
            float itemPos = (i * (items[i].GetComponent<RectTransform>().rect.width + layoutGroup.spacing));
            float distance = Mathf.Abs(centerLine - itemPos);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }
        return nearestIndex;
    }

    public void SnapToItem(int index)
    {
        if (index < 0 || index >= items.Count) return;

        // 목표 위치 계산: -(아이템인덱스 * (너비 + 간격))
        float itemWidth = items[index].GetComponent<RectTransform>().rect.width;
        float spacing = layoutGroup.spacing;
        
        float newX = -(index * (itemWidth + spacing));
        targetPosition = new Vector2(newX, contentPanel.anchoredPosition.y);
        
        isSnapping = true;
        scrollRect.velocity = Vector2.zero; // 관성 스크롤 제거

        // 포커스 변경 알림
        if (currentFocusedIndex != index)
        {
            currentFocusedIndex = index;
            mainManager.OnDeckFocused(items[index]); // 매니저에게 "이 녀석이 중앙이야"라고 알림
        }
    }
}