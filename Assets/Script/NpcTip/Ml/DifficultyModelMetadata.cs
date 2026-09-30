using UnityEngine;

/// <summary>
/// ml/artifacts/&lt;run&gt;/onnx/preprocessing.json을 그대로 읽어들인 것.
/// featureOrder/scalerMean/scalerStd 순서가 Python 학습 시점과 완전히 같아야 하므로
/// 이 값들은 하드코딩하지 않고 항상 이 파일에서 읽어서 씀.
/// (itemGradeToOrdinal은 JsonUtility가 Dictionary를 지원하지 않아 여기서는 읽지 않고,
/// 대신 기존 C# ItemGrade enum 순서를 그대로 사용함 - 둘 다 Normal=0 순서로 이미 일치)
/// </summary>
[System.Serializable]
public class DifficultyModelMetadata
{
    public string[] featureOrder;
    public string[] zoneCategories;
    public string[] classOrder;
    public float[] scalerMean;
    public float[] scalerStd;

    public static DifficultyModelMetadata LoadFromJson(TextAsset jsonAsset)
    {
        if (jsonAsset == null)
        {
            return null;
        }

        return JsonUtility.FromJson<DifficultyModelMetadata>(jsonAsset.text);
    }

    public bool IsValid()
    {
        return featureOrder != null && featureOrder.Length > 0 &&
               scalerMean != null && scalerMean.Length == featureOrder.Length &&
               scalerStd != null && scalerStd.Length == featureOrder.Length &&
               classOrder != null && classOrder.Length > 0 &&
               zoneCategories != null && zoneCategories.Length > 0;
    }
}
