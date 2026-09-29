using System;
using System.Collections.Generic;

public enum MonsterSquadRole { Solo, Pressure, Support, Flanker }

public struct MonsterSquadMember
{
    public int Id;
    public float Distance;
    public float Angle;
    public bool NeedsCover;
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

    public void Synchronize(List<MonsterSquadMember> incoming)
    {
        incoming.Sort((a, b) => a.Id.CompareTo(b.Id));
        bool changed = incoming.Count != members.Count;
        for (int i = 0; !changed && i < incoming.Count; i++)
            changed = incoming[i].Id != members[i].Id || incoming[i].NeedsCover != members[i].NeedsCover;
        if (!changed) return;

        int pressure = -1;
        // Preserve the front monster while it can still fight; wounded/retreating members get cover.
        for (int i = 0; i < incoming.Count; i++)
            if (!incoming[i].NeedsCover && assignments.TryGetValue(incoming[i].Id, out var old) &&
                old.Role == MonsterSquadRole.Pressure) { pressure = i; break; }
        if (pressure < 0)
        {
            for (int i = 0; i < incoming.Count; i++)
            {
                if (pressure < 0 || (incoming[pressure].NeedsCover && !incoming[i].NeedsCover) ||
                    (incoming[pressure].NeedsCover == incoming[i].NeedsCover && incoming[i].Distance < incoming[pressure].Distance))
                    pressure = i;
            }
        }
        if (members.Count < 2 && incoming.Count >= 2) formationAngle = incoming[pressure].Angle;
        members.Clear();
        members.AddRange(incoming);
        assignments.Clear();
        if (members.Count == 0) return;

        Assign(members[pressure], 0);
        int slot = 1;
        foreach (var member in members)
            if (member.Id != members[pressure].Id) Assign(member, slot++);
    }

    private void Assign(MonsterSquadMember member, int slot)
    {
        MonsterSquadRole role = Count <= 1 ? MonsterSquadRole.Solo
            : slot == 0 ? MonsterSquadRole.Pressure
            : slot % 2 == 0 && !member.NeedsCover ? MonsterSquadRole.Flanker : MonsterSquadRole.Support;
        assignments[member.Id] = new MonsterSquadAssignment
        {
            Role = role, Count = Count,
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
