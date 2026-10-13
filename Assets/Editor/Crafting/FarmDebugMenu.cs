using UnityEditor;
using UnityEngine;

/// <summary>
/// 테스트용: 플레이 중 심어진 작물의 성장을 빠르게 진행.
/// Ctrl+Shift+8 = 30초 진행(밀 기준 한 단계), Ctrl+Shift+7 = 전부 다 자람
/// </summary>
public static class FarmDebugMenu
{
    private const string GrowStepPath = "Tools/Debug/Farm - Grow Crops +30s %#8";
    private const string GrowAllPath = "Tools/Debug/Farm - Grow All Crops To Mature %#7";

    [MenuItem(GrowStepPath)]
    private static void GrowStep()
    {
        Advance(30f, "30초 진행");
    }

    [MenuItem(GrowAllPath)]
    private static void GrowAll()
    {
        Advance(float.MaxValue, "전부 다 자람");
    }

    [MenuItem(GrowStepPath, true)]
    [MenuItem(GrowAllPath, true)]
    private static bool CanUse()
    {
        return EditorApplication.isPlaying && FarmManager.Instance != null;
    }

    private static void Advance(float seconds, string label)
    {
        int count = FarmManager.Instance.DebugAdvanceGrowth(seconds);
        Debug.Log(count > 0
            ? $"[FarmDebug] {label}: 작물 {count}개"
            : "[FarmDebug] 자라는 중인 작물이 없습니다. 밭을 갈고 씨앗을 먼저 심으세요.");
    }
}
