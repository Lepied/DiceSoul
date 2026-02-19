using UnityEngine;
using System.Collections.Generic;

public class GoblinShaman : Enemy
{
    //  버프를 적용한 아군과, 그 아군의 '원래' 타입을 저장
    private Dictionary<Enemy, EnemyType> buffedAllies = new Dictionary<Enemy, EnemyType>();

    /// <summary>
    /// [보스 기믹 1: 영혼 보호막]
    /// 웨이브 시작 시, 자신을 제외한 모든 'Goblin' 아군에게 'Spirit' 타입을 1턴간 부여
    /// </summary>
    public override void OnWaveStart(List<Enemy> allies)
    {
        base.OnWaveStart(allies);
        buffedAllies.Clear(); // 이전 버프 목록 초기화

        if (isDead) return;

        Debug.Log($"{enemyName}이(가) [영혼 보호막]을 시전!");

        foreach (Enemy ally in allies)
        {
            // 자신(주술사)이 아니고, 이름에 "Goblin"이 포함되면
            if (ally != this && ally.enemyName.Contains("고블린")) // (Goblin, GoblinArcher)
            {
                // 1. 원래 타입 저장
                buffedAllies.Add(ally, ally.enemyType);
                // 2. 타입 강제 변경
                ally.enemyType = EnemyType.Spirit;

                string text = LocalizationManager.Instance?.GetText("COMBAT_SOUL_SHIELD") ?? "영혼 보호막!";
                EffectManager.Instance.ShowText(ally.transform, text, Color.magenta);
                // (TODO: 1턴 버프 이펙트/UI 표시)
            }
        }
    }

    /// <summary>
    /// [보스 기믹 2: 버프 턴 관리]
    /// 플레이어가 주사위를 굴리면(즉, 다음 턴이 되면) 버프를 해제합니다.
    /// </summary>
    public override void OnPlayerRoll(List<int> diceValues)
    {
        base.OnPlayerRoll(diceValues);

        // (1턴 버프였으므로, 굴림 횟수 1 -> 2로 넘어갈 때 버프 해제)
        if (DiceController.Instance != null && DiceController.Instance.currentRollCount == 2)
        {
            if (buffedAllies.Count > 0)
            {
                Debug.Log($"{enemyName}의 [영혼 보호막] 효과가 사라집니다.");
                foreach (var pair in buffedAllies)
                {
                    Enemy ally = pair.Key;
                    EnemyType originalType = pair.Value;

                    if (ally != null && !ally.isDead)
                    {
                        // 원래 타입으로 복구
                        ally.enemyType = originalType;
                        Debug.Log($"... {ally.enemyName}이(가) {originalType} 타입으로 돌아왔습니다.");
                    }
                }
                buffedAllies.Clear(); // 버프 목록 비우기
            }
        }
    }

    public override string GetGimmickDescription()
    {
        return LocalizationManager.Instance.GetText("ENEMY_GIMMICK_GOBLINSHAMAN");
    }

    // (스탯은 인스펙터에서 설정: maxHP: 150, enemyType: Spirit, isBoss: true, cost: 55, minZone: 2)
}