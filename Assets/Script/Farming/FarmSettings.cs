using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 농사 시스템 전체 설정. Resources/Farming/FarmSettings.asset 하나만 두고 FarmManager가 자동으로 읽음.
/// </summary>
[CreateAssetMenu(fileName = "FarmSettings", menuName = "Farming/Farm Settings")]
public class FarmSettings : ScriptableObject
{
    public const string ResourcePath = "Farming/FarmSettings";

    [Header("Crops")]
    public List<CropData> crops = new List<CropData>();

    [Header("Drops")]
    [Tooltip("수확물을 바닥에 떨어뜨릴 때 쓰는 범용 드롭 프리팹")]
    public DroppedItemPresenter droppedItemPrefab;

    [Header("Tilling")]
    [Tooltip("이 레이어의 콜라이더가 있는 칸은 밭으로 갈 수 없음(벽, 나무, 건물, NPC 등)")]
    public LayerMask tillBlockerMask;

    [Tooltip("괭이별로 한 번에 가는 범위. 목록에 없는 괭이는 1칸")]
    public List<HoeArea> hoeAreas = new List<HoeArea>();

    [Serializable]
    public class HoeArea
    {
        public ItemData hoe;

        [Tooltip("바라보는 방향의 옆으로 몇 칸")]
        [Min(1)] public int width = 1;

        [Tooltip("바라보는 방향으로 몇 칸")]
        [Min(1)] public int length = 1;
    }

    [Header("Visual")]
    public Color soilColor = new Color(0.36f, 0.22f, 0.12f);
    public Color ghostValidColor = new Color(0.2f, 0.55f, 1f, 0.4f);
    public Color ghostInvalidColor = new Color(1f, 0.25f, 0.25f, 0.4f);

    [Header("Save")]
    [Tooltip("성장 중일 때 저장 주기(초). 밭 갈기/심기/수확은 즉시 저장")]
    [Min(1f)] public float autoSaveInterval = 10f;

    /// <summary>
    /// 괭이의 범위(옆 폭, 앞 길이). 목록에 없으면 1x1
    /// </summary>
    public (int width, int length) GetHoeArea(ItemData hoe)
    {
        foreach (HoeArea area in hoeAreas)
        {
            if (area != null && area.hoe == hoe)
                return (Mathf.Max(1, area.width), Mathf.Max(1, area.length));
        }

        return (1, 1);
    }

    public CropData FindBySeed(ItemData seed)
    {
        if (seed == null)
            return null;

        foreach (CropData crop in crops)
        {
            if (crop != null && crop.seedItem == seed)
                return crop;
        }

        return null;
    }

    public CropData FindBySeedId(int seedItemId)
    {
        foreach (CropData crop in crops)
        {
            if (crop != null && crop.seedItem != null && crop.seedItem.itemID == seedItemId)
                return crop;
        }

        return null;
    }
}
