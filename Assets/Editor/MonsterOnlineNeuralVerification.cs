using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only verification coordinator; operates on isolated in-memory data, never player saves.
public static class MonsterOnlineNeuralVerification
{
    [Serializable] private sealed class Result
    {
        public bool passed;
        public string contract, unityVersion, error;
        public int checks;
        public float coldMultiplier, successfulMultiplier, failedMultiplier;
    }

    [MenuItem("Tools/OutPost/Monster AI/Verify Online Neural Learning")]
    public static void Verify()
    {
        var r = new Result { contract = MonsterOnlineNetworkModel.Contract, unityVersion = Application.unityVersion };
        try
        {
            var memory = new MonsterLearningModel();
            var input = MonsterOnlineNetworkModel.Encode(3, 1.8f, 1, false, false, false);
            r.coldMultiplier = memory.Weight("test", input, 3);
            for (int i = 0; i < 40; i++) memory.Observe("test", input, 3, true, .06f);
            r.successfulMultiplier = memory.Weight("test", input, 3);
            Require(r.coldMultiplier == 1 && r.successfulMultiplier > 2, "Online success learning", r);
            string saved = JsonUtility.ToJson(memory);
            var restored = JsonUtility.FromJson<MonsterLearningModel>(saved);
            Require(restored.IsValid(), "Unity JSON validation", r);
            Require(restored.Weight("test", input, 3) == r.successfulMultiplier, "Restore exact prediction", r);
            Require(restored.networks[0].replay.Count == 40, "Restore replay", r);
            for (int i = 0; i < 200; i++)
            {
                memory.Observe("test", input, 3, false, .06f);
                restored.Observe("test", input, 3, false, .06f);
            }
            r.failedMultiplier = restored.Weight("test", input, 3);
            Require(r.failedMultiplier < .7f && r.failedMultiplier >= .5f, "Failure adaptation", r);
            Require(JsonUtility.ToJson(restored) == JsonUtility.ToJson(memory), "Deterministic continuation after reload", r);
            Require(new MonsterLearningModel().Weight("test", input, 3) == 1, "New memory reset", r);
            var legacy = JsonUtility.FromJson<MonsterLearningModel>("{\"schemaVersion\":1,\"version\":1,\"entries\":[{\"monsterType\":\"test\",\"context\":\"1:0:0\",\"action\":3,\"attempts\":1,\"hits\":1,\"meanReward\":0.08}]}");
            Require(legacy.IsValid() && legacy.schemaVersion == 1, "Legacy migration detection", r);
            restored.networks[0].actionIds[3] = "WrongAction";
            Require(!restored.IsValid(), "Reject incompatible action contracts", r);
            r.passed = true;
        }
        catch (Exception e) { r.error = e.ToString(); throw; }
        finally
        {
            Directory.CreateDirectory("Reports/MonsterAI/online-neural");
            File.WriteAllText("Reports/MonsterAI/online-neural/unity_verification.json", JsonUtility.ToJson(r, true));
            Debug.Log("Online neural verification: " + JsonUtility.ToJson(r));
        }
    }

    private static void Require(bool condition, string message, Result result)
    {
        result.checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
