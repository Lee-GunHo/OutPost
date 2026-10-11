using System;
using System.Collections.Generic;

// Shared individual-combat experience. No group/formation policy is learned here.
[Serializable]
public sealed class MonsterLearningModel
{
    public const int SchemaVersion = 1;
    public int schemaVersion = SchemaVersion;
    public int version;
    public List<MonsterActionExperience> entries = new List<MonsterActionExperience>();

    public static string Context(float distance, float selfHp, bool playerAttacking, bool playerDashing)
    {
        int range = distance <= 1.8f ? 0 : distance <= 3.75f ? 1 : 2;
        return range + ":" + (selfHp <= 0.35f ? 1 : 0) + ":" + (playerDashing ? 2 : playerAttacking ? 1 : 0);
    }

    public float Weight(string type, string context, int action)
    {
        var entry = Find(type, context, action);
        if (entry == null) return 1f;
        // Sparse observations stay close to the initial policy. Never suppress an action entirely.
        float confidence = entry.attempts / (entry.attempts + 12f);
        return Math.Max(0.65f, Math.Min(1.8f, 1f + entry.meanReward * confidence));
    }

    public MonsterActionExperience Observe(string type, string context, int action, bool hit)
    {
        var entry = Find(type, context, action);
        if (entry == null)
        {
            entry = new MonsterActionExperience { monsterType = type, context = context, action = action };
            entries.Add(entry);
        }
        entry.attempts++;
        if (hit) entry.hits++;
        // Bounded, gradual adaptation to recent outcomes, not just lifetime hit rate.
        float reward = hit ? 1f : -0.25f;
        entry.meanReward += 0.08f * (reward - entry.meanReward);
        version++;
        return entry;
    }

    public bool IsValid()
    {
        if (schemaVersion != SchemaVersion || version < 0 || entries == null || entries.Count > 10000) return false;
        var keys = new HashSet<string>();
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.monsterType) || string.IsNullOrEmpty(e.context) ||
                e.action < 0 || e.action > 3 || e.attempts < 0 || e.hits < 0 || e.hits > e.attempts ||
                float.IsNaN(e.meanReward) || float.IsInfinity(e.meanReward) || e.meanReward < -0.25f || e.meanReward > 1f ||
                !keys.Add(e.monsterType + "|" + e.context + "|" + e.action)) return false;
        }
        return true;
    }

    private MonsterActionExperience Find(string type, string context, int action) =>
        entries.Find(e => e.monsterType == type && e.context == context && e.action == action);
}

[Serializable]
public sealed class MonsterActionExperience
{
    public string monsterType, context;
    public int action, attempts, hits;
    public float meanReward;
}
