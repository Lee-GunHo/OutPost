using UnityEngine;

/// <summary>
/// 규칙 기반 난관 진단기. DifficultyRuleConfig의 기준값으로 판단하며,
/// 여러 규칙이 동시에 해당하면 CombatStruggle > EquipmentLack > Lost > ResourceLack 순으로 우선.
/// 추후 ML 진단기(MLDiagnoser 등)가 나와도 성능 비교 기준(baseline)으로 계속 유지.
/// </summary>
public sealed class RuleBasedDiagnoser : IDifficultyDiagnoser
{
    private readonly DifficultyRuleConfig config;

    public RuleBasedDiagnoser(DifficultyRuleConfig config)
    {
        this.config = config;
    }

    public DifficultyDiagnosis Diagnose(PlayLogEntry snapshot)
    {
        if (config == null || snapshot == null)
        {
            return new DifficultyDiagnosis(DifficultyType.Smooth, 0.5f);
        }

        if (snapshot.RecentDeathCount5Min >= config.combatStruggleRecentDeathThreshold)
        {
            float confidence = CalcConfidence(
                snapshot.RecentDeathCount5Min, config.combatStruggleRecentDeathThreshold);
            return new DifficultyDiagnosis(DifficultyType.CombatStruggle, confidence);
        }

        ItemGrade toolGrade = DifficultyFeatureUtils.ParseToolGrade(snapshot.ToolGrade);

        if (toolGrade <= config.equipmentLackMaxGrade &&
            snapshot.CumulativeDeathCount >= config.equipmentLackDeathThreshold)
        {
            float confidence = CalcConfidence(
                snapshot.CumulativeDeathCount, config.equipmentLackDeathThreshold);
            return new DifficultyDiagnosis(DifficultyType.EquipmentLack, confidence);
        }

        bool hasActiveQuest = snapshot.QuestAcceptedCount > snapshot.QuestCompletedCount;

        if (snapshot.ZoneDwellTimeSec >= config.lostZoneDwellThresholdSec &&
            (!config.lostRequiresActiveQuest || hasActiveQuest))
        {
            float confidence = CalcConfidence(
                snapshot.ZoneDwellTimeSec, config.lostZoneDwellThresholdSec);
            return new DifficultyDiagnosis(DifficultyType.Lost, confidence);
        }

        if (snapshot.ElapsedSessionSec >= config.resourceLackElapsedThresholdSec &&
            snapshot.MonsterKillCount <= config.resourceLackMaxMonsterKill)
        {
            float confidence = CalcConfidence(
                snapshot.ElapsedSessionSec, config.resourceLackElapsedThresholdSec);
            return new DifficultyDiagnosis(DifficultyType.ResourceLack, confidence);
        }

        return new DifficultyDiagnosis(DifficultyType.Smooth, config.smoothConfidence);
    }

    private float CalcConfidence(float value, float threshold)
    {
        if (threshold <= 0f)
        {
            return config.maxConfidence;
        }

        float excessRatio = Mathf.Clamp01((value - threshold) / threshold);
        return Mathf.Lerp(config.minConfidence, config.maxConfidence, excessRatio);
    }
}
