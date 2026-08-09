using UnityEngine;

public class BossModel : MonoBehaviour
{
    [Header("Health Data")]
    [SerializeField] private int maxHp = 500;

    [Header("AI Data")]
    [SerializeField] private float detectionRange = 18f;

    [Header("Reward Data")]
    [SerializeField] private int expReward = 300;

    private int currentHp;

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