using UnityEngine;

public enum MonsterLearningMode { Baseline, Training, Frozen }

// Independent experiment switches. Baseline/Frozen never update the stored policy.
public sealed class MonsterAIExperimentModel : MonoBehaviour
{
    public MonsterLearningMode learningMode = MonsterLearningMode.Training;
    public bool cooperationEnabled = true;
    [Tooltip("Wave monsters never train. Enable only to let them use an existing learned policy.")]
    public bool wavesUseLearnedPolicy;
    public bool writeCsv = true;
    public bool showOverlay;
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
