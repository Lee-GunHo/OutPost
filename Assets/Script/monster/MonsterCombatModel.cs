using UnityEngine;

public enum MonsterAttackKind { Melee, Projectile, Circle, Charge }

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
    [SerializeField, Min(0.1f)] private float circleRadius = 3.75f;
    [SerializeField, Min(0f)] private float circleWindup = 0.8f;
    [SerializeField, Min(0f)] private float circleCooldown = 4f;
    [SerializeField, Min(0f)] private float circleDamageMultiplier = 1.2f;

    [Header("Charge")]
    [SerializeField] private bool chargeEnabled = true;
    [SerializeField, Min(0.1f)] private float chargeDistance = 3.2f;
    [SerializeField, Min(0f)] private float chargeMinRange = 1.8f;
    [SerializeField, Min(0f)] private float chargeWindup = 0.65f;
    [SerializeField, Min(0.1f)] private float chargeSpeed = 8f;
    [SerializeField, Min(0f)] private float chargeCooldown = 5f;
    [SerializeField, Min(0f)] private float chargeRecovery = 0.65f;
    [SerializeField, Min(0f)] private float chargeDamageMultiplier = 1.1f;

    [Header("Selection weights / recovery")]
    [SerializeField, Min(0f)] private float meleeWeight = 3f;
    [SerializeField, Min(0f)] private float projectileWeight = 2f;
    [SerializeField, Min(0f)] private float circleWeight = 2f;
    [SerializeField, Min(0f)] private float chargeWeight = 2f;
    [SerializeField, Min(0f)] private float recoveryDuration = 0.3f;

    private readonly float[] nextUse = { float.NegativeInfinity, float.NegativeInfinity,
        float.NegativeInfinity, float.NegativeInfinity };
    private float nextAnyAttack = float.NegativeInfinity;
    public float ProjectileRange => Mathf.Max(0.1f, projectileRange);
    public float ProjectileMinRange => Mathf.Clamp(projectileMinRange, 0f, ProjectileRange);
    public float ProjectileSpeed => Mathf.Max(0.1f, projectileSpeed);
    public float ProjectileRadius => Mathf.Max(0.01f, projectileRadius);
    public float CircleRadius => Mathf.Max(0.1f, circleRadius);
    public float RecoveryDuration => Mathf.Max(0f, recoveryDuration);
    public float ChargeDistance => Mathf.Max(0.1f, chargeDistance);
    public float ChargeMinRange => Mathf.Clamp(chargeMinRange, 0f, ChargeDistance);
    public float ChargeSpeed => Mathf.Max(0.1f, chargeSpeed);
    public bool IsCharging { get; private set; }
    public float ChargeRemainingDistance { get; private set; }

    public void BeginCharge()
    {
        ChargeRemainingDistance = ChargeDistance;
        IsCharging = true;
    }

    public void AdvanceCharge(float distance)
    {
        ChargeRemainingDistance = Mathf.Max(0f, ChargeRemainingDistance - Mathf.Max(0f, distance));
    }

    public void EndCharge()
    {
        IsCharging = false;
        ChargeRemainingDistance = 0f;
    }

    public bool IsReady(MonsterAttackKind kind, float now)
    {
        int index = (int)kind;
        if (index < 0 || index >= nextUse.Length || now < nextAnyAttack || now < nextUse[index])
            return false;
        return IsEnabled(kind);
    }

    public bool IsEnabled(MonsterAttackKind kind)
    {
        if ((int)kind < 0 || (int)kind >= nextUse.Length) return false;
        return kind == MonsterAttackKind.Melee ||
            (kind == MonsterAttackKind.Projectile ? projectileEnabled
            : kind == MonsterAttackKind.Circle ? circleEnabled : chargeEnabled);
    }

    public float GetWindup(MonsterAttackKind kind) => kind == MonsterAttackKind.Melee ? 0f
        : Mathf.Max(0f, kind == MonsterAttackKind.Projectile ? projectileWindup
            : kind == MonsterAttackKind.Circle ? circleWindup : chargeWindup);
    public float GetWeight(MonsterAttackKind kind) => Mathf.Max(0f, kind == MonsterAttackKind.Melee
        ? meleeWeight : kind == MonsterAttackKind.Projectile ? projectileWeight
            : kind == MonsterAttackKind.Circle ? circleWeight : chargeWeight);
    public float GetDamageMultiplier(MonsterAttackKind kind) => kind == MonsterAttackKind.Melee ? 1f
        : Mathf.Max(0f, kind == MonsterAttackKind.Projectile ? projectileDamageMultiplier
            : kind == MonsterAttackKind.Circle ? circleDamageMultiplier : chargeDamageMultiplier);

    public float GetRecovery(MonsterAttackKind kind) => kind == MonsterAttackKind.Charge
        ? Mathf.Max(0f, chargeRecovery) : RecoveryDuration;

    public void Commit(MonsterAttackKind kind, float now, float baseCooldown)
    {
        float cooldown = kind == MonsterAttackKind.Melee ? baseCooldown
            : kind == MonsterAttackKind.Projectile ? projectileCooldown
            : kind == MonsterAttackKind.Circle ? circleCooldown : chargeCooldown;
        nextUse[(int)kind] = now + Mathf.Max(0f, cooldown);
        float activeDuration = kind == MonsterAttackKind.Charge ? ChargeDistance / ChargeSpeed : 0f;
        nextAnyAttack = now + GetWindup(kind) + activeDuration + GetRecovery(kind) + Mathf.Max(0f, baseCooldown);
    }
}
