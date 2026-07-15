using UnityEngine;

/// <summary>
/// 절차적으로 생성된 벽과 나무의 공통 파괴 동작
/// 생성 시 전달받은 월드 타일 좌표를 그대로 저장 키로 사용
/// </summary>
public class BreakableWall : MonoBehaviour
{
    [Header("Drop Setting")]
    [Tooltip("파괴될 때 떨어질 아이템 프리팹")]
    public GameObject dropItemPrefab;

    [Tooltip("아이템 최소 드롭 개수")]
    public int minDropCount = 1;

    [Tooltip("아이템 최대 드롭 개수")]
    public int maxDropCount = 3;

    [Tooltip("아이템과 오브젝트 사이의 드롭 간격")]
    public float dropSpread = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showSaveDebugLog = true;

    private bool hasGeneratedIdentity;
    private bool isPlayerPlaced;
    private bool isBroken;

    private int worldSeed;
    private Vector2Int chunkCoord;
    private Vector2Int localCellCoord;
    private Vector2Int globalCellCoord;
    private string generatedObjectType = ChunkModificationSaveManager.ObjectTypeWall;
    private GameObject generatedRootObject;

    public void InitializeGeneratedWall(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedObject(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ChunkModificationSaveManager.ObjectTypeWall,
            generatedRootObject
        );
    }

    public void InitializeGeneratedTree(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedObject(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ChunkModificationSaveManager.ObjectTypeTree,
            generatedRootObject
        );
    }

    public void InitializePlacedWall(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        GameObject generatedRootObject = null)
    {
        this.worldSeed = worldSeed;
        this.chunkCoord = chunkCoord;
        this.localCellCoord = localCellCoord;
        this.globalCellCoord = globalCellCoord;
        generatedObjectType = ChunkModificationSaveManager.ObjectTypeWall;

        this.generatedRootObject = generatedRootObject != null
            ? generatedRootObject
            : gameObject;

        hasGeneratedIdentity = true;
        isPlayerPlaced = true;
    }

    // 이전 SeedMapGenerator와의 컴파일 호환을 위한 오버로드
    public void InitializeGeneratedWall(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedWall(
            worldSeed,
            chunkCoord,
            localCellCoord,
            localCellCoord,
            generatedRootObject
        );
    }

    public void InitializeGeneratedTree(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        GameObject generatedRootObject = null)
    {
        InitializeGeneratedTree(
            worldSeed,
            chunkCoord,
            localCellCoord,
            localCellCoord,
            generatedRootObject
        );
    }

    private void InitializeGeneratedObject(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        string objectType,
        GameObject generatedRootObject)
    {
        this.worldSeed = worldSeed;
        this.chunkCoord = chunkCoord;
        this.localCellCoord = localCellCoord;
        this.globalCellCoord = globalCellCoord;
        generatedObjectType = objectType;

        this.generatedRootObject = generatedRootObject != null
            ? generatedRootObject
            : gameObject;

        hasGeneratedIdentity = true;
        isPlayerPlaced = false;
    }

    public void Break()
    {
        if (isBroken)
            return;

        isBroken = true;

        if (!hasGeneratedIdentity)
        {
            Debug.LogWarning(
                $"{gameObject.name}: 생성 좌표가 전달되지 않아 파괴 상태를 저장할 수 없습니다."
            );
        }
        else if(isPlayerPlaced)
        {
            bool removed = PlacedBlockSaveManager
                .GetOrCreate()
                .RemovePlacedBlock(worldSeed, globalCellCoord);

            if(!removed)
            {
                Debug.LogWarning(
                    $"설치 블록 저장 기록을 찾지 못했습니다. " +
                    $"GlobalCell({globalCellCoord.x}, {globalCellCoord.y})"
                );
            }
        }
        else
        {
            ChunkModificationSaveManager saveManager =
                ChunkModificationSaveManager.GetOrCreate();

            saveManager.RegisterDestroyedObject(
                worldSeed,
                chunkCoord,
                localCellCoord,
                globalCellCoord,
                generatedObjectType
            );

            bool saveVerified = saveManager.IsObjectDestroyed(
                worldSeed,
                chunkCoord,
                localCellCoord,
                globalCellCoord,
                generatedObjectType
            );

            if (!saveVerified)
            {
                Debug.LogError(
                    $"[{generatedObjectType} 저장 검증 실패] " +
                    $"GlobalCell({globalCellCoord.x}, {globalCellCoord.y})"
                );
            }
            else if (showSaveDebugLog)
            {
                Debug.Log(
                    $"[{generatedObjectType} 저장 검증 완료] " +
                    $"Seed({worldSeed}) " +
                    $"GlobalCell({globalCellCoord.x}, {globalCellCoord.y}) " +
                    $"Chunk({chunkCoord.x}, {chunkCoord.y}) " +
                    $"Cell({localCellCoord.x}, {localCellCoord.y})"
                );
            }
        }

        SpawnDrops();

        GameObject destroyTarget = generatedRootObject != null
            ? generatedRootObject
            : gameObject;

        Destroy(destroyTarget);
    }

    private void SpawnDrops()
    {
        int minimum = Mathf.Max(0, minDropCount);
        int maximum = Mathf.Max(minimum, maxDropCount);
        int randomDropCount = Random.Range(minimum, maximum + 1);

        for (int i = 0; i < randomDropCount; i++)
        {
            if (dropItemPrefab == null)
                continue;

            Vector3 dropPosition = transform.position;
            dropPosition.x += Random.Range(-dropSpread, dropSpread);
            dropPosition.z += Random.Range(-dropSpread, dropSpread);
            dropPosition.y = 0.5f;

            Instantiate(dropItemPrefab, dropPosition, Quaternion.identity);
        }
    }
}