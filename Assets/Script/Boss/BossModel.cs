using UnityEngine;

public class BossModel : MonoBehaviour
{
    [Header("Health Data")]
    [SerializeField] private int maxHp = 500;

    [Header("AI Data")]
    [SerializeField] private float detectionRange = 18f;

    [Header("Reward Data")]
    [SerializeField] private int expReward = 300;

    [Header("Item Drop")]
    [SerializeField] private GameObject dropItemPrefab;
    public GameObject DropItemPrefab => dropItemPrefab;

    private int currentHp;

    public bool HasSpawnIdentity { get; private set; }
    public int WorldSeed { get; private set; }
    public Vector2Int SpawnChunk { get; private set; }
    public Vector2Int SpawnLocalCell { get; private set; }
    public Vector2Int SpawnGlobalCell { get; private set; }

    public void SetSpawnIdentity(int seed, Vector2Int chunk, Vector2Int local, Vector2Int global)
    {
        HasSpawnIdentity = true;
        WorldSeed = seed;
        SpawnChunk = chunk;
        SpawnLocalCell = local;
        SpawnGlobalCell = global;
    }

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

    public float DetectionRange => detectionRange;
    public int ExpReward => expReward;

    public bool IsDead => currentHp <= 0;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage, 1);
        currentHp = Mathf.Clamp(currentHp - finalDamage, 0, maxHp);
    }
}
