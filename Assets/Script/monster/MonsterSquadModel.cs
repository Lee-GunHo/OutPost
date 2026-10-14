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
    private readonly List<MonsterSquadMember> members = new List<MonsterSquadMember>();
    private readonly Dictionary<int, MonsterSquadAssignment> assignments = new Dictionary<int, MonsterSquadAssignment>();
    private float formationAngle;
    private float nextAttackTime = float.NegativeInfinity;
    private float nextFlankTime = float.PositiveInfinity;
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
        bool coverChanged = false;
        for (int i = 0; !rosterChanged && i < incoming.Count; i++)
        {
            rosterChanged = incoming[i].Id != members[i].Id;
            coverChanged |= incoming[i].NeedsCover != members[i].NeedsCover;
        }
        PlayerHp = Math.Max(0f, Math.Min(1f, situation.PlayerHpRatio));
        bool timedPhase = Formation == MonsterSquadFormation.Regroup || Formation == MonsterSquadFormation.Withdrawal;
        if (!rosterChanged && !coverChanged && now < nextReassessment &&
            !(timedPhase && now >= phaseUntil)) return false;
        var previous = new Dictionary<int, MonsterSquadAssignment>(assignments);
        nextReassessment = now + 0.75f;

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
        string reason = rosterChanged ? "JoinedOrLeft" : coverChanged ? "NeedsCoverChanged" : "ReadinessHealthReassessment";

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
                int pressure = 0;
                float best = float.NegativeInfinity;
                for (int i = 0; i < Count; i++)
                {
                    float score = FrontScore(members[i], PlayerHp);
                    if (!members[i].NeedsCover && members[i].Id == pressureId) score += 0.8f;
                    if (score > best) { best = score; pressure = i; }
                }
                if (oldCount < 2 && Count >= 2) formationAngle = members[pressure].Angle;
                pressureId = members[pressure].Id;
                Assign(members[pressure], 0, MonsterSquadRole.Pressure, false);
                var remaining = new List<MonsterSquadMember>(members);
                remaining.RemoveAt(pressure);
                remaining.Sort((a, b) =>
                {
                    int compare = SupportScore(b).CompareTo(SupportScore(a));
                    return compare != 0 ? compare : a.Id.CompareTo(b.Id);
                });
                int slot = 1;
                foreach (var member in remaining)
                {
                    bool support = member.NeedsCover || (slot % 2 == 1 && member.CanProjectile);
                    bool opening = !support && member.CanCharge && member.ChargeReady &&
                        (situation.PlayerFocusedMember == pressureId || PlayerHp <= 0.25f);
                    Assign(member, slot++, support ? MonsterSquadRole.Support : MonsterSquadRole.Flanker, opening);
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

    private static float FrontScore(MonsterSquadMember m, float playerHp) =>
        (m.NeedsCover ? -5f : 0f) + m.HpRatio * 2f + Math.Min(2f, m.MaxHp / 50f) +
        Math.Min(1f, m.MoveSpeed / 4f) - Math.Min(2f, m.Distance * 0.15f) +
        (m.CanMelee || m.CanCircle ? 1f : -1f) + (m.MeleeReady || m.CircleReady ? 1.25f : 0f) +
        (playerHp <= 0.25f && m.ChargeReady ? 0.5f : 0f);

    private static float SupportScore(MonsterSquadMember m) =>
        (m.NeedsCover ? 5f : 0f) + (m.CanProjectile ? 3f : 0f) + (m.ProjectileReady ? 1.5f : 0f) -
        (m.CanCharge ? 1f : 0f) - (m.ChargeReady ? 0.5f : 0f) - m.MoveSpeed * 0.1f;

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

    public void AdvanceFlank(float now)
    {
        if (Count <= 1 || Formation == MonsterSquadFormation.Regroup || Formation == MonsterSquadFormation.Withdrawal)
        { nextFlankTime = float.PositiveInfinity; return; }
        if (float.IsPositiveInfinity(nextFlankTime)) { nextFlankTime = now + 2.2f; return; }
        if (now < nextFlankTime) return;
        nextFlankTime = now + 2.2f;
        float step = (float)Math.PI / 6f;
        formationAngle = Normalize(formationAngle + step);
        foreach (var member in members)
        {
            var assignment = assignments[member.Id];
            assignment.Angle = Normalize(assignment.Angle + step);
            assignments[member.Id] = assignment;
        }
    }

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
