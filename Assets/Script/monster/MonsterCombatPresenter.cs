using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MonsterCombatModel), typeof(MonsterCombatView))]
public sealed class MonsterCombatPresenter : MonoBehaviour
{
    private MonsterPresenter owner;
    private MonsterModel monster;
    private MonsterCombatModel model;
    private MonsterCombatView view;
    private NavMeshAgent agent;
    private CapsuleCollider body;
    private MonsterAttackPayload chargePayload;
    private readonly List<MonsterAttackKind> available = new List<MonsterAttackKind>(4);
    private readonly float[] selectionWeights = new float[4];
    private MonsterAttackKind activeKind;
    private Transform lockedTarget;
    private Vector3 origin, direction;
    private float phaseRemaining;
    private bool released;
    public bool IsExecuting { get; private set; }
    public float TargetDistance
    {
        get
        {
            if (!owner.HasTarget) return float.PositiveInfinity;
            Vector3 offset = TargetPoint(ResolveTarget(owner.CurrentTarget), owner.transform.position) - owner.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }
    }
    public bool HasTargetLineOfSight
    {
        get
        {
            Component target = ResolveTarget(owner.CurrentTarget);
            return target != null && HasLineOfSight(target, owner.transform.position + Vector3.up * 0.3f);
        }
    }

    public bool HasAttackInRange()
    {
        if (owner == null || owner.IsDead || !owner.HasTarget) return false;
        for (int i = 0; i <= (int)MonsterAttackKind.Charge; i++)
            if (CanUse((MonsterAttackKind)i, false)) return true;
        return false;
    }

    public float GetFormationDistance(MonsterSquadRole role)
    {
        if (role == MonsterSquadRole.Pressure) return Mathf.Max(0.5f, owner.AttackRange * 0.8f);
        if (role == MonsterSquadRole.Flanker && model.IsEnabled(MonsterAttackKind.Charge) && model.GetWeight(MonsterAttackKind.Charge) > 0f)
            return Mathf.Lerp(model.ChargeMinRange, model.ChargeDistance, 0.6f);
        if (model.IsEnabled(MonsterAttackKind.Projectile) && model.GetWeight(MonsterAttackKind.Projectile) > 0f)
            return Mathf.Lerp(model.ProjectileMinRange, model.ProjectileRange, 0.65f);
        if (model.IsEnabled(MonsterAttackKind.Circle) && model.GetWeight(MonsterAttackKind.Circle) > 0f)
            return model.CircleRadius * 0.8f;
        return Mathf.Max(0.5f, owner.AttackRange * 0.8f);
    }

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
        agent = GetComponent<NavMeshAgent>();
        body = GetComponent<CapsuleCollider>();
    }

    public void GetAvailableAttacks(List<MonsterAttackKind> result)
    {
        result.Clear();
        if (owner == null || owner.IsDead || !owner.HasTarget || IsExecuting) return;
        for (int i = 0; i <= (int)MonsterAttackKind.Charge; i++)
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
        foreach (var kind in available)
        {
            float weight = owner.Behavior != null
                ? owner.Behavior.GetAttackWeight(kind, model.GetWeight(kind)) : model.GetWeight(kind);
            selectionWeights[(int)kind] = weight;
            total += weight;
        }
        if (total <= 0f) return false;
        float roll = UnityEngine.Random.value * total;
        MonsterAttackKind selected = available[0];
        foreach (var kind in available)
        {
            selected = kind;
            roll -= selectionWeights[(int)kind];
            if (roll <= 0f) break;
        }
        return TryStartAttack(selected);
    }

    public bool TryStartAttack(MonsterAttackKind kind)
    {
        if (IsExecuting || owner == null || owner.IsDead || !owner.HasTarget || !CanUse(kind))
            return false;
        if (owner.Behavior != null && !owner.Behavior.TryClaimSquadAttack()) return false;
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
        owner.Behavior?.RecordAttack(kind);
        phaseRemaining = model.GetWindup(kind);
        released = false;
        IsExecuting = true;
        view.ShowWarning(kind, origin, direction,
            kind == MonsterAttackKind.Circle ? model.CircleRadius
            : kind == MonsterAttackKind.Charge ? model.ChargeDistance : model.ProjectileRange);
        AttackStarted?.Invoke(kind);
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (!IsExecuting) return;
        if (owner.IsDead) { Cancel(); return; }
        if (model.IsCharging) return; // FixedUpdate owns the active movement phase.
        phaseRemaining -= deltaTime;
        if (phaseRemaining > 0f) return;
        if (!released)
        {
            released = true;
            view.ClearWarning();
            Execute();
            phaseRemaining = model.GetRecovery(activeKind);
        }
        else Finish();
    }

    private bool CanUse(MonsterAttackKind kind, bool requireReady = true)
    {
        if (!model.IsEnabled(kind) || model.GetWeight(kind) <= 0f ||
            (requireReady && !model.IsReady(kind, Time.time))) return false;
        Component target = ResolveTarget(owner.CurrentTarget);
        if (target == null) return false;
        Vector3 point = TargetPoint(target, owner.transform.position);
        Vector3 delta = point - owner.transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;
        bool inRange = kind == MonsterAttackKind.Melee ? owner.IsCurrentTargetInAttackRange()
            : kind == MonsterAttackKind.Circle ? distance <= model.CircleRadius
            : kind == MonsterAttackKind.Charge ? CanCharge &&
                distance >= model.ChargeMinRange && distance <= model.ChargeDistance
            : distance >= model.ProjectileMinRange && distance <= model.ProjectileRange;
        return inRange && HasLineOfSight(target, owner.transform.position + Vector3.up * 0.3f);
    }

    private void Execute()
    {
        var payload = new MonsterAttackPayload(monster, model.GetDamageMultiplier(activeKind));
        if (activeKind == MonsterAttackKind.Charge)
        {
            chargePayload = payload;
            model.BeginCharge();
        }
        else if (activeKind == MonsterAttackKind.Projectile)
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

    private bool CanCharge => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    public void FixedTick(float deltaTime)
    {
        if (!IsExecuting || !model.IsCharging)
        {
            owner.StopMove();
            return;
        }
        if (owner.IsDead) { Cancel(); return; }
        if (!CanCharge) { EndCharge(); return; }

        const float skin = 0.03f;
        Vector3 start = owner.transform.position;
        float distance = Mathf.Min(model.ChargeSpeed * Mathf.Max(0f, deltaTime), model.ChargeRemainingDistance);
        bool stopped = agent.Raycast(agent.nextPosition + direction * distance, out NavMeshHit edge);
        if (stopped)
        {
            Vector3 toEdge = edge.position - agent.nextPosition;
            toEdge.y = 0f;
            distance = Mathf.Min(distance, Mathf.Max(0f, Vector3.Dot(toEdge, direction) - skin));
        }

        // Sweep the body, not just the target point, so fast movement cannot cross thin walls.
        // Leave a small ground clearance to avoid treating the walkable floor as an obstacle.
        float radius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.z)
            : Mathf.Max(0.1f, agent.radius);
        Vector3 center = body != null ? body.bounds.center : start;
        float halfHeight = body != null ? body.bounds.extents.y : radius;
        radius = Mathf.Max(0.01f, radius - skin);
        float segment = Mathf.Max(0f, halfHeight - radius - skin);
        Vector3 bottom = center - Vector3.up * segment;
        Vector3 top = center + Vector3.up * segment;

        // Casts do not report initial overlaps. Walls take priority over damageable overlaps.
        Component contact = null;
        bool overlapping = false;
        foreach (Collider collider in Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!BlocksCharge(collider)) continue;
            overlapping = true;
            Component target = FindDamageTarget(collider);
            if (target == null) { contact = null; break; }
            if (HasLineOfSight(target, center)) contact = target;
        }
        if (overlapping)
        {
            EndCharge();
            if (contact != null) ApplyHit(contact, chargePayload);
            return;
        }

        RaycastHit[] hits = Physics.CapsuleCastAll(bottom, top, radius, direction,
            distance + skin, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (!BlocksCharge(hit.collider)) continue;
            distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - skin));
            contact = FindDamageTarget(hit.collider);
            stopped = true;
            break;
        }

        agent.Move(direction * distance);
        owner.transform.rotation = Quaternion.LookRotation(direction);
        model.AdvanceCharge(distance);
        if (stopped || model.ChargeRemainingDistance <= 0.001f)
        {
            // Resolve once, then enter recovery; multiple colliders cannot cause repeated damage.
            EndCharge();
            if (contact != null) ApplyHit(contact, chargePayload);
        }
    }

    private void EndCharge()
    {
        model.EndCharge();
        owner.StopMove();
        phaseRemaining = model.GetRecovery(MonsterAttackKind.Charge);
    }

    private bool BlocksCharge(Collider collider) => collider != null && !collider.isTrigger &&
        !collider.transform.IsChildOf(owner.transform);

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
        if (model.IsCharging) EndCharge();
        IsExecuting = false;
        lockedTarget = null;
        AttackFinished?.Invoke(activeKind, released);
    }

    private void OnDisable() { Cancel(); }
}
