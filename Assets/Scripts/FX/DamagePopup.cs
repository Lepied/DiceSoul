using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamagePopup : MonoBehaviour
{
    public TextMeshPro textMesh;
    
    public void Setup(string text, Color color, bool isCritical = false)
    {
        textMesh.text = text;
        textMesh.color = color;
        
        if (isCritical)
        {
            textMesh.fontSize *= 1.5f;
            textMesh.color = Color.red; // 치명타 색상
        }

        // 위로 튀어 오르면서 사라지기
        transform.DOLocalMoveY(transform.localPosition.y + 2f, 1f);
        textMesh.DOFade(0, 1f).OnComplete(() => Destroy(gameObject));
    }
}