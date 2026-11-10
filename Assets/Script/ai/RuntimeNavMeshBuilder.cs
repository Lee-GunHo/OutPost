using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

public class RuntimeNavMeshBuilder : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField, Min(0f)] private float buildDelay = 2f;

    // Batch a burst of chunk loads/unloads into one update.
    private const float UpdateDelay = 0.35f;
    private const float MinimumBuildInterval = 0.75f;
    private const float MaximumRequestDelay = 1f;
    private static readonly ProfilerMarker BuildMarker = new ProfilerMarker("OutPost.NavMesh.CollectAndSchedule");
    private static readonly ProfilerMarker CollectChunkMarker = new ProfilerMarker("OutPost.NavMesh.CollectChunk");

    private struct ChunkSources
    {
        public List<NavMeshBuildSource> Sources;
        public Bounds Bounds;
    }

    // Collecting the whole scene took ~280ms per rebuild, so each chunk is collected once.
    private readonly Dictionary<Vector2Int, ChunkSources> chunkSources = new Dictionary<Vector2Int, ChunkSources>();
    private readonly HashSet<Vector2Int> dirtyChunks = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> staleChunks = new List<Vector2Int>();
    private readonly List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
    private readonly List<NavMeshBuildSource> allSources = new List<NavMeshBuildSource>();

    private ChunkView chunkView;
    private bool buildRequested;
    private float nextBuildTime;
    private float firstRequestTime;
    private float lastBuildTime = float.NegativeInfinity;
    private AsyncOperation updateOperation;
    private NavMeshData runtimeData;
    private NavMeshData originalData;

    private void OnEnable()
    {
        ResolveNavMeshSurface();
        chunkView = FindFirstObjectByType<ChunkView>();
        if (chunkView != null)
            chunkView.ChunksChanged += OnChunksChanged;
        BreakableWallPresenter.GeometryChanged += OnGeometryChanged;

        nextBuildTime = Time.unscaledTime + Mathf.Max(0f, buildDelay);
        firstRequestTime = Time.unscaledTime;
        buildRequested = true;
    }

    private void OnDisable()
    {
        if (chunkView != null)
            chunkView.ChunksChanged -= OnChunksChanged;
        BreakableWallPresenter.GeometryChanged -= OnGeometryChanged;

        CancelUpdate();
    }

    private void Update()
    {
        if (updateOperation != null)
        {
            if (!updateOperation.isDone)
                return;

            updateOperation = null;
            // Keep the previous registered mesh until the initial async build is ready.
            if (runtimeData != null && navMeshSurface != null && navMeshSurface.navMeshData != runtimeData)
            {
                navMeshSurface.RemoveData();
                navMeshSurface.navMeshData = runtimeData;
                if (navMeshSurface.isActiveAndEnabled) navMeshSurface.AddData();
            }
        }

        if (!buildRequested || Time.unscaledTime < nextBuildTime ||
            Time.unscaledTime < lastBuildTime + MinimumBuildInterval)
            return;

        buildRequested = false;
        ResolveNavMeshSurface();
        if (navMeshSurface == null || !navMeshSurface.isActiveAndEnabled || chunkView == null)
        {
            Debug.LogWarning("An active NavMeshSurface and a ChunkView are required for runtime navigation.", this);
            return;
        }

        using (BuildMarker.Auto())
        {
            SyncChunkSources();
            RecollectDirtyChunks();
            if (!TryCombineSources(out Bounds bounds))
                return;

            if (runtimeData == null)
            {
                originalData = navMeshSurface.navMeshData;
                runtimeData = new NavMeshData(navMeshSurface.agentTypeID)
                {
                    name = "OutPost runtime navigation",
                    position = navMeshSurface.transform.position,
                    rotation = navMeshSurface.transform.rotation
                };
            }
            // Initial and subsequent baking both run asynchronously on our own data instance.
            updateOperation = NavMeshBuilder.UpdateNavMeshDataAsync(
                runtimeData, navMeshSurface.GetBuildSettings(), allSources, bounds);
        }
        lastBuildTime = Time.unscaledTime;
    }

    [ContextMenu("Request NavMesh Update")]
    public void Build()
    {
        if (!buildRequested) firstRequestTime = Time.unscaledTime;
        buildRequested = true;
        // Continuous streaming/mining must not postpone a rebuild indefinitely.
        float deadline = Mathf.Min(Time.unscaledTime + UpdateDelay, firstRequestTime + MaximumRequestDelay);
        nextBuildTime = Mathf.Max(nextBuildTime, deadline);
    }

    private void OnChunksChanged()
    {
        SyncChunkSources();
        Build();
    }

    private void OnGeometryChanged(Vector2Int chunkCoord)
    {
        // The broken parts are destroyed later this frame, so collect at schedule time.
        dirtyChunks.Add(chunkCoord);
        Build();
    }

    private void SyncChunkSources()
    {
        if (chunkView == null || navMeshSurface == null)
            return;

        foreach (Vector2Int coord in chunkView.LoadedChunkCoordinates)
        {
            if (!chunkSources.ContainsKey(coord))
                CollectChunk(coord);
        }

        staleChunks.Clear();
        foreach (Vector2Int coord in chunkSources.Keys)
        {
            if (!chunkView.ContainsChunk(coord))
                staleChunks.Add(coord);
        }

        foreach (Vector2Int coord in staleChunks)
        {
            chunkSources.Remove(coord);
            dirtyChunks.Remove(coord);
        }
    }

    private void RecollectDirtyChunks()
    {
        foreach (Vector2Int coord in dirtyChunks)
            CollectChunk(coord);
        dirtyChunks.Clear();
    }

    private void CollectChunk(Vector2Int coord)
    {
        if (!chunkView.TryGetLoadedChunk(coord, out Transform chunkRoot))
            return;

        using (CollectChunkMarker.Auto())
        {
            BuildMarkups();
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(chunkRoot, navMeshSurface.layerMask, navMeshSurface.useGeometry,
                navMeshSurface.defaultArea, markups, sources);

            // Same filters NavMeshSurface applies when these options are on.
            if (navMeshSurface.ignoreNavMeshAgent)
                sources.RemoveAll(s => s.component != null && s.component.GetComponent<NavMeshAgent>() != null);
            if (navMeshSurface.ignoreNavMeshObstacle)
                sources.RemoveAll(s => s.component != null && s.component.GetComponent<NavMeshObstacle>() != null);

            chunkSources[coord] = new ChunkSources { Sources = sources, Bounds = CalculateBounds(sources) };
        }
    }

    private void BuildMarkups()
    {
        markups.Clear();
        foreach (NavMeshModifier modifier in NavMeshModifier.activeModifiers)
        {
            if ((navMeshSurface.layerMask & (1 << modifier.gameObject.layer)) == 0 ||
                !modifier.AffectsAgentType(navMeshSurface.agentTypeID))
                continue;

            markups.Add(new NavMeshBuildMarkup
            {
                root = modifier.transform,
                overrideArea = modifier.overrideArea,
                area = modifier.area,
                ignoreFromBuild = modifier.ignoreFromBuild,
                applyToChildren = modifier.applyToChildren,
                overrideGenerateLinks = modifier.overrideGenerateLinks,
                generateLinks = modifier.generateLinks
            });
        }
    }

    // Bounds are in the surface's unscaled local space, matching NavMeshSurface.UpdateNavMesh.
    private Bounds CalculateBounds(List<NavMeshBuildSource> sources)
    {
        Transform surface = navMeshSurface.transform;
        Matrix4x4 worldToSurface = Matrix4x4.TRS(surface.position, surface.rotation, Vector3.one).inverse;
        Bounds result = default;
        bool hasBounds = false;

        foreach (NavMeshBuildSource source in sources)
        {
            Bounds local = source.sourceObject is Mesh mesh ? mesh.bounds : new Bounds(Vector3.zero, source.size);
            Matrix4x4 matrix = worldToSurface * source.transform;
            Vector3 x = Abs(matrix.MultiplyVector(Vector3.right));
            Vector3 y = Abs(matrix.MultiplyVector(Vector3.up));
            Vector3 z = Abs(matrix.MultiplyVector(Vector3.forward));
            var transformed = new Bounds(matrix.MultiplyPoint(local.center),
                x * local.size.x + y * local.size.y + z * local.size.z);

            if (hasBounds) result.Encapsulate(transformed);
            else result = transformed;
            hasBounds = true;
        }

        return result;
    }

    private static Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private bool TryCombineSources(out Bounds bounds)
    {
        allSources.Clear();
        bounds = default;

        foreach (ChunkSources chunk in chunkSources.Values)
        {
            if (chunk.Sources.Count == 0)
                continue;

            if (allSources.Count == 0) bounds = chunk.Bounds;
            else bounds.Encapsulate(chunk.Bounds);
            allSources.AddRange(chunk.Sources);
        }

        // Inflate slightly so co-planar floor sources are not clipped.
        bounds.Expand(0.1f);
        return allSources.Count > 0;
    }

    private void CancelUpdate()
    {
        if (updateOperation != null && !updateOperation.isDone && runtimeData != null)
            NavMeshBuilder.Cancel(runtimeData);

        updateOperation = null;
    }

    private void OnDestroy()
    {
        CancelUpdate();

        if (runtimeData == null)
            return;

        if (navMeshSurface != null && navMeshSurface.navMeshData == runtimeData)
        {
            navMeshSurface.RemoveData();
            navMeshSurface.navMeshData = originalData;
            if (navMeshSurface.isActiveAndEnabled)
                navMeshSurface.AddData();
        }

        Destroy(runtimeData);
    }

    private void ResolveNavMeshSurface()
    {
        if (navMeshSurface != null)
        {
            return;
        }

        navMeshSurface = GetComponent<NavMeshSurface>();

        if (navMeshSurface != null)
        {
            return;
        }

        navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
    }
}
