using UnityEngine;

/// <summary>
/// (신규 파일) 아이스 골렘 (Zone 3: 얼음 동굴 일반 몹)
/// [기믹]: 빙결 장갑 - '모두 짝수', '모두 홀수' 족보 데미지 0 (면역)
/// (스탯은 인스펙터에서 설정: Armored 타입)
/// </summary>
public class IceGolem : Enemy
{
    public override int CalculateDamageTaken(AttackJokbo jokbo)
    {
        string desc = jokbo.Description;

        // 마법 족보(짝수/홀수)에 면역
        if (desc.Contains("짝수") || desc.Contains("홀수"))
        {
            EffectManager.Instance.ShowText(transform.position, "면역!", Color.grey);
            return 0;
        }

        // 그 외에는 부모(Armored) 로직 (총합/트리플 50%, 나머지 100%)
        return base.CalculateDamageTaken(jokbo);
    }
}