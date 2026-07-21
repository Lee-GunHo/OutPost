using UnityEngine;

/// <summary>
/// 파괴 가능한 벽과 나무의 설정, 생성 좌표, 파괴 상태를 관리
/// 저장 매니저 호출이나 프리팹 생성 및 제거는 담당하지 않음.
/// </summary>
public class BreakableWallModel : MonoBehaviour
{
    [Header("Drop Setting")]
    [SerializeField] private GameObject dropItemPrefab;

    [Min(0)]
    [SerializeField] private int minDropCount = 1;

    [Min(0)]
    [SerializeField] private int maxDropCount = 3;

    [Min(0f)]
    [SerializeField] private float dropSpread = 0.3f;

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

    public GameObject DropItemPrefab => dropItemPrefab;
    public int MinDropCount => minDropCount;
    public int MaxDropCount => maxDropCount;
    public float DropSpread => dropSpread;
    public bool ShowSaveDebugLog => showSaveDebugLog;

    public bool HasGeneratedIdentity => hasGeneratedIdentity;
    public bool IsPlayerPlaced => isPlayerPlaced;
    public bool IsBroken => isBroken;

    public int WorldSeed => worldSeed;
    public Vector2Int ChunkCoord => chunkCoord;
    public Vector2Int LocalCellCoord => localCellCoord;
    public Vector2Int GlobalCellCoord => globalCellCoord;
    public string GeneratedObjectType => generatedObjectType;
    public GameObject GeneratedRootObject => generatedRootObject;

    /// <summary>
    /// 기존 BreakableWall 컴포넌트의 직렬화 데이터를 옮길 때 사용
    /// </summary>
    public void ConfigureDropSettings(
        GameObject newDropItemPrefab,
        int newMinDropCount,
        int newMaxDropCount,
        float newDropSpread,
        bool newShowSaveDebugLog)
    {
        dropItemPrefab = newDropItemPrefab;
        minDropCount = Mathf.Max(0, newMinDropCount);
        maxDropCount = Mathf.Max(minDropCount, newMaxDropCount);
        dropSpread = Mathf.Max(0f, newDropSpread);
        showSaveDebugLog = newShowSaveDebugLog;
    }

    public void InitializeGeneratedObject(
        int newWorldSeed,
        Vector2Int newChunkCoord,
        Vector2Int newLocalCellCoord,
        Vector2Int newGlobalCellCoord,
        string objectType,
        GameObject rootObject,
        bool playerPlaced)
    {
        worldSeed = newWorldSeed;
        chunkCoord = newChunkCoord;
        localCellCoord = newLocalCellCoord;
        globalCellCoord = newGlobalCellCoord;
        generatedObjectType = string.IsNullOrEmpty(objectType)
            ? ChunkModificationSaveManager.ObjectTypeWall
            : objectType;

        generatedRootObject = rootObject != null
            ? rootObject
            : gameObject;

        hasGeneratedIdentity = true;
        isPlayerPlaced = playerPlaced;
        isBroken = false;
    }

    public bool TryBeginBreak()
    {
        if (isBroken)
            return false;

        isBroken = true;
        return true;
    }

    private void OnValidate()
    {
        minDropCount = Mathf.Max(0, minDropCount);
        maxDropCount = Mathf.Max(minDropCount, maxDropCount);
        dropSpread = Mathf.Max(0f, dropSpread);
    }
}