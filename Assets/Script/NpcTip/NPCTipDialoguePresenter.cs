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
            { "questName", questData != null ? questData.QuestTitle : "퀘스트" }
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
