using UnityEngine;

public enum MonsterAttackKind { Melee, Projectile, Circle }

// Attack configuration and cooldown state; selection/execution belong to the presenter.
public sealed class MonsterCombatModel : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private bool projectileEnabled = true;
    [SerializeField, Min(0.1f)] private float projectileRange = 6f;
    [SerializeField, Min(0f)] private float projectileMinRange = 1.8f;
    [SerializeField, Min(0f)] private float projectileWindup = 0.55f;
    [SerializeField, Min(0f)] private float projectileCooldown = 3.5f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 7f;
    [SerializeField, Min(0.01f)] private float projectileRadius = 0.15f;
    [SerializeField, Min(0f)] private float projectileDamageMultiplier = 0.8f;

    [Header("Circle")]
    [SerializeField] private bool circleEnabled = true;
    [SerializeField, Min(0.1f)] private float circleRadius = 2.5f;
    [SerializeField, Min(0f)] private float circleWindup = 0.8f;
    [SerializeField, Min(0f)] private float circleCooldown = 4f;
    [SerializeField, Min(0f)] private float circleDamageMultiplier = 1.2f;

    [Header("Selection weights / recovery")]
    [SerializeField, Min(0f)] private float meleeWeight = 3f;
    [SerializeField, Min(0f)] private float projectileWeight = 2f;
    [SerializeField, Min(0f)] private float circleWeight = 2f;
    [SerializeField, Min(0f)] private float recoveryDuration = 0.3f;

    private readonly float[] nextUse = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
    private float nextAnyAttack = float.NegativeInfinity;
    public float ProjectileRange => Mathf.Max(0.1f, projectileRange);
    public float ProjectileMinRange => Mathf.Clamp(projectileMinRange, 0f, ProjectileRange);
    public float ProjectileSpeed => Mathf.Max(0.1f, projectileSpeed);
    public float ProjectileRadius => Mathf.Max(0.01f, projectileRadius);
    public float CircleRadius => Mathf.Max(0.1f, circleRadius);
    public float RecoveryDuration => Mathf.Max(0f, recoveryDuration);

    public bool IsReady(MonsterAttackKind kind, float now)
    {
        int index = (int)kind;
        if (index < 0 || index >= nextUse.Length || now < nextAnyAttack || now < nextUse[index])
            return false;
        return kind == MonsterAttackKind.Melee ||
            (kind == MonsterAttackKind.Projectile ? projectileEnabled : circleEnabled);
    }

    public float GetWindup(MonsterAttackKind kind) => kind == MonsterAttackKind.Melee ? 0f
        : Mathf.Max(0f, kind == MonsterAttackKind.Projectile ? projectileWindup : circleWindup);
    public float GetWeight(MonsterAttackKind kind) => Mathf.Max(0f, kind == MonsterAttackKind.Melee
        ? meleeWeight : kind == MonsterAttackKind.Projectile ? projectileWeight : circleWeight);
    public float GetDamageMultiplier(MonsterAttackKind kind) => kind == MonsterAttackKind.Melee ? 1f
        : Mathf.Max(0f, kind == MonsterAttackKind.Projectile ? projectileDamageMultiplier : circleDamageMultiplier);

    public void Commit(MonsterAttackKind kind, float now, float baseCooldown)
    {
        float cooldown = kind == MonsterAttackKind.Melee ? baseCooldown
            : kind == MonsterAttackKind.Projectile ? projectileCooldown : circleCooldown;
        nextUse[(int)kind] = now + Mathf.Max(0f, cooldown);
        nextAnyAttack = now + GetWindup(kind) + RecoveryDuration + Mathf.Max(0f, baseCooldown);
    }
}
