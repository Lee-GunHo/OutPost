using System;
using System.Collections.Generic;

/// <summary>
/// ml/ml_pipeline/data_prep.py의 add_derived_features + build_feature_matrix + StandardScaler
/// 전처리를 그대로 이식. DifficultyModelMetadata.featureOrder 순서를 그대로 따라가므로
/// Python이 학습 시 저장한 순서가 바뀌어도(피처가 추가/삭제돼도) 코드 수정 없이 맞춰짐.
/// MLDiagnoser와 Unity EditMode 테스트(MLDiagnoserParityTests)가 공통으로 사용.
/// </summary>
public static class DifficultyFeatureBuilder
{
    public static float[] Build(
        string toolGrade,
        int cumulativeDeathCount,
        int recentDeathCount5min,
        int monsterKillCount,
        int bossKillCount,
        int questAcceptedCount,
        int questCompletedCount,
        string currentZone,
        float zoneDwellTimeSec,
        float elapsedSessionSec,
        DifficultyModelMetadata metadata)
    {
        int toolGradeOrdinal = (int)DifficultyFeatureUtils.ParseToolGrade(toolGrade);

        Dictionary<string, float> values = new Dictionary<string, float>
        {
            { "toolGradeOrdinal", toolGradeOrdinal },
            { "cumulativeDeathCount", cumulativeDeathCount },
            { "recentDeathCount5min", recentDeathCount5min },
            { "deathRatePerMin5", recentDeathCount5min / 5.0f },
            { "monsterKillCount", monsterKillCount },
            { "bossKillCount", bossKillCount },
            { "killDeathRatio", monsterKillCount / (float)Math.Max(cumulativeDeathCount, 1) },
            { "questAcceptedCount", questAcceptedCount },
            { "questCompletedCount", questCompletedCount },
            { "questCompletionRatio", questCompletedCount / (float)Math.Max(questAcceptedCount, 1) },
            { "zoneDwellTimeSec", zoneDwellTimeSec },
            { "elapsedSessionSec", elapsedSessionSec },
        };

        bool matchedKnownZone = false;

        foreach (string zone in metadata.zoneCategories)
        {
            bool isCurrentZone = zone == currentZone;
            values[$"zone_{zone}"] = isCurrentZone ? 1f : 0f;
            matchedKnownZone |= isCurrentZone;
        }

        // Python data_prep.add_derived_features와 동일하게, 알 수 없는 구역은 Unknown으로 취급.
        if (!matchedKnownZone)
        {
            string unknownKey = "zone_Unknown";
            if (values.ContainsKey(unknownKey))
            {
                values[unknownKey] = 1f;
            }
        }

        float[] input = new float[metadata.featureOrder.Length];

        for (int i = 0; i < metadata.featureOrder.Length; i++)
        {
            string featureName = metadata.featureOrder[i];
            float rawValue = values.TryGetValue(featureName, out float v) ? v : 0f;
            float mean = metadata.scalerMean[i];
            float std = metadata.scalerStd[i];

            input[i] = std > 0f ? (rawValue - mean) / std : rawValue - mean;
        }

        return input;
    }
}
