using System;

/// <summary>
/// 농사 규칙(순수 함수). MonoBehaviour/씬과 분리해서 EditMode 테스트로 검증.
/// </summary>
public static class FarmRules
{
    /// <summary>
    /// 성장 시간 → 단계(0 = 막 심음, stageCount-1 = 다 자람). 단계는 시간을 균등하게 나눔.
    /// </summary>
    public static int GetStage(float growth, float growSeconds, int stageCount)
    {
        if (stageCount <= 1)
            return 0;

        if (growSeconds <= 0f || growth >= growSeconds)
            return stageCount - 1;

        if (growth <= 0f)
            return 0;

        int stage = (int)(growth / growSeconds * (stageCount - 1));
        return Math.Min(stage, stageCount - 2); // 다 자라기 전에는 마지막 단계로 표시하지 않음
    }

    public static bool IsMature(float growth, float growSeconds)
    {
        return growth >= growSeconds;
    }

    /// <summary>
    /// 플레이어 → 목표 칸 방향을 상하좌우 중 하나로. 같은 칸이면 위(+z)
    /// </summary>
    public static UnityEngine.Vector2Int GetFacing(float deltaX, float deltaZ)
    {
        if (deltaX == 0f && deltaZ == 0f)
            return new UnityEngine.Vector2Int(0, 1);

        return Math.Abs(deltaX) >= Math.Abs(deltaZ)
            ? new UnityEngine.Vector2Int(Math.Sign(deltaX), 0)
            : new UnityEngine.Vector2Int(0, Math.Sign(deltaZ));
    }

    /// <summary>
    /// 괭이 범위 칸 목록. 목표 칸을 중심으로 바라보는 방향(facing)으로 length칸, 옆으로 width칸.
    /// 짝수 크기는 바라보는 방향 앞쪽/오른쪽으로 한 칸 더 감.
    /// </summary>
    public static System.Collections.Generic.List<UnityEngine.Vector2Int> GetAreaCells(
        UnityEngine.Vector2Int target, UnityEngine.Vector2Int facing, int width, int length)
    {
        UnityEngine.Vector2Int side = new UnityEngine.Vector2Int(facing.y, -facing.x);
        System.Collections.Generic.List<UnityEngine.Vector2Int> cells = new System.Collections.Generic.List<UnityEngine.Vector2Int>();

        for (int forward = -(Math.Max(1, length) - 1) / 2; forward <= Math.Max(1, length) / 2; forward++)
        {
            for (int across = -(Math.Max(1, width) - 1) / 2; across <= Math.Max(1, width) / 2; across++)
                cells.Add(target + facing * forward + side * across);
        }

        return cells;
    }

    /// <summary>
    /// [min, max] 범위(양 끝 포함)에서 하나를 고름. max가 min보다 작으면 min.
    /// </summary>
    public static int RollAmount(Random random, int min, int max)
    {
        if (max <= min)
            return Math.Max(0, min);

        return random.Next(Math.Max(0, min), max + 1);
    }

    /// <summary>
    /// 수확 결과. 다 자랐으면 작물+씨앗을 굴리고, 덜 자랐으면 씨앗 1개만 돌려줌(심은 것 취소).
    /// </summary>
    public static (int cropAmount, int seedAmount) RollHarvest(
        Random random, bool isMature,
        int minCrop, int maxCrop, int minSeed, int maxSeed)
    {
        if (!isMature)
            return (0, 1);

        return (RollAmount(random, minCrop, maxCrop), RollAmount(random, minSeed, maxSeed));
    }
}
