using System;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// ONNX(3단계 산출물)로 로컬 추론하는 난관 진단기. IDifficultyDiagnoser를 구현하므로
/// NPCTipDialoguePresenter 입장에서는 RuleBasedDiagnoser와 완전히 동일하게 취급 가능.
/// 모델/Worker는 생성 시 1회만 로드하고, Dispose()가 호출될 때(씬 종료 등) 해제함.
/// 모델이 없거나, 추론 중 예외가 나거나, 확신도가 minConfidence 미만이면
/// 생성자로 주입받은 fallbackDiagnoser(보통 RuleBasedDiagnoser) 결과로 대체.
/// </summary>
public sealed class MLDiagnoser : IDifficultyDiagnoser, IDisposable
{
    private readonly Worker worker;
    private readonly DifficultyModelMetadata metadata;
    private readonly IDifficultyDiagnoser fallbackDiagnoser;
    private readonly float minConfidence;
    private readonly bool isReady;

    public bool IsReady => isReady;

    public MLDiagnoser(
        ModelAsset modelAsset,
        TextAsset metadataJson,
        IDifficultyDiagnoser fallbackDiagnoser,
        float minConfidence)
    {
        this.fallbackDiagnoser = fallbackDiagnoser;
        this.minConfidence = minConfidence;

        if (modelAsset == null || metadataJson == null)
        {
            Debug.LogWarning("MLDiagnoser: 모델 또는 전처리 메타데이터가 연결되지 않아 규칙 기반으로만 동작합니다.");
            isReady = false;
            return;
        }

        try
        {
            metadata = DifficultyModelMetadata.LoadFromJson(metadataJson);

            if (metadata == null || !metadata.IsValid())
            {
                Debug.LogError("MLDiagnoser: preprocessing.json 형식이 올바르지 않습니다. 규칙 기반으로 대체합니다.");
                isReady = false;
                return;
            }

            Model runtimeModel = ModelLoader.Load(modelAsset);
            worker = new Worker(runtimeModel, BackendType.CPU);
            isReady = true;
        }
        catch (Exception exception)
        {
            Debug.LogError("MLDiagnoser 초기화 실패, 규칙 기반으로 대체합니다: " + exception.Message);
            isReady = false;
        }
    }

    public DifficultyDiagnosis Diagnose(PlayLogEntry snapshot)
    {
        if (!isReady || snapshot == null)
        {
            return fallbackDiagnoser.Diagnose(snapshot);
        }

        try
        {
            float[] probabilities = Infer(snapshot);

            int bestIndex = ArgMax(probabilities);
            float confidence = probabilities[bestIndex];

            if (confidence < minConfidence)
            {
                Debug.Log($"MLDiagnoser 확신도 부족({confidence:F2} < {minConfidence:F2}), 규칙 기반으로 대체합니다.");
                return fallbackDiagnoser.Diagnose(snapshot);
            }

            if (!Enum.TryParse(metadata.classOrder[bestIndex], out DifficultyType type))
            {
                Debug.LogError($"MLDiagnoser: 알 수 없는 클래스 이름 '{metadata.classOrder[bestIndex]}', 규칙 기반으로 대체합니다.");
                return fallbackDiagnoser.Diagnose(snapshot);
            }

            return new DifficultyDiagnosis(type, confidence);
        }
        catch (Exception exception)
        {
            Debug.LogError("MLDiagnoser 추론 실패, 규칙 기반으로 대체합니다: " + exception.Message);
            return fallbackDiagnoser.Diagnose(snapshot);
        }
    }

    /// <summary>ONNX 추론만 실행해서 클래스별 확률을 반환. 테스트에서도 재사용.</summary>
    public float[] Infer(PlayLogEntry snapshot)
    {
        float[] input = DifficultyFeatureBuilder.Build(
            snapshot.ToolGrade, snapshot.CumulativeDeathCount, snapshot.RecentDeathCount5Min,
            snapshot.MonsterKillCount, snapshot.BossKillCount,
            snapshot.QuestAcceptedCount, snapshot.QuestCompletedCount,
            snapshot.CurrentZone, snapshot.ZoneDwellTimeSec, snapshot.ElapsedSessionSec,
            metadata);

        using Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, input.Length), input);
        worker.Schedule(inputTensor);
        using Tensor<float> outputTensor = (Tensor<float>)worker.PeekOutput();

        return outputTensor.DownloadToArray();
    }

    private static int ArgMax(float[] values)
    {
        int bestIndex = 0;

        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > values[bestIndex])
            {
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public void Dispose()
    {
        worker?.Dispose();
    }
}
