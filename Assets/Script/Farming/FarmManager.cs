using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 농사 시스템 본체. 밭 목록/성장/갈기/심기/수확/저장.
/// 씬에 배치할 필요 없음 — ChunkManager가 있는 씬이 열리면 자동 생성(ProgressTracker와 같은 방식).
/// 설정은 Resources/Farming/FarmSettings 에셋에서 읽음.
///
/// 입력 경로
/// - 좌클릭(공격): PlayerPresenter가 TryHandlePrimaryAction을 먼저 호출 → 마우스 칸의 작물 수확 / 괭이로 갈기·되돌리기
/// - 우클릭(씨앗 심기)과 미리보기: FarmInputController
/// </summary>
public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance { get; private set; }

    private readonly Dictionary<Vector2Int, FarmPlot> plots = new Dictionary<Vector2Int, FarmPlot>();
    private readonly Dictionary<Vector2Int, FarmPlotView> views = new Dictionary<Vector2Int, FarmPlotView>();
    private readonly System.Random random = new System.Random();

    private FarmSettings settings;
    private ChunkManager chunkManager;
    private int worldSeed;
    private bool isLoaded;
    private bool isDirty;
    private float saveTimer;

    public FarmSettings Settings => settings;
    public ChunkManager Chunks => chunkManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForScene()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => EnsureCreated();
        EnsureCreated();
    }

    private static void EnsureCreated()
    {
        if (Instance != null || FindFirstObjectByType<ChunkManager>() == null)
            return;

        new GameObject("FarmManager").AddComponent<FarmManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        settings = Resources.Load<FarmSettings>(FarmSettings.ResourcePath);

        if (settings == null)
        {
            Debug.LogError($"[Farm] Resources/{FarmSettings.ResourcePath} 에셋이 없어 농사 시스템을 끕니다.");
            enabled = false;
            return;
        }

        gameObject.AddComponent<FarmInputController>();
    }

    private void Start()
    {
        chunkManager = FindFirstObjectByType<ChunkManager>();
        if (chunkManager == null)
        {
            enabled = false;
            return;
        }

    }

    // 월드 시드는 ChunkManager 초기화 이후에 확정되므로 첫 Update에서 불러옴(Start 순서에 의존하지 않게)
    private void LoadWorld()
    {
        worldSeed = chunkManager.GlobalSeed;

        foreach (FarmPlot plot in FarmSaveStore.Load(worldSeed, settings))
            AddPlot(plot);

        isLoaded = true;
    }

    private void Update()
    {
        if (!isLoaded)
        {
            LoadWorld();
            return;
        }

        float delta = Time.deltaTime;
        bool anyGrowing = false;

        foreach (KeyValuePair<Vector2Int, FarmPlot> pair in plots)
        {
            FarmPlot plot = pair.Value;

            if (!plot.HasCrop || plot.IsMature)
                continue;

            anyGrowing = true;

            if (plot.Grow(delta))
                views[pair.Key].Refresh(plot);
        }

        if (anyGrowing)
            isDirty = true;

        saveTimer += Time.unscaledDeltaTime;
        if (isDirty && saveTimer >= settings.autoSaveInterval)
            SaveNow();
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SaveNow();
        Instance = null;
    }

    // ---------------------------------------------------------------- 조회

    public bool HasPlot(Vector2Int cell) => plots.ContainsKey(cell);

    public bool TryGetPlot(Vector2Int cell, out FarmPlot plot) => plots.TryGetValue(cell, out plot);

    public Vector2Int WorldToCell(Vector3 worldPosition) => chunkManager.WorldToGlobalCell(worldPosition);

    public Vector3 CellToWorld(Vector2Int cell) => chunkManager.GlobalCellToWorldPosition(cell, 0f);

    /// <summary>
    /// 마우스가 가리키는 바닥(y=0) 칸. 블록 설치 미리보기와 같은 기준이라 미리보기 칸 = 실제 동작 칸
    /// </summary>
    public bool TryGetMouseCell(out Vector2Int cell)
    {
        cell = default;
        Camera camera = Camera.main;

        if (!isLoaded || camera == null || UnityEngine.InputSystem.Mouse.current == null)
            return false;

        Ray ray = camera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());

        if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
            return false;

        cell = WorldToCell(ray.GetPoint(enter));
        return true;
    }

    public bool IsInRange(Vector2Int cell, Vector3 playerPosition, float range)
    {
        Vector3 cellPosition = CellToWorld(cell);
        cellPosition.y = 0f;
        playerPosition.y = 0f;
        return Vector3.Distance(cellPosition, playerPosition) <= range;
    }

    /// <summary>
    /// 밭으로 갈 수 있는 칸: 청크가 로드되어 있고, 밭/설치 블록/벽·나무 등이 없는 땅
    /// </summary>
    public bool CanTill(Vector2Int cell)
    {
        if (!isLoaded || plots.ContainsKey(cell))
            return false;

        if (!chunkManager.TryGetLoadedChunk(chunkManager.GlobalCellToChunkCoord(cell), out _))
            return false;

        if (PlacedBlockSaveManager.GetOrCreate().TryGetPlacedBlock(worldSeed, cell, out _))
            return false;

        float cellSize = chunkManager.CellSize;
        Vector3 center = chunkManager.GlobalCellToWorldPosition(cell, 1f);
        Vector3 halfExtents = new Vector3(cellSize * 0.45f, 0.9f, cellSize * 0.45f);

        return !Physics.CheckBox(center, halfExtents, Quaternion.identity,
            settings.tillBlockerMask, QueryTriggerInteraction.Ignore);
    }

    public bool CanPlant(Vector2Int cell)
    {
        return plots.TryGetValue(cell, out FarmPlot plot) && !plot.HasCrop;
    }

    // ---------------------------------------------------------------- 동작

    /// <summary>
    /// 좌클릭(공격) 시 PlayerPresenter가 먼저 호출. 처리했으면 true → 공격하지 않음.
    /// 마우스가 가리키는 칸만 대상으로 해서, 몬스터를 치다가 사이에 있는 작물이 수확되지 않게 함.
    /// </summary>
    public bool TryHandlePrimaryAction(ItemData heldItem, Vector3 playerPosition, float range)
    {
        if (!TryGetMouseCell(out Vector2Int cell))
            return false;

        if (!IsInRange(cell, playerPosition, range))
            return false;

        plots.TryGetValue(cell, out FarmPlot plot);

        if (plot != null && plot.HasCrop)
        {
            Harvest(plot);
            SfxPlayer.PlayAt(SfxId.Harvest, CellToWorld(cell));
            return true;
        }

        if (heldItem == null || heldItem.toolType != ToolType.Hoe)
            return false;

        if (plot != null)
        {
            RemovePlot(cell); // 빈 밭을 괭이로 다시 치면 그 한 칸만 땅으로 되돌림
            SfxPlayer.PlayAt(SfxId.Till, CellToWorld(cell));
            return true;
        }

        // 괭이 등급별 범위(예: 금 괭이 5x5) 중 갈 수 있는 칸만 밭으로
        int tilledCount = 0;

        foreach (Vector2Int areaCell in GetHoeCells(heldItem, cell, playerPosition))
        {
            if (!CanTill(areaCell))
                continue;

            AddPlot(new FarmPlot(areaCell));
            tilledCount++;
        }

        if (tilledCount == 0)
            return false;

        SfxPlayer.PlayAt(SfxId.Till, CellToWorld(cell));
        MarkChangedAndSave();
        return true;
    }

    /// <summary>
    /// 괭이로 target을 쳤을 때 갈리는 칸들. 플레이어가 바라보는 방향 기준으로 범위를 잡음
    /// </summary>
    public List<Vector2Int> GetHoeCells(ItemData hoe, Vector2Int target, Vector3 playerPosition)
    {
        (int width, int length) = settings.GetHoeArea(hoe);
        Vector3 delta = CellToWorld(target) - playerPosition;
        Vector2Int facing = FarmRules.GetFacing(delta.x, delta.z);
        return FarmRules.GetAreaCells(target, facing, width, length);
    }

    /// <summary>
    /// 테스트용: 심어진 모든 작물의 성장 시간을 seconds만큼 진행. 다 자라게 하려면 큰 값(예: float.MaxValue)
    /// </summary>
    public int DebugAdvanceGrowth(float seconds)
    {
        int grownCount = 0;

        foreach (FarmPlot plot in plots.Values)
        {
            if (!plot.HasCrop || plot.IsMature)
                continue;

            plot.Grow(Mathf.Min(seconds, plot.Crop.growSeconds));
            views[plot.Cell].Refresh(plot);
            grownCount++;
        }

        if (grownCount > 0)
            MarkChangedAndSave();

        return grownCount;
    }

    public bool Plant(Vector2Int cell, CropData crop)
    {
        if (crop == null || !plots.TryGetValue(cell, out FarmPlot plot) || plot.HasCrop)
            return false;

        plot.Plant(crop);
        views[cell].Refresh(plot);
        SfxPlayer.PlayAt(SfxId.Plant, CellToWorld(cell));
        MarkChangedAndSave();
        return true;
    }

    private void Harvest(FarmPlot plot)
    {
        CropData crop = plot.Crop;
        (int cropAmount, int seedAmount) = FarmRules.RollHarvest(
            random, plot.IsMature,
            crop.minCropYield, crop.maxCropYield, crop.minSeedYield, crop.maxSeedYield);

        Vector3 dropPosition = CellToWorld(plot.Cell);
        SpawnDrop(crop.cropItem, cropAmount, dropPosition);
        SpawnDrop(crop.seedItem, seedAmount, dropPosition);

        plot.ClearCrop();
        views[plot.Cell].Refresh(plot);
        MarkChangedAndSave();
    }

    private void SpawnDrop(ItemData item, int amount, Vector3 position)
    {
        if (item == null || amount <= 0)
            return;

        if (settings.droppedItemPrefab == null)
        {
            Debug.LogWarning($"[Farm] FarmSettings.droppedItemPrefab이 없어 {item.itemName} x{amount}을 떨어뜨리지 못했습니다.");
            return;
        }

        Vector2 offset = Random.insideUnitCircle * 0.3f;
        DroppedItemPresenter dropped = Instantiate(settings.droppedItemPrefab,
            position + new Vector3(offset.x, 0.3f, offset.y), Quaternion.identity);
        dropped.Configure(item, amount);
    }

    private void AddPlot(FarmPlot plot)
    {
        plots[plot.Cell] = plot;

        GameObject viewObject = new GameObject($"FarmPlot_{plot.Cell.x}_{plot.Cell.y}");
        viewObject.transform.SetParent(transform, false);
        viewObject.transform.position = CellToWorld(plot.Cell);

        FarmPlotView view = viewObject.AddComponent<FarmPlotView>();
        view.Initialize(settings.soilColor);
        view.Refresh(plot);
        views[plot.Cell] = view;
    }

    private void RemovePlot(Vector2Int cell)
    {
        plots.Remove(cell);

        if (views.TryGetValue(cell, out FarmPlotView view))
        {
            Destroy(view.gameObject);
            views.Remove(cell);
        }

        MarkChangedAndSave();
    }

    private void MarkChangedAndSave()
    {
        isDirty = true;
        SaveNow();
    }

    private void SaveNow()
    {
        saveTimer = 0f;

        if (!isLoaded || !isDirty)
            return;

        FarmSaveStore.Save(worldSeed, plots.Values);
        isDirty = false;
    }
}
