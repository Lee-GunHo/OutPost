using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 진행도 값을 임의로 넣어보고 조언 NPC가 어떤 팁을 고르는지 확인하는 에디터 전용 디버그 창.
/// Tools > NpcTip > Advice Tip Debugger. 플레이하지 않아도 동작(실제 선택 함수 AdviceTipSelector를 그대로 호출).
/// </summary>
public class AdviceTipDebugWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources/NpcTip";
    private const string AssetPath = AssetFolder + "/AdviceTipSet.asset";

    private static readonly string[] QuestIds =
    {
        AdviceTipDefaults.Quest1Id, AdviceTipDefaults.Quest2Id, AdviceTipDefaults.Quest3Id
    };

    private AdviceTipSet tipSet;
    private readonly QuestProgressState[] questStates = new QuestProgressState[3];
    private int deathCount;
    private int recentDeathCount;
    private int killCount;
    private int toolTier;
    private bool combatDiscovered;
    private bool shopDiscovered;
    private bool hintAvailable = true;
    private string hintText = "안전구역에서 북동쪽 가까운 곳";
    private Vector2 scroll;

    [MenuItem("Tools/NpcTip/Advice Tip Debugger")]
    public static void Open()
    {
        GetWindow<AdviceTipDebugWindow>("Advice Tip Debugger");
    }

    /// <summary>
    /// 인스펙터에서 팁/priority/수치를 조정할 수 있게 기본값이 채워진 AdviceTipSet 에셋을 생성.
    /// 이미 있으면 건드리지 않음(덮어쓰지 않음).
    /// </summary>
    [MenuItem("Tools/NpcTip/Create Advice Tip Set Asset")]
    public static void CreateAdviceTipSetAsset()
    {
        AdviceTipSet existing = AssetDatabase.LoadAssetAtPath<AdviceTipSet>(AssetPath);

        if (existing == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(AssetFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "NpcTip");
            }

            existing = CreateInstance<AdviceTipSet>();
            AssetDatabase.CreateAsset(existing, AssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("AdviceTipSet 에셋 생성 완료: " + AssetPath);
        }
        else
        {
            Debug.Log("AdviceTipSet 에셋이 이미 있어 그대로 둡니다: " + AssetPath);
        }

        Selection.activeObject = existing;
    }

    private void OnEnable()
    {
        if (tipSet == null)
        {
            tipSet = AssetDatabase.LoadAssetAtPath<AdviceTipSet>(AssetPath);
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        tipSet = (AdviceTipSet)EditorGUILayout.ObjectField("Tip Set", tipSet, typeof(AdviceTipSet), false);

        if (tipSet == null)
        {
            EditorGUILayout.HelpBox(
                "Tip Set이 비어 있어 코드 기본값(AdviceTipDefaults)으로 계산합니다. " +
                "인스펙터에서 조정하려면 Tools > NpcTip > Create Advice Tip Set Asset을 실행하세요.",
                MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("진행도 값", EditorStyles.boldLabel);

        for (int i = 0; i < QuestIds.Length; i++)
        {
            questStates[i] = (QuestProgressState)EditorGUILayout.EnumPopup(
                "퀘스트 " + QuestIds[i], questStates[i]);
        }

        deathCount = Mathf.Max(0, EditorGUILayout.IntField("사망 횟수 (누적)", deathCount));
        recentDeathCount = Mathf.Max(0, EditorGUILayout.IntField("RecentDeath", recentDeathCount));
        killCount = Mathf.Max(0, EditorGUILayout.IntField("처치 횟수", killCount));
        toolTier = Mathf.Max(0, EditorGUILayout.IntField("도구 등급 (0~4)", toolTier));
        combatDiscovered = EditorGUILayout.Toggle("전투 NPC 발견", combatDiscovered);
        shopDiscovered = EditorGUILayout.Toggle("상점 NPC 발견", shopDiscovered);
        hintAvailable = EditorGUILayout.Toggle("위치 힌트 계산 가능", hintAvailable);

        using (new EditorGUI.DisabledScope(!hintAvailable))
        {
            hintText = EditorGUILayout.TextField("위치 힌트 문구", hintText);
        }

        DrawPlayModeButtons();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("선택 결과", EditorStyles.boldLabel);

        IReadOnlyList<AdviceTip> tips = tipSet != null ? tipSet.Tips : AdviceTipDefaults.CreateDefaultTips();
        string defaultText = tipSet != null ? tipSet.DefaultText : AdviceTipDefaults.DefaultText;
        ProgressSnapshot snapshot = BuildSnapshot();
        TipSelectionResult result = AdviceTipSelector.Select(snapshot, tips, defaultText);

        EditorGUILayout.LabelField("팁", result.TipId);
        EditorGUILayout.LabelField("priority", result.Priority.ToString());
        EditorGUILayout.LabelField("이유", result.Reason, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("RecentDeath 초기화",
            AdviceTipSelector.ShouldResetRecentDeath(
                result, tipSet != null && tipSet.ResetRecentDeathOnAnyAdviceTalk) ? "예" : "아니오");

        string shownText = (result.Text ?? string.Empty).Replace(
            AdviceTipSelector.CombatNpcHintToken, snapshot.CombatNpcHint ?? string.Empty);
        EditorGUILayout.HelpBox(shownText, MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("전체 팁 (O = 조건 만족)", EditorStyles.boldLabel);

        foreach (AdviceTip tip in tips)
        {
            if (tip == null)
            {
                continue;
            }

            bool satisfied = AdviceTipSelector.AreAllSatisfied(tip.conditions, snapshot);
            EditorGUILayout.LabelField(
                (satisfied ? "O  " : "-  ") + tip.tipId,
                "priority " + tip.priority + (tip == result.Tip ? "   ← 선택됨" : string.Empty));
        }

        EditorGUILayout.EndScrollView();
    }

    private ProgressSnapshot BuildSnapshot()
    {
        ProgressSnapshot snapshot = new ProgressSnapshot
        {
            DeathCount = deathCount,
            RecentDeathCount = recentDeathCount,
            KillCount = killCount,
            ToolTier = toolTier,
            CombatNpcHint = hintAvailable ? hintText : null
        };

        for (int i = 0; i < QuestIds.Length; i++)
        {
            snapshot.QuestStates[QuestIds[i]] = questStates[i];
        }

        if (combatDiscovered)
        {
            snapshot.DiscoveredNpcIds.Add(NPCRole.Combat.ToString());
        }

        if (shopDiscovered)
        {
            snapshot.DiscoveredNpcIds.Add(NPCRole.Shop.ToString());
        }

        return snapshot;
    }

    // 플레이 중에는 실제 게임 값과 주고받을 수 있음 (퀘스트 상태는 저장+인벤토리에서 계산되므로 읽기만 가능)
    private void DrawPlayModeButtons()
    {
        using (new EditorGUI.DisabledScope(!Application.isPlaying || ProgressTracker.Instance == null))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("플레이 중인 실제 값 불러오기"))
            {
                LoadFromGame();
            }

            if (GUILayout.Button("사망/발견 값을 게임에 적용"))
            {
                ProgressTracker.Instance.EditorSetDebugValues(
                    deathCount, recentDeathCount, combatDiscovered, shopDiscovered);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void LoadFromGame()
    {
        ProgressSnapshot snapshot = ProgressTracker.Instance.BuildSnapshot(
            tipSet != null ? tipSet.ToolTierItemIds : AdviceTipDefaults.CreateDefaultToolTierItemIds());

        for (int i = 0; i < QuestIds.Length; i++)
        {
            questStates[i] = snapshot.GetQuestState(QuestIds[i]);
        }

        deathCount = snapshot.DeathCount;
        recentDeathCount = snapshot.RecentDeathCount;
        killCount = snapshot.KillCount;
        toolTier = snapshot.ToolTier;
        combatDiscovered = snapshot.IsNpcDiscovered(NPCRole.Combat.ToString());
        shopDiscovered = snapshot.IsNpcDiscovered(NPCRole.Shop.ToString());
    }
}
