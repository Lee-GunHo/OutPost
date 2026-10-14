using System.Collections.Generic;
using UnityEngine;

// One scene-local coordinator, automatically created when a normal monster becomes active.
[DefaultExecutionOrder(-100)]
public sealed class MonsterSquadPresenter : MonoBehaviour
{
    private sealed class Encounter
    {
        public readonly string Id = System.Guid.NewGuid().ToString("N");
        public Vector3 RallyPoint;
        public readonly MonsterSquadModel Model = new MonsterSquadModel();
        public readonly List<MonsterBehaviorPresenter> Members = new List<MonsterBehaviorPresenter>();
        public readonly List<MonsterSquadMember> Snapshot = new List<MonsterSquadMember>();
    }

    private static MonsterSquadPresenter instance;
    private readonly HashSet<MonsterBehaviorPresenter> registered = new HashSet<MonsterBehaviorPresenter>();
    private readonly Dictionary<Transform, Encounter> encounters = new Dictionary<Transform, Encounter>();
    private readonly Dictionary<MonsterBehaviorPresenter, Encounter> membership = new Dictionary<MonsterBehaviorPresenter, Encounter>();
    private readonly List<Transform> emptyEncounters = new List<Transform>();
    private readonly List<MonsterPresenter> crowd = new List<MonsterPresenter>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public static void Register(MonsterBehaviorPresenter member)
    {
        if (!Application.isPlaying) return;
        if (instance == null)
        {
            var coordinator = new GameObject("Monster encounter coordinator");
            instance = coordinator.AddComponent<MonsterSquadPresenter>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(coordinator, member.gameObject.scene);
        }
        instance.registered.Add(member);
    }

    public static void Unregister(MonsterBehaviorPresenter member)
    {
        if (instance == null) return;
        instance.registered.Remove(member);
        instance.membership.Remove(member);
    }

    private void Update()
    {
        MonsterLearningPresenter.Instance?.Telemetry.Sample(registered);
        membership.Clear();
        foreach (var encounter in encounters.Values)
        {
            encounter.Members.Clear();
            encounter.Snapshot.Clear();
        }
        foreach (var member in registered)
        {
            if (member == null) continue;
            if (!MonsterLearningPresenter.CooperationEnabled || !member.TryGetSquadTarget(out Transform target))
            {
                member.SetSquadAssignment(new MonsterSquadAssignment { Role = MonsterSquadRole.Solo, Count = 1 });
                continue;
            }
            if (!encounters.TryGetValue(target, out Encounter encounter))
            {
                encounter = new Encounter();
                encounters.Add(target, encounter);
            }
            encounter.Members.Add(member);
            encounter.Snapshot.Add(member.GetSquadSnapshot(target));
            membership[member] = encounter;
        }
        emptyEncounters.Clear();
        foreach (var pair in encounters)
        {
            Encounter encounter = pair.Value;
            if (encounter.Members.Count == 0) { emptyEncounters.Add(pair.Key); continue; }
            var player = pair.Key.GetComponentInParent<PlayerPresenter>();
            if (player == null) continue;
            var playerState = player.GetComponent<PlayerStateManager>();
            int focusId = 0;
            if (playerState != null && playerState.IsAttacking)
            {
                float bestDot = 0.5f;
                foreach (var member in encounter.Members)
                {
                    if (encounter.Model.GetAssignment(member.GetInstanceID()).Role != MonsterSquadRole.Pressure) continue;
                    Vector3 direction = member.transform.position - player.transform.position;
                    direction.y = 0f;
                    float dot = Vector3.Dot(player.transform.forward, direction.normalized);
                    if (direction.sqrMagnitude <= 16f && dot > bestDot) { bestDot = dot; focusId = member.GetInstanceID(); }
                }
            }
            var oldFormation = encounter.Model.Formation;
            bool changed = encounter.Model.Synchronize(encounter.Snapshot, Time.time, new MonsterSquadSituation
            {
                PlayerHpRatio = player.CurrentHp / (float)Mathf.Max(1, player.MaxHp),
                PlayerFocusedMember = focusId,
                AllowWaveWithdrawal = MonsterLearningPresenter.Instance != null && MonsterLearningPresenter.Instance.Settings.waveSquadWithdrawal
            });
            if (oldFormation != MonsterSquadFormation.Regroup && encounter.Model.Formation == MonsterSquadFormation.Regroup)
            {
                float angle = encounter.Model.RallyAngle;
                encounter.RallyPoint = player.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 6f;
            }
            if (changed) MonsterLearningPresenter.Instance?.Write("squad_decisions.csv",
                "time,groupId,mode,cooperation,formation,formationChanged,reason,count,averageHp,playerHp,playerFocusedMember,waveWithdrawalAllowed,squadPolicyVersion",
                Time.time, encounter.Id, MonsterLearningPresenter.Instance.Settings.learningMode, true, encounter.Model.Formation,
                oldFormation != encounter.Model.Formation, encounter.Model.ChangeReason, encounter.Model.Count,
                encounter.Model.AverageHp, encounter.Model.PlayerHp, focusId,
                MonsterLearningPresenter.Instance.Settings.waveSquadWithdrawal, 2);
            encounter.Model.AdvanceFlank(Time.time);
            encounter.Members.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            int rallySlot = 0;
            float rallyAngle = encounter.Model.RallyAngle;
            Vector3 lateral = new Vector3(-Mathf.Sin(rallyAngle), 0f, Mathf.Cos(rallyAngle));
            foreach (var member in encounter.Members)
            {
                member.SetRallyPosition(encounter.RallyPoint + lateral * (rallySlot++ - (encounter.Members.Count - 1) * 0.5f) * 1.4f);
                var assignment = encounter.Model.GetAssignment(member.GetInstanceID());
                member.SetSquadAssignment(assignment);
                if (changed) MonsterLearningPresenter.Instance?.RecordFormation(encounter.Id, member.Owner, assignment, encounter.Model.ChangeReason);
            }
        }
        foreach (Transform target in emptyEncounters) encounters.Remove(target);
    }

    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;
        crowd.Clear();
        foreach (var member in registered)
            if (member != null && member.isActiveAndEnabled && member.Owner != null && !member.Owner.IsDead)
                crowd.Add(member.Owner);
        // Each pair is corrected once, including monsters fleeing or assaulting the nexus.
        for (int i = 0; i < crowd.Count; i++)
            for (int j = i + 1; j < crowd.Count; j++) crowd[i].SeparateFrom(crowd[j], Time.deltaTime);
    }

    public static void RecordDamage(MonsterPresenter owner, float dealt, float taken)
    {
        if (!MonsterLearningPresenter.CooperationEnabled || instance == null || owner == null || owner.Behavior == null) return;
        if (instance.membership.TryGetValue(owner.Behavior, out Encounter encounter)) encounter.Model.RecordDamage(dealt, taken);
    }

    public static string GetDebugSummary()
    {
        if (!MonsterLearningPresenter.CooperationEnabled) return "Formation: disabled";
        if (instance == null || instance.encounters.Count == 0) return "Formation: no active group";
        foreach (var encounter in instance.encounters.Values)
            if (encounter.Model.Count > 0)
                return "Formation: " + encounter.Model.Formation + " | Members: " + encounter.Model.Count +
                    "\nReason: " + encounter.Model.FormationReason;
        return "Formation: no active group";
    }

    public static bool CanAttack(MonsterBehaviorPresenter member, bool commit)
    {
        if (!MonsterLearningPresenter.CooperationEnabled || instance == null || !instance.membership.TryGetValue(member, out Encounter encounter)) return true;
        int active = 0;
        foreach (var other in encounter.Members)
            if (other != null && other.isActiveAndEnabled && other.IsAttacking) active++;
        return commit ? encounter.Model.TryStartAttack(Time.time, active)
            : encounter.Model.CanStartAttack(Time.time, active);
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
