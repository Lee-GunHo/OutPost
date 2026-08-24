using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 생성된 청크 GameObject의 등록, 부모 설정, 제거를 담당
/// 어떤 청크가 필요한지는 판단하지 않음.
/// </summary>
public class ChunkView : MonoBehaviour
{
    [Header("Chunk Parent")]
    [Tooltip("생성된 청크들을 정리할 부모 오브젝트")]
    [SerializeField] private Transform chunkParent;

    private readonly Dictionary<Vector2Int, GameObject> loadedChunks =
        new Dictionary<Vector2Int, GameObject>();

    public IEnumerable<Vector2Int> LoadedChunkCoordinates => loadedChunks.Keys;

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
        return true;
    }

    public void RemoveChunk(Vector2Int chunkCoord)
    {
        if (!loadedChunks.TryGetValue(chunkCoord, out GameObject chunkObject))
            return;

        loadedChunks.Remove(chunkCoord);

        if (chunkObject != null)
            Destroy(chunkObject);
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
        foreach (GameObject chunkObject in loadedChunks.Values)
        {
            if (chunkObject != null)
                Destroy(chunkObject);
        }

        loadedChunks.Clear();
    }
}