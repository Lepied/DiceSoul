using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildingHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image buildingImage;
    [SerializeField] private Outline outline;
    
    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1.2f, 1.2f, 1.2f, 1f);
    
    [Header("Scale Settings")]
    [SerializeField] private bool useScaleEffect = false;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float scaleSpeed = 5f;
    
    private Vector3 originalScale;
    private Vector3 targetScale;

    
    void Start()
    {

        buildingImage = GetComponent<Image>();
        outline = GetComponent<Outline>();
        buildingImage.color = normalColor;
        buildingImage.alphaHitTestMinimumThreshold = 0.1f;
        outline.enabled = false;
        
        originalScale = transform.localScale;
        targetScale = originalScale;
    }
    
    void Update()
    {
        if (useScaleEffect)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale, 
                targetScale, 
                Time.deltaTime * scaleSpeed
            );
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.enabled = true;
        buildingImage.color = hoverColor;
        
        if (useScaleEffect)
            targetScale = originalScale * hoverScale;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        outline.enabled = false;
        buildingImage.color = normalColor;
        if (useScaleEffect)
            targetScale = originalScale;
    }
    
    //버튼 비활성화하거나해야할때 쓰기
    public void ForceReset()
    { 
        outline.enabled = false;
        buildingImage.color = normalColor;
        
        transform.localScale = originalScale;
        targetScale = originalScale;
    }
}
