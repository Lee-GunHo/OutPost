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

    private ChunkView chunkView;
    private bool buildRequested;
    private float nextBuildTime;
    private float firstRequestTime;
    private float lastBuildTime = float.NegativeInfinity;
    private AsyncOperation updateOperation;
    private NavMeshData runtimeData;
    private NavMeshData originalData;
    private PlayerPresenter excludedPlayer;

    private void OnEnable()
    {
        ResolveNavMeshSurface();
        chunkView = FindFirstObjectByType<ChunkView>();
        if (chunkView != null)
            chunkView.ChunksChanged += Build;
        BreakableWallPresenter.GeometryChanged += Build;

        nextBuildTime = Time.unscaledTime + Mathf.Max(0f, buildDelay);
        firstRequestTime = Time.unscaledTime;
        buildRequested = true;
    }

    private void OnDisable()
    {
        if (chunkView != null)
            chunkView.ChunksChanged -= Build;
        BreakableWallPresenter.GeometryChanged -= Build;

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
        if (navMeshSurface == null || !navMeshSurface.isActiveAndEnabled)
        {
            Debug.LogWarning("An active NavMeshSurface is required for runtime navigation.", this);
            return;
        }

        ExcludePlayersFromBuild();
        using (BuildMarker.Auto())
        {
            Physics.SyncTransforms();
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
            // Source collection remains synchronous in Unity's API, but initial and
            // subsequent baking both run asynchronously on our own data instance.
            updateOperation = navMeshSurface.UpdateNavMesh(runtimeData);
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

    private void ExcludePlayersFromBuild()
    {
        if (excludedPlayer != null) return;
        excludedPlayer = FindFirstObjectByType<PlayerPresenter>();
        if (excludedPlayer == null) return;
        NavMeshModifier modifier = excludedPlayer.GetComponent<NavMeshModifier>();
        if (modifier == null) modifier = excludedPlayer.gameObject.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
        modifier.applyToChildren = true;
        modifier.enabled = true;
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
