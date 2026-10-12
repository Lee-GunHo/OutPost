using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// NPC 팁 대화의 중앙 코디네이터. 진단(IDifficultyDiagnoser) → 대사 선택(NPCTipDialogueSet) →
/// FSM(NPCDialogueStateManager)으로 노드 진행 → NPCInteractionUI에 표시 → PlayLogManager에 기록.
/// </summary>
[RequireComponent(typeof(NPCDialogueStateManager))]
public class NPCTipDialoguePresenter : MonoBehaviour
{
    public static NPCTipDialoguePresenter Instance { get; private set; }

    [Header("진단/대사 데이터")]
    [SerializeField] private DifficultyRuleConfig ruleConfig;
    [SerializeField] private NPCTipDialogueSet dialogueSet;

    [Header("조건 기반 조언 팁 (조언 NPC 전용)")]
    [Tooltip("비워두면 Resources/NpcTip/AdviceTipSet을 찾고, 그것도 없으면 코드 기본값으로 동작")]
    [SerializeField] private AdviceTipSet adviceTipSet;
    [Tooltip("조건을 만족하는 조언 팁이 없을 때, 기본 대사 대신 기존 난관 진단 대사(dialogueSet)를 보여줌")]
    [SerializeField] private bool useDiagnosisDialogueAsDefault = true;

    [Header("진단 방식 (4단계)")]
    [SerializeField] private DiagnosisMode diagnosisMode = DiagnosisMode.Rule;
    [Tooltip("3단계 산출물(difficulty_mlp.onnx)을 Assets에 넣으면 자동으로 임포트되는 모델 자산")]
    [SerializeField] private ModelAsset mlModelAsset;
    [Tooltip("3단계 산출물 preprocessing.json")]
    [SerializeField] private TextAsset mlMetadataJson;
    [Tooltip("ML 진단 확신도가 이 값 미만이면 규칙 기반 결과로 대체")]
    [SerializeField, Range(0f, 1f)] private float mlMinConfidence = 0.5f;

    private NPCDialogueStateManager stateManager;
    private RuleBasedDiagnoser ruleDiagnoser;
    private MLDiagnoser mlDiagnoser;

    // AB 모드일 때 세션 시작 시 한 번 뽑아서 고정하는 실제 적용 모드(Rule 또는 ML).
    private DiagnosisMode effectiveMode;

    private readonly Dictionary<NPCPresenter, Dictionary<DifficultyType, string>> lastEntryIdByNpc =
        new Dictionary<NPCPresenter, Dictionary<DifficultyType, string>>();

    private NPCPresenter currentNpc;
    private PlayerPresenter currentPlayer;
    private PlayLogEntry currentSnapshot;
    private DifficultyDiagnosis currentDiagnosis;
    private NPCTipEntry currentEntry;
    private NPCTipNode currentNode;
    private string currentCombatNpcHint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        stateManager = GetComponent<NPCDialogueStateManager>();
        ruleDiagnoser = new RuleBasedDiagnoser(ruleConfig);
        mlDiagnoser = new MLDiagnoser(mlModelAsset, mlMetadataJson, ruleDiagnoser, mlMinConfidence);
    }

    private void Start()
    {
        effectiveMode = diagnosisMode;

        if (diagnosisMode == DiagnosisMode.AB)
        {
            effectiveMode = UnityEngine.Random.value < 0.5f ? DiagnosisMode.Rule : DiagnosisMode.ML;
            PlayLogManager.Instance?.RecordAbTestAssignment(effectiveMode.ToString());
        }
    }

    private void OnDestroy()
    {
        mlDiagnoser?.Dispose();
    }

    /// <summary>
    /// NPCInteractionUI의 "대화" 버튼에서 호출. 진단부터 첫 대사 표시까지 한 번에 진행.
    /// </summary>
    public void StartTipDialogue(NPCPresenter npc, PlayerPresenter player)
    {
        currentNpc = npc;
        currentPlayer = player;

        if (NPCInteractionUI.Instance == null)
        {
            return;
        }

        // 조언 NPC는 진행도 조건에 맞는 팁을 먼저 시도. 맞는 팁이 없으면 아래 기존 진단 대사로 이어짐.
        if (npc != null && npc.GetRole() == NPCRole.Advice && TryStartAdviceTip())
        {
            return;
        }

        if (PlayLogManager.Instance == null || dialogueSet == null)
        {
            NPCInteractionUI.Instance.ShowFallbackDialogue();
            return;
        }

        currentSnapshot = PlayLogManager.Instance.BuildCurrentSnapshot("TipDialogue");

        if (currentSnapshot == null)
        {
            NPCInteractionUI.Instance.ShowFallbackDialogue();
            return;
        }

        currentDiagnosis = RunDiagnosis(currentSnapshot);

        string lastEntryId = GetLastEntryId(npc, currentDiagnosis.Type);
        currentEntry = dialogueSet.PickEntry(currentDiagnosis.Type, lastEntryId);

        if (currentEntry == null || currentEntry.GetStartNode() == null)
        {
            NPCInteractionUI.Instance.ShowFallbackDialogue();
            return;
        }

        SetLastEntryId(npc, currentDiagnosis.Type, currentEntry.entryId);

        ShowNode(currentEntry.GetStartNode());
        LogCurrentStep(null);

        TipFollowUpManager.Instance?.OnTipGiven(
            currentDiagnosis.Type, currentEntry.entryId, currentSnapshot);
    }

    /// <summary>
    /// 진행도 스냅샷으로 조언 팁을 골라 한 줄 대사로 보여줌.
    /// 기본 대사가 선택됐고 기존 진단 대사로 넘기도록 설정돼 있으면 false를 반환.
    /// </summary>
    private bool TryStartAdviceTip()
    {
        AdviceTipSet tipSet = ResolveAdviceTipSet();
        ProgressTracker tracker = ProgressTracker.GetOrCreate();

        ProgressSnapshot progress = tracker.BuildSnapshot(tipSet.ToolTierItemIds);
        progress.CombatNpcHint = TryBuildCombatNpcHint(tipSet.CombatNpcHint);

        TipSelectionResult result = AdviceTipSelector.Select(progress, tipSet.Tips, tipSet.DefaultText);

#if UNITY_EDITOR
        Debug.Log($"[AdviceTip] 선택된 팁: {result.TipId} / priority: {result.Priority} / 이유: {result.Reason}");
#endif

        bool canUseDiagnosisDialogue = PlayLogManager.Instance != null && dialogueSet != null;

        if (result.IsDefault && useDiagnosisDialogueAsDefault && canUseDiagnosisDialogue)
        {
            return false;
        }

        if (string.IsNullOrEmpty(result.Text))
        {
            return false;
        }

        currentSnapshot = PlayLogManager.Instance != null
            ? PlayLogManager.Instance.BuildCurrentSnapshot("TipDialogue")
            : null;
        currentCombatNpcHint = progress.CombatNpcHint;

        // 기존 대화 FSM/UI를 그대로 쓰기 위해 선택지 없는 한 줄짜리 대화로 감쌈
        currentEntry = new NPCTipEntry
        {
            entryId = result.TipId,
            nodes = new[] { new NPCTipNode { nodeId = "advice", textTemplate = result.Text } }
        };

        ShowNode(currentEntry.GetStartNode());

        PlayLogManager.Instance?.RecordTipDialogue("AdviceTip", 1f, currentEntry.entryId, currentNode.nodeId, null);

        if (AdviceTipSelector.ShouldResetRecentDeath(result, tipSet.ResetRecentDeathOnAnyAdviceTalk))
        {
            tracker.ResetRecentDeath();
        }

        return true;
    }

    private AdviceTipSet ResolveAdviceTipSet()
    {
        if (adviceTipSet == null)
        {
            adviceTipSet = Resources.Load<AdviceTipSet>(AdviceTipSet.ResourcesPath);
        }

        if (adviceTipSet == null)
        {
            adviceTipSet = ScriptableObject.CreateInstance<AdviceTipSet>();
        }

        return adviceTipSet;
    }

    /// <summary>
    /// 전투 NPC의 위치를 안전구역 기준 "방향 + 거리 단계" 문구로 변환. 구할 수 없으면 오류 없이 null.
    /// </summary>
    private static string TryBuildCombatNpcHint(CombatNpcHintSettings settings)
    {
        try
        {
            SeedMapModel seedMapModel = FindFirstObjectByType<SeedMapModel>();

            if (seedMapModel == null || seedMapModel.CellSize <= 0f)
            {
                return null;
            }

            foreach (NPCPresenter npc in FindObjectsByType<NPCPresenter>(FindObjectsSortMode.None))
            {
                if (npc.GetRole() != NPCRole.Combat)
                {
                    continue;
                }

                float cellSize = seedMapModel.CellSize;

                return CombatNpcHintBuilder.TryBuild(
                    seedMapModel.PlayerStartPosition / cellSize,
                    npc.transform.position / cellSize,
                    settings,
                    out string hint)
                    ? hint
                    : null;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("전투 NPC 위치 힌트 계산 실패, 힌트 없이 진행: " + exception.Message);
        }

        return null;
    }

    /// <summary>
    /// 선택지 버튼 클릭 시 NPCInteractionUI에서 호출.
    /// </summary>
    public void SelectChoice(int choiceIndex)
    {
        if (currentNode == null || currentNode.choices == null ||
            choiceIndex < 0 || choiceIndex >= currentNode.choices.Length)
        {
            return;
        }

        NPCTipChoice choice = currentNode.choices[choiceIndex];

        LogCurrentStep(choice.choiceText);

        NPCTipNode nextNode = currentEntry.FindNode(choice.nextNodeId);

        if (nextNode == null)
        {
            stateManager.ChangeState(stateManager.EndedState);
            return;
        }

        ShowNode(nextNode);
    }

    /// <summary>
    /// NPCInteractionUI.Close()에서 호출. 대화창을 닫으면 다음 방문을 위해 상태를 초기화.
    /// </summary>
    public void EndConversation()
    {
        currentNpc = null;
        currentPlayer = null;
        currentEntry = null;
        currentNode = null;

        stateManager.ChangeState(stateManager.IdleState);
    }

    /// <summary>
    /// 인스펙터에서 고른 DiagnosisMode에 따라 실제로 쓸 진단기를 선택.
    /// Compare 모드는 둘 다 돌려서 로그로 비교하고, 대사는 ML 결과로 보여줌.
    /// </summary>
    private DifficultyDiagnosis RunDiagnosis(PlayLogEntry snapshot)
    {
        // AB 모드는 Start()에서 이미 Rule/ML 중 하나로 고정됐으므로, 여기서는 effectiveMode만 봄.
        switch (effectiveMode)
        {
            case DiagnosisMode.ML:
                return mlDiagnoser.Diagnose(snapshot);

            case DiagnosisMode.Compare:
            {
                DifficultyDiagnosis ruleResult = ruleDiagnoser.Diagnose(snapshot);
                DifficultyDiagnosis mlResult = mlDiagnoser.Diagnose(snapshot);

                PlayLogManager.Instance?.RecordDiagnosisComparison(
                    ruleResult.Type.ToString(), ruleResult.Confidence,
                    mlResult.Type.ToString(), mlResult.Confidence);

                return mlResult;
            }

            case DiagnosisMode.Rule:
            case DiagnosisMode.AB: // Start()에서 실패해 기본값이 그대로 남아있는 방어적 경우
            default:
                return ruleDiagnoser.Diagnose(snapshot);
        }
    }

    private void ShowNode(NPCTipNode node)
    {
        currentNode = node;
        stateManager.ChangeState(stateManager.ShowingLineState);
    }

    // ---- FSM 상태 콜백 (NPCDialogueXxxState.Enter()에서 호출) ----

    public void EnterIdle()
    {
        NPCInteractionUI.Instance?.HideTipChoices();
    }

    public void EnterShowingLine()
    {
        if (currentNode == null || currentNpc == null)
        {
            return;
        }

        string text = NPCTipTextFormatter.Format(currentNode.textTemplate, BuildVariables());

        NPCInteractionUI.Instance?.ShowTipLine(currentNpc.GetNPCName(), text, OnTypingComplete);
    }

    public void EnterWaitingChoice()
    {
        if (currentNode?.choices == null)
        {
            return;
        }

        string[] labels = new string[currentNode.choices.Length];

        for (int i = 0; i < currentNode.choices.Length; i++)
        {
            labels[i] = currentNode.choices[i].choiceText;
        }

        NPCInteractionUI.Instance?.ShowTipChoices(labels, SelectChoice);
    }

    public void EnterEnded()
    {
        NPCInteractionUI.Instance?.HideTipChoices();
    }

    private void OnTypingComplete()
    {
        bool hasChoices = currentNode?.choices != null && currentNode.choices.Length > 0;

        stateManager.ChangeState(hasChoices ? stateManager.WaitingChoiceState : stateManager.EndedState);
    }

    private void LogCurrentStep(string chosenChoiceText)
    {
        if (PlayLogManager.Instance == null || currentEntry == null || currentNode == null)
        {
            return;
        }

        PlayLogManager.Instance.RecordTipDialogue(
            currentDiagnosis.Type.ToString(),
            currentDiagnosis.Confidence,
            currentEntry.entryId,
            currentNode.nodeId,
            chosenChoiceText
        );
    }

    private Dictionary<string, string> BuildVariables()
    {
        QuestData questData = currentNpc != null ? currentNpc.GetQuestData() : null;

        return new Dictionary<string, string>
        {
            { "toolGrade", currentSnapshot != null ? currentSnapshot.ToolGrade : "Normal" },
            { "deathCount", currentSnapshot != null ? currentSnapshot.CumulativeDeathCount.ToString() : "0" },
            { "questName", questData != null ? questData.QuestTitle : "퀘스트" },
            { "combatNpcHint", currentCombatNpcHint ?? string.Empty }
        };
    }

    private string GetLastEntryId(NPCPresenter npc, DifficultyType type)
    {
        if (!lastEntryIdByNpc.TryGetValue(npc, out Dictionary<DifficultyType, string> perType))
        {
            return null;
        }

        return perType.TryGetValue(type, out string entryId) ? entryId : null;
    }

    private void SetLastEntryId(NPCPresenter npc, DifficultyType type, string entryId)
    {
        if (!lastEntryIdByNpc.TryGetValue(npc, out Dictionary<DifficultyType, string> perType))
        {
            perType = new Dictionary<DifficultyType, string>();
            lastEntryIdByNpc[npc] = perType;
        }

        perType[type] = entryId;
    }
}
