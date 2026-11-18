using System;
using System.Collections.Generic;

// Shared individual-combat experience. No group/formation policy is learned here.
[Serializable]
public sealed class MonsterLearningModel
{
    public const int SchemaVersion = 2;
    public const float MaximumLearningMultiplier = 2.5f, MinimumLearningMultiplier = .5f;
    public int schemaVersion = SchemaVersion;
    public int version;
    public List<MonsterActionExperience> entries = new List<MonsterActionExperience>();
    public List<MonsterOnlineNetworkModel> networks = new List<MonsterOnlineNetworkModel>();
    private static readonly string[] NormalActions = { "Melee", "Projectile", "Circle", "Charge" };
    public static string NormalKey(string type) => "normal/v2/" + type;

    public static bool IsEligible(bool naturalSpawn, bool registeredTest, bool waveMonster)
        => !waveMonster && (naturalSpawn || registeredTest);

    public static string Context(float distance, float selfHp, bool playerAttacking, bool playerDashing)
    {
        int range = distance <= 1.8f ? 0 : distance <= 3.75f ? 1 : 2;
        return range + ":" + (selfHp <= 0.35f ? 1 : 0) + ":" + (playerDashing ? 2 : playerAttacking ? 1 : 0);
    }

    public float Weight(string type, float[] input, int action)
    {
        var network = networks.Find(n => n.key == NormalKey(type));
        return network == null || !MonsterOnlineNetworkModel.ValidInput(input) ? 1f : network.Multiplier(input, action);
    }

    public float Prediction(string type, float[] input, int action)
    {
        var network = networks.Find(n => n.key == NormalKey(type));
        return network == null ? .5f : network.Predict(input, action);
    }

    // Separate keys/action contracts allow future boss adapters without mixing normal/boss experience.
    // The boss controller is deliberately NOT connected yet.
    public MonsterOnlineNetworkModel GetOrCreateNetwork(string key, string[] actionIds)
    {
        var network = networks.Find(n => n.key == key);
        if (network != null)
        {
            if (network.actionIds.Length != actionIds.Length) throw new ArgumentException("Policy action contract changed: use a new key.");
            for (int i = 0; i < actionIds.Length; i++)
                if (network.actionIds[i] != actionIds[i]) throw new ArgumentException("Policy action order changed: use a new key.");
            return network;
        }
        network = MonsterOnlineNetworkModel.Create(key, actionIds);
        networks.Add(network);
        return network;
    }

    public float Observe(string type, float[] input, int action, bool hit, float rate)
        => ObservePolicy(NormalKey(type), NormalActions, input, action, hit, rate);

    public float ObservePolicy(string key, string[] actionIds, float[] input, int action, bool hit, float rate)
    {
        float loss = GetOrCreateNetwork(key, actionIds).Observe(input, action, hit, rate);
        version++;
        return loss;
    }

    public bool IsValid()
    {
        if ((schemaVersion != 1 && schemaVersion != SchemaVersion) || version < 0 || entries == null || entries.Count > 10000) return false;
        var keys = new HashSet<string>();
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.monsterType) || string.IsNullOrEmpty(e.context) ||
                e.action < 0 || e.action > 3 || e.attempts < 0 || e.hits < 0 || e.hits > e.attempts ||
                float.IsNaN(e.meanReward) || float.IsInfinity(e.meanReward) || e.meanReward < -0.25f || e.meanReward > 1f ||
                !keys.Add(e.monsterType + "|" + e.context + "|" + e.action)) return false;
        }
        if (schemaVersion == 1) return true; // Validate legacy data before backing it up and cold-starting v2.
        if (networks == null || networks.Count > 256 || entries.Count != 0) return false;
        keys.Clear();
        foreach (var n in networks)
        {
            if (n == null || !n.IsValid() || !keys.Add(n.key)) return false;
            if (n.key.StartsWith("normal/v2/", StringComparison.Ordinal))
            {
                if (n.actionIds.Length != NormalActions.Length) return false;
                for (int i = 0; i < NormalActions.Length; i++) if (n.actionIds[i] != NormalActions[i]) return false;
            }
        }
        return true;
    }

}

[Serializable]
public sealed class MonsterActionExperience
{
    public string monsterType, context;
    public int action, attempts, hits;
    public float meanReward;
}
