using UnityEngine;

// Immutable hit data survives the firing monster's death or a change of target.
public sealed class MonsterAttackPayload
{
    public readonly int Damage;
    public readonly StatusEffectType EffectType;
    public readonly float Chance, Duration, Value, TickInterval;
    public readonly int AttackModifier, DefenseModifier;

    public MonsterAttackPayload(MonsterModel source, float multiplier)
    {
        Damage = Mathf.Max(0, Mathf.RoundToInt(source.AttackPower * multiplier));
        EffectType = source.AttackStatusEffectType;
        Chance = source.StatusEffectChance;
        Duration = source.StatusEffectDuration;
        Value = source.StatusEffectValue;
        TickInterval = source.StatusEffectTickInterval;
        AttackModifier = source.StatusAttackModifier;
        DefenseModifier = source.StatusDefenseModifier;
    }
}

public sealed class MonsterProjectileModel
{
    public Vector3 Position { get; private set; }
    public Vector3 Direction { get; }
    public float Speed { get; }
    public float Radius { get; }
    public float RemainingDistance { get; private set; }
    public MonsterAttackPayload Payload { get; }
    public bool Resolved { get; private set; }

    public MonsterProjectileModel(Vector3 origin, Vector3 direction, float speed, float radius,
        float distance, MonsterAttackPayload payload)
    {
        Position = origin;
        Direction = direction.normalized;
        Speed = speed;
        Radius = radius;
        RemainingDistance = distance;
        Payload = payload;
    }

    public float StepDistance(float deltaTime) => Mathf.Min(RemainingDistance, Speed * Mathf.Max(0f, deltaTime));
    public void Advance(float distance)
    {
        Position += Direction * distance;
        RemainingDistance = Mathf.Max(0f, RemainingDistance - distance);
    }
    public void Resolve() { Resolved = true; }
}
