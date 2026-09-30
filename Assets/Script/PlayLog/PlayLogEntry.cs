using System;
using System.Globalization;

/// <summary>
/// play_snapshots.csv의 한 행(row)에 해당하는 데이터.
/// 필드 순서가 곧 CSV 컬럼 순서이므로 Header/ToCsvRow가 항상 같은 순서를 유지해야 함.
/// </summary>
public class PlayLogEntry
{
    public string Timestamp;
    public string SessionId;
    public float ElapsedSessionSec;
    public string EventType;

    public string ToolGrade;
    public string ToolType;

    public int CumulativeDeathCount;
    public int RecentDeathCount5Min;
    public string LastDeathCause;

    public int MonsterKillCount;
    public int BossKillCount;

    public int QuestAcceptedCount;
    public int QuestCompletedCount;

    public string CurrentZone;
    public float ZoneDwellTimeSec;

    public float PosX;
    public float PosY;
    public float PosZ;

    public static string Header =>
        "timestamp,sessionId,elapsedSessionSec,eventType," +
        "toolGrade,toolType," +
        "cumulativeDeathCount,recentDeathCount5min,lastDeathCause," +
        "monsterKillCount,bossKillCount," +
        "questAcceptedCount,questCompletedCount," +
        "currentZone,zoneDwellTimeSec," +
        "posX,posY,posZ";

    public string ToCsvRow()
    {
        CultureInfo culture = CultureInfo.InvariantCulture;

        return string.Join(",",
            Timestamp,
            SessionId,
            ElapsedSessionSec.ToString("F1", culture),
            EventType,
            EscapeField(ToolGrade),
            EscapeField(ToolType),
            CumulativeDeathCount.ToString(culture),
            RecentDeathCount5Min.ToString(culture),
            EscapeField(LastDeathCause),
            MonsterKillCount.ToString(culture),
            BossKillCount.ToString(culture),
            QuestAcceptedCount.ToString(culture),
            QuestCompletedCount.ToString(culture),
            EscapeField(CurrentZone),
            ZoneDwellTimeSec.ToString("F1", culture),
            PosX.ToString("F2", culture),
            PosY.ToString("F2", culture),
            PosZ.ToString("F2", culture)
        );
    }

    private static string EscapeField(string field)
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
}
