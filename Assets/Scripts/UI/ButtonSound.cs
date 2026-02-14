using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [Header("사운드 설정")]
    public SoundConfig clickSound;
    public SoundConfig hoverSound;
    
    [Header("재생 옵션")]
    public bool playClickSound = true;
    public bool playHoverSound = false;
    
    private Button button;
    
    void Awake()
    {
        button = GetComponent<Button>();
    }
    
    // 버튼 클릭 시 호출
    public void OnPointerClick(PointerEventData eventData)
    {
    
        if (clickSound != null && clickSound.HasSound())
        {
            SoundManager.Instance.PlaySoundConfig(clickSound);
        }
    }
    
    // 마우스 호버 시 호출
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null && hoverSound.HasSound())
        {
            SoundManager.Instance.PlaySoundConfig(hoverSound);
        }
    }



}