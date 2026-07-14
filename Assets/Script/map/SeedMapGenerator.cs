using UnityEngine;

/// <summary>
/// 청크 좌표와 전역 시드를 사용해 바닥, 광물 벽, 나무 공터를 생성
/// 플레이어 시작 위치를 기준으로 정사각형 거리 구간별 벽 프리팹을 선택
/// 파괴 기록이 있는 벽은 청크가 다시 로드되어도 생성안함.
/// </summary>
public class SeedMapGenerator : MonoBehaviour
{
    [Header("Floor Prefab")]
    [SerializeField] private GameObject floorPrefab;

    [Header("Wall Prefabs")]
    [SerializeField] private GameObject dirtWallPrefab;
    [SerializeField] private GameObject stoneWallPrefab;
    [SerializeField] private GameObject copperWallPrefab;
    [SerializeField] private GameObject silverWallPrefab;
    [SerializeField] private GameObject goldWallPrefab;

    [Header("Generator Settings")]
    [Tooltip("각 칸에 벽이 생성될 확률")]
    [Range(0, 100)]
    [SerializeField] private int wallPercent = 35;

    [Tooltip("타일 한 칸의 실제 월드 크기")]
    [Min(0.01f)]
    public float cellSize = 1f;

    [SerializeField] private float wallYOffset = 1f;
    [SerializeField] private float floorYOffset = 0f;

    [Header("Player Start Position")]
    [SerializeField] private Vector3 playerStartPosition = Vector3.zero;

    [Header("Square Wall Ranges - Tile Distance")]
    [Tooltip("시작점에서 X 또는 Z 방향으로 이 칸 수 이내에는 벽을 만들지 않습니다.")]
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
    [SerializeField] private GameObject treePrefab;

    [Tooltip("청크 하나에 나무 공터가 생길 확률")]
    [Range(0, 100)]
    [SerializeField] private int treeClearingChance = 10;

    [SerializeField] private int clearingWidth = 5;
    [SerializeField] private int clearingLength = 10;

    [Tooltip("공터 내부 한 칸에 나무가 생성될 확률")]
    [Range(0, 100)]
    [SerializeField] private int treeFillPercent = 35;

    [SerializeField] private int clearingMargin = 1;
    [SerializeField] private float treeYOffset = 0f;

    private struct TreeClearingInfo
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

    public GameObject GenerateChunk(
        Vector2Int chunkCoord,
        int chunkSize,
        int globalSeed)
    {
        ChunkModificationSaveManager saveManager =
            ChunkModificationSaveManager.GetOrCreate();

        saveManager.InitializeWorld(globalSeed);

        GameObject chunkObject = new GameObject();

        Vector3 chunkWorldPosition = new Vector3(
            chunkCoord.x * chunkSize * cellSize,
            0f,
            chunkCoord.y * chunkSize * cellSize
        );

        chunkObject.transform.position = chunkWorldPosition;

        int chunkSeed = GetChunkSeed(globalSeed, chunkCoord);

        GenerateObjects(
            chunkObject.transform,
            chunkCoord,
            chunkSize,
            chunkSeed,
            globalSeed,
            saveManager
        );

        return chunkObject;
    }

    private int GetChunkSeed(int globalSeed, Vector2Int chunkCoord)
    {
        unchecked
        {
            int hash = globalSeed;
            hash = hash * 73856093 ^ chunkCoord.x * 19349663;
            hash = hash * 83492791 ^ chunkCoord.y * 297121507;
            return hash;
        }
    }

    private void GenerateObjects(
        Transform chunkTransform,
        Vector2Int chunkCoord,
        int chunkSize,
        int chunkSeed,
        int globalSeed,
        ChunkModificationSaveManager saveManager)
    {
        System.Random random = new System.Random(chunkSeed);
        TreeClearingInfo clearing = CreateTreeClearing(random, chunkSize);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                Vector2Int localCellCoord = new Vector2Int(x, z);
                Vector2Int globalCellCoord = new Vector2Int(
                    chunkCoord.x * chunkSize + x,
                    chunkCoord.y * chunkSize + z
                );

                Vector3 localCellPosition = new Vector3(
                    x * cellSize,
                    0f,
                    z * cellSize
                );

                Vector3 worldCellPosition =
                    chunkTransform.position + localCellPosition;

                CreateFloor(worldCellPosition, chunkTransform);

                float squareDistance =
                    GetSquareDistanceInTiles(worldCellPosition);

                // 시작 지점 안전 구역은 바닥만 생성
                if (squareDistance <= safeRange)
                    continue;

                // 공터 구역에는 광물 벽 대신 나무를 배치
                if (clearing.Contains(x, z))
                {
                    TryCreateTree(
                        worldCellPosition,
                        chunkTransform,
                        random,
                        globalSeed,
                        chunkCoord,
                        localCellCoord,
                        globalCellCoord,
                        saveManager
                    );
                    continue;
                }

                // 저장 여부와 무관하게 기존과 같은 랜덤 호출 순서를 유지해야
                // 청크의 다른 칸 배치가 안바뀜
                bool shouldCreateWall = random.Next(0, 100) < wallPercent;

                if (!shouldCreateWall)
                    continue;

                GameObject selectedWallPrefab =
                    GetWallPrefab(squareDistance, random);

                if (selectedWallPrefab == null)
                    continue;

                // 과거에 파괴된 벽이면 프리팹을 다시 안만듦
                if (saveManager.IsWallDestroyed(
                    globalSeed,
                    chunkCoord,
                    localCellCoord,
                    globalCellCoord))
                {
                    continue;
                }

                Vector3 wallPosition = worldCellPosition;
                wallPosition.y += wallYOffset;

                GameObject wallObject = Instantiate(
                    selectedWallPrefab,
                    wallPosition,
                    Quaternion.identity,
                    chunkTransform
                );

                wallObject.name = selectedWallPrefab.name;

                // BreakableWall이 프리팹 루트가 아니라 콜라이더가 있는
                // 자식 오브젝트에 붙어 있어도 모두 찾아서 초기화
                BreakableWall[] breakableWalls =
                    wallObject.GetComponentsInChildren<BreakableWall>(true);

                if (breakableWalls.Length > 0)
                {
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
                }
                else
                {
                    Debug.LogWarning(
                        selectedWallPrefab.name +
                        " 프리팹 또는 자식 오브젝트에 BreakableWall이 없어 " +
                        "파괴 상태를 저장할 수 없습니다."
                    );
                }
            }
        }
    }

    private void CreateFloor(
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

    /// <summary>
    /// X 거리와 Z 거리 중 큰 값을 사용
    /// 따라서 구간이 원형이 아니라 정사각형으로 만들어짐
    /// </summary>
    private float GetSquareDistanceInTiles(Vector3 worldCellPosition)
    {
        float xDistance =
            Mathf.Abs(worldCellPosition.x - playerStartPosition.x) / cellSize;

        float zDistance =
            Mathf.Abs(worldCellPosition.z - playerStartPosition.z) / cellSize;

        return Mathf.Max(xDistance, zDistance);
    }

    private GameObject GetWallPrefab(
        float distance,
        System.Random random)
    {
        if (distance <= dirtOnlyRange)
            return dirtWallPrefab;

        if (distance <= dirtStoneRange)
            return ChooseHalf(dirtWallPrefab, stoneWallPrefab, random);

        if (distance <= stoneOnlyRange)
            return stoneWallPrefab;

        if (distance <= stoneCopperRange)
            return ChooseHalf(stoneWallPrefab, copperWallPrefab, random);

        if (distance <= copperOnlyRange)
            return copperWallPrefab;

        if (distance <= copperSilverRange)
            return ChooseHalf(copperWallPrefab, silverWallPrefab, random);

        if (distance <= silverOnlyRange)
            return silverWallPrefab;

        if (distance <= silverGoldRange)
            return ChooseHalf(silverWallPrefab, goldWallPrefab, random);

        return goldWallPrefab;
    }

    private GameObject ChooseHalf(
        GameObject firstPrefab,
        GameObject secondPrefab,
        System.Random random)
    {
        if (firstPrefab == null)
            return secondPrefab;

        if (secondPrefab == null)
            return firstPrefab;

        return random.Next(0, 2) == 0
            ? firstPrefab
            : secondPrefab;
    }

    private TreeClearingInfo CreateTreeClearing(
        System.Random random,
        int chunkSize)
    {
        TreeClearingInfo result = new TreeClearingInfo();

        if (treePrefab == null)
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

    private void TryCreateTree(
        Vector3 worldCellPosition,
        Transform chunkTransform,
        System.Random random,
        int globalSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        ChunkModificationSaveManager saveManager)
    {
        if (treePrefab == null)
            return;

        // 청크 전용 System.Random을 사용하므로 같은 시드에서는
        // 같은 공터의 같은 칸에서 항상 같은 확률 결과가 나옴
        // 파괴된 나무라도 랜덤 호출은 먼저 수행해 다른 칸의 배치가 바뀌지 않게 함
        bool shouldCreateTree = random.Next(0, 100) < treeFillPercent;

        if (!shouldCreateTree)
            return;

        // 이전에 베어낸 나무는 같은 시드로 청크를 다시 생성해도 만들지 않음
        if (saveManager.IsTreeDestroyed(
            globalSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord))
        {
            return;
        }

        Vector3 treePosition = worldCellPosition;
        treePosition.y += treeYOffset;

        GameObject treeObject = Instantiate(
            treePrefab,
            treePosition,
            Quaternion.identity,
            chunkTransform
        );

        treeObject.name = treePrefab.name;

        // 나무 역시 파괴 스크립트가 자식에 붙어 있을 수 있으므로
        // 프리팹 전체에서 BreakableWall 계열 컴포넌트를 찾는당
        BreakableWall[] breakableTrees =
            treeObject.GetComponentsInChildren<BreakableWall>(true);

        if (breakableTrees.Length > 0)
        {
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
        }
        else
        {
            Debug.LogWarning(
                treePrefab.name +
                " 프리팹 또는 자식 오브젝트에 BreakableTree가 없어 " +
                "나무 파괴 상태를 저장할 수 없습니다."
            );
        }
    }

    private void OnValidate()
    {
        safeRange = Mathf.Max(0, safeRange);
        dirtOnlyRange = Mathf.Max(safeRange, dirtOnlyRange);
        dirtStoneRange = Mathf.Max(dirtOnlyRange, dirtStoneRange);
        stoneOnlyRange = Mathf.Max(dirtStoneRange, stoneOnlyRange);
        stoneCopperRange = Mathf.Max(stoneOnlyRange, stoneCopperRange);
        copperOnlyRange = Mathf.Max(stoneCopperRange, copperOnlyRange);
        copperSilverRange = Mathf.Max(copperOnlyRange, copperSilverRange);
        silverOnlyRange = Mathf.Max(copperSilverRange, silverOnlyRange);
        silverGoldRange = Mathf.Max(silverOnlyRange, silverGoldRange);

        clearingWidth = Mathf.Max(1, clearingWidth);
        clearingLength = Mathf.Max(1, clearingLength);
        clearingMargin = Mathf.Max(0, clearingMargin);
    }
}