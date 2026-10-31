using UnityEngine;

/// <summary>
/// BreakableWallModel과 BreakableWallView를 연결하고
/// 파괴 저장 및 설치 블록 저장 제거를 제어
/// </summary>
[RequireComponent(typeof(BreakableWallModel))]
[RequireComponent(typeof(BreakableWallView))]
public class BreakableWallPresenter : MonoBehaviour
{
    public static event System.Action GeometryChanged;

    /// <summary>
    /// 완전히 파괴되어 오브젝트가 제거되기 직전에 호출 (상자 내용물 드롭 등)
    /// </summary>
    public event System.Action<BreakableWallPresenter> Broken;

    [Header("MVP")]
    [SerializeField] private BreakableWallModel model;
    [SerializeField] private BreakableWallView view;

    [Tooltip("켜면 플레이어가 직접 설치한 것만 부술 수 있음. 씬에 미리 놓인 오브젝트는 파괴 상태를 저장할 수 없어 다시 생기므로 상자처럼 내용물이 있는 경우 켤 것")]
    [SerializeField] private bool onlyBreakIfPlayerPlaced;

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

    /// <summary>
    /// 남은 내구도와 상관없이 즉시 완전 파괴
    /// </summary>
    public void Break()
    {
        ResolveComponents();

        if (model == null)
            return;

        ApplyDamage(model.CurrentDurability);
    }

    /// <summary>
    /// 곡괭이로 한 번 채굴. 데미지는 MiningRules.CalculateDamage(pickPower, 벽 단단함)
    /// </summary>
    public void Hit(int pickPower)
    {
        ResolveComponents();

        if (model == null)
            return;

        ApplyDamage(MiningRules.CalculateDamage(pickPower, model.Hardness));
    }

    private void ApplyDamage(int damage)
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

        if (onlyBreakIfPlayerPlaced && !model.IsPlayerPlaced)
            return;

        // 파츠가 지워지기 전에 소리/파편에 쓸 정보를 먼저 잡아둠
        Renderer debrisSource = FindDebrisRenderer();
        Vector3 feedbackPosition = debrisSource != null ? debrisSource.bounds.center : transform.position;

        int previousStage = model.CurrentStage;
        bool isFullyBroken = model.ApplyDamage(damage, out int firstClearedStage);
        bool stageChanged = model.CurrentStage != previousStage;

        HitFeedback.BlockHit(feedbackPosition, GetSoundType(), isFullyBroken, debrisSource);

        for (int stage = firstClearedStage; stage < model.CurrentStage; stage++)
            view.DestroyStageParts(GetVisualRoot(), stage);

        // Placed blocks use carving. Natural walls belong to the baked geometry.
        if (!model.IsPlayerPlaced && (stageChanged || isFullyBroken))
            GeometryChanged?.Invoke();

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

        Broken?.Invoke(this);

        view.SpawnDrops(
            model.DropItemPrefab,
            model.MinDropCount,
            model.MaxDropCount,
            model.DropSpread
        );

        view.DestroyTarget(model.GeneratedRootObject);
    }

    // 나무 = 나무 소리, 프리팹 이름에 광석/돌이 들어가면 돌 소리, 흙벽은 흙 소리.
    // 설치한 제작대/상자처럼 그 외의 것은 나무 소리
    private BlockSoundType GetSoundType()
    {
        if (model.GeneratedObjectType == ChunkModificationSaveManager.ObjectTypeTree)
            return BlockSoundType.Wood;

        string objectName = GetVisualRoot().name;

        // GoldCraftingBox처럼 등급 이름이 들어간 제작대는 광석이 아님
        if (objectName.Contains("Crafting"))
            return BlockSoundType.Wood;

        if (objectName.Contains("Copper") || objectName.Contains("Silver") || objectName.Contains("Gold"))
            return BlockSoundType.Ore;

        if (objectName.Contains("Stone"))
            return BlockSoundType.Stone;

        if (objectName.Contains("Dirt"))
            return BlockSoundType.Dirt;

        return BlockSoundType.Wood;
    }

    // 외곽선(__InteractionOutline) 같은 보조 렌더러는 빼고 실제 블록 렌더러를 고름
    private Renderer FindDebrisRenderer()
    {
        foreach (Renderer candidate in GetVisualRoot().GetComponentsInChildren<Renderer>())
        {
            if (candidate.enabled && !candidate.name.StartsWith("__"))
                return candidate;
        }

        return null;
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
