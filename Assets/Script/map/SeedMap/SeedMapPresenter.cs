using System.Collections;
using UnityEngine;

/// <summary>
/// SeedMapModel의 생성 규칙과 SeedMapView의 오브젝트 생성을 연결함.
/// 저장 관리자 조회와 셀 단위 생성 순서를 조정
/// </summary>
public class SeedMapPresenter : MonoBehaviour
{
    [Header("MVP")]
    [SerializeField] private SeedMapModel model;
    [SerializeField] private SeedMapView view;

    public float CellSize => model != null ? model.CellSize : 1f;

    // 기존 generator.cellSize 접근을 위한 읽기 전용 호환 프로퍼티
    public float cellSize => CellSize;

    /// <summary>
    /// 청크 하나를 프레임에 걸쳐 나눠서 생성함. 한 프레임에 frameTimeBudgetMs(밀리초)만큼
    /// 시간을 쓰면 그 자리에서 한 프레임을 양보함(칸 개수가 아니라 실제 걸린 시간 기준이라
    /// 칸마다 비용이 다르거나 기기 성능이 달라도 프레임당 부담이 일정하게 유지됨).
    /// 완료되면 onComplete로 생성된 청크 루트를 전달함(실패 시 null).
    /// </summary>
    public IEnumerator GenerateChunkRoutine(
        Vector2Int chunkCoord,
        int chunkSize,
        int globalSeed,
        float frameTimeBudgetMs,
        System.Action<GameObject> onComplete)
    {
        if (!ValidateReferences())
        {
            onComplete?.Invoke(null);
            yield break;
        }

        ChunkModificationSaveManager saveManager =
            ChunkModificationSaveManager.GetOrCreate();
        saveManager.InitializeWorld(globalSeed);

        PlacedBlockSaveManager placedBlockSaveManager =
            PlacedBlockSaveManager.GetOrCreate();
        placedBlockSaveManager.InitializeWorld(globalSeed);

        Vector3 chunkWorldPosition =
            model.GetChunkWorldPosition(chunkCoord, chunkSize);

        GameObject chunkObject = view.CreateChunkRoot(chunkWorldPosition);

        int chunkSeed = model.GetChunkSeed(globalSeed, chunkCoord);

        yield return GenerateObjectsRoutine(
            chunkObject.transform,
            chunkCoord,
            chunkSize,
            chunkSeed,
            globalSeed,
            saveManager,
            placedBlockSaveManager,
            frameTimeBudgetMs
        );

        onComplete?.Invoke(chunkObject);
    }

    private bool ValidateReferences()
    {
        if (model == null)
        {
            Debug.LogError("SeedMapPresenter 오류: SeedMapModel이 연결되지 않았습니다.", this);
            return false;
        }

        if (view == null)
        {
            Debug.LogError("SeedMapPresenter 오류: SeedMapView가 연결되지 않았습니다.", this);
            return false;
        }

        return true;
    }

    private IEnumerator GenerateObjectsRoutine(
        Transform chunkTransform,
        Vector2Int chunkCoord,
        int chunkSize,
        int chunkSeed,
        int globalSeed,
        ChunkModificationSaveManager saveManager,
        PlacedBlockSaveManager placedBlockSaveManager,
        float frameTimeBudgetMs)
    {
        System.Random random = new System.Random(chunkSeed);

        SeedMapModel.TreeClearingInfo clearing =
            model.CreateTreeClearing(
                random,
                chunkSize,
                view.CanCreateTree
            );

        double budgetMs = Mathf.Max(0.1f, frameTimeBudgetMs);
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                GenerateCell(
                    chunkTransform,
                    chunkCoord,
                    chunkSize,
                    x,
                    z,
                    globalSeed,
                    random,
                    clearing,
                    saveManager,
                    placedBlockSaveManager
                );

                if (stopwatch.Elapsed.TotalMilliseconds < budgetMs)
                    continue;

                yield return null;
                stopwatch.Restart();
            }
        }
    }

    private void GenerateCell(
        Transform chunkTransform,
        Vector2Int chunkCoord,
        int chunkSize,
        int x,
        int z,
        int globalSeed,
        System.Random random,
        SeedMapModel.TreeClearingInfo clearing,
        ChunkModificationSaveManager saveManager,
        PlacedBlockSaveManager placedBlockSaveManager)
    {
        Vector2Int localCellCoord = new Vector2Int(x, z);

        Vector2Int globalCellCoord = model.GetGlobalCellCoordinate(
            chunkCoord,
            chunkSize,
            x,
            z
        );

        Vector3 localCellPosition = model.GetLocalCellPosition(x, z);
        Vector3 worldCellPosition = chunkTransform.position + localCellPosition;

        view.CreateFloor(worldCellPosition, chunkTransform);

        bool hasPlacedBlock = TryCreatePlacedBlock(
            worldCellPosition,
            chunkTransform,
            globalSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            placedBlockSaveManager
        );

        float squareDistance =
            model.GetSquareDistanceInTiles(worldCellPosition);

        if (model.IsSafeArea(squareDistance))
            return;

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
                saveManager,
                hasPlacedBlock
            );
            return;
        }

        // 저장 여부와 상관없이 기존과 같은 Random 호출 순서를 유지
        bool shouldCreateWall = model.ShouldCreateWall(random);

        if (!shouldCreateWall)
            return;

        SeedMapModel.WallType wallType =
            model.SelectWallType(squareDistance, random);

        if (hasPlacedBlock)
            return;

        if (saveManager.IsWallDestroyed(
            globalSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord))
        {
            return;
        }

        view.CreateGeneratedWall(
            wallType,
            worldCellPosition,
            chunkTransform,
            globalSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord
        );
    }

    private bool TryCreatePlacedBlock(
        Vector3 worldCellPosition,
        Transform chunkTransform,
        int globalSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        PlacedBlockSaveManager placedBlockSaveManager)
    {
        if (!placedBlockSaveManager.TryGetPlacedBlock(
            globalSeed,
            globalCellCoord,
            out int itemId))
        {
            return false;
        }

        ItemData itemData = model.FindPlaceableItem(itemId);

        if (itemData == null)
        {
            Debug.LogWarning(
                $"설치 블록 ItemData를 찾지 못했습니다. itemID = {itemId}",
                this
            );

            // 저장 기록이 존재하므로 기존 자연 생성물은 막아야함.
            return true;
        }

        view.CreatePlacedBlock(
            itemData,
            worldCellPosition,
            chunkTransform,
            globalSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord
        );

        return true;
    }

    private void TryCreateTree(
        Vector3 worldCellPosition,
        Transform chunkTransform,
        System.Random random,
        int globalSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        ChunkModificationSaveManager saveManager,
        bool suppressCreation)
    {
        // 파괴/설치 여부보다 먼저 Random을 호출하여 시드 결과를 보존
        bool shouldCreateTree = model.ShouldCreateTree(random);

        if (!shouldCreateTree)
            return;

        if (suppressCreation)
            return;

        if (saveManager.IsTreeDestroyed(
            globalSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord))
        {
            return;
        }

        view.CreateTree(
            worldCellPosition,
            chunkTransform,
            globalSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord
        );
    }
}