using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시드 기반 맵 생성 규칙과 설정을 담당
/// 프리팹 생성이나 GameObject 조작은 담당하지 않음.
/// </summary>
public class SeedMapModel : MonoBehaviour
{
    public enum WallType
    {
        None,
        Dirt,
        Stone,
        Copper,
        Silver,
        Gold
    }

    public struct TreeClearingInfo
    {
        public bool IsEnabled;
        public int StartX;
        public int StartZ;
        public int Width;
        public int Length;

        public bool Contains(int x, int z)
        {
            if (!IsEnabled)
                return false;

            return x >= StartX && x < StartX + Width &&
                   z >= StartZ && z < StartZ + Length;
        }
    }

    [Header("Generator Settings")]
    [Tooltip("각 칸에 벽이 생성될 확률")]
    [Range(0, 100)]
    [SerializeField] private int wallPercent = 35;

    [Tooltip("타일 한 칸의 실제 월드 크기")]
    [Min(0.01f)]
    [SerializeField] private float cellSize = 1f;

    [Header("Player Start Position")]
    [SerializeField] private Vector3 playerStartPosition = Vector3.zero;

    [Header("Square Wall Ranges - Tile Distance")]
    [Tooltip("시작점에서 X 또는 Z 방향으로 이 칸 수 이내에는 벽을 만들지 않음")]
    [SerializeField] private int safeRange = 10;
    [SerializeField] private int dirtOnlyRange = 13;
    [SerializeField] private int dirtStoneRange = 15;
    [SerializeField] private int stoneOnlyRange = 18;
    [SerializeField] private int stoneCopperRange = 20;
    [SerializeField] private int copperOnlyRange = 23;
    [SerializeField] private int copperSilverRange = 25;
    [SerializeField] private int silverOnlyRange = 28;
    [SerializeField] private int silverGoldRange = 30;

    [Header("Tree Clearing")]
    [Tooltip("청크 하나에 나무 공터가 생길 확률")]
    [Range(0, 100)]
    [SerializeField] private int treeClearingChance = 10;

    [SerializeField] private int clearingWidth = 5;
    [SerializeField] private int clearingLength = 10;

    [Tooltip("공터 내부 한 칸에 나무가 생성될 확률")]
    [Range(0, 100)]
    [SerializeField] private int treeFillPercent = 35;

    [SerializeField] private int clearingMargin = 1;

    [Header("Player Placed Blocks")]
    [Tooltip("itemID로 설치 블록 데이터를 복구할 때 사용하는 목록")]
    [SerializeField]
    private List<ItemData> placeableBlockItems =
        new List<ItemData>();

    public float CellSize => cellSize;
    public int SafeRange => safeRange;

    public int GetChunkSeed(int globalSeed, Vector2Int chunkCoord)
    {
        unchecked
        {
            int hash = globalSeed;
            hash = hash * 73856093 ^ chunkCoord.x * 19349663;
            hash = hash * 83492791 ^ chunkCoord.y * 297121507;
            return hash;
        }
    }

    public Vector3 GetChunkWorldPosition(
        Vector2Int chunkCoord,
        int chunkSize)
    {
        return new Vector3(
            chunkCoord.x * chunkSize * cellSize,
            0f,
            chunkCoord.y * chunkSize * cellSize
        );
    }

    public Vector3 GetLocalCellPosition(int x, int z)
    {
        return new Vector3(x * cellSize, 0f, z * cellSize);
    }

    public Vector2Int GetGlobalCellCoordinate(
        Vector2Int chunkCoord,
        int chunkSize,
        int localX,
        int localZ)
    {
        return new Vector2Int(
            chunkCoord.x * chunkSize + localX,
            chunkCoord.y * chunkSize + localZ
        );
    }

    public float GetSquareDistanceInTiles(Vector3 worldCellPosition)
    {
        float xDistance =
            Mathf.Abs(worldCellPosition.x - playerStartPosition.x) / cellSize;

        float zDistance =
            Mathf.Abs(worldCellPosition.z - playerStartPosition.z) / cellSize;

        return Mathf.Max(xDistance, zDistance);
    }

    public bool IsSafeArea(float squareDistance)
    {
        return squareDistance <= safeRange;
    }

    public bool ShouldCreateWall(System.Random random)
    {
        return random.Next(0, 100) < wallPercent;
    }

    public bool ShouldCreateTree(System.Random random)
    {
        return random.Next(0, 100) < treeFillPercent;
    }

    public WallType SelectWallType(
        float distance,
        System.Random random)
    {
        if (distance <= dirtOnlyRange)
            return WallType.Dirt;

        if (distance <= dirtStoneRange)
            return ChooseHalf(WallType.Dirt, WallType.Stone, random);

        if (distance <= stoneOnlyRange)
            return WallType.Stone;

        if (distance <= stoneCopperRange)
            return ChooseHalf(WallType.Stone, WallType.Copper, random);

        if (distance <= copperOnlyRange)
            return WallType.Copper;

        if (distance <= copperSilverRange)
            return ChooseHalf(WallType.Copper, WallType.Silver, random);

        if (distance <= silverOnlyRange)
            return WallType.Silver;

        if (distance <= silverGoldRange)
            return ChooseHalf(WallType.Silver, WallType.Gold, random);

        return WallType.Gold;
    }

    public TreeClearingInfo CreateTreeClearing(
        System.Random random,
        int chunkSize,
        bool canCreateTree)
    {
        TreeClearingInfo result = new TreeClearingInfo();

        if (!canCreateTree)
            return result;

        if (random.Next(0, 100) >= treeClearingChance)
            return result;

        int margin = Mathf.Max(0, clearingMargin);
        int availableSize = chunkSize - margin * 2;

        if (availableSize <= 0)
            return result;

        int width = Mathf.Clamp(clearingWidth, 1, availableSize);
        int length = Mathf.Clamp(clearingLength, 1, availableSize);

        if (random.Next(0, 2) == 1)
        {
            int temp = width;
            width = length;
            length = temp;
        }

        int maxStartX = chunkSize - margin - width;
        int maxStartZ = chunkSize - margin - length;

        if (maxStartX < margin || maxStartZ < margin)
            return result;

        result.IsEnabled = true;
        result.StartX = random.Next(margin, maxStartX + 1);
        result.StartZ = random.Next(margin, maxStartZ + 1);
        result.Width = width;
        result.Length = length;

        return result;
    }

    public ItemData FindPlaceableItem(int itemId)
    {
        foreach (ItemData itemData in placeableBlockItems)
        {
            if (itemData != null && itemData.itemID == itemId)
                return itemData;
        }

        return null;
    }

    private static WallType ChooseHalf(
        WallType first,
        WallType second,
        System.Random random)
    {
        return random.Next(0, 2) == 0 ? first : second;
    }

    private void OnValidate()
    {
        wallPercent = Mathf.Clamp(wallPercent, 0, 100);
        cellSize = Mathf.Max(0.01f, cellSize);

        safeRange = Mathf.Max(0, safeRange);
        dirtOnlyRange = Mathf.Max(safeRange, dirtOnlyRange);
        dirtStoneRange = Mathf.Max(dirtOnlyRange, dirtStoneRange);
        stoneOnlyRange = Mathf.Max(dirtStoneRange, stoneOnlyRange);
        stoneCopperRange = Mathf.Max(stoneOnlyRange, stoneCopperRange);
        copperOnlyRange = Mathf.Max(stoneCopperRange, copperOnlyRange);
        copperSilverRange = Mathf.Max(copperOnlyRange, copperSilverRange);
        silverOnlyRange = Mathf.Max(copperSilverRange, silverOnlyRange);
        silverGoldRange = Mathf.Max(silverOnlyRange, silverGoldRange);

        treeClearingChance = Mathf.Clamp(treeClearingChance, 0, 100);
        treeFillPercent = Mathf.Clamp(treeFillPercent, 0, 100);
        clearingWidth = Mathf.Max(1, clearingWidth);
        clearingLength = Mathf.Max(1, clearingLength);
        clearingMargin = Mathf.Max(0, clearingMargin);
    }
}