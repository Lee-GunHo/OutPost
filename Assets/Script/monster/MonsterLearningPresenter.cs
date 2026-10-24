using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MonsterAIExperimentModel), typeof(MonsterLearningView))]
public sealed class MonsterLearningPresenter : MonoBehaviour
{
    public const string FileName = "monster_learning.json";
    public static MonsterLearningPresenter Instance { get; private set; }
    public static bool CooperationEnabled => Instance == null || Instance.Settings.cooperationEnabled;
    public MonsterCombatTelemetryPresenter Telemetry { get; private set; }
    public MonsterAIExperimentModel Settings { get; private set; }
    public int Generation => generation;
    public MonsterLearningModel Memory { get; private set; } = new MonsterLearningModel();
    private MonsterLearningView view;
    private readonly Dictionary<string, StreamWriter> writers = new Dictionary<string, StreamWriter>();
    private string directory;
    private int generation, completed;
    private bool dirty, storageBlocked, csvFailed;
    private float nextFlush;
    private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; }

    public static MonsterLearningPresenter Ensure(MonoBehaviour owner)
    {
        if (Instance != null) return Instance;
        var go = new GameObject("Monster AI research");
        SceneManager.MoveGameObjectToScene(go, owner.gameObject.scene);
        return go.AddComponent<MonsterLearningPresenter>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Settings = GetComponent<MonsterAIExperimentModel>();
        view = GetComponent<MonsterLearningView>();
        directory = Path.Combine(Application.persistentDataPath, "MonsterAIResearch",
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + "_" + Guid.NewGuid().ToString("N"));
        Telemetry = gameObject.AddComponent<MonsterCombatTelemetryPresenter>();
        Reload();
        ExportSnapshot("initial_policy.json");
    }

    public MonsterLearningModel Snapshot() => JsonUtility.FromJson<MonsterLearningModel>(JsonUtility.ToJson(Memory));

    public MonsterCombatTelemetryModel Begin(MonsterPresenter owner, MonsterAttackKind action,
        string context, float distance, float selfHp, PlayerPresenter player, int policyVersion,
        string eligible, string probabilities, float probability)
    {
        var behavior = owner.GetComponent<MonsterBehaviorModel>();
        return new MonsterCombatTelemetryModel
        {
            battleId = Telemetry.Join(owner, player),
            recorder = this, generation = generation, actionId = Guid.NewGuid().ToString("N"),
            monsterId = owner.CombatIdentity, monsterType = owner.LearningType,
            context = context, action = (int)action, startedAt = Time.time,
            distance = distance, selfHpRatio = selfHp,
            playerHpRatio = player.CurrentHp / (float)Mathf.Max(1, player.MaxHp),
            role = behavior != null ? behavior.SquadRole.ToString() : "Solo",
            mode = Settings.learningMode.ToString(), cooperation = Settings.cooperationEnabled,
            naturalSpawn = owner.IsNaturalSpawn, registeredLearningTest = owner.IsRegisteredLearningTest,
            learningEligible = owner.IsLearningEligible,
            train = owner.IsLearningEligible && Settings.learningMode == MonsterLearningMode.Training,
            learnedWeightVersion = policyVersion, eligibleActions = eligible,
            selectionProbabilities = probabilities, selectedProbability = probability
        };
    }

    public void Complete(MonsterCombatTelemetryModel record)
    {
        if (record.generation != generation) return; // A load/reset invalidates in-flight observations.
        completed++;
        bool evaluable = record.released && (record.outcome == "Hit" || record.outcome == "Miss" || record.outcome == "NoDamageContact");
        if (record.train && Settings.learningMode == MonsterLearningMode.Training && evaluable && !storageBlocked)
        {
            float oldWeight = Memory.Weight(record.monsterType, record.context, record.action);
            var entry = Memory.Observe(record.monsterType, record.context, record.action, record.damageDealt > 0);
            dirty = true;
            Write("learning_updates.csv", "actionId,monsterType,context,action,attempts,hits,reward,oldWeight,newWeight,version",
                record.actionId, record.monsterType, record.context, (MonsterAttackKind)record.action,
                entry.attempts, entry.hits, record.damageDealt > 0 ? 1f : -0.25f, oldWeight,
                Memory.Weight(record.monsterType, record.context, record.action), Memory.version);
        }
        Write("actions.csv", "actionId,battleId,monsterId,monsterType,startTime,endTime,mode,cooperation,naturalSpawn,role,context,selfHpRatio,playerHpRatio,distance,action,eligibleActions,probabilities,selectedProbability,policyVersion,released,outcome,damageDealt,registeredLearningTest,learningEligible",
            record.actionId, record.battleId, record.monsterId, record.monsterType, record.startedAt, Time.time, record.mode,
            record.cooperation, record.naturalSpawn, record.role, record.context, record.selfHpRatio,
            record.playerHpRatio, record.distance, (MonsterAttackKind)record.action, record.eligibleActions,
            record.selectionProbabilities, record.selectedProbability, record.learnedWeightVersion,
            record.released, record.outcome, record.damageDealt, record.registeredLearningTest, record.learningEligible);
    }

    public void RecordFormation(string group, MonsterPresenter owner, MonsterSquadAssignment assignment, string reason)
    {
        var stats = owner.GetComponent<MonsterModel>();
        Write("formations.csv", "time,mode,cooperation,groupId,monsterId,count,formation,role,angle,reason,exploitOpening,squadPolicyVersion,hpRatio,moveSpeed,projectileReady,chargeReady,frontAttackReady,isWave,currentHp,effectiveMoveSpeed,effectiveProjectileRange,damageTakenMultiplier",
            Time.time, Settings.learningMode, Settings.cooperationEnabled, group, owner.CombatIdentity,
            assignment.Count, assignment.Formation, assignment.Role, assignment.Angle, reason, assignment.ExploitOpening, MonsterSquadModel.PolicyVersion,
            stats.CurrentHp / (float)Mathf.Max(1, stats.MaxHp), owner.MoveSpeed,
            owner.Combat.IsAttackReady(MonsterAttackKind.Projectile), owner.Combat.IsAttackReady(MonsterAttackKind.Charge),
            owner.Combat.IsAttackReady(MonsterAttackKind.Melee) || owner.Combat.IsAttackReady(MonsterAttackKind.Circle), owner.IsWaveMonster,
            stats.CurrentHp, owner.MoveSpeed * owner.Behavior.AdvanceSpeedMultiplier,
            owner.Combat.EffectiveProjectileRange, owner.Behavior.DamageTakenMultiplier);
    }

    private void Update()
    {
        Settings.SetDiagnostics(Memory.version, completed, directory);
        view.Render(Settings.showOverlay, "Monster AI | " + Settings.learningMode + " | Cooperation: " + Settings.cooperationEnabled +
            "\nPolicy version: " + Memory.version + " | Resolved actions: " + completed +
            "\nNew monsters use a policy snapshot. Natural spawns train; waves do not." +
            "\nCSV / snapshots: persistentDataPath/MonsterAIResearch" +
            (Settings.showOverlay ? "\n" + MonsterSquadPresenter.GetDebugSummary() : ""));
        if (Time.unscaledTime < nextFlush) return;
        nextFlush = Time.unscaledTime + 10f;
        SaveNow();
    }

    public bool SaveNow()
    {
        FlushLogs();
        if (!dirty || storageBlocked) return !storageBlocked;
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath + ".tmp", JsonUtility.ToJson(Memory, true));
            if (File.Exists(SavePath)) File.Replace(SavePath + ".tmp", SavePath, null);
            else File.Move(SavePath + ".tmp", SavePath);
            dirty = false;
            return true;
        }
        catch (Exception e) { Debug.LogWarning("Monster learning save failed: " + e.Message); return false; }
    }

    public void Reload()
    {
        Telemetry?.ResetForLoad();
        generation++;
        dirty = false;
        storageBlocked = false;
        Memory = new MonsterLearningModel();
        try
        {
            if (!File.Exists(SavePath)) return;
            var loaded = JsonUtility.FromJson<MonsterLearningModel>(File.ReadAllText(SavePath));
            if (loaded == null || !loaded.IsValid()) throw new InvalidDataException("Invalid learning schema/data.");
            Memory = loaded;
        }
        catch (Exception e)
        {
            storageBlocked = true; // Preserve the existing file; don't silently overwrite a broken save.
            Debug.LogWarning("Monster learning load failed; using baseline without saving: " + e.Message);
        }
    }

    public static void ClearRuntimeMemory()
    {
        if (Instance == null) return;
        Instance.Telemetry?.ResetForLoad();
        Instance.generation++;
        Instance.Memory = new MonsterLearningModel();
        Instance.dirty = false;
        Instance.storageBlocked = false;
    }

    [ContextMenu("Flush Logs and Export Current Policy")]
    public void ExportResearchNow()
    {
        SaveNow();
        ExportCurrentPolicy();
        Debug.Log("Monster AI research: " + directory);
    }

    [ContextMenu("Open Research Folder")]
    public void OpenResearchFolder()
    {
        ExportResearchNow();
        Directory.CreateDirectory(directory);
        Application.OpenURL(new Uri(directory + Path.DirectorySeparatorChar).AbsoluteUri);
    }

    [ContextMenu("Export Current Policy Snapshot")]
    public void ExportCurrentPolicy() { ExportSnapshot("policy_v" + Memory.version + ".json"); }

    private void ExportSnapshot(string file)
    {
        if (!Settings.writeCsv) return;
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, file), JsonUtility.ToJson(Memory, true));
        }
        catch (Exception e) { Debug.LogWarning("Monster policy export failed: " + e.Message); }
    }

    public void Write(string file, string header, params object[] values)
    {
        if (!Settings.writeCsv || csvFailed) return;
        try
        {
            if (!writers.TryGetValue(file, out var writer))
            {
                Directory.CreateDirectory(directory);
                writer = new StreamWriter(Path.Combine(directory, file), false, new UTF8Encoding(true));
                writer.WriteLine(header);
                writers.Add(file, writer);
            }
            var cells = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                cells[i] = "\"" + (Convert.ToString(values[i], CultureInfo.InvariantCulture) ?? "").Replace("\"", "\"\"") + "\"";
            writer.WriteLine(string.Join(",", cells));
        }
        catch (Exception e) { csvFailed = true; Debug.LogWarning("Monster CSV logging stopped: " + e.Message); }
    }

    private void FlushLogs()
    {
        if (csvFailed) return;
        try { foreach (var writer in writers.Values) writer.Flush(); }
        catch (Exception e) { csvFailed = true; Debug.LogWarning("Monster CSV flush failed: " + e.Message); }
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
    private void OnApplicationQuit() { SaveNow(); }
    private void OnDestroy()
    {
        if (Instance != this) return;
        Telemetry?.EndSession();
        SaveNow();
        ExportCurrentPolicy();
        foreach (var writer in writers.Values)
            try { writer.Dispose(); } catch (IOException) { }
        writers.Clear();
        Instance = null;
    }
}
