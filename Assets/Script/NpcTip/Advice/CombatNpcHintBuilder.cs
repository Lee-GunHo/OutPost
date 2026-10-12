using System;
using UnityEngine;

[Serializable]
public class CombatNpcHintDistanceStep
{
    [Tooltip("안전구역 중심에서 이 거리(타일) 이하면 이 단계")]
    public float maxDistanceTiles;
    public string label;
}

/// <summary>
/// 전투 NPC 위치 힌트 문구 설정. 거리는 모두 타일 단위(SeedMapModel의 구역 range와 같은 단위).
/// </summary>
[Serializable]
public class CombatNpcHintSettings
{
    [Tooltip("이 거리(타일) 이하면 방향 없이 veryNearText만 사용")]
    public float veryNearDistanceTiles = 16f;
    public string veryNearText = "안전구역 바로 근처";

    [Tooltip("가까운 단계부터 순서대로. 어느 단계에도 안 들면 farLabel")]
    public CombatNpcHintDistanceStep[] distanceSteps =
    {
        new CombatNpcHintDistanceStep { maxDistanceTiles = 90f, label = "가까운 곳" },
        new CombatNpcHintDistanceStep { maxDistanceTiles = 250f, label = "조금 떨어진 곳" },
        new CombatNpcHintDistanceStep { maxDistanceTiles = 420f, label = "꽤 먼 곳" }
    };
    public string farLabel = "아주 먼 곳";

    [Tooltip("북(+Z)부터 시계 방향 8개: 북, 북동, 동, 남동, 남, 남서, 서, 북서")]
    public string[] directionNames = { "북", "북동", "동", "남동", "남", "남서", "서", "북서" };

    [Tooltip("{direction}, {distance}가 치환됨")]
    public string format = "안전구역에서 {direction}쪽 {distance}";
}

/// <summary>
/// 전투 NPC의 실제 위치를 "안전구역 기준 8방위 + 거리 단계" 문구로 바꾸는 순수 함수.
/// 좌표나 거리 숫자는 결과 문자열에 절대 넣지 않음.
/// </summary>
public static class CombatNpcHintBuilder
{
    /// <param name="originTiles">안전구역 중심(타일 단위, XZ 평면 사용)</param>
    /// <param name="targetTiles">전투 NPC 위치(타일 단위)</param>
    public static bool TryBuild(
        Vector3 originTiles,
        Vector3 targetTiles,
        CombatNpcHintSettings settings,
        out string hint)
    {
        hint = null;

        if (settings == null)
        {
            return false;
        }

        float dx = targetTiles.x - originTiles.x;
        float dz = targetTiles.z - originTiles.z;

        if (float.IsNaN(dx) || float.IsNaN(dz) || float.IsInfinity(dx) || float.IsInfinity(dz))
        {
            return false;
        }

        float distance = Mathf.Sqrt(dx * dx + dz * dz);

        if (distance <= settings.veryNearDistanceTiles)
        {
            hint = settings.veryNearText;
            return !string.IsNullOrEmpty(hint);
        }

        if (settings.directionNames == null || settings.directionNames.Length != 8 ||
            string.IsNullOrEmpty(settings.format))
        {
            return false;
        }

        // 0도 = 북(+Z), 90도 = 동(+X). 45도 단위로 반올림해 8방위 인덱스로 변환.
        float angle = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        int directionIndex = ((Mathf.RoundToInt(angle / 45f) % 8) + 8) % 8;
        string direction = settings.directionNames[directionIndex];

        if (string.IsNullOrEmpty(direction))
        {
            return false;
        }

        hint = settings.format
            .Replace("{direction}", direction)
            .Replace("{distance}", GetDistanceLabel(distance, settings));

        return true;
    }

    private static string GetDistanceLabel(float distance, CombatNpcHintSettings settings)
    {
        if (settings.distanceSteps != null)
        {
            foreach (CombatNpcHintDistanceStep step in settings.distanceSteps)
            {
                if (step != null && distance <= step.maxDistanceTiles)
                {
                    return step.label ?? string.Empty;
                }
            }
        }

        return settings.farLabel ?? string.Empty;
    }
}
