using System;
using System.Collections.Generic;

// Encounter membership and measurement state, independent of scene queries.
public sealed class MonsterEncounterTelemetryModel
{
    public readonly string Id = Guid.NewGuid().ToString("N");
    public readonly Dictionary<string, MonsterParticipantTelemetryModel> Members = new Dictionary<string, MonsterParticipantTelemetryModel>();
    public float StartedAt, LastEngagedAt, OverlapSeconds, SurroundedSeconds;
    public int PlayerStartHp, PlayerEndHp, DamageToMonsters, SurroundCount, RoleSwitches;
    public bool WasSurrounded, Cooperation;
    public string Mode;
}

public sealed class MonsterParticipantTelemetryModel
{
    public string Id, Type, Role;
    public float JoinedAt, LastSeenAt;
    public bool Exited;
}
