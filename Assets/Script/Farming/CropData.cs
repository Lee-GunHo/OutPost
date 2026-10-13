using UnityEngine;

/// <summary>
/// 작물 하나의 설정. Resources/Farming/FarmSettings의 crops 목록에 등록해서 사용.
/// </summary>
[CreateAssetMenu(fileName = "New Crop", menuName = "Farming/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Items")]
    [Tooltip("심을 때 쓰는 씨앗 아이템")]
    public ItemData seedItem;

    [Tooltip("다 자라면 얻는 작물 아이템")]
    public ItemData cropItem;

    [Header("Growth")]
    [Tooltip("심은 뒤 다 자라기까지 걸리는 플레이 시간(초)")]
    [Min(1f)] public float growSeconds = 120f;

    [Tooltip("성장 단계 수(마지막 단계 = 다 자람)")]
    [Range(2, 6)] public int stageCount = 4;

    [Header("Harvest (다 자랐을 때)")]
    [Min(0)] public int minCropYield = 1;
    [Min(0)] public int maxCropYield = 3;
    [Min(0)] public int minSeedYield = 1;
    [Min(0)] public int maxSeedYield = 2;

    [Header("Visual")]
    public Color stemColor = new Color(0.35f, 0.7f, 0.25f);

    [Tooltip("다 자랐을 때 보이는 열매/작물 색")]
    public Color produceColor = new Color(1f, 0.55f, 0.1f);

    [Tooltip("단계별 모양 프리팹(선택). 비워두면 줄기+열매 모양을 코드로 만듦. 개수는 stageCount와 같게")]
    public GameObject[] stagePrefabs;

    private void OnValidate()
    {
        maxCropYield = Mathf.Max(minCropYield, maxCropYield);
        maxSeedYield = Mathf.Max(minSeedYield, maxSeedYield);
    }
}
