using System;
using System.Collections.Generic;

public enum MonsterSquadRole { Solo, Pressure, Support, Flanker, Regroup, Withdraw }
public enum MonsterSquadFormation { Solo, Pair, Triangle, Surround, Regroup, Withdrawal }

public struct MonsterSquadMember
{
    public int Id;
    public float Distance, Angle, HpRatio, MaxHp, MoveSpeed;
    public bool NeedsCover, IsWave;
    public bool CanMelee, CanCircle, CanProjectile, CanCharge;
    public bool MeleeReady, CircleReady, ProjectileReady, ChargeReady;
}

public struct MonsterSquadSituation
{
    public float PlayerHpRatio;
    public int PlayerFocusedMember;
    public bool AllowWaveWithdrawal;
}

public struct MonsterSquadAssignment
{
    public MonsterSquadRole Role;
    public MonsterSquadFormation Formation;
    public int Count;
    public float Angle;
    public bool ExploitOpening;
}

// Rule-based group policy. No learned weights or player-pattern memory are read here.
public sealed class MonsterSquadModel
{
    public const int PolicyVersion = 3;
    public const float ReassessmentInterval = 2f;
    public static float SpeedMultiplier(MonsterSquadRole role) => role == MonsterSquadRole.Flanker ? 1.25f : 1f;
    public static float DamageTakenMultiplier(MonsterSquadRole role) => role == MonsterSquadRole.Pressure ? 0.5f : 1f;
    public static float ProjectileRangeMultiplier(MonsterSquadRole role) => role == MonsterSquadRole.Support ? 1.5f : 1f;

    // Round to whole members, keeping a front and a ranged support in groups of two or more.
    public static void RoleCounts(int count, out int pressure, out int support, out int flank)
    {
        if (count <= 1) { pressure = support = flank = 0; return; }
        pressure = Math.Max(1, (int)Math.Round(count / 6f, MidpointRounding.AwayFromZero));
        flank = Math.Min(count - pressure - 1, (int)Math.Round(count / 3f, MidpointRounding.AwayFromZero));
        support = count - pressure - flank;
    }
    private readonly List<MonsterSquadMember> members = new List<MonsterSquadMember>();
    private readonly Dictionary<int, MonsterSquadAssignment> assignments = new Dictionary<int, MonsterSquadAssignment>();
    private float formationAngle;
    private float nextAttackTime = float.NegativeInfinity;
    private float nextReassessment, phaseUntil, nextCollapseAllowed;
    private float tradeWindowStart, damageDealt, damageTaken;
    private int pressureId;
    public int Count => members.Count;
    public MonsterSquadFormation Formation { get; private set; }
    public string ChangeReason { get; private set; } = "Initial";
    public string FormationReason { get; private set; } = "Initial";
    public float RallyAngle { get; private set; }
    public float AverageHp { get; private set; }
    public float PlayerHp { get; private set; } = 1f;

    public void RecordDamage(float dealt, float taken)
    {
        damageDealt += Math.Max(0f, dealt);
        damageTaken += Math.Max(0f, taken);
    }

    public bool Synchronize(List<MonsterSquadMember> incoming, float now = 0f) =>
        Synchronize(incoming, now, new MonsterSquadSituation { PlayerHpRatio = 1f });

    public bool Synchronize(List<MonsterSquadMember> incoming, float now, MonsterSquadSituation situation)
    {
        incoming.Sort((a, b) => a.Id.CompareTo(b.Id));
        var previousFormation = Formation;
        int oldCount = Count;
        bool rosterChanged = incoming.Count != oldCount;
        for (int i = 0; !rosterChanged && i < incoming.Count; i++)
        {
            rosterChanged = incoming[i].Id != members[i].Id;
        }
        PlayerHp = Math.Max(0f, Math.Min(1f, situation.PlayerHpRatio));
        bool timedPhase = Formation == MonsterSquadFormation.Regroup || Formation == MonsterSquadFormation.Withdrawal;
        if (!rosterChanged && now < nextReassessment &&
            !(timedPhase && now >= phaseUntil)) return false;
        var previous = new Dictionary<int, MonsterSquadAssignment>(assignments);
        nextReassessment = now + ReassessmentInterval;

        if (oldCount == 0 && incoming.Count > 0) formationAngle = incoming[0].Angle;
        bool frontLost = pressureId != 0 && !incoming.Exists(m => m.Id == pressureId);
        float hp = 0f, capacity = 0f;
        foreach (var member in incoming) { hp += member.HpRatio; capacity += member.MaxHp; }
        AverageHp = incoming.Count > 0 ? hp / incoming.Count : 0f;
        bool poorTrade = now - tradeWindowStart >= 6f && damageTaken >= Math.Max(10f, capacity * 0.25f) &&
            damageDealt < damageTaken * 0.15f && PlayerHp >= 0.6f;
        if (now - tradeWindowStart >= 6f)
        {
            tradeWindowStart = now;
            damageDealt = damageTaken = 0f;
        }
        bool critical = AverageHp <= 0.25f && PlayerHp >= 0.6f;
        bool lostFormation = oldCount >= 3 && incoming.Count > 0 && incoming.Count <= oldCount / 2;
        string reason = rosterChanged ? "JoinedOrLeft" : "HealthRoleReassessment";

        if (incoming.Count == 0)
        {
            Formation = MonsterSquadFormation.Solo;
            pressureId = 0;
        }
        else if (Formation == MonsterSquadFormation.Withdrawal && now < phaseUntil)
        {
            reason = "WithdrawalInProgress";
        }
        else if (Formation == MonsterSquadFormation.Regroup && now < phaseUntil)
        {
            reason = "RegroupInProgress";
        }
        else if (Formation == MonsterSquadFormation.Regroup)
        {
            bool canWithdraw = incoming.Exists(m => !m.IsWave || situation.AllowWaveWithdrawal);
            if (critical && canWithdraw)
            {
                Formation = MonsterSquadFormation.Withdrawal;
                phaseUntil = now + 4f;
                reason = "LowHealthWithdrawal";
            }
            else
            {
                Formation = NormalFormation(incoming.Count);
                reason = "RegroupFinished";
            }
            nextCollapseAllowed = now + 12f;
        }
        else if (Formation == MonsterSquadFormation.Withdrawal)
        {
            Formation = NormalFormation(incoming.Count);
            nextCollapseAllowed = now + 12f;
            reason = "WithdrawalOrderExpired";
        }
        else if (now >= nextCollapseAllowed && (oldCount >= 2 || incoming.Count >= 2) &&
            (critical || poorTrade || ((frontLost || lostFormation) && PlayerHp > 0.25f)))
        {
            Formation = MonsterSquadFormation.Regroup;
            phaseUntil = now + 3f;
            // Gather on the previous front's side instead of circling to the opposite side.
            RallyAngle = formationAngle;
            reason = critical ? "LowAverageHealth" : poorTrade ? "UnfavorableDamageTrade" :
                frontLost ? "FrontLeft" : "InsufficientFormationMembers";
        }
        else Formation = NormalFormation(incoming.Count);

        members.Clear();
        members.AddRange(incoming);
        assignments.Clear();
        if (Count > 0)
        {
            if (Formation == MonsterSquadFormation.Regroup || Formation == MonsterSquadFormation.Withdrawal)
            {
                int slot = 0;
                foreach (var member in members)
                    Assign(member, slot++, Formation == MonsterSquadFormation.Withdrawal && (!member.IsWave || situation.AllowWaveWithdrawal)
                        ? MonsterSquadRole.Withdraw : MonsterSquadRole.Regroup, false);
            }
            else
            {
                var ranked = new List<MonsterSquadMember>(members);
                ranked.Sort((a, b) =>
                {
                    int compare = (b.HpRatio * b.MaxHp).CompareTo(a.HpRatio * a.MaxHp);
                    return compare != 0 ? compare : a.Id.CompareTo(b.Id);
                });
                if (oldCount < 2 && Count >= 2) formationAngle = ranked[0].Angle;
                pressureId = ranked[0].Id;
                if (Count == 1) Assign(ranked[0], 0, MonsterSquadRole.Solo, false);
                else
                {
                    RoleCounts(Count, out int pressure, out int support, out int flank);
                    AssignRoleSlots(ranked.GetRange(0, pressure), MonsterSquadRole.Pressure, previous, situation);
                    AssignRoleSlots(ranked.GetRange(pressure, flank), MonsterSquadRole.Flanker, previous, situation);
                    AssignRoleSlots(ranked.GetRange(pressure + flank, support), MonsterSquadRole.Support, previous, situation);
                }
            }
        }
        bool changed = rosterChanged || previousFormation != Formation;
        foreach (var pair in assignments)
            changed |= !previous.TryGetValue(pair.Key, out var old) || old.Role != pair.Value.Role ||
                AngleDistance(old.Angle, pair.Value.Angle) > 0.001f || old.ExploitOpening != pair.Value.ExploitOpening;
        ChangeReason = reason;
        if (previousFormation != Formation) FormationReason = reason;
        return changed;
    }

    private static MonsterSquadFormation NormalFormation(int count) => count <= 1 ? MonsterSquadFormation.Solo
        : count == 2 ? MonsterSquadFormation.Pair : count == 3 ? MonsterSquadFormation.Triangle : MonsterSquadFormation.Surround;

    private void AssignRoleSlots(List<MonsterSquadMember> roleMembers, MonsterSquadRole role,
        Dictionary<int, MonsterSquadAssignment> previous, MonsterSquadSituation situation)
    {
        var slots = new List<float>();
        for (int i = 0; i < roleMembers.Count; i++)
        {
            // Support stays behind our front, not on the opposite side of the player.
            // Flankers fill the remaining circle (two flankers: +120 and -120 degrees).
            float offset = role == MonsterSquadRole.Flanker
                ? (i + 1) * (float)(Math.PI * 2) / (roleMembers.Count + 1)
                : roleMembers.Count == 1 ? 0f
                : (i / (float)(roleMembers.Count - 1) - 0.5f) * (float)Math.PI *
                    (role == MonsterSquadRole.Support ? 0.4f : 0.22f);
            slots.Add(Normalize(formationAngle + offset));
        }
        // Keep existing members in their slot when HP order changes within the same role.
        roleMembers.Sort((a, b) =>
        {
            bool oldA = previous.TryGetValue(a.Id, out var pa) && pa.Role == role;
            bool oldB = previous.TryGetValue(b.Id, out var pb) && pb.Role == role;
            return oldA != oldB ? (oldA ? -1 : 1) : a.Id.CompareTo(b.Id);
        });
        foreach (var member in roleMembers)
        {
            float desired = previous.TryGetValue(member.Id, out var old) && old.Role == role ? old.Angle : member.Angle;
            int best = 0;
            for (int i = 1; i < slots.Count; i++)
                if (AngleDistance(desired, slots[i]) < AngleDistance(desired, slots[best])) best = i;
            assignments[member.Id] = new MonsterSquadAssignment
            {
                Role = role, Formation = Formation, Count = Count, Angle = slots[best],
                ExploitOpening = role == MonsterSquadRole.Flanker && member.CanCharge && member.ChargeReady &&
                    (situation.PlayerFocusedMember == pressureId || PlayerHp <= 0.25f)
            };
            slots.RemoveAt(best);
        }
    }

    private void Assign(MonsterSquadMember member, int slot, MonsterSquadRole role, bool opening)
    {
        assignments[member.Id] = new MonsterSquadAssignment
        {
            Role = Formation == MonsterSquadFormation.Solo ? MonsterSquadRole.Solo : role,
            Formation = Formation, Count = Count, ExploitOpening = opening,
            Angle = Normalize(formationAngle + slot * (float)(Math.PI * 2) / Count)
        };
    }

    private static float Normalize(float angle)
    {
        float full = (float)(Math.PI * 2);
        return (angle % full + full) % full;
    }
    private static float AngleDistance(float a, float b)
    {
        float gap = Math.Abs(Normalize(a) - Normalize(b));
        return Math.Min(gap, (float)(Math.PI * 2) - gap);
    }

    public MonsterSquadAssignment GetAssignment(int id) => assignments.TryGetValue(id, out var value)
        ? value : new MonsterSquadAssignment { Role = MonsterSquadRole.Solo, Count = 1 };

    public bool CanStartAttack(float now, int activeAttacks) =>
        Formation != MonsterSquadFormation.Regroup && Formation != MonsterSquadFormation.Withdrawal &&
        (Count <= 1 || (now >= nextAttackTime && activeAttacks < (Count >= 3 ? 2 : 1)));

    public bool TryStartAttack(float now, int activeAttacks)
    {
        if (!CanStartAttack(now, activeAttacks)) return false;
        if (Count > 1) nextAttackTime = now + (PlayerHp <= 0.25f ? 0.5f : 0.65f);
        return true;
    }
}
