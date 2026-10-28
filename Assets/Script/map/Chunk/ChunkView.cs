using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// 생성된 청크 GameObject의 등록, 부모 설정, 제거를 담당
/// 어떤 청크가 필요한지는 판단하지 않음.
/// </summary>
public class ChunkView : MonoBehaviour
{
    private static readonly ProfilerMarker ReleaseMarker = new ProfilerMarker("OutPost.Chunk.ReleaseObject");
    [Header("Chunk Parent")]
    [Tooltip("생성된 청크들을 정리할 부모 오브젝트")]
    [SerializeField] private Transform chunkParent;

    private readonly Dictionary<Vector2Int, GameObject> loadedChunks =
        new Dictionary<Vector2Int, GameObject>();

    public IEnumerable<Vector2Int> LoadedChunkCoordinates => loadedChunks.Keys;

    public event Action ChunksChanged;

    public bool ContainsChunk(Vector2Int chunkCoord)
    {
        return loadedChunks.ContainsKey(chunkCoord);
    }

    public bool AddChunk(Vector2Int chunkCoord, GameObject chunkObject)
    {
        if (chunkObject == null)
            return false;

        if (loadedChunks.ContainsKey(chunkCoord))
            return false;

        chunkObject.name = $"Chunk {chunkCoord}";

        if (chunkParent != null)
            chunkObject.transform.SetParent(chunkParent, true);

        loadedChunks.Add(chunkCoord, chunkObject);
        ChunksChanged?.Invoke();
        return true;
    }

    public void RemoveChunk(Vector2Int chunkCoord)
    {
        if (!loadedChunks.TryGetValue(chunkCoord, out GameObject chunkObject))
            return;

        loadedChunks.Remove(chunkCoord);

        ReleaseChunkObject(chunkObject);

        ChunksChanged?.Invoke();
    }

    public IEnumerator RemoveChunkRoutine(Vector2Int chunkCoord, float budgetMs)
    {
        if (!loadedChunks.TryGetValue(chunkCoord, out GameObject chunkObject)) yield break;
        loadedChunks.Remove(chunkCoord);
        yield return ReleaseChunkRoutine(chunkObject, budgetMs);
        ChunksChanged?.Invoke();
    }

    public bool TryGetLoadedChunk(
        Vector2Int chunkCoord,
        out Transform chunkTransform)
    {
        chunkTransform = null;

        if (!loadedChunks.TryGetValue(chunkCoord, out GameObject chunkObject))
            return false;

        if (chunkObject == null)
            return false;

        chunkTransform = chunkObject.transform;
        return true;
    }

    public void ClearAllChunks()
    {
        if (loadedChunks.Count == 0)
            return;

        foreach (GameObject chunkObject in loadedChunks.Values)
            ReleaseChunkObject(chunkObject);

        loadedChunks.Clear();
        ChunksChanged?.Invoke();
    }

    /// <summary>
    /// 청크 루트의 자식들을 파괴 대신 GameObjectPool로 되돌리고, 빈 청크 루트만 파괴함.
    /// 채굴이 진행되어 일부 파츠가 이미 사라진 벽/나무는 재사용이 안전하지 않으므로 그대로 파괴함.
    ///
    /// 일반 스트리밍에서는 ReleaseChunkRoutine으로 분산 처리하고,
    /// 이 동기 함수는 명시적인 즉시 정리에만 사용한다.
    /// </summary>
    public static void ReleaseChunkObject(GameObject chunkObject)
    {
        // Explicit synchronous cleanup only (shutdown/immediate-load conflict).
        IEnumerator routine = ReleaseChunkRoutine(chunkObject, float.MaxValue);
        while (routine.MoveNext()) { }
    }

    public static IEnumerator ReleaseChunkRoutine(GameObject chunkObject, float budgetMs)
    {
        if (chunkObject == null) yield break;
        Transform root = chunkObject.transform;
        GameObjectPool pool = GameObjectPool.GetOrCreate();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        double budget = Mathf.Max(0.1f, budgetMs);
        int releasedThisFrame = 0;
        while (root != null && root.childCount > 0)
        {
            using (ReleaseMarker.Auto())
            {
                GameObject child = root.GetChild(root.childCount - 1).gameObject;
                // Detach one at a time so hierarchy callbacks and OnDisable work also
                // fit the frame budget. Destroying the root must not destroy pooled items.
                child.transform.SetParent(null, true);
                BreakableWallModel breakable = child.GetComponent<BreakableWallModel>();
                if (breakable != null && breakable.CurrentStage > 0)
                {
                    child.SetActive(false);
                    Destroy(child);
                }
                else pool.Despawn(child);
            }
            // Physics/render cleanup can be deferred beyond SetActive/Destroy.
            // Limit the object count as well as the measured synchronous time.
            if (++releasedThisFrame >= 32 || watch.Elapsed.TotalMilliseconds >= budget)
            {
                yield return null;
                watch.Restart();
                releasedThisFrame = 0;
            }
        }
        if (chunkObject != null) Destroy(chunkObject);
    }
}
