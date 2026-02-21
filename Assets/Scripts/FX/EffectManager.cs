using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("설정")]
    public GameObject popupPrefab;
    public Transform popupContainer;
    public float textSpawnDelay = 0.2f; // 같은 몬스터에게서 텍스트가 뜨는 간격

    //  오브젝트 풀 
    private Queue<DamagePopup> pool = new Queue<DamagePopup>();

    // 위치 기반 요청 대기열 
    private Dictionary<Vector3, Queue<PopupRequest>> popupQueues = new Dictionary<Vector3, Queue<PopupRequest>>();
    
    // 위치별 실행 중인 코루틴
    private Dictionary<Vector3, Coroutine> activeCoroutines = new Dictionary<Vector3, Coroutine>();

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

    // Vector3 위치 기반 팝업 
    public void ShowText(Vector3 position, string message, Color color, bool isCritical = false)
    {
        if (!popupQueues.ContainsKey(position))
        {
            popupQueues.Add(position, new Queue<PopupRequest>());
        }

        popupQueues[position].Enqueue(new PopupRequest { 
            Position = position,
            Text = message, 
            Color = color, 
            IsCritical = isCritical
        });

        if (!activeCoroutines.ContainsKey(position))
        {
            Coroutine co = StartCoroutine(ProcessQueueRoutine(position));
            activeCoroutines.Add(position, co);
        }
    }

    // 위치별 큐 처리 코루틴
    IEnumerator ProcessQueueRoutine(Vector3 position)
    {
        while (popupQueues.ContainsKey(position) && popupQueues[position].Count > 0)
        {
            PopupRequest req = popupQueues[position].Dequeue();
            SpawnPopup(req);
            yield return new WaitForSeconds(textSpawnDelay); 
        }

        // 할 일이 끝나면 코루틴 목록에서 제거
        activeCoroutines.Remove(position);
        popupQueues.Remove(position);
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

    public void ShowDamage(Transform target, int damage, bool isCritical = false)
    {
        if (target != null)
            ShowText(target.position, damage.ToString(), isCritical ? Color.red : Color.white, isCritical);
    }

    public void ShowHeal(Transform target, int amount)
    {
        if (target != null)
            ShowText(target.position, $"+{amount}", Color.green);
    }

    public void ShowText(Transform target, string message, Color color)
    {
        if (target != null)
            ShowText(target.position, message, color);
    }
}