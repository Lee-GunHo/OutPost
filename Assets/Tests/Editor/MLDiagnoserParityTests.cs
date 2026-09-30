using System;
using NUnit.Framework;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 3단계 Python 파이프라인이 만든 test_vectors.json(원본 값 + Python이 계산한 확률)을
/// Unity가 DifficultyFeatureBuilder + 실제 ONNX 추론으로 재현해서 오차 이내로 같은지 확인.
/// ml/artifacts/&lt;run&gt;/onnx/{difficulty_mlp.onnx, preprocessing.json, test_vectors.json}을
/// 아래 경로에 복사해둬야 통과함 (에디터 설정 안내 참고).
/// </summary>
public class MLDiagnoserParityTests
{
    private const string ModelDir = "Assets/MLModels/DifficultyDiagnosis";
    private const string ModelPath = ModelDir + "/difficulty_mlp.onnx";
    private const string MetadataPath = ModelDir + "/preprocessing.json";
    private const string TestVectorsPath = ModelDir + "/test_vectors.json";

    private const float Tolerance = 0.02f;

    [Test]
    public void OnnxOutputMatchesPythonWithinTolerance()
    {
        ModelAsset modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelPath);
        TextAsset metadataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(MetadataPath);
        TextAsset testVectorAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(TestVectorsPath);

        if (modelAsset == null || metadataAsset == null || testVectorAsset == null)
        {
            Assert.Ignore(
                $"모델/메타데이터/테스트 벡터를 {ModelDir}에서 찾지 못했습니다. " +
                "ml/artifacts/<run>/onnx/의 산출물을 복사해두면 이 테스트가 실행됩니다.");
            return;
        }

        DifficultyModelMetadata metadata = DifficultyModelMetadata.LoadFromJson(metadataAsset);
        Assert.IsTrue(metadata != null && metadata.IsValid(), "preprocessing.json 형식이 올바르지 않습니다.");

        TestVectorFile testFile = JsonUtility.FromJson<TestVectorFile>(testVectorAsset.text);
        Assert.IsNotNull(testFile);
        Assert.IsTrue(testFile.samples != null && testFile.samples.Length > 0, "테스트 샘플이 비어 있습니다.");

        Model runtimeModel = ModelLoader.Load(modelAsset);

        using Worker worker = new Worker(runtimeModel, BackendType.CPU);

        int sampleIndex = 0;

        foreach (TestVectorSample sample in testFile.samples)
        {
            float[] input = DifficultyFeatureBuilder.Build(
                sample.toolGrade, sample.cumulativeDeathCount, sample.recentDeathCount5min,
                sample.monsterKillCount, sample.bossKillCount,
                sample.questAcceptedCount, sample.questCompletedCount,
                sample.currentZone, sample.zoneDwellTimeSec, sample.elapsedSessionSec,
                metadata);

            using Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, input.Length), input);
            worker.Schedule(inputTensor);
            using Tensor<float> outputTensor = (Tensor<float>)worker.PeekOutput();

            float[] actual = outputTensor.DownloadToArray();

            Assert.AreEqual(sample.expectedProbabilities.Length, actual.Length,
                $"샘플 {sampleIndex}: 출력 클래스 개수가 다릅니다.");

            for (int classIndex = 0; classIndex < actual.Length; classIndex++)
            {
                string className = metadata.classOrder.Length > classIndex
                    ? metadata.classOrder[classIndex]
                    : classIndex.ToString();

                Assert.AreEqual(
                    sample.expectedProbabilities[classIndex], actual[classIndex], Tolerance,
                    $"샘플 {sampleIndex}, 클래스 {className}: Python={sample.expectedProbabilities[classIndex]:F4}, " +
                    $"Unity={actual[classIndex]:F4} (허용 오차 {Tolerance})");
            }

            sampleIndex++;
        }
    }

    [Serializable]
    private class TestVectorFile
    {
        public string[] featureOrder;
        public string[] classOrder;
        public TestVectorSample[] samples;
    }

    [Serializable]
    private class TestVectorSample
    {
        public string toolGrade;
        public int cumulativeDeathCount;
        public int recentDeathCount5min;
        public int monsterKillCount;
        public int bossKillCount;
        public int questAcceptedCount;
        public int questCompletedCount;
        public string currentZone;
        public float zoneDwellTimeSec;
        public float elapsedSessionSec;
        public float[] expectedProbabilities;
    }
}
