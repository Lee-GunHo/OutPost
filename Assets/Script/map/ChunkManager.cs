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

    // 생성된 청크들을 보기 좋게 정리하기 위한 부모 오브젝트 자리
    public Transform chunkParent;

    // 실제 청크 내부의 Tilemap과 타일 생성을 담당하는 스크립트 자리
    public SeedMapGenerator generator;

    [Header("Chunk Settings")]
    [Tooltip("청크 하나의 크기")]
    public int chunkSize = 16;

    [Tooltip("플레이어 주변 몇 청크까지")]
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

    void Start()
    {
        // 처음엔 lastChunkCoord를 말이 안되는 값으로 설정해야
        // 게임 시작 직후 무조건 청크가 한 번 생성됨.
        lastChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        // 게임 시작 시 플레이어 주변 청크 생성
        UpdateChunks();

        // 현재 청크 좌표 기록
        lastChunkCoord = GetChunkCoordFromPosition(player.position);
    }

    void Update()
    {
        // 현재 플레이어 위치를 청크 좌표로 변환
        currentChunkCoord = GetChunkCoordFromPosition(player.position);

        // 플레이어가 이전과 같은 청크에 있으면 청크를 다시 검사할 필요가 없음.
        // 다른 청크로 넘어 갔을 대 언로드/로드 갱신
        if(currentChunkCoord != lastChunkCoord)
        {
            UpdateChunks();

            // 현재 청크 좌표를 마지막 청크 좌표로 저장.
            lastChunkCoord = currentChunkCoord;
        }
    }

    /// <summary>
    /// 월드 좌표를 청크 좌표로 바꾸는 함수
    /// </summary>
    /// <param name="position">현재 플레이어 위치</param>
    /// <returns></returns>
    private Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int chunkX = Mathf.FloorToInt(position.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(position.z / chunkSize);

        return new Vector2Int(chunkX, chunkZ);
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
            for(int y = -viewDistance; y <= viewDistance; y++)
            {
                // 현재 플레이어 청크 기준으로 주변 청크 좌표
                Vector2Int coord = new Vector2Int(
                    currentChunkCoord.x + x,
                    currentChunkCoord.y + y
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
            int distanceY = Mathf.Abs(chunk.Key.y - currentChunkCoord.y);

            // viewDistance의 범위 밖이면 제거 대상임.
            if(distanceX > viewDistance || distanceY > viewDistance)
            {
                Destroy(chunk.Value);
                chunksToRemove.Add(chunk.Key);
            }
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

        // 보기 좋게 이름 지정
        chunkObject.name = "Chunk " + chunkCoord;

        // chunkParent가 연결되어 있으면 그 아래에 청크 넣기
        if(chunkParent != null)
        {
            chunkObject.transform.parent = chunkParent;
        }

        // 생성된 청크를 Dictionary에 등록
        loadedChunks.Add(chunkCoord, chunkObject);
    }
}
