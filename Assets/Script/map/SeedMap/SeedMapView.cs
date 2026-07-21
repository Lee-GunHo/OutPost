using UnityEngine;

/// <summary>
/// 맵 오브젝트의 실제 생성과 BreakableWall 초기화를 담당.
/// 생성 확률이나 광물 구간 판단은 담당하지 않음.
/// </summary>
public class SeedMapView : MonoBehaviour
{
    [Header("Floor Prefab")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private float floorYOffset = 0f;

    [Header("Wall Prefabs")]
    [SerializeField] private GameObject dirtWallPrefab;
    [SerializeField] private GameObject stoneWallPrefab;
    [SerializeField] private GameObject copperWallPrefab;
    [SerializeField] private GameObject silverWallPrefab;
    [SerializeField] private GameObject goldWallPrefab;
    [SerializeField] private float wallYOffset = 1f;

    [Header("Tree Prefab")]
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private float treeYOffset = 0f;

    public bool CanCreateTree => treePrefab != null;

    public GameObject CreateChunkRoot(Vector3 worldPosition)
    {
        GameObject chunkObject = new GameObject("Chunk");
        chunkObject.transform.position = worldPosition;
        return chunkObject;
    }

    public void CreateFloor(
        Vector3 worldCellPosition,
        Transform chunkTransform)
    {
        if (floorPrefab == null)
            return;

        Vector3 floorPosition = worldCellPosition;
        floorPosition.y += floorYOffset;

        GameObject floorObject = Instantiate(
            floorPrefab,
            floorPosition,
            Quaternion.identity,
            chunkTransform
        );

        floorObject.name = "Floor";
    }

    public bool CreateGeneratedWall(
        SeedMapModel.WallType wallType,
        Vector3 worldCellPosition,
        Transform chunkTransform,
        int globalSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord)
    {
        GameObject wallPrefab = GetWallPrefab(wallType);

        if (wallPrefab == null)
            return false;

        Vector3 wallPosition = worldCellPosition;
        wallPosition.y += wallYOffset;

        GameObject wallObject = Instantiate(
            wallPrefab,
            wallPosition,
            Quaternion.identity,
            chunkTransform
        );

        wallObject.name = wallPrefab.name;

        BreakableWall[] breakableWalls =
            wallObject.GetComponentsInChildren<BreakableWall>(true);

        if (breakableWalls.Length == 0)
        {
            Debug.LogWarning(
                $"{wallPrefab.name} 프리팹 또는 자식에 BreakableWall이 없어 " +
                "파괴 상태를 저장할 수 없습니다.",
                wallObject
            );
            return true;
        }

        foreach (BreakableWall breakableWall in breakableWalls)
        {
            breakableWall.InitializeGeneratedWall(
                globalSeed,
                chunkCoord,
                localCellCoord,
                globalCellCoord,
                wallObject
            );
        }

        return true;
    }

    public bool CreateTree(
        Vector3 worldCellPosition,
        Transform chunkTransform,
        int globalSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord)
    {
        if (treePrefab == null)
            return false;

        Vector3 treePosition = worldCellPosition;
        treePosition.y += treeYOffset;

        GameObject treeObject = Instantiate(
            treePrefab,
            treePosition,
            Quaternion.identity,
            chunkTransform
        );

        treeObject.name = treePrefab.name;

        BreakableWall[] breakableTrees =
            treeObject.GetComponentsInChildren<BreakableWall>(true);

        if (breakableTrees.Length == 0)
        {
            Debug.LogWarning(
                $"{treePrefab.name} 프리팹 또는 자식에 BreakableWall이 없어 " +
                "나무 파괴 상태를 저장할 수 없습니다.",
                treeObject
            );
            return true;
        }

        foreach (BreakableWall breakableTree in breakableTrees)
        {
            breakableTree.InitializeGeneratedTree(
                globalSeed,
                chunkCoord,
                localCellCoord,
                globalCellCoord,
                treeObject
            );
        }

        return true;
    }

    public bool CreatePlacedBlock(
        ItemData itemData,
        Vector3 worldCellPosition,
        Transform chunkTransform,
        int globalSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord)
    {
        if (itemData == null)
            return false;

        if (itemData.placeablePrefab == null)
        {
            Debug.LogWarning(
                $"{itemData.itemName}의 placeablePrefab이 비어 있습니다."
            );
            return false;
        }

        Vector3 blockPosition = worldCellPosition;
        blockPosition.y += itemData.placeableYOffset;

        GameObject blockObject = Instantiate(
            itemData.placeablePrefab,
            blockPosition,
            Quaternion.identity,
            chunkTransform
        );

        blockObject.name = itemData.placeablePrefab.name;

        BreakableWall[] breakableWalls =
            blockObject.GetComponentsInChildren<BreakableWall>(true);

        if (breakableWalls.Length == 0)
        {
            Debug.LogWarning(
                $"{itemData.placeablePrefab.name}에 BreakableWall이 없습니다.",
                blockObject
            );
            return true;
        }

        foreach (BreakableWall breakableWall in breakableWalls)
        {
            breakableWall.InitializePlacedWall(
                globalSeed,
                chunkCoord,
                localCellCoord,
                globalCellCoord,
                blockObject
            );
        }

        return true;
    }

    private GameObject GetWallPrefab(SeedMapModel.WallType wallType)
    {
        switch (wallType)
        {
            case SeedMapModel.WallType.Dirt:
                return dirtWallPrefab;

            case SeedMapModel.WallType.Stone:
                return stoneWallPrefab;

            case SeedMapModel.WallType.Copper:
                return copperWallPrefab;

            case SeedMapModel.WallType.Silver:
                return silverWallPrefab;

            case SeedMapModel.WallType.Gold:
                return goldWallPrefab;

            default:
                return null;
        }
    }
}