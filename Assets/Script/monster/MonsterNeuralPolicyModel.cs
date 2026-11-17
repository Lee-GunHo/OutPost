using System;

// Pure data/feature contract. Also compiled by the offline dataset exporter, without Unity.
[Serializable]
public sealed class MonsterNeuralPolicyModel
{
    public const string ResourcePath = "MonsterCombat/monster_attack_mlp";
    public const string Version = "attack-preference-mlp-v1";
    public const int InputCount = 4, OutputCount = 4;
    public const float InputScale = 8f, MaxDistance = 12f, MaxMeleeRange = 6f;
    public string status = "NotLoaded";
    public string failureReason = "";
    public int inferenceCount, fallbackCount;

    [Serializable]
    public struct Observation
    {
        public float distance, meleeRange;
        public bool lowHealth, recentlyHit;
    }

    public static bool Encode(Observation observation, float[] destination)
    {
        if (destination == null || destination.Length != InputCount ||
            !Finite(observation.distance) || !Finite(observation.meleeRange) ||
            observation.distance < 0f || observation.distance > MaxDistance ||
            observation.meleeRange < 0f || observation.meleeRange > MaxMeleeRange) return false;
        destination[0] = observation.distance / InputScale;
        destination[1] = (observation.distance - observation.meleeRange) / InputScale;
        destination[2] = observation.lowHealth ? 1f : 0f;
        destination[3] = observation.recentlyHit ? 1f : 0f;
        return true;
    }

    public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    // Existing rules, retained as teacher and explicit fallback. No squad or online learning here.
    // Output order MUST match MonsterAttackKind: Melee, Projectile, Circle, Charge.
    public static float TeacherPreference(int action, Observation observation)
    {
        bool low = observation.lowHealth, hit = observation.recentlyHit;
        switch (action)
        {
            case 0: return low ? 0.65f : 2f;
            case 1:
                return (observation.distance > observation.meleeRange + 0.5f ? 2f : 1f) *
                    (low ? 2.5f : 1f) * (hit ? 1.4f : 1f);
            case 2:
                return (observation.distance <= 2f ? 2.5f : 1f) *
                    (low ? 1.7f : 1f) * (hit ? 2f : 1f);
            case 3:
                return (observation.distance >= 2.3f ? 2.2f : 1f) *
                    (low ? 0.35f : 1f) * (hit ? 0.6f : 1f);
            default: throw new ArgumentOutOfRangeException(nameof(action));
        }
    }
}
