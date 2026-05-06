using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// SeedMapGenerator.cs는 특정 청크 좌표를 받아서
/// 해당 청크 안에 Tilemap을 만들고
/// 시드 기반으로 타일을 배치하는 스크립트입니다.
/// </summary>
public class SeedMapGenerator : MonoBehaviour
{
    [Header("Prefab Settings")]

    [Tooltip("바닥 프리팹. 랜덤 결과가 바닥이면 해당 프리팹이 배치됨.")]
    public GameObject floorPrefab;

    [Tooltip("벽 프리팹. 랜덤 결과가 벽이면 해당 프리팹이 배치됨.")]
    public GameObject wallPrefab;

    [Header("Generator Setting")]

    [Tooltip("벽이 생성 될 확률")]
    [Range(0, 100)]
    public int wallPercent = 35;

    // 타일 한 칸의 실제 월드 크기
    public float cellSize = 1f;

    // 벽 프리팹의 Y 위치 보정값
    public float wallYOffset = 1f;

    // 바닥 프리팹의 Y 위치 보정값
    public float floorYOffset = 0f;

    [Header("Player Safe Area")]
    public float safeRadius = 5f;

    // 플레이어 시작 위치
    public Vector3 playerStartPosition = Vector3.zero;

    /// <summary>
    /// ChunkManager.cs가 호출하는 함수.
    /// chunkCoord : 생성할 청크의 좌표
    /// chunkSize : 청크 한 번의 타일 개수
    /// globalSeed : 전체 맵 기준 시드
    /// </summary>
    public GameObject GenerateChunk(Vector2Int chunkCoord, int chunkSize, int globalSeed)
    {
        // 1. 청크 부모 오브젝트 생성
        GameObject chunkObject = new GameObject();

        // 2. 청크의 월드 시작 위치 계산
        Vector3 chunkWorldPosition = new Vector3(
            chunkCoord.x * chunkSize * cellSize,
            0f,
            chunkCoord.y * chunkSize * cellSize
        );

        chunkObject.transform.position = chunkWorldPosition;

        // 3. 청크 별 고유 시드 생성
        int chunkSeed = GetChunkSeed(globalSeed, chunkCoord);

        // 4. 청크 내부 프리팹 배치
        GenerateObjects(chunkObject.transform, chunkSize, chunkSeed);

        // 5. 생성된 청크 오브젝트 반환
        return chunkObject;
    }

    /// <summary>
    /// 전체 시드와 청크 좌표를 섞어서 청크 전용 시드 만드는 함수
    /// </summary>
    /// <param name="globalSeed">전체 맵 기준 시드</param>
    /// <param name="chunkCoord">생성할 청크의 좌표</param>
    /// <returns></returns>
    private int GetChunkSeed(int globalSeed, Vector2Int chunkCoord)
    {
        // int 범위를 넘는 계산이 생겨도 
        // 오류를 내지 않고 자연스럽게 값이 순환 되도록 함.
        // 해시 계산 검색하다가 가져옴
        unchecked
        {
            int hash = globalSeed;

            // 서로 다른 큰 소수를 곱해서 좌표값 섞기
            hash = hash * 73856093 ^ chunkCoord.x * 19349663;
            hash = hash * 83492791 ^ chunkCoord.y * 297121507;

            return hash;
        }
    }

    /// <summary>
    /// 청크 내부에 실제로 타일을 배치하는 함수
    /// </summary>
    /// <param name="tilemap">타일 데이터를 저장하고 배치</param>
    /// <param name="chunkSize">청크 한 변의 타일 개수</param>
    /// <param name="chunkSeed">청크별 고유 시드</param>
    private void GenerateObjects(Transform chunkTransform, int chunkSize, int chunkSeed)
    {
        // 같은 seed를 넣으면 항상 같은 랜덤 순서가 나옴.
        System.Random random = new System.Random(chunkSeed);

        // 청크 내부 좌표 순회
        for(int x = 0; x < chunkSize; x++)
        {
            for(int z = 0; z < chunkSize; z++)
            {
                // 현재 칸의 로컬 위치 계산
                // cellSize를 곱해서 실제 월드 간격으로 바꿈
                Vector3 localCellPosition = new Vector3(
                    x * cellSize,
                    0f,
                    z * cellSize
                );

                Vector3 worldCellPosition = chunkTransform.position + localCellPosition;

                // 1. 바닥은 모든 칸에 생성한다.
                // 그래야 플레이어가 이동할 지형이 되니까
                if(floorPrefab != null)
                {
                    Vector3 floorPosition = worldCellPosition;
                    floorPosition.y += floorYOffset;

                    GameObject floorObject = Instantiate(
                        floorPrefab,
                        floorPosition,
                        Quaternion.identity,
                        chunkTransform
                    );

                    floorObject.name = "Floor";
                }

                // 2. 현재 칸이 플레이어 시작 위치 주변인지 확인
                float distanceFromPlayerStart = Vector3.Distance(
                    new Vector3(worldCellPosition.x, 0f, worldCellPosition.z),
                    new Vector3(playerStartPosition.x, 0f, playerStartPosition.z)
                );

                // 3. 플레이어 시작 반경 안이면 벽 생성 금지
                if(distanceFromPlayerStart <= safeRadius)
                {
                    continue;
                }

                // 4. 랜덤 값으로 벽 생성 여부 결정
                int randomValue = random.Next(0, 100);

                // randomValue가 wallPercent보다 작으면 벽 생성
                if(randomValue < wallPercent)
                {
                    if(wallPrefab != null)
                    {
                        Vector3 wallPosition = worldCellPosition;
                        wallPosition.y += wallYOffset;

                        GameObject wallObject = Instantiate(
                            wallPrefab,
                            wallPosition,
                            Quaternion.identity,
                            chunkTransform
                        );

                        wallObject.name = "Wall";
                    }
                }
            }
        }
    }
}
