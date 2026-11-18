using System;
public static class OnlineNeuralChecks
{
    private static int checks;
    private static void Check(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
    public static string Run()
    {
        checks = 0;
        var memory = new MonsterLearningModel();
        var input = MonsterOnlineNetworkModel.Encode(3, 1.8f, 1, false, false, false);
        Check(memory.Weight("normal", input, 3) == 1f, "Cold start must use baseline");
        for (int i = 0; i < 4; i++) memory.Observe("normal", input, 3, true, .06f);
        Check(memory.Weight("normal", input, 3) > 1.1f, "Early success should visibly increase multiplier");
        Check(memory.Weight("other", input, 3) == 1f, "Types must not contaminate each other");
        Check(memory.Weight("normal", input, 1) == 1f, "Unobserved actions must stay neutral");
        var network = memory.networks[0];
        Check(network.outputBias[0] == 0 && network.attempts[0] == 0, "Unselected output is not labelled a failure");
        float first = network.Predict(input, 3);
        for (int i = 0; i < 196; i++) memory.Observe("normal", input, 3, true, .06f);
        Check(network.Predict(input, 3) > first, "SGD must learn successful outcome");
        Check(memory.Weight("normal", input, 3) > 2.3f && memory.Weight("normal", input, 3) <= 2.5f, "Higher multiplier ceiling");
        for (int i = 0; i < 400; i++) memory.Observe("normal", input, 3, false, .06f);
        Check(memory.Weight("normal", input, 3) < .65f && memory.Weight("normal", input, 3) >= .5f, "Adapt to changed habits");
        Check(memory.version == 600 && network.hits[3] == 200 && network.replay.Count == 128, "Bounded replay/counts");
        Check(memory.IsValid(), "Valid neural memory rejected");
        float preserved = network.inputWeights[0]; network.inputWeights[0] = float.NaN;
        Check(!memory.IsValid(), "Corrupt neural save accepted"); network.inputWeights[0] = preserved;
        var varied = MonsterOnlineNetworkModel.Create("context-test", new[] {"A", "B"});
        var otherInput = MonsterOnlineNetworkModel.Encode(9, 1.8f, .2f, true, false, true);
        for (int i = 0; i < 400; i++) { varied.Observe(input, 0, true); varied.Observe(otherInput, 0, false); }
        Check(varied.Predict(input, 0) > .8f && varied.Predict(otherInput, 0) < .2f, "MLP must distinguish continuous situations");
        Check(MonsterOnlineNetworkModel.Encode(float.NaN, 1, 1, false, false, false) == null, "NaN input rejected");
        var boss = memory.GetOrCreateNetwork("boss/future-v1", new[] {"Smash", "Dash", "Spore", "Wave", "Summon"});
        boss.Observe(input, 4, true);
        Check(boss.actionIds.Length == 5 && boss.attempts[4] == 1, "Future bosses support separate extensible action sets");
        bool mismatch = false;
        try { memory.GetOrCreateNetwork("boss/future-v1", new[] {"Dash", "Smash"}); } catch (ArgumentException) { mismatch = true; }
        Check(mismatch, "Changed action contracts must be rejected");
        memory.networks.Add(network);
        Check(!memory.IsValid(), "Duplicate network keys rejected");
        var legacy = new MonsterLearningModel { schemaVersion = 1 };
        legacy.entries.Add(new MonsterActionExperience { monsterType="normal", context="0:0:0", action=0, attempts=1, hits=1, meanReward=.08f });
        Check(legacy.IsValid(), "Legacy statistics must remain readable for backup migration");

        return checks + " online neural model checks passed.";
    }
}
