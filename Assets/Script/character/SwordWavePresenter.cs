using System;
using UnityEngine;

[RequireComponent(typeof(SwordWaveView))]
public sealed class SwordWavePresenter : MonoBehaviour
{
    private SwordWaveModel model;
    private SwordWaveView view;
    private Transform owner;

    public static void Spawn(Vector3 origin, Vector3 direction, int damage, Transform owner, Camera camera)
    {
        var projectile = new GameObject("Player sword wave");
        projectile.transform.position = origin;
        var presenter = projectile.AddComponent<SwordWavePresenter>();
        presenter.owner = owner;
        presenter.model = new SwordWaveModel(origin, direction, damage, ScreenRange(camera, origin, direction));
        presenter.view = projectile.GetComponent<SwordWaveView>();
        presenter.view.Initialize(direction);
        SfxPlayer.PlayAt(SfxId.ProjectileFire, origin);
    }

    // World distance to the visible screen edge in the firing direction.
    private static float ScreenRange(Camera camera, Vector3 origin, Vector3 direction)
    {
        if (camera == null || !OnScreen(camera, origin)) return 20f;
        float low = 0f, high = 100f;
        for (int i = 0; i < 16; i++)
        {
            float mid = (low + high) * 0.5f;
            if (OnScreen(camera, origin + direction * mid)) low = mid;
            else high = mid;
        }
        return Mathf.Max(0.1f, low);
    }
    private static bool OnScreen(Camera camera, Vector3 position)
    {
        Vector3 point = camera.WorldToViewportPoint(position);
        return point.z > 0f && point.z < camera.farClipPlane &&
            point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
    }

    private void FixedUpdate()
    {
        if (model == null || model.Resolved) return;
        Collider[] overlaps = Physics.OverlapSphere(model.Position, model.Radius, ~0, QueryTriggerInteraction.Collide);
        // At spawn, geometry wins over a target sharing the starting volume.
        foreach (Collider collider in overlaps)
            if (IsObstacle(collider)) { Finish(collider); return; }
        foreach (Collider collider in overlaps)
            if (FindEnemy(collider) != null) { Finish(collider); return; }

        float distance = model.StepDistance(Time.fixedDeltaTime);
        RaycastHit[] hits = Physics.SphereCastAll(model.Position, model.Radius, model.Direction,
            distance, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (!IsObstacle(hit.collider) && FindEnemy(hit.collider) == null) continue;
            view.SetPosition(model.Position + model.Direction * hit.distance);
            Finish(hit.collider);
            return;
        }
        model.Advance(distance);
        view.SetPosition(model.Position);
        if (model.RemainingDistance <= 0f) Finish(null);
    }

    private bool IsOwner(Collider collider) => collider == null ||
        (owner != null && (collider.transform == owner || collider.transform.IsChildOf(owner)));

    private IDamageable FindEnemy(Collider collider)
    {
        if (IsOwner(collider)) return null;
        MonsterPresenter monster = collider.GetComponentInParent<MonsterPresenter>();
        if (monster != null && !monster.IsDead) return monster;
        BossPresenter boss = collider.GetComponentInParent<BossPresenter>();
        return boss != null && !boss.IsDead ? boss : null;
    }

    private bool IsObstacle(Collider collider)
    {
        if (IsOwner(collider) || FindEnemy(collider) != null) return false;
        if (collider.GetComponentInParent<BreakableWall>() != null ||
            collider.GetComponentInParent<NexusPresenter>() != null) return true;
        // Ignore detection/pickup triggers, but stop at all solid environment colliders.
        return !collider.isTrigger;
    }

    private void Finish(Collider collider)
    {
        model.Resolve(); // Resolve before damage callbacks: a projectile can hit only once.
        IDamageable enemy = collider != null ? FindEnemy(collider) : null;
        if (enemy != null)
        {
            enemy.TakeDamage(model.Damage);
            HitFeedback.PlayerStruckEnemy();
        }
        view.Remove(collider != null);
    }
}
