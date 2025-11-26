using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamagePopup : MonoBehaviour
{
    public TextMeshPro textMesh;
    private EffectManager manager;

    public void Setup(string text, Color color, bool isCritical, EffectManager mgr)
    {
        manager = mgr;
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = isCritical ? 6 : 4; 
        transform.localScale = Vector3.one;

        // 애니메이션 시작
        PlayAnimation(isCritical);
    }

    private void PlayAnimation(bool isCritical)
    {
        // DOTween Sequence 사용 (무료 버전 호환)
        Sequence seq = DOTween.Sequence();

        //  위로  떠오르기
        seq.Append(transform.DOMoveY(transform.position.y + 1f, 0.6f).SetEase(Ease.OutQuad));

        //치명타면 처음에 확 커졌다 작아짐 (펀치 효과)
        if (isCritical)
        {
            transform.localScale = Vector3.zero;
            seq.Join(transform.DOScale(1.5f, 0.3f).SetEase(Ease.OutBack));
        }

        // 흐려지기 
        seq.Insert(0.4f, textMesh.DOFade(0, 0.4f));

        // 4. 끝나면 풀로 반환
        seq.OnComplete(() => {
            if(manager != null) manager.ReturnToPool(this);
            else Destroy(gameObject);
        });
    }
}