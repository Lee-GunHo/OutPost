using System;
using System.Collections.Generic;

// Runtime-trained MLP. No Unity, pretrained asset, teacher labels or Python process.
// One independent sigmoid output per action; BCE gradients use ONLY the chosen action.
[Serializable]
public sealed class MonsterOnlineNetworkModel
{
    public const int Inputs = 7, Hidden = 16, ReplayCapacity = 128;
    public const string Contract = "combat-outcome-mlp-v2";
    public string key;
    public string[] actionIds;
    public float[] inputWeights, hiddenBias, outputWeights, outputBias;
    public int[] attempts, hits;
    public int updates, replayCursor;
    public List<MonsterNeuralExperience> replay = new List<MonsterNeuralExperience>();

    public static MonsterOnlineNetworkModel Create(string key, string[] actions)
    {
        if (string.IsNullOrEmpty(key) || actions == null || actions.Length < 1 || actions.Length > 32)
            throw new ArgumentException("Invalid neural policy contract.");
        var ids = new HashSet<string>();
        foreach (string id in actions)
            if (string.IsNullOrEmpty(id) || !ids.Add(id)) throw new ArgumentException("Action IDs must be unique and stable.");
        var n = new MonsterOnlineNetworkModel { key = key, actionIds = (string[])actions.Clone(),
            inputWeights = new float[Inputs * Hidden], hiddenBias = new float[Hidden],
            outputWeights = new float[Hidden * actions.Length], outputBias = new float[actions.Length],
            attempts = new int[actions.Length], hits = new int[actions.Length] };
        var random = new Random(1731);
        for (int i = 0; i < n.inputWeights.Length; i++) n.inputWeights[i] = (float)(random.NextDouble() - .5) * .8f;
        // Zero output heads give exactly p=.5 / multiplier=1 at cold start.
        return n;
    }

    public static float[] Encode(float distance, float meleeRange, float hp, bool recentHit, bool attacking, bool dashing)
    {
        if (!Finite(distance) || !Finite(meleeRange) || !Finite(hp) || distance < 0 || meleeRange < 0) return null;
        return new[] { Clamp(distance / 12f, 0, 1), Clamp((distance - meleeRange) / 12f, -1, 1),
            Clamp(hp, 0, 1), recentHit ? 1f : 0f, !attacking && !dashing ? 1f : 0f,
            attacking && !dashing ? 1f : 0f, dashing ? 1f : 0f };
    }

    public float Predict(float[] input, int action)
    {
        if (!ValidInput(input) || action < 0 || action >= actionIds.Length) throw new ArgumentException("Invalid neural observation/action.");
        return Forward(input, action, new float[Hidden]);
    }

    private float Forward(float[] input, int action, float[] hidden)
    {
        for (int h = 0; h < Hidden; h++)
        {
            float sum = hiddenBias[h];
            for (int i = 0; i < Inputs; i++) sum += inputWeights[h * Inputs + i] * input[i];
            hidden[h] = (float)Math.Tanh(sum);
        }
        float logit = outputBias[action];
        for (int h = 0; h < Hidden; h++) logit += outputWeights[action * Hidden + h] * hidden[h];
        return 1f / (1f + (float)Math.Exp(-Clamp(logit, -20, 20)));
    }

    public float Multiplier(float[] input, int action)
    {
        if (attempts[action] == 0) return 1f;
        float centered = (Predict(input, action) - .5f) * 2f;
        float confidence = attempts[action] / (attempts[action] + 4f);
        return Clamp(1f + centered * confidence * (centered >= 0 ? 1.5f : .5f),
            MonsterLearningModel.MinimumLearningMultiplier, MonsterLearningModel.MaximumLearningMultiplier);
    }

    public float Observe(float[] input, int action, bool success, float rate = .06f)
    {
        if (!ValidInput(input) || action < 0 || action >= actionIds.Length || !Finite(rate) || rate <= 0 || rate > .2f)
            throw new ArgumentException("Invalid training sample/rate.");
        var sample = new MonsterNeuralExperience { input = (float[])input.Clone(), action = action, success = success };
        if (replay.Count < ReplayCapacity) replay.Add(sample);
        else { replay[replayCursor] = sample; replayCursor = (replayCursor + 1) % ReplayCapacity; }
        attempts[action]++; if (success) hits[action]++;
        float loss = Train(sample, rate);
        // Bounded work per completed action, never every frame. Retain recent counter-examples.
        for (int k = 0; k < 3; k++) Train(replay[(int)(((long)updates * 31 + k * 47) % replay.Count)], rate);
        updates++;
        return loss;
    }

    private float Train(MonsterNeuralExperience sample, float rate)
    {
        var hidden = new float[Hidden];
        float probability = Forward(sample.input, sample.action, hidden);
        float target = sample.success ? 1f : 0f;
        float error = probability - target; // sigmoid + binary cross-entropy derivative
        int offset = sample.action * Hidden;
        for (int h = 0; h < Hidden; h++)
        {
            float gradient = error * outputWeights[offset + h] * (1f - hidden[h] * hidden[h]);
            for (int i = 0; i < Inputs; i++)
                inputWeights[h * Inputs + i] = Step(inputWeights[h * Inputs + i], gradient * sample.input[i], rate);
            hiddenBias[h] = Step(hiddenBias[h], gradient, rate);
            outputWeights[offset + h] = Step(outputWeights[offset + h], error * hidden[h], rate);
        }
        outputBias[sample.action] = Step(outputBias[sample.action], error, rate);
        return -(float)Math.Log(Math.Max(1e-7f, sample.success ? probability : 1 - probability));
    }

    private static float Step(float weight, float gradient, float rate) => Clamp(weight - rate * Clamp(gradient, -1, 1), -8, 8);
    private static float Clamp(float value, float low, float high) => Math.Max(low, Math.Min(high, value));
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    public static bool ValidInput(float[] input)
    {
        if (input == null || input.Length != Inputs) return false;
        foreach (float value in input) if (!Finite(value) || value < -1 || value > 1) return false;
        return true;
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(key) || actionIds == null || actionIds.Length < 1 || actionIds.Length > 32 ||
            updates < 0 || replayCursor < 0 || replayCursor >= ReplayCapacity || replay == null || replay.Count > ReplayCapacity) return false;
        var ids = new HashSet<string>();
        foreach (string id in actionIds) if (string.IsNullOrEmpty(id) || !ids.Add(id)) return false;
        if (!ValidWeights(inputWeights, Inputs * Hidden) || !ValidWeights(hiddenBias, Hidden) ||
            !ValidWeights(outputWeights, Hidden * actionIds.Length) || !ValidWeights(outputBias, actionIds.Length) ||
            attempts == null || hits == null || attempts.Length != actionIds.Length || hits.Length != actionIds.Length) return false;
        long total = 0;
        for (int a = 0; a < actionIds.Length; a++)
        {
            if (attempts[a] < 0 || hits[a] < 0 || hits[a] > attempts[a]) return false;
            total += attempts[a];
        }
        if (total != updates || replay.Count != Math.Min(updates, ReplayCapacity)) return false;
        foreach (var e in replay) if (e == null || !ValidInput(e.input) || e.action < 0 || e.action >= actionIds.Length) return false;
        return true;
    }

    private static bool ValidWeights(float[] weights, int count)
    {
        if (weights == null || weights.Length != count) return false;
        foreach (float w in weights) if (!Finite(w) || Math.Abs(w) > 8f) return false;
        return true;
    }
}

[Serializable]
public sealed class MonsterNeuralExperience
{
    public float[] input;
    public int action;
    public bool success;
}
