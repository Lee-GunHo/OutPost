using UnityEngine;

/// <summary>
/// 밭 한 칸의 상태(순수 데이터). crop이 null이면 빈 밭.
/// </summary>
public class FarmPlot
{
    public Vector2Int Cell { get; }
    public CropData Crop { get; private set; }
    public float Growth { get; private set; }

    public bool HasCrop => Crop != null;
    public bool IsMature => HasCrop && FarmRules.IsMature(Growth, Crop.growSeconds);
    public int Stage => HasCrop ? FarmRules.GetStage(Growth, Crop.growSeconds, Crop.stageCount) : -1;

    public FarmPlot(Vector2Int cell, CropData crop = null, float growth = 0f)
    {
        Cell = cell;
        Crop = crop;
        Growth = crop != null ? Mathf.Max(0f, growth) : 0f;
    }

    public void Plant(CropData crop)
    {
        Crop = crop;
        Growth = 0f;
    }

    public void ClearCrop()
    {
        Crop = null;
        Growth = 0f;
    }

    /// <summary>
    /// 성장 진행. 단계가 바뀌었으면 true(모양 갱신 필요)
    /// </summary>
    public bool Grow(float deltaSeconds)
    {
        if (!HasCrop || IsMature || deltaSeconds <= 0f)
            return false;

        int before = Stage;
        Growth = Mathf.Min(Crop.growSeconds, Growth + deltaSeconds);
        return Stage != before;
    }
}
