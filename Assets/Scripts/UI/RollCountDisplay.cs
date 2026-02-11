using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class RollCountDisplay : MonoBehaviour
{
    [Header("References")]
    public Transform pipContainer;
    public GameObject pipIconPrefab;
    public Sprite filledPipSprite;
    public Sprite emptyPipSprite;
    
    [Header("Layout")]
    public float arcRadius = 100f;
    public float startAngle = 150f;
    public float endAngle = 210f;
    
    [Tooltip("가운데쪽 거리 가중치")]
    public float centerRadiusBoost = 20f;
    
    [Header("Animation")]
    public bool useAnimation = true;
    public float animationDuration = 0.3f;
    
    private List<Image> pipIcons = new List<Image>();
    private int maxRollCount = 0;
    private int remainingRolls = 0;
    
    public void Initialize(int maxCount)
    {      
        maxRollCount = maxCount;
        ClearPips();
        
        for (int i = 0; i < maxRollCount; i++)
        {
            GameObject pipObj = Instantiate(pipIconPrefab, pipContainer);
            pipObj.name = $"PipIcon_{i}";
            
            Image pipImage = pipObj.GetComponent<Image>();
            pipImage.sprite = filledPipSprite;
            pipImage.raycastTarget = false; // 클릭 무시

            RectTransform pipRect = pipObj.GetComponent<RectTransform>();
            Vector2 arcPosition = CalculateArcPosition(i, maxRollCount);
            pipRect.anchoredPosition = arcPosition;
            
            pipIcons.Add(pipImage);
        }
        
        remainingRolls = maxRollCount;
    }
    
    public void ResetRollCount()
    {
        remainingRolls = maxRollCount;
        
        foreach (var pip in pipIcons)
        {
            if (pip != null)
                pip.sprite = filledPipSprite;
        }
    }

    
    public void UpdateDisplay(int currentRollCount, int maxRolls)
    {
        if (maxRolls != maxRollCount)
        {
            Initialize(maxRolls);
        }
        
        remainingRolls = maxRolls - currentRollCount;
        int usedRolls = currentRollCount;
        
        for (int i = 0; i < pipIcons.Count && i < maxRollCount; i++)
        {
            if (pipIcons[i] == null) continue;
            
            if (i >= remainingRolls)
            {
                pipIcons[i].sprite = emptyPipSprite;
            }
            else
            {
                pipIcons[i].sprite = filledPipSprite;
            }
        }
    }
    
    public void EmptyPipWithAnimation(int pipIndex)
    {
        if (pipIndex < 0 || pipIndex >= pipIcons.Count)
            return;
        
        Image pip = pipIcons[pipIndex];
        if (pip == null) return;
        
        RectTransform pipRect = pip.GetComponent<RectTransform>();
        
        if (useAnimation)
        {
            Sequence seq = DOTween.Sequence();
            
            seq.Append(pipRect.DOScale(1.1f, 0.1f));
            seq.Append(pipRect.DOScale(1.0f, 0.1f));
            seq.AppendCallback(() => 
            {
                pip.sprite = emptyPipSprite;
            });
            Color originalColor = pip.color;
            seq.Append(pip.DOColor(new Color(0.7f, 0.7f, 0.7f, 1f), 0.15f));
            seq.Append(pip.DOColor(originalColor, 0.15f));
        }
        else
        {
            pip.sprite = emptyPipSprite;
        }
    }
    
    public void FillPipWithAnimation(int pipIndex)
    {
        if (pipIndex < 0 || pipIndex >= pipIcons.Count)
            return;
        
        Image pip = pipIcons[pipIndex];
        if (pip == null) return;
        
        RectTransform pipRect = pip.GetComponent<RectTransform>();
        
        if (useAnimation)
        {
            Sequence seq = DOTween.Sequence();
            
            // 스프라이트를 먼저
            seq.AppendCallback(() => 
            {
                pip.sprite = filledPipSprite;
            });
            
            // 반짝
            Color originalColor = pip.color;
            seq.Append(pip.DOColor(new Color(1f, 1f, 0.5f, 1f), 0.15f)); // 황금빛
            seq.Append(pip.DOColor(originalColor, 0.2f));
            
            // 크기바꾸기
            seq.Join(pipRect.DOScale(1.1f, 0.15f));
            seq.Append(pipRect.DOScale(1.0f, 0.2f).SetEase(Ease.OutBack));
        }
        else
        {
            pip.sprite = filledPipSprite;
        }
    }
    
    public void OnDiceRolled(int currentRollCount)
    {
        int pipToEmpty = maxRollCount - currentRollCount;
        
        if (pipToEmpty >= 0 && pipToEmpty < pipIcons.Count)
        {
            EmptyPipWithAnimation(pipToEmpty);
        }
        
        remainingRolls = maxRollCount - currentRollCount;
    }
    
    public int GetRemainingRolls()
    {
        return remainingRolls;
    }
    
    private void ClearPips()
    {
        foreach (var pip in pipIcons)
        {
            if (pip != null)
                Destroy(pip.gameObject);
        }
        pipIcons.Clear();
    }
    
    // 위치계산
    private Vector2 CalculateArcPosition(int index, int total)
    {
        // 각 pip 간 각도
        float totalAngleRange = endAngle - startAngle;
        float angleStep = (total > 1) ? totalAngleRange / (total - 1) : 0;
        
        // 현재 pip의 각도
        float angle = startAngle + (angleStep * index);
        
        // 중앙각 계산
        float centerAngle = (startAngle + endAngle) / 2f;
        
        // 현재 각도가 중앙에 얼마나 가까운지
        float angleFromCenter = Mathf.Abs(angle - centerAngle);
        float maxAngleFromCenter = totalAngleRange / 2f;
        float centerProximity = 1f - (angleFromCenter / maxAngleFromCenter);
        
        // 양 끝으로 갈수록 반지름 증가
        float adjustedRadius = arcRadius - (centerRadiusBoost * centerProximity);
        
        float radian = angle * Mathf.Deg2Rad;
        float x = Mathf.Cos(radian) * adjustedRadius;
        float y = Mathf.Sin(radian) * adjustedRadius;
        
        return new Vector2(x, y);
    }
    
    void OnDestroy()
    {
        DOTween.Kill(transform);
        foreach (var pip in pipIcons)
        {
            if (pip != null)
                DOTween.Kill(pip.transform);
        }
    }
}
