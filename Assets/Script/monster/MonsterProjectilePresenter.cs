using System;
using UnityEngine;

[RequireComponent(typeof(MonsterProjectileView))]
public sealed class MonsterProjectilePresenter : MonoBehaviour
{
    private MonsterProjectileModel model;
    private MonsterProjectileView view;
    private Transform owner;

    public static void Spawn(Vector3 origin, Vector3 direction, MonsterCombatModel settings,
        MonsterAttackPayload payload, Transform owner, Material material, float range)
    {
        GameObject projectile = new GameObject("Monster projectile");
        projectile.transform.position = origin;
        var presenter = projectile.AddComponent<MonsterProjectilePresenter>();
        presenter.owner = owner;
        presenter.model = new MonsterProjectileModel(origin, direction, settings.ProjectileSpeed,
            settings.ProjectileRadius, range, payload);
        presenter.view = projectile.GetComponent<MonsterProjectileView>();
        presenter.view.Initialize(settings.ProjectileRadius, material);
        SfxPlayer.PlayAt(SfxId.ProjectileFire, origin);
    }

    private void FixedUpdate()
    {
        if (model == null || model.Resolved) return;
        // Also check the starting volume: casts alone miss initial overlaps.
        Collider[] overlaps = Physics.OverlapSphere(model.Position, model.Radius, ~0, QueryTriggerInteraction.Collide);
        // Overlap results have no distance ordering. A wall must win over an
        // overlapping player so spawning inside a wall cannot deal damage through it.
        foreach (Collider collider in overlaps)
        {
            if (!MonsterCombatPresenter.IsWallCollider(collider) ||
                !MonsterCombatPresenter.BlocksAttack(collider, owner)) continue;
            Hit(collider);
            return;
        }
        foreach (Collider collider in overlaps)
        {
            if (!MonsterCombatPresenter.BlocksAttack(collider, owner)) continue;
            Hit(collider);
            return;
        }
        float distance = model.StepDistance(Time.fixedDeltaTime);
        RaycastHit[] hits = Physics.SphereCastAll(model.Position, model.Radius, model.Direction,
            distance, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (!MonsterCombatPresenter.BlocksAttack(hit.collider, owner)) continue;
            Hit(hit.collider);
            return;
        }
        model.Advance(distance);
        view.SetPosition(model.Position);
        if (model.RemainingDistance <= 0f) Finish();
    }

    private void Hit(Collider collider)
    {
        model.Resolve();
        Component target = MonsterCombatPresenter.IsWallCollider(collider)
            ? null : MonsterCombatPresenter.FindDamageTarget(collider);
        if (target != null) MonsterCombatPresenter.ApplyHit(target, model.Payload);
        model.Payload.Observation?.Complete(target == null ? "Blocked" : "OtherTarget");
        view.Remove(true);
    }

    private void Finish()
    {
        model.Resolve();
        model.Payload.Observation?.Complete("Miss");
        view.Remove();
    }

    private void OnDisable()
    {
        if (model != null && !model.Resolved)
        {
            model.Resolve();
            model.Payload.Observation?.Complete("Interrupted");
        }
    }
}
