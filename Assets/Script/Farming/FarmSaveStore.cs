using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 밭 저장. 다른 월드 저장(placed_blocks_{seed}.json)과 같은 방식으로 월드 시드별 JSON 파일 하나.
/// 작물은 씨앗 itemID로 저장해서 CropData 에셋 이름이 바뀌어도 유지됨.
/// </summary>
public static class FarmSaveStore
{
    public const string FilePrefix = "farm_";

    [Serializable]
    private class FarmSaveData
    {
        public int worldSeed;
        public List<FarmPlotRecord> plots = new List<FarmPlotRecord>();
    }

    [Serializable]
    private class FarmPlotRecord
    {
        public int cellX;
        public int cellZ;
        public int seedItemId = -1; // -1 = 빈 밭
        public float growth;
    }

    public static string GetFilePath(int worldSeed)
    {
        return Path.Combine(Application.persistentDataPath, $"{FilePrefix}{worldSeed}.json");
    }

    public static List<FarmPlot> Load(int worldSeed, FarmSettings settings)
    {
        List<FarmPlot> plots = new List<FarmPlot>();
        string path = GetFilePath(worldSeed);

        if (!File.Exists(path))
            return plots;

        try
        {
            FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(File.ReadAllText(path));

            if (data?.plots == null)
                return plots;

            foreach (FarmPlotRecord record in data.plots)
            {
                CropData crop = record.seedItemId >= 0 ? settings.FindBySeedId(record.seedItemId) : null;

                if (record.seedItemId >= 0 && crop == null)
                    Debug.LogWarning($"[Farm] 씨앗 itemID {record.seedItemId}에 해당하는 CropData가 없어 빈 밭으로 불러옴");

                plots.Add(new FarmPlot(new Vector2Int(record.cellX, record.cellZ), crop, record.growth));
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Farm] 밭 저장 파일을 읽지 못했습니다: {path}\n{exception}");
        }

        return plots;
    }

    public static void Save(int worldSeed, IEnumerable<FarmPlot> plots)
    {
        FarmSaveData data = new FarmSaveData { worldSeed = worldSeed };

        foreach (FarmPlot plot in plots)
        {
            data.plots.Add(new FarmPlotRecord
            {
                cellX = plot.Cell.x,
                cellZ = plot.Cell.y,
                seedItemId = plot.HasCrop && plot.Crop.seedItem != null ? plot.Crop.seedItem.itemID : -1,
                growth = plot.Growth,
            });
        }

        string path = GetFilePath(worldSeed);

        try
        {
            // 쓰는 도중 종료돼도 기존 파일이 깨지지 않게 임시 파일에 쓰고 교체
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

            if (File.Exists(path))
                File.Delete(path);

            File.Move(tempPath, path);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Farm] 밭 저장에 실패했습니다: {path}\n{exception}");
        }
    }
}
