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

    [Header("Durability (코어키퍼식 채굴)")]
    [Tooltip("벽 내구도(HP). 곡괭이 채굴력만큼 깎이고 0이 되면 파괴")]
    [Min(1)]
    [SerializeField] private int maxDurability = 10;

    [Tooltip("단단함. MiningRules.CalculateDamage에서 채굴력과 비교 (0 = 제한 없음)")]
    [Min(0)]
    [SerializeField] private int hardness;

    [Header("Stage Settings")]
    [Tooltip("내구도를 몇 단계로 나눠 보여줄지. Up0/Down0, Up1/Down1 ... 형태로 단계가 나뉜 프리팹에서 사용")]
    [Min(1)]
    [SerializeField] private int stageCount = 1;

    private int currentStage;
    private int damageTaken;

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

    public int StageCount => Mathf.Max(1, stageCount);
    public int CurrentStage => currentStage;

    public int MaxDurability => Mathf.Max(1, maxDurability);
    public int Hardness => hardness;
    public int CurrentDurability => Mathf.Max(0, MaxDurability - damageTaken);

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
        currentStage = 0;
        damageTaken = 0;
    }

    /// <summary>
    /// 채굴 데미지를 적용. 내구도가 남아있으면 false(부분 파괴), 0이 되면 isBroken을 true로 만들고 true(완전 파괴)
    /// 이번 타격으로 넘어간 단계 번호(Up{n}/Down{n})는 [firstClearedStage, currentStage) 범위
    /// (데미지가 크면 한 번에 여러 단계가 지워질 수 있음)
    /// </summary>
    public bool ApplyDamage(int damage, out int firstClearedStage)
    {
        firstClearedStage = currentStage;

        if (isBroken || damage <= 0)
            return false;

        damageTaken = Mathf.Min(MaxDurability, damageTaken + damage);
        currentStage = MiningRules.GetClearedStageCount(damageTaken, MaxDurability, StageCount);

        if (damageTaken >= MaxDurability)
        {
            isBroken = true;
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        minDropCount = Mathf.Max(0, minDropCount);
        maxDropCount = Mathf.Max(minDropCount, maxDropCount);
        dropSpread = Mathf.Max(0f, dropSpread);
        stageCount = Mathf.Max(1, stageCount);
        maxDurability = Mathf.Max(1, maxDurability);
        hardness = Mathf.Max(0, hardness);
    }
}