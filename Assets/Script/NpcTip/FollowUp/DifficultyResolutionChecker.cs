/// <summary>
/// 팁을 준 시점의 스냅샷(baseline)과 이후 스냅샷(current)을 비교해서
/// 난관 유형별로 "실제로 풀렸다"고 볼 수 있는지 판정하는 간단한 휴리스틱.
/// RuleBasedDiagnoser처럼 v1 규칙이며, 더 정교한 기준이 필요하면 여기만 수정하면 됨.
/// </summary>
public static class DifficultyResolutionChecker
{
    public static bool IsResolved(DifficultyType type, PlayLogEntry baseline, PlayLogEntry current)
    {
        if (baseline == null || current == null)
        {
            return false;
        }

        switch (type)
        {
            case DifficultyType.EquipmentLack:
                return DifficultyFeatureUtils.ParseToolGrade(current.ToolGrade) >
                       DifficultyFeatureUtils.ParseToolGrade(baseline.ToolGrade);

            case DifficultyType.CombatStruggle:
                return current.RecentDeathCount5Min < baseline.RecentDeathCount5Min;

            case DifficultyType.Lost:
                return current.CurrentZone != baseline.CurrentZone ||
                       current.ZoneDwellTimeSec < baseline.ZoneDwellTimeSec;

            case DifficultyType.ResourceLack:
                return current.MonsterKillCount > baseline.MonsterKillCount;

            case DifficultyType.Smooth:
            default:
                // 애초에 문제가 없었다고 진단한 경우이므로 별도 해결 조건이 의미 없음.
                return true;
        }
    }
}
