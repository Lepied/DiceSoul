using UnityEngine;

// 슬러지 
public class Sludge : Enemy
{
    [Header("슬러지 기믹")]
    [Tooltip("분열할 때 소환할 슬라임 프리팹")]
    public GameObject slimePrefab;
    public int splitCount = 2;

    // 기믹
    // 죽을 때 Slime으로 분열

    protected override void OnDeath()
    {
        // 분열 처리
        if (StageManager.Instance != null && WaveGenerator.Instance != null && slimePrefab != null)
        {
            string text = LocalizationManager.Instance?.GetText("COMBAT_SPLIT") ?? "분열!";
            EffectManager.Instance.ShowText(transform, text, new Color(0.5f, 0.8f, 0.3f));

            Vector3 leftPos = transform.position + Vector3.left * 1.2f;
            Vector3 rightPos = transform.position + Vector3.right * 1.2f;

            // 슬라임 소환
            for (int i = 0; i < splitCount; i++)
            {
                Vector3 spawnPos = i == 0 ? leftPos : rightPos;
                GameObject slimeObj = WaveGenerator.Instance.SpawnFromPool(slimePrefab, spawnPos, Quaternion.identity);

                if (slimeObj != null)
                {
                    Enemy slime = slimeObj.GetComponent<Enemy>();
                    if (slime != null && StageManager.Instance != null)
                    {
                        StageManager.Instance.activeEnemies.Add(slime);
                    }
                }
            }
        }

        base.OnDeath();
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_SLUDGE");
    }
}
