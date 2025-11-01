using UnityEngine;
using UnityEngine.UI;

// 이 스크립트는 Canvas가 있는 곳에만 붙어야 함
[RequireComponent(typeof(Canvas))]
public class SetEventCamera : MonoBehaviour
{
    void Start()
    {
        Canvas worldCanvas = GetComponent<Canvas>();

        // Event Camera 슬롯이 비어있다면,
        if (worldCanvas.worldCamera == null)
        {
            // 씬(Scene)에 있는 "MainCamera" 태그를 가진 카메라를 찾아서 할당
            worldCanvas.worldCamera = Camera.main;
        }
    }
}