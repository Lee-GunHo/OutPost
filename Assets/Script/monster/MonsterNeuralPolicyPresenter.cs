// Legacy offline-model verification only. Live combat uses MonsterOnlineNetworkModel.
using System;
using Unity.InferenceEngine;
using UnityEngine;

// One shared CPU worker per research coordinator, not one worker per monster.
// Existing MonsterLearningView displays diagnostics; this presenter owns inference and cleanup only.
public sealed class MonsterNeuralPolicyPresenter : IDisposable
{
    public MonsterNeuralPolicyModel Model { get; } = new MonsterNeuralPolicyModel();
    private Worker worker;
    private Tensor<float> input;
    private readonly float[] features = new float[MonsterNeuralPolicyModel.InputCount];
    private bool attemptedLoad;

    public void Initialize(ModelAsset asset = null)
    {
        if (attemptedLoad) return;
        attemptedLoad = true;
        try
        {
            if (asset == null) asset = Resources.Load<ModelAsset>(MonsterNeuralPolicyModel.ResourcePath);
            if (asset == null) throw new InvalidOperationException("Bundled ONNX model is missing.");
            var network = ModelLoader.Load(asset);
            if (network.inputs.Count != 1 || network.outputs.Count != 1)
                throw new InvalidOperationException("Expected one input and one output.");
            worker = new Worker(network, BackendType.CPU);
            input = new Tensor<float>(new TensorShape(1, MonsterNeuralPolicyModel.InputCount), features);
            // Warm up outside combat; also detects unsupported operators and wrong tensor shapes.
            Predict(new MonsterNeuralPolicyModel.Observation { distance = 3f, meleeRange = 1.8f }, new float[4]);
            Model.status = "Ready";
        }
        catch (Exception exception) { Fail(exception); }
    }

    public bool TryPredict(MonsterNeuralPolicyModel.Observation observation, float[] result, out string source)
    {
        source = "RuleFallback";
        if (!MonsterNeuralPolicyModel.Encode(observation, features))
        {
            Model.fallbackCount++;
            source = "RuleOutOfDomain";
            return false;
        }
        Initialize();
        if (worker == null) { Model.fallbackCount++; return false; }
        try
        {
            Predict(observation, result);
            Model.inferenceCount++;
            source = "MLP";
            return true;
        }
        catch (Exception exception)
        {
            Fail(exception);
            Model.fallbackCount++;
            return false;
        }
    }

    private void Predict(MonsterNeuralPolicyModel.Observation observation, float[] result)
    {
        if (!MonsterNeuralPolicyModel.Encode(observation, features) || result == null || result.Length != 4)
            throw new InvalidOperationException("Invalid neural policy observation or output buffer.");
        input.Upload(features);
        worker.Schedule(input);
        var output = worker.PeekOutput() as Tensor<float>;
        if (output == null || output.shape.rank != 2 || output.shape[0] != 1 || output.shape[1] != 4)
            throw new InvalidOperationException("Expected preference output [1,4].");
        using (var values = output.ReadbackAndClone())
        {
            for (int i = 0; i < 4; i++)
            {
                float value = values[i];
                if (!MonsterNeuralPolicyModel.Finite(value) || value <= 0f || value > 20f)
                    throw new InvalidOperationException("Non-positive, non-finite or excessive neural preference.");
                result[i] = value;
            }
        }
    }

    private void Fail(Exception exception)
    {
        Dispose();
        Model.status = "Fallback";
        Model.failureReason = exception.Message;
        Debug.LogWarning("[Monster MLP] Using original rules: " + exception.Message);
    }

    public void Dispose()
    {
        worker?.Dispose();
        worker = null;
        input?.Dispose();
        input = null;
    }
}
