using UnityEngine;

public enum MonsterBehaviorAction { Idle, Chase, Attack, Retreat, HoldDistance, Formation, Flee }

// Decision tuning and memory. Navigation and FSM transitions belong to presenters.
public sealed class MonsterBehaviorModel : MonoBehaviour
{
    public const float FlankAttackAngleTolerance = 35f;
    public const float FlankAttackDistanceAllowance = 1f;
    public const float SupportHoldDistanceScale = 0.7f;
    public const float SupportHoldAngleTolerance = 40f;
    public const float SupportHoldOuterAllowance = 0.75f;
    public const float FormationCatchUpAllowance = 1.5f;

    [SerializeField, Range(0f, 1f)] private float lowHealthRatio = 0.35f;
    [SerializeField, Min(0f)] private float recentHitDuration = 2f;
    [SerializeField, Min(0.1f)] private float retreatTriggerDistance = 2.5f;
    [SerializeField, Min(0.1f)] private float preferredDistance = 4f;
    [SerializeField, Min(0.1f)] private float retreatDistance = 3f;
    [SerializeField, Min(0.1f)] private float retreatDuration = 1.2f;
    [SerializeField, Min(0f)] private float retreatCooldown = 5f;
    [SerializeField, Min(0.1f)] private float retreatSpeedMultiplier = 1.2f;

    public MonsterSquadFormation SquadFormation { get; private set; }
    public bool ExploitOpening { get; private set; }
    public Vector3 RallyPosition { get; private set; }
    public void SetRallyPosition(Vector3 position) { RallyPosition = position; }
    public void BeginSquadWithdrawal() { IsFleeing = true; }

    public float AdvanceSpeedMultiplier => MonsterSquadModel.SpeedMultiplier(squadRole);

    [Header("Disengage and despawn")]
    [SerializeField, Range(0f, 1f)] private float fleeChance = 0.05f;
    [SerializeField, Min(1f)] private float fleeCheckInterval = 8f;
    [SerializeField, Min(1f)] private float fleeDespawnDistance = 18f;
    [SerializeField, Min(0.1f)] private float fleeSpeedMultiplier = 1.2f;
    private float nextFleeCheck = float.PositiveInfinity;
    public bool IsFleeing { get; private set; }
    public float FleeSpeedMultiplier => Mathf.Max(0.1f, fleeSpeedMultiplier);

    public bool TryBeginFlee(bool targetsPlayer, float now, float roll)
    {
        if (IsFleeing) return true;
        if (!targetsPlayer) { nextFleeCheck = float.PositiveInfinity; return false; }
        // Roll once per interval at a decision boundary, never once per frame.
        if (float.IsPositiveInfinity(nextFleeCheck))
        {
            nextFleeCheck = now + Mathf.Max(1f, fleeCheckInterval);
            return false;
        }
        if (now < nextFleeCheck) return false;
        nextFleeCheck = now + Mathf.Max(1f, fleeCheckInterval);
        IsFleeing = roll < fleeChance;
        return IsFleeing;
    }

    public bool ShouldDespawnAfterFlee(float distance) => IsFleeing && distance >= Mathf.Max(1f, fleeDespawnDistance);

    public void EndFlee(float now)
    {
        IsFleeing = false;
        nextFleeCheck = now + Mathf.Max(1f, fleeCheckInterval);
    }

    private float lastHitTime = float.NegativeInfinity;
    private float nextRetreatTime = float.NegativeInfinity;
    private float retreatEndTime;
    private MonsterAttackKind? lastAttack;
    [SerializeField] private MonsterSquadRole squadRole = MonsterSquadRole.Solo;
    [SerializeField] private int engagedMonsterCount = 1;
    public MonsterSquadRole SquadRole => squadRole;
    public int EngagedMonsterCount => engagedMonsterCount;
    public float AssignedFormationAngle { get; private set; }
    public float FormationAngle => AssignedFormationAngle;

    public void SetSquadAssignment(MonsterSquadAssignment assignment)
    {
        SquadFormation = assignment.Formation;
        ExploitOpening = assignment.ExploitOpening;
        squadRole = assignment.Role;
        engagedMonsterCount = assignment.Count;
        AssignedFormationAngle = assignment.Angle;
    }
    public bool IsRetreating { get; private set; }
    public float RetreatDistance => Mathf.Max(0.1f, retreatDistance);
    public float RetreatSpeedMultiplier => Mathf.Max(0.1f, retreatSpeedMultiplier);
    public float PreferredDistance => Mathf.Max(retreatTriggerDistance + 0.5f, preferredDistance);

    public void RecordHit(float now) { lastHitTime = now; }
    public void RecordAttack(MonsterAttackKind kind) { lastAttack = kind; }
    public bool WasRecentlyHit(float now) => now - lastHitTime < Mathf.Max(0f, recentHitDuration);
    public bool IsLowHealth(float healthRatio) => healthRatio <= lowHealthRatio;

    public bool WantsRetreat(float healthRatio, float distance, bool targetsPlayer, float now)
    {
        return targetsPlayer && now >= nextRetreatTime && distance <= retreatTriggerDistance &&
            (IsLowHealth(healthRatio) || WasRecentlyHit(now));
    }

    public bool WantsDistance(float healthRatio, float distance, bool targetsPlayer, float now)
    {
        return targetsPlayer && (IsLowHealth(healthRatio) || WasRecentlyHit(now)) && distance <= PreferredDistance;
    }

    public void BeginRetreat(float now)
    {
        IsRetreating = true;
        retreatEndTime = now + Mathf.Max(0.1f, retreatDuration);
        nextRetreatTime = now + Mathf.Max(0f, retreatCooldown);
    }

    public bool RetreatExpired(float now) => now >= retreatEndTime;
    public void EndRetreat() { IsRetreating = false; }
    public void DelayFailedRetreat(float now) { nextRetreatTime = now + 0.75f; }

    public static bool AllowsAttack(MonsterSquadRole role, MonsterAttackKind kind) =>
        !(role == MonsterSquadRole.Pressure && kind == MonsterAttackKind.Projectile) &&
        !(role == MonsterSquadRole.Support && kind == MonsterAttackKind.Charge);

    public float AttackWeight(MonsterAttackKind kind, float baseWeight, float healthRatio,
        float distance, float meleeRange, float now, bool cooperating = true)
    {
        bool low = IsLowHealth(healthRatio);
        bool hit = WasRecentlyHit(now);
        float preference;
        switch (kind)
        {
            case MonsterAttackKind.Melee:
                preference = low ? 0.65f : 2f;
                break;
            case MonsterAttackKind.Projectile:
                preference = distance > meleeRange + 0.5f ? 2f : 1f;
                if (low) preference *= 2.5f;
                if (hit) preference *= 1.4f;
                break;
            case MonsterAttackKind.Circle:
                preference = distance <= 2f ? 2.5f : 1f;
                if (low) preference *= 1.7f;
                if (hit) preference *= 2f;
                break;
            default:
                preference = distance >= 2.3f ? 2.2f : 1f;
                if (low) preference *= 0.35f;
                if (hit) preference *= 0.6f;
                break;
        }
        // Prefer variety, while still allowing the only legal attack to be used.
        if (lastAttack == kind) preference *= 0.3f;
        if (cooperating)
        {
            if (!AllowsAttack(squadRole, kind)) return 0f;
            if (squadRole == MonsterSquadRole.Pressure)
                preference *= kind == MonsterAttackKind.Charge ? 6f : 1f;
            else if (squadRole == MonsterSquadRole.Support)
                preference *= kind == MonsterAttackKind.Projectile ? 12f : 0.15f;
            if (ExploitOpening && squadRole == MonsterSquadRole.Flanker && kind == MonsterAttackKind.Charge)
                preference *= 1.5f;
        }
        return Mathf.Max(0f, baseWeight) * preference;
    }
}
