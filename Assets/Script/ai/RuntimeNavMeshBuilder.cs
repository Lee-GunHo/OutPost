using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class RuntimeNavMeshBuilder : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField, Min(0f)] private float buildDelay = 2f;

    // Batch a burst of chunk loads/unloads into one update.
    private const float UpdateDelay = 0.15f;

    private ChunkView chunkView;
    private bool buildRequested;
    private float nextBuildTime;
    private AsyncOperation updateOperation;
    private NavMeshData runtimeData;
    private NavMeshData originalData;

    private void OnEnable()
    {
        ResolveNavMeshSurface();
        chunkView = FindFirstObjectByType<ChunkView>();
        if (chunkView != null)
            chunkView.ChunksChanged += Build;

        nextBuildTime = Time.unscaledTime + Mathf.Max(0f, buildDelay);
        buildRequested = true;
    }

    private void OnDisable()
    {
        if (chunkView != null)
            chunkView.ChunksChanged -= Build;

        CancelUpdate();
    }

    private void Update()
    {
        if (updateOperation != null)
        {
            if (!updateOperation.isDone)
                return;

            updateOperation = null;
        }

        if (!buildRequested || Time.unscaledTime < nextBuildTime)
            return;

        buildRequested = false;
        ResolveNavMeshSurface();
        if (navMeshSurface == null || !navMeshSurface.isActiveAndEnabled)
        {
            Debug.LogWarning("An active NavMeshSurface is required for runtime navigation.", this);
            return;
        }

        ExcludePlayersFromBuild();
        Physics.SyncTransforms();

        if (runtimeData == null)
        {
            // Build once, then keep this data registered while updating its changed tiles.
            // Do not update or destroy a shared, baked asset directly.
            originalData = navMeshSurface.navMeshData;
            navMeshSurface.BuildNavMesh();
            if (navMeshSurface.navMeshData != originalData)
                runtimeData = navMeshSurface.navMeshData;
        }
        else
        {
            updateOperation = navMeshSurface.UpdateNavMesh(runtimeData);
        }
    }

    [ContextMenu("Request NavMesh Update")]
    public void Build()
    {
        buildRequested = true;
        nextBuildTime = Mathf.Max(nextBuildTime, Time.unscaledTime + UpdateDelay);
    }

    private static void ExcludePlayersFromBuild()
    {
        // The player's collider is not map geometry. Excluding the whole hierarchy
        // keeps the floor below the player walkable without disabling collisions.
        foreach (PlayerPresenter player in FindObjectsByType<PlayerPresenter>(FindObjectsSortMode.None))
        {
            NavMeshModifier modifier = player.GetComponent<NavMeshModifier>();
            if (modifier == null)
                modifier = player.gameObject.AddComponent<NavMeshModifier>();

            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;
            modifier.enabled = true;
        }
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
