using System;
using System.Collections.Generic;

public enum MonsterSquadRole { Solo, Pressure, Support, Flanker }

public struct MonsterSquadMember
{
    public int Id;
    public float Distance;
    public float Angle;
    public bool NeedsCover;
    public float HpRatio, MaxHp, MoveSpeed;
    public bool CanProjectile, CanCharge;
}

public struct MonsterSquadAssignment
{
    public MonsterSquadRole Role;
    public int Count;
    public float Angle;
}

// Shared encounter data; no scene queries, movement, or presentation here.
public sealed class MonsterSquadModel
{
    private readonly List<MonsterSquadMember> members = new List<MonsterSquadMember>();
    private readonly Dictionary<int, MonsterSquadAssignment> assignments = new Dictionary<int, MonsterSquadAssignment>();
    private float formationAngle;
    private float nextAttackTime = float.NegativeInfinity;
    private float nextFlankTime = float.PositiveInfinity;
    public int Count => members.Count;
    public string ChangeReason { get; private set; } = "Initial";
    private float nextReassessment;

    public bool Synchronize(List<MonsterSquadMember> incoming, float now = 0f)
    {
        incoming.Sort((a, b) => a.Id.CompareTo(b.Id));
        bool rosterChanged = incoming.Count != members.Count;
        bool healthChanged = false;
        for (int i = 0; !rosterChanged && i < incoming.Count; i++)
        {
            rosterChanged = incoming[i].Id != members[i].Id;
            healthChanged |= incoming[i].NeedsCover != members[i].NeedsCover;
        }
        if (!rosterChanged && !healthChanged && now < nextReassessment) return false;
        nextReassessment = now + 2f;
        int pressure = -1;
        float best = float.NegativeInfinity;
        for (int i = 0; i < incoming.Count; i++)
        {
            var member = incoming[i];
            float score = FrontScore(member);
            // Hysteresis prevents tiny health/distance changes from constantly swapping roles.
            if (!member.NeedsCover && assignments.TryGetValue(member.Id, out var old) && old.Role == MonsterSquadRole.Pressure)
                score += 1f;
            if (score > best) { best = score; pressure = i; }
        }
        if (members.Count < 2 && incoming.Count >= 2) formationAngle = incoming[pressure].Angle;
        var previous = new Dictionary<int, MonsterSquadAssignment>(assignments);
        members.Clear();
        members.AddRange(incoming);
        assignments.Clear();
        if (members.Count > 0)
        {
            Assign(members[pressure], 0, MonsterSquadRole.Pressure);
            var remaining = new List<MonsterSquadMember>(members);
            remaining.RemoveAt(pressure);
            // A ranged-capable or wounded monster gets the support slot first.
            remaining.Sort((a, b) =>
            {
                int compare = SupportScore(b).CompareTo(SupportScore(a));
                return compare != 0 ? compare : a.Id.CompareTo(b.Id);
            });
            int slot = 1;
            foreach (var member in remaining)
            {
                bool support = member.NeedsCover || (slot % 2 == 1 && member.CanProjectile);
                Assign(member, slot++, support ? MonsterSquadRole.Support : MonsterSquadRole.Flanker);
            }
        }
        bool changed = rosterChanged;
        foreach (var pair in assignments)
            changed |= !previous.TryGetValue(pair.Key, out var old) || old.Role != pair.Value.Role || old.Angle != pair.Value.Angle;
        ChangeReason = rosterChanged ? "JoinedOrLeft" : healthChanged ? "NeedsCoverChanged" : "CapabilityHealthReassessment";
        return changed;
    }

    private static float FrontScore(MonsterSquadMember m) =>
        (m.NeedsCover ? -5f : 0f) + m.HpRatio * 2f + Math.Min(2f, m.MaxHp / 50f) +
        Math.Min(1f, m.MoveSpeed / 4f) - Math.Min(2f, m.Distance * 0.15f);

    private static float SupportScore(MonsterSquadMember m) =>
        (m.NeedsCover ? 5f : 0f) + (m.CanProjectile ? 3f : 0f) - (m.CanCharge ? 1f : 0f) - m.MoveSpeed * 0.1f;

    private void Assign(MonsterSquadMember member, int slot, MonsterSquadRole role)
    {
        assignments[member.Id] = new MonsterSquadAssignment
        {
            Role = Count <= 1 ? MonsterSquadRole.Solo : role, Count = Count,
            Angle = formationAngle + slot * (float)(Math.PI * 2) / Count
        };
    }

    public MonsterSquadAssignment GetAssignment(int id) => assignments.TryGetValue(id, out var value)
        ? value : new MonsterSquadAssignment { Role = MonsterSquadRole.Solo, Count = 1 };

    public void AdvanceFlank(float now)
    {
        if (Count <= 1) { nextFlankTime = float.PositiveInfinity; return; }
        if (float.IsPositiveInfinity(nextFlankTime)) { nextFlankTime = now + 2.2f; return; }
        if (now < nextFlankTime) return;
        nextFlankTime = now + 2.2f;
        float step = (float)Math.PI / 6f; // 30 degrees, preserving spacing between every slot.
        formationAngle = (formationAngle + step) % (float)(Math.PI * 2);
        foreach (var member in members)
        {
            MonsterSquadAssignment assignment = assignments[member.Id];
            assignment.Angle = (assignment.Angle + step) % (float)(Math.PI * 2);
            assignments[member.Id] = assignment;
        }
    }

    public bool CanStartAttack(float now, int activeAttacks) => Count <= 1 ||
        (now >= nextAttackTime && activeAttacks < (Count >= 3 ? 2 : 1));

    public bool TryStartAttack(float now, int activeAttacks)
    {
        if (!CanStartAttack(now, activeAttacks)) return false;
        if (Count > 1) nextAttackTime = now + 0.65f;
        return true;
    }
}
