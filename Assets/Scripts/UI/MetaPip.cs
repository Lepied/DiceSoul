using UnityEngine;
using UnityEngine.UI;

public class MetaPip : MonoBehaviour
{
    [Header("연결")]
    public Image baseCircleImage;  
    public GameObject checkmarkObject;


    //상태 갱신
    public void SetStatus(bool isUnlocked)
    {
        if (checkmarkObject != null)
        {
            checkmarkObject.SetActive(isUnlocked);
        }
    }
}