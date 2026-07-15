using System.Collections.Generic;
using UnityEngine;

// 플레이어 위치를 기준으로
// 주변 청크를 생성하고
// 멀어진 청크를 제거하는 관리자 역할
public class ChunkManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("플레이어의 위치를 알기 위해 필요함")]
    public Transform player;

    [Tooltip("생성된 청크들을 정리할 부모 오브젝트")]
    public Transform chunkParent;

    [Tooltip("실제 청크 오브젝트 생성을 담당하는 스크립트")]
    public SeedMapGenerator generator;

    [Header("Chunk Settings")]
    [Tooltip("청크 한 변의 타일 개수")]
    public int chunkSize = 16;

    [Tooltip("플레이어 주변 몇 청크까지 유지할지")]
    public int viewDistance = 2;

    [Header("Seed Settings")]
    [Tooltip("전체 맵의 기준 시드")]
    public int globalSeed = 1234;

    // 현재 플레이어가 위치한 청크 좌표
    private Vector2Int currentChunkCoord;

    // 직전 프레임에서 플레이어가 위치했던 청크 좌표
    // 플레이어가 다른 청크로 갔는지 확인할 때 사용
    private Vector2Int lastChunkCoord;

    // 현재 생성되어 있는 청크들을 저장하는 dictionary
    // Key : 청크 좌표
    // Value : 실제 청크 오브젝트
    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();

    public int ChunkSize => chunkSize;
    public int GlobalSeed => globalSeed;
    public float CellSize => generator != null ? generator.cellSize : 1f;

    private void Start()
    {
        if(player == null)
        {
            Debug.LogError("ChunkManager 오류 : Player가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        if(generator == null)
        {
            Debug.LogError("ChunkManager 오류 : SeedMapGenerator가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        // 청크가 생성되기 전에 현재 시드의 파괴 기록을 먼저 불러온다.
        ChunkModificationSaveManager saveManager = ChunkModificationSaveManager.GetOrCreate();

        saveManager.InitializeWorld(globalSeed);

        // 처음엔 lastChunkCoord를 말이 안되는 값으로 설정해야
        // 게임 시작 직후 무조건 청크가 한 번 생성됨.
        lastChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        // 게임 시작 시 플레이어 주변 청크 생성
        UpdateChunks();

        // 현재 청크 좌표 기록
        lastChunkCoord = GetChunkCoordFromPosition(player.position);
    }

    private void Update()
    {
        // 현재 플레이어 위치를 청크 좌표로 변환
        currentChunkCoord = GetChunkCoordFromPosition(player.position);

        if (currentChunkCoord == lastChunkCoord)
            return;

        UpdateChunks();
        lastChunkCoord = currentChunkCoord;
    }

    /// <summary>
    /// 월드 좌표를 청크 좌표로 바꾸는 함수
    /// </summary>
    /// <param name="position">현재 플레이어 위치</param>
    /// <returns></returns>
    private Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        float chunkWorldSize = chunkSize * CellSize;

        int chunkX = Mathf.FloorToInt(position.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(position.z / chunkSize);

        return new Vector2Int(chunkX, chunkZ);
    }

    public Vector2Int WorldToGlobalCell(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / CellSize),
            Mathf.RoundToInt(worldPosition.z / CellSize)
        );
    }

    public Vector3 GlobalCellToWorldPosition(Vector2Int globalCell, float yOffset = 0f)
    {
        return new Vector3(
            globalCell.x * CellSize,
            yOffset,
            globalCell.y * CellSize
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

    public bool TryGetLoadedChunk(Vector2Int chunkCoord, out Transform chunkTransform)
    {
        chunkTransform = null;

        if(!loadedChunks.TryGetValue(chunkCoord, out GameObject chunkObject))
            return false;

        if(chunkObject == null)
            return false;

        chunkTransform = chunkObject.transform;
        return true;
    }

    private static int FloorDivide(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;

        if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
            quotient--;

        return quotient;
    }

    /// <summary>
    /// 플레이어 주변 청크 생성하고, 멀어진 청크 제거하는 함수
    /// </summary>
    private void UpdateChunks()
    {
        // 혹시나 UpdateChunks가 Start에서 먼저 호출될 수 있기 때문에
        // 현재 플레이어 청크 좌표 다시 계산
        currentChunkCoord = GetChunkCoordFromPosition(player.position);

        // 1. 플레이어 주변 청크 생성
        for(int x = -viewDistance; x <= viewDistance; x++)
        {
            for(int z = -viewDistance; z <= viewDistance; z++)
            {
                // 현재 플레이어 청크 기준으로 주변 청크 좌표
                Vector2Int coord = new Vector2Int(
                    currentChunkCoord.x + x,
                    currentChunkCoord.y + z
                );

                // 이미 생성된 청크라면 다시 만들지 않음.
                if(!loadedChunks.ContainsKey(coord))
                {
                    CreateChunk(coord);
                }
            }
        }

        // 2. 멀어진 청크 제거
        // Dictionary를 순회하며 바로 제거하면 오류 날 수 있어서
        // 먼저 제거할 청크 좌표만 따로 저장
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        foreach(KeyValuePair<Vector2Int, GameObject > chunk in loadedChunks)
        {
            // 현재 플레이어 청크와 해당 청크 사이의 거리
            int distanceX = Mathf.Abs(chunk.Key.x - currentChunkCoord.x);
            int distanceZ = Mathf.Abs(chunk.Key.y - currentChunkCoord.y);

            if (distanceX <= viewDistance && distanceZ <= viewDistance)
                continue;

            if(chunk.Value != null) 
                Destroy(chunk.Value);

            chunksToRemove.Add(chunk.Key);
        }

        // 실제 Dictionary에서 제거
        foreach(Vector2Int coord in chunksToRemove)
        {
            loadedChunks.Remove(coord);
        }
    }

    // 특정 좌표의 청크를 생성하는 함수.
    private void CreateChunk(Vector2Int chunkCoord)
    {
        // SeedMapGenerator에게 실제 청크 생성 맡김
        GameObject chunkObject = generator.GenerateChunk(
            chunkCoord,
            chunkSize,
            globalSeed
        );

        if(chunkObject == null)
        {
            Debug.LogError("청크 생성 실패 : " + chunkCoord);
            return;
        }

        chunkObject.name = "Chunk " + chunkCoord;

        if (chunkParent != null)
            chunkObject.transform.SetParent(chunkParent, true);

        loadedChunks.Add(chunkCoord, chunkObject);
    }
}
