using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("설정")]
    public GameObject popupPrefab;
    public Transform popupContainer; // 텍스트들을 모아둘 부모 오브젝트 (Hierarchy 정리용)
    public float textSpawnDelay = 0.2f; // 같은 몬스터에게서 텍스트가 뜨는 간격

    //  오브젝트 풀 (재사용 대기소)
    private Queue<DamagePopup> pool = new Queue<DamagePopup>();

    //  몬스터별 요청 대기열 (Key: 몬스터 Transform)
    private Dictionary<Transform, Queue<PopupRequest>> popupQueues = new Dictionary<Transform, Queue<PopupRequest>>();
    
    //몬스터별 실행 중인 코루틴 
    private Dictionary<Transform, Coroutine> activeCoroutines = new Dictionary<Transform, Coroutine>();

    struct PopupRequest
    {
        public Vector3 Position; // 텍스트가 뜰 위치 (요청 시점 기준)
        public string Text;
        public Color Color;
        public bool IsCritical;
    }

    void Awake() 
    { 
        Instance = this; 
        if(popupContainer == null)
        {
            GameObject go = new GameObject("Popups");
            go.transform.SetParent(transform);
            popupContainer = go.transform;
        }
    }

    // (Target: 텍스트를 띄울 대상 몬스터)
    public void ShowPopup(Transform target, string text, Color color, bool isCritical = false)
    {
        if (target == null) return; // 대상이 없으면 무시

        if (!popupQueues.ContainsKey(target))
        {
            popupQueues.Add(target, new Queue<PopupRequest>());
        }

        popupQueues[target].Enqueue(new PopupRequest { 
            Position = target.position, // 현재 위치 저장
            Text = text, 
            Color = color, 
            IsCritical = isCritical 
        });

        if (!activeCoroutines.ContainsKey(target))
        {
            Coroutine co = StartCoroutine(ProcessQueueRoutine(target));
            activeCoroutines.Add(target, co);
        }
    }

    // 각 몬스터별로 따로 돌아가는 처리기
    IEnumerator ProcessQueueRoutine(Transform target)
    {
        while (target != null && popupQueues.ContainsKey(target) && popupQueues[target].Count > 0)
        {
            PopupRequest req = popupQueues[target].Dequeue();
            SpawnPopup(req);
            yield return new WaitForSeconds(textSpawnDelay); 
        }

        // 할 일이 끝나면 코루틴 목록에서 제거 (퇴근)
        if(target != null) activeCoroutines.Remove(target);
    }

    private void SpawnPopup(PopupRequest req)
    {
        DamagePopup popup = GetPopupFromPool();
  
        float randomX = Random.Range(-0.3f, 0.3f);
        Vector3 spawnPos = req.Position + new Vector3(randomX, 1f, 0);
        
        popup.transform.position = spawnPos;
        popup.Setup(req.Text, req.Color, req.IsCritical, this); 
    }

    // 풀링 시스템 
    private DamagePopup GetPopupFromPool()
    {
        DamagePopup popup = null;
        if (pool.Count > 0)
        {
            popup = pool.Dequeue();
            popup.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = Instantiate(popupPrefab, popupContainer);
            popup = go.GetComponent<DamagePopup>();
        }
        return popup;
    }

    public void ReturnToPool(DamagePopup popup)
    {
        popup.gameObject.SetActive(false);
        pool.Enqueue(popup);
    }

    // 메모리정리
    public void RemoveQueue(Transform target)
    {
        if (activeCoroutines.ContainsKey(target))
        {
            StopCoroutine(activeCoroutines[target]);
            activeCoroutines.Remove(target);
        }
        if (popupQueues.ContainsKey(target))
        {
            popupQueues.Remove(target);
        }
    }
    public void ShowDamage(Transform target, int damage, bool isCritical = false) => ShowPopup(target, damage.ToString(), isCritical ? Color.red : Color.white, isCritical);
    public void ShowHeal(Transform target, int amount) => ShowPopup(target, $"+{amount}", Color.green);
    public void ShowText(Transform target, string message, Color color) => ShowPopup(target, message, color);
}