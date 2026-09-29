using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MonsterCombatModel), typeof(MonsterCombatView))]
public sealed class MonsterCombatPresenter : MonoBehaviour
{
    private MonsterPresenter owner;
    private MonsterModel monster;
    private MonsterCombatModel model;
    private MonsterCombatView view;
    private readonly List<MonsterAttackKind> available = new List<MonsterAttackKind>(3);
    private MonsterAttackKind activeKind;
    private Transform lockedTarget;
    private Vector3 origin, direction;
    private float phaseRemaining;
    private bool released;
    public bool IsExecuting { get; private set; }

    // Optional policy for a future learning agent. Only legal actions are accepted.
    public Func<IReadOnlyList<MonsterAttackKind>, MonsterAttackKind> SelectionPolicy { get; set; }
    public event Action<MonsterAttackKind> AttackStarted;
    public event Action<MonsterAttackKind, bool> AttackFinished;

    private void Awake()
    {
        owner = GetComponent<MonsterPresenter>();
        monster = GetComponent<MonsterModel>();
        model = GetComponent<MonsterCombatModel>();
        view = GetComponent<MonsterCombatView>();
    }

    public void GetAvailableAttacks(List<MonsterAttackKind> result)
    {
        result.Clear();
        if (owner == null || owner.IsDead || !owner.HasTarget || IsExecuting) return;
        for (int i = 0; i < 3; i++)
        {
            var kind = (MonsterAttackKind)i;
            if (CanUse(kind)) result.Add(kind);
        }
    }

    public bool HasAvailableAttack()
    {
        if (IsExecuting) return true;
        GetAvailableAttacks(available);
        return available.Count > 0;
    }

    public bool TryStartSelectedAttack()
    {
        GetAvailableAttacks(available);
        if (available.Count == 0) return false;
        if (SelectionPolicy != null)
            return TryStartAttack(SelectionPolicy(available));

        float total = 0f;
        foreach (var kind in available) total += model.GetWeight(kind);
        float roll = UnityEngine.Random.value * total;
        MonsterAttackKind selected = available[0];
        foreach (var kind in available)
        {
            selected = kind;
            roll -= model.GetWeight(kind);
            if (roll <= 0f) break;
        }
        return TryStartAttack(selected);
    }

    public bool TryStartAttack(MonsterAttackKind kind)
    {
        if (IsExecuting || owner == null || owner.IsDead || !owner.HasTarget || !CanUse(kind))
            return false;
        activeKind = kind;
        lockedTarget = owner.CurrentTarget;
        origin = owner.transform.position + Vector3.up * 0.3f;
        Vector3 point = TargetPoint(ResolveTarget(lockedTarget), origin);
        direction = point - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) direction = owner.transform.forward;
        direction.Normalize();
        owner.StopMove();
        owner.transform.rotation = Quaternion.LookRotation(direction);
        model.Commit(kind, Time.time, owner.AttackCooldown);
        phaseRemaining = model.GetWindup(kind);
        released = false;
        IsExecuting = true;
        view.ShowWarning(kind, origin, direction,
            kind == MonsterAttackKind.Circle ? model.CircleRadius : model.ProjectileRange);
        AttackStarted?.Invoke(kind);
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (!IsExecuting) return;
        if (owner.IsDead) { Cancel(); return; }
        phaseRemaining -= deltaTime;
        if (phaseRemaining > 0f) return;
        if (!released)
        {
            released = true;
            view.ClearWarning();
            Execute();
            phaseRemaining = model.RecoveryDuration;
        }
        else Finish();
    }

    private bool CanUse(MonsterAttackKind kind)
    {
        if (!model.IsReady(kind, Time.time)) return false;
        Component target = ResolveTarget(owner.CurrentTarget);
        if (target == null) return false;
        Vector3 point = TargetPoint(target, owner.transform.position);
        Vector3 delta = point - owner.transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;
        bool inRange = kind == MonsterAttackKind.Melee ? owner.IsCurrentTargetInAttackRange()
            : kind == MonsterAttackKind.Circle ? distance <= model.CircleRadius
            : distance >= model.ProjectileMinRange && distance <= model.ProjectileRange;
        return inRange && HasLineOfSight(target, owner.transform.position + Vector3.up * 0.3f);
    }

    private void Execute()
    {
        var payload = new MonsterAttackPayload(monster, model.GetDamageMultiplier(activeKind));
        if (activeKind == MonsterAttackKind.Projectile)
        {
            // Aim is fixed at windup start, so moving sideways can dodge the shot.
            MonsterProjectilePresenter.Spawn(origin, direction, model, payload, owner.transform, view.AttackMaterial);
        }
        else if (activeKind == MonsterAttackKind.Circle)
        {
            var damaged = new HashSet<Component>();
            Collider[] hits = Physics.OverlapCapsule(origin - Vector3.up, origin + Vector3.up,
                model.CircleRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                Component target = FindDamageTarget(hit);
                if (target == null || !damaged.Add(target)) continue;
                Vector3 offset = TargetPoint(target, origin) - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude <= model.CircleRadius * model.CircleRadius && HasLineOfSight(target, origin))
                    ApplyHit(target, payload);
            }
        }
        else
        {
            Component target = ResolveTarget(lockedTarget);
            if (target != null && lockedTarget == owner.CurrentTarget && owner.IsCurrentTargetInAttackRange()
                && HasLineOfSight(target, origin)) ApplyHit(target, payload);
        }
    }

    private bool HasLineOfSight(Component target, Vector3 start)
    {
        Vector3 end = TargetPoint(target, start);
        end.y = start.y;
        Vector3 delta = end - start;
        if (delta.sqrMagnitude < 0.0001f) return true;
        RaycastHit[] hits = Physics.RaycastAll(start, delta.normalized, delta.magnitude + 0.05f,
            ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (!BlocksAttack(hit.collider, owner.transform)) continue;
            return FindDamageTarget(hit.collider) == target;
        }
        return true;
    }

    private static Vector3 TargetPoint(Component target, Vector3 from)
    {
        if (target is NexusPresenter nexus) return nexus.GetClosestAttackPoint(from);
        return target != null ? target.transform.position : from;
    }

    private static Component ResolveTarget(Transform target)
    {
        if (target == null) return null;
        PlayerPresenter player = target.GetComponentInParent<PlayerPresenter>() ?? target.GetComponentInChildren<PlayerPresenter>();
        if (player != null) return player.IsDead ? null : player;
        NexusPresenter nexus = target.GetComponentInParent<NexusPresenter>() ?? target.GetComponentInChildren<NexusPresenter>();
        return nexus != null && !nexus.IsHealthDepleted ? nexus : null;
    }

    public static Component FindDamageTarget(Collider collider) => collider == null ? null : ResolveTarget(collider.transform);

    public static bool BlocksAttack(Collider collider, Transform owner)
    {
        return collider != null && !collider.isTrigger &&
            (owner == null || !collider.transform.IsChildOf(owner)) &&
            collider.GetComponentInParent<MonsterPresenter>() == null &&
            collider.GetComponentInParent<BossPresenter>() == null;
    }

    public static void ApplyHit(Component target, MonsterAttackPayload payload)
    {
        if (target is PlayerPresenter player)
        {
            if (player.IsDead) return;
            player.TakeDamage(payload.Damage);
            if (!player.IsDead && payload.EffectType != StatusEffectType.None &&
                UnityEngine.Random.value < payload.Chance)
                player.AddStatusEffect(new StatusEffectData(payload.EffectType, payload.Duration, payload.Value,
                    payload.TickInterval, payload.AttackModifier, payload.DefenseModifier));
        }
        else if (target is NexusPresenter nexus) nexus.TakeDamage(payload.Damage);
    }

    public void Cancel()
    {
        view?.ClearWarning();
        if (IsExecuting) Finish();
    }

    private void Finish()
    {
        IsExecuting = false;
        lockedTarget = null;
        AttackFinished?.Invoke(activeKind, released);
    }

    private void OnDisable() { Cancel(); }
}
