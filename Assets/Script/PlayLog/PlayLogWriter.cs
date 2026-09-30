using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Application.persistentDataPath/PlayLog/ 아래에 CSV를 append 전용으로 기록.
/// 파일이 없을 때만 헤더를 1회 기록.
/// </summary>
public static class PlayLogWriter
{
    private const string FolderName = "PlayLog";
    private const string SnapshotFileName = "play_snapshots.csv";
    private const string LabelFileName = "session_labels.csv";
    private const string TipDialogueFileName = "tip_dialogue_log.csv";
    private const string DiagnosisComparisonFileName = "diagnosis_comparison_log.csv";
    private const string TipFeedbackFileName = "tip_feedback_log.csv";
    private const string TipResolutionFileName = "tip_resolution_log.csv";
    private const string AbTestAssignmentFileName = "ab_test_assignment.csv";

    public static string FolderPath => Path.Combine(Application.persistentDataPath, FolderName);
    public static string SnapshotFilePath => Path.Combine(FolderPath, SnapshotFileName);
    public static string LabelFilePath => Path.Combine(FolderPath, LabelFileName);
    public static string TipDialogueFilePath => Path.Combine(FolderPath, TipDialogueFileName);
    public static string DiagnosisComparisonFilePath => Path.Combine(FolderPath, DiagnosisComparisonFileName);
    public static string TipFeedbackFilePath => Path.Combine(FolderPath, TipFeedbackFileName);
    public static string TipResolutionFilePath => Path.Combine(FolderPath, TipResolutionFileName);
    public static string AbTestAssignmentFilePath => Path.Combine(FolderPath, AbTestAssignmentFileName);

    public static void AppendSnapshot(PlayLogEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        try
        {
            EnsureFolderExists();
            EnsureHeader(SnapshotFilePath, PlayLogEntry.Header);

            File.AppendAllText(SnapshotFilePath, entry.ToCsvRow() + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PlayLog 스냅샷 기록 실패: " + exception.Message);
        }
    }

    public static void AppendSessionLabel(
        string sessionId,
        string timestamp,
        float sessionPlayTimeSec,
        string difficultyLabel)
    {
        try
        {
            EnsureFolderExists();
            const string header = "sessionId,timestamp,sessionPlayTimeSec,difficultyLabel";
            EnsureHeader(LabelFilePath, header);

            string row = string.Join(",",
                sessionId,
                timestamp,
                sessionPlayTimeSec.ToString("F1", CultureInfo.InvariantCulture),
                EscapeCsvField(difficultyLabel));

            File.AppendAllText(LabelFilePath, row + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PlayLog 세션 라벨 기록 실패: " + exception.Message);
        }
    }

    public static void AppendTipDialogue(
        string sessionId,
        string timestamp,
        string difficultyType,
        float confidence,
        string entryId,
        string nodeId,
        string chosenChoiceText)
    {
        try
        {
            EnsureFolderExists();
            const string header = "sessionId,timestamp,difficultyType,confidence,entryId,nodeId,chosenChoiceText";
            EnsureHeader(TipDialogueFilePath, header);

            string row = string.Join(",",
                sessionId,
                timestamp,
                difficultyType,
                confidence.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsvField(entryId),
                EscapeCsvField(nodeId),
                EscapeCsvField(chosenChoiceText));

            File.AppendAllText(TipDialogueFilePath, row + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PlayLog 팁 대화 로그 기록 실패: " + exception.Message);
        }
    }

    public static void AppendDiagnosisComparison(
        string sessionId,
        string timestamp,
        string ruleType,
        float ruleConfidence,
        string mlType,
        float mlConfidence)
    {
        try
        {
            EnsureFolderExists();
            const string header =
                "sessionId,timestamp,ruleType,ruleConfidence,mlType,mlConfidence,agreement";
            EnsureHeader(DiagnosisComparisonFilePath, header);

            string agreement = ruleType == mlType ? "Match" : "Mismatch";

            string row = string.Join(",",
                sessionId,
                timestamp,
                ruleType,
                ruleConfidence.ToString("F2", CultureInfo.InvariantCulture),
                mlType,
                mlConfidence.ToString("F2", CultureInfo.InvariantCulture),
                agreement);

            File.AppendAllText(DiagnosisComparisonFilePath, row + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PlayLog 진단 비교 로그 기록 실패: " + exception.Message);
        }
    }

    public static void AppendTipFeedback(
        string sessionId,
        string timestamp,
        string difficultyType,
        string entryId,
        int? rating)
    {
        try
        {
            EnsureFolderExists();
            const string header = "sessionId,timestamp,difficultyType,entryId,rating,skipped";
            EnsureHeader(TipFeedbackFilePath, header);

            bool skipped = !rating.HasValue;
            string ratingText = rating.HasValue ? rating.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

            string row = string.Join(",",
                sessionId,
                timestamp,
                difficultyType,
                EscapeCsvField(entryId),
                ratingText,
                skipped ? "true" : "false");

            File.AppendAllText(TipFeedbackFilePath, row + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PlayLog 팁 만족도 로그 기록 실패: " + exception.Message);
        }
    }

    public static void AppendTipResolution(
        string sessionId,
        string tipTimestamp,
        string checkTimestamp,
        string difficultyType,
        string entryId,
        bool resolved,
        float? minutesToResolve)
    {
        try
        {
            EnsureFolderExists();
            const string header =
                "sessionId,tipTimestamp,checkTimestamp,difficultyType,entryId,resolved,minutesToResolve";
            EnsureHeader(TipResolutionFilePath, header);

            string minutesText = minutesToResolve.HasValue
                ? minutesToResolve.Value.ToString("F1", CultureInfo.InvariantCulture)
                : string.Empty;

            string row = string.Join(",",
                sessionId,
                tipTimestamp,
                checkTimestamp,
                difficultyType,
                EscapeCsvField(entryId),
                resolved ? "true" : "false",
                minutesText);

            File.AppendAllText(TipResolutionFilePath, row + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PlayLog 팁 해결 여부 로그 기록 실패: " + exception.Message);
        }
    }

    public static void AppendAbTestAssignment(
        string sessionId,
        string timestamp,
        string group)
    {
        try
        {
            EnsureFolderExists();
            const string header = "sessionId,timestamp,group";
            EnsureHeader(AbTestAssignmentFilePath, header);

            string row = string.Join(",", sessionId, timestamp, group);

            File.AppendAllText(AbTestAssignmentFilePath, row + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("PlayLog A/B 배정 로그 기록 실패: " + exception.Message);
        }
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        if (field.IndexOf(',') >= 0 || field.IndexOf('"') >= 0 || field.IndexOf('\n') >= 0)
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }

    private static void EnsureFolderExists()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }
    }

    private static void EnsureHeader(string filePath, string header)
    {
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, header + Environment.NewLine);
        }
    }
}
