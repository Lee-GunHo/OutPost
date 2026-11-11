using UnityEngine;

public enum MonsterLearningMode { Baseline, Training, Frozen }

// Independent experiment switches. Baseline/Frozen never update the stored policy.
public sealed class MonsterAIExperimentModel : MonoBehaviour
{
    public MonsterLearningMode learningMode = MonsterLearningMode.Training;
    public bool cooperationEnabled = true;
    [Tooltip("Allow wave monsters to fully flee (squad orders and individual chance). Default: regroup and resume combat.")]
    public bool waveSquadWithdrawal;
    [Tooltip("Wave monsters never train. Enable only to let them use an existing learned policy.")]
    public bool wavesUseLearnedPolicy;
    public bool writeCsv = true;
    public bool showOverlay;
    // Runtime-only demonstration override. Never writes synthetic learning experience.
    [System.NonSerialized] public bool maximumChargeDemo;
    public bool UsesMaximumChargeDemo(bool registeredTest) => maximumChargeDemo &&
        registeredTest && learningMode == MonsterLearningMode.Frozen;
    public float DemoAttackMultiplier(MonsterAttackKind kind) =>
        kind == MonsterAttackKind.Charge ? MonsterLearningModel.MaximumLearningMultiplier : 1f;
    [SerializeField] private int learnedVersion;
    [SerializeField] private int completedActions;
    [SerializeField] private string outputDirectory;
    public void SetDiagnostics(int version, int actions, string directory)
    {
        learnedVersion = version;
        completedActions = actions;
        outputDirectory = directory;
    }
}
