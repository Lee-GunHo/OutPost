using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MonsterBehaviorModel))]
public sealed class MonsterBehaviorPresenter : MonoBehaviour
{
    private MonsterPresenter owner;
    private MonsterModel monster;
    private MonsterBehaviorModel model;
    private NavMeshAgent agent;
    private NavMeshPath retreatPath;
    private NavMeshPath formationPath;
    private NavMeshPath fleePath;
    private Transform fleeTarget;
    private float nextFleePlan, lastFleeProgressTime;
    private Vector3 lastFleePosition;
    private bool fleeMovementActive, fleeSavedRotation, fleeSavedBraking;
    private float fleeSavedSpeed, fleeSavedStoppingDistance;
    private float nextFormationPlan;
    private bool formationPathValid, formationPathNeedsApply, formationUsesWaypoint;
    private Vector3 plannedFormationPoint;
    private Transform retreatTarget;
    private bool savedRotation, savedBraking;
    private float savedSpeed, savedStoppingDistance;
    private static readonly float[] retreatAngles = { 0f, 45f, -45f, 90f, -90f };

    private float HealthRatio => monster.CurrentHp / (float)Mathf.Max(1, monster.MaxHp);
    private bool CanNavigate => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    private void Awake()
    {
        owner = GetComponent<MonsterPresenter>();
        monster = GetComponent<MonsterModel>();
        model = GetComponent<MonsterBehaviorModel>();
        agent = GetComponent<NavMeshAgent>();
        retreatPath = new NavMeshPath();
        formationPath = new NavMeshPath();
        fleePath = new NavMeshPath();
    }

    private void OnEnable() { MonsterSquadPresenter.Register(this); }
    public MonsterPresenter Owner => owner;
    public bool IsAttacking => owner != null && !owner.IsDead && owner.Combat.IsExecuting;
    private bool IsCooperating => model.SquadRole != MonsterSquadRole.Solo && owner.HasTarget && owner.IsCurrentTargetPlayer;
    public float AdvanceSpeedMultiplier => IsCooperating ? model.AdvanceSpeedMultiplier : 1f;
    public bool TryClaimSquadAttack() => !model.IsFleeing && (!IsCooperating || MonsterSquadPresenter.CanAttack(this, true));

    public bool TryGetSquadTarget(out Transform target)
    {
        target = null;
        if (!isActiveAndEnabled || owner == null || owner.IsDead || model.IsFleeing) return false;
        owner.UpdateTarget();
        if (!owner.HasTarget || !owner.IsCurrentTargetPlayer) return false;
        var player = owner.CurrentTarget.GetComponentInParent<PlayerPresenter>();
        if (player == null || !player.isActiveAndEnabled || player.IsDead) return false;
        target = player.transform;
        return true;
    }

    public MonsterSquadMember GetSquadSnapshot(Transform target)
    {
        Vector3 delta = transform.position - target.position;
        delta.y = 0f;
        return new MonsterSquadMember
        {
            Id = GetInstanceID(), Distance = delta.magnitude,
            Angle = Mathf.Atan2(delta.z, delta.x),
            NeedsCover = model.IsLowHealth(HealthRatio) || model.IsRetreating
        };
    }

    public void SetSquadAssignment(MonsterSquadAssignment assignment)
    {
        if (assignment.Role != model.SquadRole || assignment.Count != model.EngagedMonsterCount ||
            !Mathf.Approximately(assignment.Angle, model.AssignedFormationAngle))
        {
            nextFormationPlan = 0f;
            formationPathValid = false;
        }
        model.SetSquadAssignment(assignment);
    }

    public void RecordHit() { model.RecordHit(Time.time); }
    public void RecordAttack(MonsterAttackKind kind) { model.RecordAttack(kind); }

    public float GetAttackWeight(MonsterAttackKind kind, float baseWeight) =>
        model.AttackWeight(kind, baseWeight, HealthRatio, owner.Combat.TargetDistance, owner.AttackRange, Time.time);

    public MonsterBehaviorAction Decide()
    {
        // Keep the flee intention through hit stun and normal aggro-distance changes.
        if (model.IsFleeing)
        {
            if (fleeTarget != null) return MonsterBehaviorAction.Flee;
            CancelFlee();
        }
        owner.UpdateTarget();
        bool fightingPlayer = owner.HasTarget && owner.IsCurrentTargetPlayer && !owner.IsDead;
        if (model.TryBeginFlee(fightingPlayer, Time.time, Random.value))
        {
            fleeTarget = owner.CurrentTarget;
            SetSquadAssignment(new MonsterSquadAssignment { Role = MonsterSquadRole.Solo, Count = 1 });
            return MonsterBehaviorAction.Flee;
        }
        if (owner.IsDead || !owner.HasTarget) return MonsterBehaviorAction.Idle;
        float distance = owner.Combat.TargetDistance;
        if (model.WantsRetreat(HealthRatio, distance, owner.IsCurrentTargetPlayer, Time.time))
        {
            if (TryPlanRetreat()) return MonsterBehaviorAction.Retreat;
            model.DelayFailedRetreat(Time.time);
        }
        if (IsCooperating && TryFormationDecision(out MonsterBehaviorAction formationAction)) return formationAction;
        if (owner.Combat.HasAvailableAttack() && (!IsCooperating || MonsterSquadPresenter.CanAttack(this, false)))
            return MonsterBehaviorAction.Attack;
        // Wait for an attack cooldown at a useful distance. Do not hold behind an obstruction.
        if (model.WantsDistance(HealthRatio, distance, owner.IsCurrentTargetPlayer, Time.time) &&
            owner.Combat.HasAttackInRange())
            return MonsterBehaviorAction.HoldDistance;
        return MonsterBehaviorAction.Chase;
    }

    private bool TryFormationDecision(out MonsterBehaviorAction action)
    {
        action = MonsterBehaviorAction.Formation;
        if (!CanNavigate) return false;
        float radius = owner.Combat.GetFormationDistance(model.SquadRole);
        Vector3 desired = owner.CurrentTarget.position +
            new Vector3(Mathf.Cos(model.FormationAngle), 0f, Mathf.Sin(model.FormationAngle)) * radius;
        Vector3 offset = desired - transform.position;
        offset.y = 0f;
        float tolerance = model.SquadRole == MonsterSquadRole.Pressure ? 0.2f
            : model.SquadRole == MonsterSquadRole.Flanker ? 0.3f : 0.35f;
        bool atPosition = offset.sqrMagnitude <= tolerance * tolerance;
        if (atPosition && model.TryRearFlank(Time.time, Random.value))
        {
            desired = owner.CurrentTarget.position +
                new Vector3(Mathf.Cos(model.FormationAngle), 0f, Mathf.Sin(model.FormationAngle)) * radius;
            nextFormationPlan = 0f;
            formationPathValid = false;
            atPosition = false;
        }
        if (atPosition && owner.Combat.HasTargetLineOfSight)
        {
            if (!owner.Combat.HasAttackInRange()) return false;
            action = owner.Combat.HasAvailableAttack() && MonsterSquadPresenter.CanAttack(this, false)
                ? MonsterBehaviorAction.Attack : MonsterBehaviorAction.HoldDistance;
            return true;
        }
        if (Time.time >= nextFormationPlan)
        {
            nextFormationPlan = Time.time + 0.25f;
            // Approach the opposite side around the player, instead of walking through them.
            Vector3 radial = transform.position - owner.CurrentTarget.position;
            float currentAngle = Mathf.Atan2(radial.z, radial.x) * Mathf.Rad2Deg;
            float angleDelta = Mathf.DeltaAngle(currentAngle, model.FormationAngle * Mathf.Rad2Deg);
            formationUsesWaypoint = Mathf.Abs(angleDelta) > 50f;
            if (formationUsesWaypoint)
            {
                float stepAngle = (currentAngle + Mathf.Clamp(angleDelta, -45f, 45f)) * Mathf.Deg2Rad;
                desired = owner.CurrentTarget.position +
                    new Vector3(Mathf.Cos(stepAngle), 0f, Mathf.Sin(stepAngle)) * radius;
            }
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            formationPathValid = NavMesh.SamplePosition(desired, out NavMeshHit hit, 0.75f, filter) &&
                agent.CalculatePath(hit.position, formationPath) && formationPath.status == NavMeshPathStatus.PathComplete;
            if (formationPathValid)
            {
                plannedFormationPoint = hit.position;
                formationPathNeedsApply = true;
                // A reachable but occluded slot must not make the monster wait forever behind a wall.
                Vector3 gap = plannedFormationPoint - transform.position;
                gap.y = 0f;
                if (gap.sqrMagnitude <= 0.3f * 0.3f && !owner.Combat.HasTargetLineOfSight)
                    formationPathValid = false;
            }
        }
        if (!formationPathValid) return false; // Fall back to individual chase/attack in tight spaces.
        // Sampling can shift the slot slightly; consider the reachable point too.
        Vector3 remaining = plannedFormationPoint - transform.position;
        remaining.y = 0f;
        if (!formationUsesWaypoint && remaining.sqrMagnitude <= 0.3f * 0.3f && owner.Combat.HasTargetLineOfSight)
        {
            if (!owner.Combat.HasAttackInRange()) return false;
            action = owner.Combat.HasAvailableAttack() && MonsterSquadPresenter.CanAttack(this, false)
                ? MonsterBehaviorAction.Attack : MonsterBehaviorAction.HoldDistance;
        }
        return true;
    }

    public void BeginFormation()
    {
        owner.StopMove();
        formationPathNeedsApply = true;
    }

    public void MoveFormation()
    {
        if (!CanNavigate || !formationPathValid || !IsCooperating) { owner.StopMove(); return; }
        agent.speed = owner.MoveSpeed * AdvanceSpeedMultiplier;
        agent.autoBraking = true;
        agent.stoppingDistance = 0.1f;
        agent.isStopped = false;
        if (formationPathNeedsApply)
        {
            formationPathNeedsApply = false;
            if (!agent.SetPath(formationPath))
            {
                formationPathValid = false;
                owner.StopMove();
            }
        }
    }

    private bool TryPlanRetreat()
    {
        return owner.IsCurrentTargetPlayer && TryPlanAwayFrom(owner.CurrentTarget, model.RetreatDistance, retreatPath);
    }

    private bool TryPlanAwayFrom(Transform threat, float stepDistance, NavMeshPath path)
    {
        if (!CanNavigate || threat == null) return false;
        Vector3 target = threat.position;
        Vector3 away = transform.position - target;
        away.y = 0f;
        float initialDistance = away.magnitude;
        if (initialDistance < 0.01f) away = -transform.forward;
        away.Normalize();
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        foreach (float angle in retreatAngles)
        {
            Vector3 candidate = agent.nextPosition + Quaternion.Euler(0f, angle, 0f) * away * stepDistance;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.6f, filter) ||
                !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                continue;
            Vector3 separation = hit.position - target;
            separation.y = 0f;
            if (separation.magnitude < initialDistance + 0.5f) continue;
            // Reject paths which first approach the player or make a long detour around a wall.
            Vector3 previous = agent.nextPosition;
            float length = 0f;
            bool safe = true;
            foreach (Vector3 corner in path.corners)
            {
                Vector3 leg = corner - previous;
                leg.y = 0f;
                Vector3 toTarget = target - previous;
                toTarget.y = 0f;
                float t = leg.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(toTarget, leg) / leg.sqrMagnitude) : 0f;
                if ((toTarget - leg * t).magnitude < initialDistance - 0.1f) { safe = false; break; }
                length += leg.magnitude;
                previous = corner;
            }
            if (safe && length <= stepDistance * 1.6f) return true;
        }
        return false;
    }

    public void BeginFleeMovement()
    {
        owner.StopMove();
        if (agent != null)
        {
            fleeSavedRotation = agent.updateRotation;
            fleeSavedBraking = agent.autoBraking;
            fleeSavedSpeed = agent.speed;
            fleeSavedStoppingDistance = agent.stoppingDistance;
            fleeMovementActive = true;
            agent.updateRotation = true;
            agent.autoBraking = false;
            agent.stoppingDistance = 0.1f;
            agent.speed = owner.MoveSpeed * model.FleeSpeedMultiplier;
        }
        nextFleePlan = Time.time;
        lastFleeProgressTime = Time.time;
        lastFleePosition = transform.position;
    }

    public bool TickFlee()
    {
        if (!model.IsFleeing || owner.IsDead || fleeTarget == null) { CancelFlee(); return false; }
        Vector3 separation = transform.position - fleeTarget.position;
        separation.y = 0f;
        if (model.ShouldDespawnAfterFlee(separation.magnitude))
        {
            // Escape is not a kill: skip Dead(), drops, experience and kill/quest progress.
            PauseFleeMovement();
            gameObject.SetActive(false);
            Destroy(gameObject);
            return true;
        }

        Vector3 progress = transform.position - lastFleePosition;
        progress.y = 0f;
        if (progress.sqrMagnitude >= 0.25f * 0.25f)
        {
            lastFleePosition = transform.position;
            lastFleeProgressTime = Time.time;
        }
        if (Time.time - lastFleeProgressTime >= 4f)
        {
            // A corner or missing NavMesh must not leave a wave waiting forever.
            CancelFlee();
            return false;
        }
        if (CanNavigate && Time.time >= nextFleePlan)
        {
            nextFleePlan = Time.time + 0.5f;
            if (TryPlanAwayFrom(fleeTarget, 6f, fleePath) || TryPlanAwayFrom(fleeTarget, 3f, fleePath))
            {
                agent.isStopped = false;
                if (!agent.SetPath(fleePath)) owner.StopMove();
            }
            else owner.StopMove();
        }
        return true;
    }

    public void PauseFleeMovement()
    {
        if (!fleeMovementActive) return;
        fleeMovementActive = false;
        if (agent != null)
        {
            agent.updateRotation = fleeSavedRotation;
            agent.autoBraking = fleeSavedBraking;
            agent.speed = fleeSavedSpeed;
            agent.stoppingDistance = fleeSavedStoppingDistance;
        }
        owner.StopMove();
    }

    private void CancelFlee()
    {
        PauseFleeMovement();
        model.EndFlee(Time.time);
        fleeTarget = null;
    }

    public void BeginRetreat()
    {
        owner.StopMove();
        if (!CanNavigate) { model.DelayFailedRetreat(Time.time); return; }
        savedRotation = agent.updateRotation;
        savedBraking = agent.autoBraking;
        savedSpeed = agent.speed;
        savedStoppingDistance = agent.stoppingDistance;
        retreatTarget = owner.CurrentTarget;
        model.BeginRetreat(Time.time);
        agent.updateRotation = false;
        agent.autoBraking = true;
        agent.stoppingDistance = 0.1f;
        agent.speed = owner.MoveSpeed * model.RetreatSpeedMultiplier;
        agent.isStopped = false;
        if (!agent.SetPath(retreatPath)) EndRetreat();
    }

    public bool TickRetreat()
    {
        owner.UpdateTarget();
        if (!model.IsRetreating || owner.IsDead || !CanNavigate || !owner.HasTarget ||
            owner.CurrentTarget != retreatTarget || !owner.IsCurrentTargetPlayer || model.RetreatExpired(Time.time))
            return false;
        FaceTarget();
        return agent.pathPending || (agent.hasPath && !agent.isPathStale &&
            agent.pathStatus == NavMeshPathStatus.PathComplete &&
            agent.remainingDistance > agent.stoppingDistance + 0.1f);
    }

    public void FaceTarget()
    {
        if (!owner.HasTarget) return;
        Vector3 facing = owner.CurrentTarget.position - transform.position;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(facing);
    }

    public void EndRetreat()
    {
        if (model == null || !model.IsRetreating) return;
        model.EndRetreat();
        if (agent != null)
        {
            agent.updateRotation = savedRotation;
            agent.autoBraking = savedBraking;
            agent.speed = savedSpeed;
            agent.stoppingDistance = savedStoppingDistance;
        }
        retreatTarget = null;
        owner.StopMove();
    }

    private void OnDisable()
    {
        EndRetreat();
        if (model != null) CancelFlee();
        MonsterSquadPresenter.Unregister(this);
        if (model != null) model.SetSquadAssignment(new MonsterSquadAssignment { Role = MonsterSquadRole.Solo, Count = 1 });
    }
}
