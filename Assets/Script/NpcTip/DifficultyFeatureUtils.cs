using System;

/// <summary>
/// RuleBasedDiagnoser와 MLDiagnoser(DifficultyFeatureBuilder)가 공통으로 쓰는 값 파싱 유틸.
/// </summary>
public static class DifficultyFeatureUtils
{
    /// <summary>
    /// PlayLogEntry.ToolGrade 문자열(ItemGrade.ToString() 또는 도구 미장착 시 "None")을
    /// ItemGrade enum으로 변환. 매핑 안 되는 값("None" 포함)은 가장 낮은 등급(Normal)으로 취급.
    /// </summary>
    public static ItemGrade ParseToolGrade(string toolGrade)
    {
        if (!string.IsNullOrEmpty(toolGrade) && Enum.TryParse(toolGrade, out ItemGrade grade))
        {
            return grade;
        }

        return ItemGrade.Normal;
    }
}
