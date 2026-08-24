using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 청크 시스템 설정 및 좌표 계산 담당
/// GameObject 생성/삭제와 플레이어 추적은 담당하지 않음.
/// </summary>
public class ChunkModel : MonoBehaviour
{
    [Header("Chunk Settings")]
    [Tooltip("청크 한 변의 타일 개수")]
    [Min(1)]
    [SerializeField] private int chunkSize = 16;

    [Tooltip("플레이어 주변 몇 청크까지 유지할지")]
    [Min(0)]
    [SerializeField] private int viewDistance = 2;

    [Header("Seed Settings")]
    [Tooltip("전체 맵의 기준 시드")]
    [SerializeField] private int globalSeed = 1234;

    private Vector2Int currentChunkCoord;
    private Vector2Int lastChunkCoord;
    private bool isInitialized;

    public int ChunkSize => chunkSize;
    public int ViewDistance => viewDistance;
    public int GlobalSeed => globalSeed;
    public Vector2Int CurrentChunkCoord => currentChunkCoord;

    public void Initialize(Vector3 playerPosition, float cellSize)
    {
        currentChunkCoord = WorldToChunkCoord(playerPosition, cellSize);
        lastChunkCoord = new Vector2Int(int.MinValue, int.MinValue);
        isInitialized = true;
    }

    /// <summary>
    /// 플레이어 위치를 반영하고, 청크가 변경되었는지 반환
    /// </summary>
    public bool UpdatePlayerPosition(Vector3 playerPosition, float cellSize)
    {
        if (!isInitialized)
            Initialize(playerPosition, cellSize);

        currentChunkCoord = WorldToChunkCoord(playerPosition, cellSize);
        return currentChunkCoord != lastChunkCoord;
    }

    public void ConfirmCurrentChunk()
    {
        lastChunkCoord = currentChunkCoord;
    }

    public Vector2Int WorldToChunkCoord(Vector3 worldPosition, float cellSize)
    {
        float safeCellSize = Mathf.Max(0.01f, cellSize);
        float chunkWorldSize = chunkSize * safeCellSize;

        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkWorldSize),
            Mathf.FloorToInt(worldPosition.z / chunkWorldSize)
        );
    }

    public IEnumerable<Vector2Int> GetRequiredChunkCoordinates()
    {
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                yield return new Vector2Int(
                    currentChunkCoord.x + x,
                    currentChunkCoord.y + z
                );
            }
        }
    }

    public bool ShouldKeepChunk(Vector2Int chunkCoord)
    {
        int distanceX = Mathf.Abs(chunkCoord.x - currentChunkCoord.x);
        int distanceZ = Mathf.Abs(chunkCoord.y - currentChunkCoord.y);

        return distanceX <= viewDistance && distanceZ <= viewDistance;
    }

    public Vector2Int WorldToGlobalCell(Vector3 worldPosition, float cellSize)
    {
        float safeCellSize = Mathf.Max(0.01f, cellSize);

        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / safeCellSize),
            Mathf.RoundToInt(worldPosition.z / safeCellSize)
        );
    }

    public Vector3 GlobalCellToWorldPosition(
        Vector2Int globalCell,
        float cellSize,
        float yOffset = 0f)
    {
        float safeCellSize = Mathf.Max(0.01f, cellSize);

        return new Vector3(
            globalCell.x * safeCellSize,
            yOffset,
            globalCell.y * safeCellSize
        );
    }

    public Vector2Int GlobalCellToChunkCoord(Vector2Int globalCell)
    {
        return new Vector2Int(
            FloorDivide(globalCell.x, chunkSize),
            FloorDivide(globalCell.y, chunkSize)
        );
    }

    public Vector2Int GlobalCellToLocalCell(Vector2Int globalCell)
    {
        Vector2Int chunkCoord = GlobalCellToChunkCoord(globalCell);

        return new Vector2Int(
            globalCell.x - chunkCoord.x * chunkSize,
            globalCell.y - chunkCoord.y * chunkSize
        );
    }

    private static int FloorDivide(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;

        if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
            quotient--;

        return quotient;
    }

    private void OnValidate()
    {
        chunkSize = Mathf.Max(1, chunkSize);
        viewDistance = Mathf.Max(0, viewDistance);
    }
}