using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

// 유물 디버그 컨트롤러
// - 키보드 단축키로 빠른 테스트
// - ~ 키로 콘솔창 열어서 명령어 입력
// 
// [사용법]
// 1. 빈 GameObject에 이 스크립트 연결
// 2. 게임 실행
// 3. ~ 키로 콘솔 사용

public class RelicDebugController : MonoBehaviour
{
    public static RelicDebugController Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private bool enableInBuild = false; // 빌드에서도 활성화?

    // 콘솔 UI (자동 생성)
    private bool showConsole = false;
    private string inputText = "";
    private List<string> logHistory = new List<string>();
    private Vector2 scrollPosition;
    private const int MAX_LOG_LINES = 50;

    // 명령어 히스토리
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 빌드에서 비활성화
#if !UNITY_EDITOR
        if (!enableInBuild)
        {
            enabled = false;
            return;
        }
#endif

        Log("<color=cyan>=== 유물 디버그 콘솔 ===</color>");
        Log("<color=yellow>~ 키로 콘솔 열기 | help 입력하여 명령어 확인</color>");
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 콘솔 토글 (~ 또는 ` 키)
        if (keyboard.backquoteKey.wasPressedThisFrame)
        {
            showConsole = !showConsole;
            if (showConsole) inputText = "";
        }

        // 콘솔이 열려있으면 단축키 무시
        if (showConsole) return;

        // ===== 단축키 =====

        // F1: 상태 출력
        if (keyboard.f1Key.wasPressedThisFrame)
        {
            ExecuteCommand("status");
        }

        // F2: 생존 프리셋
        if (keyboard.f2Key.wasPressedThisFrame)
        {
            ExecuteCommand("preset survival");
        }

        // F3: 공격 프리셋
        if (keyboard.f3Key.wasPressedThisFrame)
        {
            ExecuteCommand("preset damage");
        }

        // F4: 주사위 프리셋
        if (keyboard.f4Key.wasPressedThisFrame)
        {
            ExecuteCommand("preset dice");
        }
    }

    void OnGUI()
    {
        if (!showConsole) return;

        // 배경
        GUI.Box(new Rect(10, 10, 600, 400), "");

        // 로그 영역
        GUILayout.BeginArea(new Rect(20, 20, 580, 300));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(570), GUILayout.Height(290));

        foreach (string log in logHistory)
        {
            GUILayout.Label(log);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // 입력 영역
        GUI.SetNextControlName("ConsoleInput");
        inputText = GUI.TextField(new Rect(20, 330, 500, 25), inputText);

        // 자동 포커스
        if (showConsole)
        {
            GUI.FocusControl("ConsoleInput");
        }

        // 버튼
        if (GUI.Button(new Rect(530, 330, 60, 25), "실행") ||
            (Event.current.isKey && Event.current.keyCode == KeyCode.Return))
        {
            if (!string.IsNullOrEmpty(inputText))
            {
                ExecuteCommand(inputText);
                commandHistory.Add(inputText);
                historyIndex = commandHistory.Count;
                inputText = "";
            }
            Event.current.Use();
        }

        // 히스토리 네비게이션 (위/아래 화살표)
        if (Event.current.isKey)
        {
            if (Event.current.keyCode == KeyCode.UpArrow && commandHistory.Count > 0)
            {
                historyIndex = Mathf.Max(0, historyIndex - 1);
                inputText = commandHistory[historyIndex];
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.DownArrow && commandHistory.Count > 0)
            {
                historyIndex = Mathf.Min(commandHistory.Count, historyIndex + 1);
                inputText = historyIndex < commandHistory.Count ? commandHistory[historyIndex] : "";
                Event.current.Use();
            }
        }

        // 단축키 안내
        GUI.Label(new Rect(20, 360, 580, 40),
            "<color=gray>F1:상태 | F2~F4:프리셋",
            new GUIStyle(GUI.skin.label) { richText = true });
    }

    // ===== 명령어 실행 =====

    public void ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;

        Log($"<color=white>> {command}</color>");

        string[] parts = command.Trim().ToLower().Split(' ');
        string cmd = parts[0];
        string arg1 = parts.Length > 1 ? parts[1] : "";
        string arg2 = parts.Length > 2 ? parts[2] : "";

        switch (cmd)
        {
            // === 도움말 ===
            case "help":
            case "?":
                ShowHelp();
                break;

            // === 유물 관리 ===
            case "add":
                if (string.IsNullOrEmpty(arg1))
                    Log("<color=red>사용법: add [유물ID]</color>");
                else
                    AddRelic(arg1.ToUpper());
                break;

            case "remove":
            case "rm":
                if (string.IsNullOrEmpty(arg1))
                    Log("<color=red>사용법: remove [유물ID]</color>");
                else
                    RemoveRelic(arg1.ToUpper());
                break;

            case "clear":
                ClearAllRelics();
                break;

            case "list":
                ListOwnedRelics();
                break;

            case "all":
                ListAllRelics();
                break;

            case "find":
            case "search":
                if (string.IsNullOrEmpty(arg1))
                    Log("<color=red>사용법: find [검색어]</color>");
                else
                    FindRelics(arg1);
                break;

            // === 프리셋 ===
            case "preset":
                ApplyPreset(arg1);
                break;

            // === 치트 ===
            case "heal":
            case "hp":
                int healAmount = 999;
                if (!string.IsNullOrEmpty(arg1)) int.TryParse(arg1, out healAmount);
                Heal(healAmount);
                break;

            case "damage":
            case "dmg":
                int dmgAmount = 5;
                if (!string.IsNullOrEmpty(arg1)) int.TryParse(arg1, out dmgAmount);
                TakeDamage(dmgAmount);
                break;

            case "gold":
            case "g":
                int goldAmount = 500;
                if (!string.IsNullOrEmpty(arg1)) int.TryParse(arg1, out goldAmount);
                AddGold(goldAmount);
                break;

            case "roll":
            case "rolls":
                int rollAmount = 3;
                if (!string.IsNullOrEmpty(arg1)) int.TryParse(arg1, out rollAmount);
                AddRolls(rollAmount);
                break;

            case "kill":
                KillEnemy();
                break;

            case "clearwave":
            case "nextwave":
            case "skipwave":
                ClearWave();
                break;

            case "status":
            case "stat":
                ShowStatus();
                break;

            case "cls":
            case "clear_log":
                logHistory.Clear();
                break;

            case "victory":
            case "win":
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ProcessVictory();
                    Log("<color=yellow>🎉 승리 처리!</color>");
                }
                else
                {
                    Log("<color=red>GameManager를 찾을 수 없습니다.</color>");
                }
                break;

            default:
                // 유물 ID로 직접 추가 시도
                if (cmd.StartsWith("rlc_"))
                {
                    AddRelic(cmd.ToUpper());
                }
                else
                {
                    Log($"<color=red>알 수 없는 명령어: {cmd}</color>");
                    Log("<color=yellow>help 입력하여 명령어 확인</color>");
                }
                break;
        }
    }

    // ===== 명령어 구현 =====

    private void ShowHelp()
    {
        Log("<color=cyan>=== 명령어 목록 ===</color>");
        Log("<color=yellow>[유물]</color>");
        Log("  add [ID]     - 유물 추가 (예: add rlc_vampire_fang)");
        Log("  remove [ID]  - 유물 제거");
        Log("  clear        - 모든 유물 제거");
        Log("  list         - 보유 유물 목록");
        Log("  all          - 전체 유물 ID 목록");
        Log("  find [검색어] - 유물 검색");
        Log("");
        Log("<color=yellow>[프리셋]</color>");
        Log("  preset survival - 생존 유물 세트");
        Log("  preset damage   - 공격 유물 세트");
        Log("  preset dice     - 주사위 조작 세트");
        Log("  preset economy  - 경제 유물 세트");
        Log("");
        Log("<color=yellow>[치트]</color>");
        Log("  heal [양]    - 체력 회복 (기본 999)");
        Log("  damage [양]  - 데미지 받기");
        Log("  gold [양]    - 골드 추가 (기본 500)");
        Log("  roll [양]    - 굴림 횟수 추가");
        Log("  kill         - 현재 적 처치");
        Log("  clearwave    - 웨이브 클리어 (다음 웨이브로)");
        Log("  victory      - 즉시 승리");
        Log("  status       - 게임 상태 출력");
    }

    private void AddRelic(string relicID)
    {
        if (!relicID.StartsWith("RLC_")) relicID = "RLC_" + relicID;

        if (RelicDB.Instance == null)
        {
            Log("<color=red>RelicDB가 없습니다!</color>");
            return;
        }

        var relic = RelicDB.Instance.GetRelicByID(relicID);
        if (relic == null)
        {
            Log($"<color=red>유물을 찾을 수 없음: {relicID}</color>");
            FindRelics(relicID.Replace("RLC_", ""));
            return;
        }

        if (GameManager.Instance == null)
        {
            Log("<color=red>GameManager가 없습니다!</color>");
            return;
        }

        int currentCount = GameManager.Instance.activeRelics.Count(r => r.RelicID == relicID);
        if (relic.MaxCount > 0 && currentCount >= relic.MaxCount)
        {
            Log($"<color=yellow>[{relic.Name}] 최대 보유 개수({relic.MaxCount}) 도달</color>");
            return;
        }

        GameManager.Instance.AddRelic(relic);
        Log($"<color=green>[+] {relic.Name} 추가됨</color> ({currentCount + 1}/{(relic.MaxCount > 0 ? relic.MaxCount.ToString() : "∞")})");
    }

    private void RemoveRelic(string relicID)
    {
        if (!relicID.StartsWith("RLC_")) relicID = "RLC_" + relicID;

        if (GameManager.Instance == null) return;

        var relic = GameManager.Instance.activeRelics.FirstOrDefault(r => r.RelicID == relicID);
        if (relic != null)
        {
            GameManager.Instance.activeRelics.Remove(relic);
            UpdateRelicUI();
            Log($"<color=red>[-] {relic.Name} 제거됨</color>");
        }
        else
        {
            Log($"<color=yellow>보유하지 않은 유물: {relicID}</color>");
        }
    }

    private void ClearAllRelics()
    {
        if (GameManager.Instance == null) return;
        int count = GameManager.Instance.activeRelics.Count;
        GameManager.Instance.activeRelics.Clear();
        UpdateRelicUI();
        Log($"<color=yellow>모든 유물 {count}개 제거됨</color>");
    }

    /// <summary>
    /// 유물 UI 업데이트
    /// </summary>
    private void UpdateRelicUI()
    {
        if (UIManager.Instance != null && GameManager.Instance != null)
        {
            UIManager.Instance.UpdateRelicPanel(GameManager.Instance.activeRelics);
        }
    }

    private void ListOwnedRelics()
    {
        if (GameManager.Instance == null) return;

        var relics = GameManager.Instance.activeRelics;
        if (relics.Count == 0)
        {
            Log("<color=gray>보유 유물 없음</color>");
            return;
        }

        Log($"<color=cyan>=== 보유 유물 ({relics.Count}개) ===</color>");
        var grouped = relics.GroupBy(r => r.RelicID).OrderBy(g => g.First().Name);
        foreach (var g in grouped)
        {
            Log($"  {g.First().Name} x{g.Count()} <color=gray>({g.Key})</color>");
        }
    }

    private void ListAllRelics()
    {
        if (RelicDB.Instance == null) return;

        var field = typeof(RelicDB).GetField("allRelics",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return;

        var allRelics = field.GetValue(RelicDB.Instance) as Dictionary<string, Relic>;
        if (allRelics == null) return;

        Log($"<color=cyan>=== 전체 유물 ({allRelics.Count}개) ===</color>");
        foreach (var kvp in allRelics.OrderBy(x => x.Value.Name))
        {
            Log($"  {kvp.Value.Name} <color=gray>({kvp.Key})</color>");
        }
    }

    private void FindRelics(string query)
    {
        if (RelicDB.Instance == null) return;

        var field = typeof(RelicDB).GetField("allRelics",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return;

        var allRelics = field.GetValue(RelicDB.Instance) as Dictionary<string, Relic>;
        if (allRelics == null) return;

        query = query.ToLower();
        var matches = allRelics.Where(kvp =>
            kvp.Key.ToLower().Contains(query) ||
            kvp.Value.Name.ToLower().Contains(query)).ToList();

        if (matches.Count == 0)
        {
            Log($"<color=gray>'{query}' 검색 결과 없음</color>");
            return;
        }

        Log($"<color=cyan>=== '{query}' 검색 결과 ({matches.Count}개) ===</color>");
        foreach (var kvp in matches.OrderBy(x => x.Value.Name))
        {
            Log($"  {kvp.Value.Name} <color=gray>({kvp.Key})</color>");
        }
    }

    private void ApplyPreset(string preset)
    {
        ClearAllRelics();

        switch (preset)
        {
            case "survival":
            case "생존":
                AddRelic("RLC_VAMPIRE_FANG");
                AddRelic("RLC_PHOENIX_FEATHER");
                AddRelic("RLC_SMALL_SHIELD");
                AddRelic("RLC_TOUGH_ARMOR");
                AddRelic("RLC_REGEN_BRACELET");
                Log("<color=cyan>[프리셋] 생존 세트 적용됨</color>");
                break;

            case "damage":
            case "공격":
                AddRelic("RLC_WHETSTONE");
                AddRelic("RLC_GLASS_CANNON");
                AddRelic("RLC_BLOODLUST");
                AddRelic("RLC_HOURGLASS");
                AddRelic("RLC_SWIFT_HANDS");
                Log("<color=cyan>[프리셋] 공격 세트 적용됨</color>");
                break;

            case "dice":
            case "주사위":
                AddRelic("RLC_ALCHEMY");
                AddRelic("RLC_IRON_DICE");
                AddRelic("RLC_QUICK_RELOAD");
                AddRelic("RLC_LUCKY_CLOVER");
                AddRelic("RLC_COUNTERWEIGHT");
                Log("<color=cyan>[프리셋] 주사위 조작 세트 적용됨</color>");
                break;

            case "economy":
            case "경제":
                AddRelic("RLC_GOLD_DICE");
                AddRelic("RLC_LUCKY_CHARM");
                AddRelic("RLC_MERCHANT_CARD");
                AddRelic("RLC_SPRING");
                AddRelic("RLC_PLUTOCRACY");
                Log("<color=cyan>[프리셋] 경제 세트 적용됨</color>");
                break;

            default:
                Log("<color=red>프리셋: survival, damage, dice, economy</color>");
                break;
        }
    }

    private void Heal(int amount)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.HealPlayer(amount);
        Log($"<color=green>[치트] 체력 +{amount}</color>");
    }

    private void TakeDamage(int amount)
    {
        if (GameManager.Instance == null) return;

        var ctx = new DamageContext
        {
            OriginalDamage = amount,
            FinalDamage = amount,
            Source = "Debug"
        };
        GameEvents.RaiseBeforePlayerDamaged(ctx);

        if (!ctx.Cancelled)
        {
            GameManager.Instance.PlayerHealth -= ctx.FinalDamage;
            Log($"<color=red>[치트] 데미지 {ctx.FinalDamage}</color>");
        }
        else
        {
            Log("<color=yellow>[유물] 데미지 무효화!</color>");
        }
    }

    private void AddGold(int amount)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.AddGold(amount, GoldSource.Bonus);
        Log($"<color=yellow>[치트] 골드 +{amount}</color>");
    }

    private void AddRolls(int amount)
    {
        var dc = FindAnyObjectByType<DiceController>();
        if (dc != null)
        {
            dc.ApplyRollBonus(amount);
            Log($"<color=cyan>[치트] 굴림 횟수 +{amount}</color>");
        }
    }

    private void KillEnemy()
    {
        if (StageManager.Instance == null) return;

        var field = typeof(StageManager).GetField("activeEnemies",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return;

        var enemies = field.GetValue(StageManager.Instance) as List<Enemy>;
        if (enemies == null) return;

        var enemy = enemies.FirstOrDefault(e => e != null && !e.isDead);
        if (enemy != null)
        {
            // 직접 체력을 0으로 설정 (리플렉션)
            var hpField = typeof(Enemy).GetProperty("currentHP",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (hpField != null && hpField.CanWrite)
            {
                hpField.SetValue(enemy, 0);
            }
            else
            {
                // currentHP가 protected set이면 필드로 직접 접근
                var backingField = typeof(Enemy).GetField("<currentHP>k__BackingField",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (backingField != null)
                {
                    backingField.SetValue(enemy, 0);
                }
            }
            Log("<color=red>[치트] 현재 적 처치</color>");
        }
        else
        {
            Log("<color=gray>처치할 적이 없습니다</color>");
        }
    }

    private void ClearWave()
    {
        if (GameManager.Instance == null)
        {
            Log("<color=red>GameManager를 찾을 수 없습니다.</color>");
            return;
        }

        // 웨이브 클리어 처리 (굴림 보너스 0)
        GameManager.Instance.ProcessWaveClear(true, 0);
        Log("<color=green>[치트] 웨이브 클리어! 다음 웨이브로 진행</color>");
    }

    private void ShowStatus()
    {
        if (GameManager.Instance == null)
        {
            Log("<color=red>GameManager 없음</color>");
            return;
        }

        var gm = GameManager.Instance;
        var dc = FindAnyObjectByType<DiceController>();

        Log("<color=cyan>=== 게임 상태 ===</color>");
        Log($"  체력: {gm.PlayerHealth}/{gm.MaxPlayerHealth}");
        Log($"  골드: {gm.CurrentGold}");
        if (dc != null)
            Log($"  굴림: {dc.currentRollCount}/{dc.maxRolls}");
        Log($"  유물: {gm.activeRelics.Count}개");
        Log($"  존/웨이브: {gm.CurrentZone}-{gm.CurrentWave}");
    }

    private void Log(string message)
    {
        logHistory.Add(message);
        if (logHistory.Count > MAX_LOG_LINES)
            logHistory.RemoveAt(0);

        scrollPosition = new Vector2(0, float.MaxValue); // 자동 스크롤

        // Unity 콘솔에도 출력
        Debug.Log($"[RelicDebug] {message.Replace("<color=", "").Replace("</color>", "").Replace(">", "")}");
    }
}
