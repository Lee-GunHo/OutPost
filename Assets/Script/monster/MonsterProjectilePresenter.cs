using System;
using UnityEngine;

[RequireComponent(typeof(MonsterProjectileView))]
public sealed class MonsterProjectilePresenter : MonoBehaviour
{
    private MonsterProjectileModel model;
    private MonsterProjectileView view;
    private Transform owner;

    public static void Spawn(Vector3 origin, Vector3 direction, MonsterCombatModel settings,
        MonsterAttackPayload payload, Transform owner, Material material)
    {
        GameObject projectile = new GameObject("Monster projectile");
        projectile.transform.position = origin;
        var presenter = projectile.AddComponent<MonsterProjectilePresenter>();
        presenter.owner = owner;
        presenter.model = new MonsterProjectileModel(origin, direction, settings.ProjectileSpeed,
            settings.ProjectileRadius, settings.ProjectileRange, payload);
        presenter.view = projectile.GetComponent<MonsterProjectileView>();
        presenter.view.Initialize(settings.ProjectileRadius, material);
    }

    private void FixedUpdate()
    {
        if (model == null || model.Resolved) return;
        // Also check the starting volume: casts alone miss initial overlaps.
        foreach (Collider collider in Physics.OverlapSphere(model.Position, model.Radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!MonsterCombatPresenter.BlocksAttack(collider, owner)) continue;
            Hit(collider);
            return;
        }
        float distance = model.StepDistance(Time.fixedDeltaTime);
        RaycastHit[] hits = Physics.SphereCastAll(model.Position, model.Radius, model.Direction,
            distance, ~0, QueryTriggerInteraction.Ignore);
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
        Component target = MonsterCombatPresenter.FindDamageTarget(collider);
        if (target != null) MonsterCombatPresenter.ApplyHit(target, model.Payload);
        view.Remove();
    }

    private void Finish() { model.Resolve(); view.Remove(); }
}
