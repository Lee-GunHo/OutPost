using UnityEngine;

/// <summary>
/// BreakableWallModel과 BreakableWallView를 연결하고
/// 파괴 저장 및 설치 블록 저장 제거를 제어
/// </summary>
[RequireComponent(typeof(BreakableWallModel))]
[RequireComponent(typeof(BreakableWallView))]
public class BreakableWallPresenter : MonoBehaviour
{
    [Header("MVP")]
    [SerializeField] private BreakableWallModel model;
    [SerializeField] private BreakableWallView view;

    protected BreakableWallModel Model => model;

    protected virtual void Awake()
    {
        ResolveComponents();
        ApplyLegacySerializedData();
    }

    /// <summary>
    /// 기존 BreakableWall Inspector 데이터를 Model로 전달하기 위한 확장 지점
    /// </summary>
    protected virtual void ApplyLegacySerializedData()
    {
    }

    private void ResolveComponents()
    {
        if (model == null)
            model = GetComponent<BreakableWallModel>();

        if (model == null)
            model = gameObject.AddComponent<BreakableWallModel>();

        if (view == null)
            view = GetComponent<BreakableWallView>();

        if (view == null)
            view = gameObject.AddComponent<BreakableWallView>();
    }

    public void InitializeGeneratedWall(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedObject(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ChunkModificationSaveManager.ObjectTypeWall,
            generatedRootObject,
            false
        );
    }

    public void InitializeGeneratedTree(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedObject(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ChunkModificationSaveManager.ObjectTypeTree,
            generatedRootObject,
            false
        );
    }

    public void InitializePlacedWall(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedObject(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ChunkModificationSaveManager.ObjectTypeWall,
            generatedRootObject,
            true
        );
    }

    // 이전 SeedMapGenerator와의 컴파일 호환용 오버로드
    public void InitializeGeneratedWall(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedWall(
            worldSeed,
            chunkCoord,
            localCellCoord,
            localCellCoord,
            generatedRootObject
        );
    }

    public void InitializeGeneratedTree(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedTree(
            worldSeed,
            chunkCoord,
            localCellCoord,
            localCellCoord,
            generatedRootObject
        );
    }

    private void InitializeGeneratedObject(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        string objectType,
        GameObject generatedRootObject,
        bool isPlayerPlaced)
    {
        ResolveComponents();

        if (model == null)
        {
            Debug.LogError(gameObject.name + "에 BreakableWallModel이 없습니다.");
            return;
        }

        model.InitializeGeneratedObject(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            objectType,
            generatedRootObject,
            isPlayerPlaced
        );
    }

    public void Break()
    {
        ResolveComponents();
        ApplyLegacySerializedData();

        if (model == null || view == null)
        {
            Debug.LogError(gameObject.name + "의 BreakableWall MVP 구성이 올바르지 않습니다.");
            return;
        }

        if (model.IsBroken)
            return;

        bool isFullyBroken = model.AdvanceStage(out int clearedStage);

        view.DestroyStageParts(GetVisualRoot(), clearedStage);

        if (!isFullyBroken)
            return;

        if (!model.HasGeneratedIdentity)
        {
            Debug.LogWarning(
                gameObject.name +
                ": 생성 좌표가 전달되지 않아 파괴 상태를 저장할 수 없습니다."
            );
        }
        else if (model.IsPlayerPlaced)
        {
            RemovePlacedBlockRecord();
        }
        else
        {
            SaveGeneratedObjectDestruction();
        }

        view.SpawnDrops(
            model.DropItemPrefab,
            model.MinDropCount,
            model.MaxDropCount,
            model.DropSpread
        );

        view.DestroyTarget(model.GeneratedRootObject);
    }

    /// <summary>
    /// 단계별 파츠(Up{n}/Down{n})를 제거할 대상 루트. 생성 좌표가 아직 없으면 이 오브젝트 자신을 사용
    /// </summary>
    private GameObject GetVisualRoot()
    {
        return model.GeneratedRootObject != null
            ? model.GeneratedRootObject
            : gameObject;
    }

    private void RemovePlacedBlockRecord()
    {
        bool removed = PlacedBlockSaveManager
            .GetOrCreate()
            .RemovePlacedBlock(model.WorldSeed, model.GlobalCellCoord);

        if (removed)
            return;

        Debug.LogWarning(
            "설치 블록 저장 기록을 찾지 못했습니다. " +
            $"GlobalCell({model.GlobalCellCoord.x}, {model.GlobalCellCoord.y})"
        );
    }

    private void SaveGeneratedObjectDestruction()
    {
        ChunkModificationSaveManager saveManager =
            ChunkModificationSaveManager.GetOrCreate();

        saveManager.RegisterDestroyedObject(
            model.WorldSeed,
            model.ChunkCoord,
            model.LocalCellCoord,
            model.GlobalCellCoord,
            model.GeneratedObjectType
        );

        bool saveVerified = saveManager.IsObjectDestroyed(
            model.WorldSeed,
            model.ChunkCoord,
            model.LocalCellCoord,
            model.GlobalCellCoord,
            model.GeneratedObjectType
        );

        if (!saveVerified)
        {
            Debug.LogError(
                $"[{model.GeneratedObjectType} 저장 검증 실패] " +
                $"GlobalCell({model.GlobalCellCoord.x}, {model.GlobalCellCoord.y})"
            );

            return;
        }

        if (!model.ShowSaveDebugLog)
            return;

        Debug.Log(
            $"[{model.GeneratedObjectType} 저장 검증 완료] " +
            $"Seed({model.WorldSeed}) " +
            $"GlobalCell({model.GlobalCellCoord.x}, {model.GlobalCellCoord.y}) " +
            $"Chunk({model.ChunkCoord.x}, {model.ChunkCoord.y}) " +
            $"Cell({model.LocalCellCoord.x}, {model.LocalCellCoord.y})"
        );
    }
}