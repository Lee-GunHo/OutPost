using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only validation coordinator. Does not enter Play mode or change scenes/save data.
[InitializeOnLoad]
public static class MonsterNeuralPolicyVerification
{
    private const string ReportFolder = "Reports/MonsterAI/neural-policy";
    private const string RequestPath = "Temp/MonsterNeuralVerify.request";
    [Serializable] private sealed class ProbeSet { public Probe[] cases; }
    [Serializable] private sealed class Probe
    {
        public float distance, meleeRange;
        public bool lowHealth, recentlyHit;
        public float[] expected;
    }
    [Serializable] private sealed class Result
    {
        public bool passed;
        public string modelVersion, unityVersion, backend, error;
        public int probes, inferenceCount, assertions;
        public float maximumAbsoluteDifference;
    }

    static MonsterNeuralPolicyVerification() { EditorApplication.update += CheckRequest; }

    private static void CheckRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode ||
            !File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        Verify();
    }

    [MenuItem("Tools/OutPost/Monster AI/Legacy - Verify Pretrained MLP")]
    public static void Verify()
    {
        var result = new Result
        {
            modelVersion = MonsterNeuralPolicyModel.Version, unityVersion = Application.unityVersion,
            backend = "Unity.InferenceEngine CPU"
        };
        GameObject testObject = null;
        try
        {
            AssetDatabase.ImportAsset("Assets/Resources/MonsterCombat/monster_attack_mlp.onnx", ImportAssetOptions.ForceUpdate);
            var probes = JsonUtility.FromJson<ProbeSet>(File.ReadAllText(ReportFolder + "/unity_probes.json"));
            var actual = new float[4];
            using (var presenter = new MonsterNeuralPolicyPresenter())
            {
                foreach (var probe in probes.cases)
                {
                    var observation = new MonsterNeuralPolicyModel.Observation
                    {
                        distance = probe.distance, meleeRange = probe.meleeRange,
                        lowHealth = probe.lowHealth, recentlyHit = probe.recentlyHit
                    };
                    Require(presenter.TryPredict(observation, actual, out string source) && source == "MLP", "Neural inference fell back.", result);
                    for (int i = 0; i < 4; i++)
                    {
                        float difference = Mathf.Abs(actual[i] - probe.expected[i]);
                        result.maximumAbsoluteDifference = Mathf.Max(result.maximumAbsoluteDifference, difference);
                        Require(difference < 0.0001f, "Python / Unity output mismatch.", result);
                    }
                    result.probes++;
                }
                Require(!presenter.TryPredict(new MonsterNeuralPolicyModel.Observation { distance = 13f }, actual, out string outOfDomain) &&
                    outOfDomain == "RuleOutOfDomain", "Out-of-domain input must fall back.", result);
                Require(!presenter.TryPredict(new MonsterNeuralPolicyModel.Observation { distance = float.NaN }, actual, out _), "NaN input accepted.", result);
                result.inferenceCount = presenter.Model.inferenceCount;
            }

            // Boundary semantics must remain exact in the original rule fallback.
            var near = new MonsterNeuralPolicyModel.Observation { distance = 2f, meleeRange = 1.5f, lowHealth = true, recentlyHit = true };
            Require(Mathf.Approximately(MonsterNeuralPolicyModel.TeacherPreference(1, near), 3.5f), "Projectile boundary changed.", result);
            Require(Mathf.Approximately(MonsterNeuralPolicyModel.TeacherPreference(2, near), 8.5f), "Circle boundary changed.", result);
            near.distance = 2.3f;
            Require(Mathf.Approximately(MonsterNeuralPolicyModel.TeacherPreference(3, near), 0.462f), "Charge boundary changed.", result);
            testObject = new GameObject("MLP verification (temporary)") { hideFlags = HideFlags.HideAndDontSave };
            var behavior = testObject.AddComponent<MonsterBehaviorModel>();
            behavior.SetSquadAssignment(new MonsterSquadAssignment { Role = MonsterSquadRole.Pressure });
            Require(behavior.ApplyRoleWeight(MonsterAttackKind.Projectile, 2f, 7f, true) == 0f, "Pressure projectile ban lost.", result);
            Require(Mathf.Approximately(behavior.ApplyRoleWeight(MonsterAttackKind.Charge, 2f, 2.2f, true), 26.4f), "Pressure charge multiplier changed.", result);
            behavior.SetSquadAssignment(new MonsterSquadAssignment { Role = MonsterSquadRole.Support });
            Require(behavior.ApplyRoleWeight(MonsterAttackKind.Charge, 2f, 2.2f, true) == 0f, "Support charge ban lost.", result);
            Require(behavior.ApplyRoleWeight(MonsterAttackKind.Projectile, 2f, 2f, true) == 48f, "Support multiplier changed.", result);
            Require(behavior.ApplyRoleWeight(MonsterAttackKind.Charge, 2f, 2.2f, false) == 4.4f, "Disabled cooperation still applies role restrictions.", result);
            result.passed = true;
        }
        catch (Exception exception) { result.error = exception.ToString(); Debug.LogException(exception); }
        finally
        {
            if (testObject != null) UnityEngine.Object.DestroyImmediate(testObject);
            Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(ReportFolder + "/unity_verification.json", JsonUtility.ToJson(result, true));
        }
        Debug.Log("[Monster MLP] Verification " + (result.passed ? "PASSED" : "FAILED") + ": " + ReportFolder + "/unity_verification.json");
    }

    private static void Require(bool condition, string message, Result result)
    {
        result.assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
