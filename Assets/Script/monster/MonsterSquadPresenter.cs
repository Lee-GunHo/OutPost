using System.Collections.Generic;
using UnityEngine;

// One scene-local coordinator, automatically created when a normal monster becomes active.
[DefaultExecutionOrder(-100)]
public sealed class MonsterSquadPresenter : MonoBehaviour
{
    private sealed class Encounter
    {
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
        membership.Clear();
        foreach (var encounter in encounters.Values)
        {
            encounter.Members.Clear();
            encounter.Snapshot.Clear();
        }
        foreach (var member in registered)
        {
            if (member == null) continue;
            if (!member.TryGetSquadTarget(out Transform target))
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
            encounter.Model.Synchronize(encounter.Snapshot);
            encounter.Model.AdvanceFlank(Time.time);
            foreach (var member in encounter.Members)
                member.SetSquadAssignment(encounter.Model.GetAssignment(member.GetInstanceID()));
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

    public static bool CanAttack(MonsterBehaviorPresenter member, bool commit)
    {
        if (instance == null || !instance.membership.TryGetValue(member, out Encounter encounter)) return true;
        int active = 0;
        foreach (var other in encounter.Members)
            if (other != null && other.isActiveAndEnabled && other.IsAttacking) active++;
        return commit ? encounter.Model.TryStartAttack(Time.time, active)
            : encounter.Model.CanStartAttack(Time.time, active);
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
