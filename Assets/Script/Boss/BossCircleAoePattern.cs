using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossCircleAoeOriginType
{
    BossPosition = 0,
    RandomAroundTarget = 1,
    TargetPosition = 2
}

public class BossCircleAoePattern : BossPatternBase
{
    [Header("AOE Position Data")]
    [SerializeField] private BossCircleAoeOriginType originType = BossCircleAoeOriginType.BossPosition;
    [SerializeField] private float randomSpawnRadius = 4f;
    [SerializeField] private float minDistanceBetweenCircles = 1.5f;
    [SerializeField] private int randomPositionTryCount = 20;

    [Header("AOE Shape Data")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float hitHeight = 3f;
    [SerializeField] private int circlesPerWave = 1;
    [SerializeField] private int waveCount = 1;
    [SerializeField] private float intervalBetweenWaves = 0.4f;

    [Header("Timing Data")]
    [SerializeField] private float warningDelay = 0.8f;
    [SerializeField] private float afterAttackDelay = 0.2f;

    [Header("Damage Data")]
    [SerializeField] private bool applyDamage = true;
    [SerializeField] private int damage = 20;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Status Effect Data")]
    [SerializeField] private bool applyStatusEffect = false;
    [SerializeField] private StatusEffectType statusEffectType = StatusEffectType.None;
    [SerializeField] private float statusEffectDuration = 5f;
    [SerializeField] private float statusEffectValue = 3f;
    [SerializeField] private float statusEffectTickInterval = 1f;
    [SerializeField] private int statusAttackModifier = 0;
    [SerializeField] private int statusDefenseModifier = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;
    [SerializeField] private bool useTargetFallbackCheck = true;

    protected override IEnumerator ExecutePattern(BossPresenter boss)
    {
        if (boss == null)
        {
            yield break;
        }

        boss.StopMove();
        boss.LookAtTarget();

        int safeWaveCount = Mathf.Max(1, waveCount);

        for (int waveIndex = 0; waveIndex < safeWaveCount; waveIndex++)
        {
            Vector3[] centers = CreateAoeCenters(boss);

            ShowWarnings(centers);

            yield return new WaitForSeconds(warningDelay);

            ApplyAoeEffects(boss, centers);

            if (waveIndex < safeWaveCount - 1)
            {
                yield return new WaitForSeconds(intervalBetweenWaves);
            }
        }

        yield return new WaitForSeconds(afterAttackDelay);
    }

    private Vector3[] CreateAoeCenters(BossPresenter boss)
    {
        int safeCircleCount = Mathf.Max(1, circlesPerWave);
        List<Vector3> centers = new List<Vector3>();

        for (int i = 0; i < safeCircleCount; i++)
        {
            Vector3 center = GetAoeCenter(boss, centers);
            centers.Add(center);
        }

        return centers.ToArray();
    }

    private Vector3 GetAoeCenter(BossPresenter boss, List<Vector3> existingCenters)
    {
        switch (originType)
        {
            case BossCircleAoeOriginType.TargetPosition:
                return GetTargetPosition(boss);

            case BossCircleAoeOriginType.RandomAroundTarget:
                return GetRandomPositionAroundTarget(boss, existingCenters);

            case BossCircleAoeOriginType.BossPosition:
            default:
                return GetBossPosition(boss);
        }
    }

    private Vector3 GetBossPosition(BossPresenter boss)
    {
        Vector3 center = boss.transform.position;

        if (boss.Target != null)
        {
            center.y = boss.Target.position.y;
        }

        return center;
    }

    private Vector3 GetTargetPosition(BossPresenter boss)
    {
        if (boss.Target == null)
        {
            return GetBossPosition(boss);
        }

        return boss.Target.position;
    }

    private Vector3 GetRandomPositionAroundTarget(BossPresenter boss, List<Vector3> existingCenters)
    {
        if (boss.Target == null)
        {
            return GetBossPosition(boss);
        }

        Vector3 bestCenter = boss.Target.position;

        for (int tryIndex = 0; tryIndex < randomPositionTryCount; tryIndex++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * randomSpawnRadius;

            Vector3 center = boss.Target.position;
            center.x += randomCircle.x;
            center.z += randomCircle.y;

            if (IsFarEnoughFromOtherCenters(center, existingCenters))
            {
                return center;
            }

            bestCenter = center;
        }

        return bestCenter;
    }

    private bool IsFarEnoughFromOtherCenters(Vector3 center, List<Vector3> existingCenters)
    {
        foreach (Vector3 existingCenter in existingCenters)
        {
            Vector2 centerXZ = new Vector2(center.x, center.z);
            Vector2 existingXZ = new Vector2(existingCenter.x, existingCenter.z);

            float distance = Vector2.Distance(centerXZ, existingXZ);

            if (distance < minDistanceBetweenCircles)
            {
                return false;
            }
        }

        return true;
    }

    private void ShowWarnings(Vector3[] centers)
    {
        foreach (Vector3 center in centers)
        {
            BossAoeWarningView.Create(center, radius, warningDelay);
        }

        if (showDebugLog)
        {
            Debug.Log($"Boss AOE warnings created: {centers.Length}");
        }
    }

    private void ApplyAoeEffects(BossPresenter boss, Vector3[] centers)
    {
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        HashSet<PlayerPresenter> statusAppliedPlayers = new HashSet<PlayerPresenter>();

        foreach (Vector3 center in centers)
        {
            Collider[] colliders = GetCollidersInAoe(center);

            if (showDebugLog)
            {
                Debug.Log($"Boss AOE detected colliders: {colliders.Length}, center: {center}, radius: {radius}");
            }

            foreach (Collider collider in colliders)
            {
                ApplyEffectToCollider(
                    boss,
                    collider,
                    damagedTargets,
                    statusAppliedPlayers
                );
            }

            if (useTargetFallbackCheck)
            {
                ApplyEffectToTargetDirectly(
                    boss,
                    center,
                    damagedTargets,
                    statusAppliedPlayers
                );
            }
        }
    }

    private void ApplyEffectToTargetDirectly(
    BossPresenter boss,
    Vector3 center,
    HashSet<IDamageable> damagedTargets,
    HashSet<PlayerPresenter> statusAppliedPlayers
)
    {
        if (boss == null || boss.Target == null)
        {
            return;
        }

        Vector2 centerXZ = new Vector2(center.x, center.z);
        Vector2 targetXZ = new Vector2(boss.Target.position.x, boss.Target.position.z);

        float distance = Vector2.Distance(centerXZ, targetXZ);

        if (distance > radius)
        {
            if (showDebugLog)
            {
                Debug.Log($"Boss AOE target fallback missed. Distance: {distance}, Radius: {radius}");
            }

            return;
        }

        if (applyDamage)
        {
            IDamageable damageable = boss.Target.GetComponentInParent<IDamageable>();

            if (damageable != null && !damagedTargets.Contains(damageable))
            {
                damageable.TakeDamage(damage);
                damagedTargets.Add(damageable);

                if (showDebugLog)
                {
                    Debug.Log("Boss AOE fallback damage applied: " + damage);
                }
            }
            else if (showDebugLog && damageable == null)
            {
                Debug.LogWarning("Boss AOE fallback could not find IDamageable on target.");
            }
        }

        if (applyStatusEffect)
        {
            PlayerPresenter player = boss.Target.GetComponentInParent<PlayerPresenter>();

            if (player != null && !statusAppliedPlayers.Contains(player))
            {
                ApplyStatusEffectToPlayer(player);
                statusAppliedPlayers.Add(player);

                if (showDebugLog)
                {
                    Debug.Log("Boss AOE fallback status applied: " + statusEffectType);
                }
            }
            else if (showDebugLog && player == null)
            {
                Debug.LogWarning("Boss AOE fallback could not find PlayerPresenter on target.");
            }
        }
    }

    private void ApplyStatusEffectToPlayer(PlayerPresenter player)
    {
        if (statusEffectType == StatusEffectType.None)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("Status effect type is None.");
            }

            return;
        }

        if (player == null)
        {
            return;
        }

        StatusEffectData effectData = new StatusEffectData(
            statusEffectType,
            statusEffectDuration,
            statusEffectValue,
            statusEffectTickInterval,
            statusAttackModifier,
            statusDefenseModifier
        );

        player.AddStatusEffect(effectData);
    }

    private Collider[] GetCollidersInAoe(Vector3 center)
    {
        Vector3 bottom = center + Vector3.down * 1f;
        Vector3 top = center + Vector3.up * hitHeight;

        if (targetLayer.value == 0)
        {
            Debug.LogWarning("Boss AOE Target Layer is empty. It will detect all layers for testing.");

            return Physics.OverlapCapsule(
                bottom,
                top,
                radius,
                ~0,
                queryTriggerInteraction
            );
        }

        return Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            targetLayer,
            queryTriggerInteraction
        );
    }

    private void ApplyEffectToCollider(
        BossPresenter boss,
        Collider collider,
        HashSet<IDamageable> damagedTargets,
        HashSet<PlayerPresenter> statusAppliedPlayers
    )
    {
        if (collider == null)
        {
            return;
        }

        BossPresenter hitBoss = collider.GetComponentInParent<BossPresenter>();

        if (hitBoss == boss)
        {
            return;
        }

        if (showDebugLog)
        {
            Debug.Log("Boss AOE hit collider: " + collider.name);
        }

        if (applyDamage)
        {
            ApplyDamage(collider, damagedTargets);
        }

        if (applyStatusEffect)
        {
            ApplyStatusEffect(collider, statusAppliedPlayers);
        }
    }

    private void ApplyDamage(Collider collider, HashSet<IDamageable> damagedTargets)
    {
        IDamageable damageable = collider.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("IDamageable was not found on collider parent: " + collider.name);
            }

            return;
        }

        if (damagedTargets.Contains(damageable))
        {
            return;
        }

        damageable.TakeDamage(damage);
        damagedTargets.Add(damageable);

        if (showDebugLog)
        {
            Debug.Log("Boss AOE damage applied: " + damage);
        }
    }

    private void ApplyStatusEffect(Collider collider, HashSet<PlayerPresenter> statusAppliedPlayers)
    {
        PlayerPresenter player = collider.GetComponentInParent<PlayerPresenter>();

        if (player == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("PlayerPresenter was not found on collider parent: " + collider.name);
            }

            return;
        }

        if (statusAppliedPlayers.Contains(player))
        {
            return;
        }

        ApplyStatusEffectToPlayer(player);
        statusAppliedPlayers.Add(player);

        if (showDebugLog)
        {
            Debug.Log("Boss AOE status effect applied: " + statusEffectType);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (originType == BossCircleAoeOriginType.BossPosition)
        {
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * hitHeight);
    }
}