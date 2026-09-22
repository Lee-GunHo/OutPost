using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Applies a navigation obstacle to the physical geometry created by a presenter/view.
/// Placement rules and saved block data remain in their existing models and presenters.
/// </summary>
[DisallowMultipleComponent]
public sealed class NavigationObstacleView : MonoBehaviour
{
    public static void ApplyTo(GameObject root)
    {
        NavigationObstacleView view = root.GetComponent<NavigationObstacleView>();
        if (view == null)
            view = root.AddComponent<NavigationObstacleView>();
        view.ConfigureFromColliders();
    }

    private void ConfigureFromColliders()
    {
        Physics.SyncTransforms();
        Bounds localBounds = default;
        bool found = false;

        foreach (Collider body in GetComponentsInChildren<Collider>(true))
        {
            if (!body.enabled || body.isTrigger || !body.gameObject.activeInHierarchy)
                continue;

            Bounds worldBounds = body.bounds;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = new Vector3(
                    x == 0 ? worldBounds.min.x : worldBounds.max.x,
                    y == 0 ? worldBounds.min.y : worldBounds.max.y,
                    z == 0 ? worldBounds.min.z : worldBounds.max.z);
                Vector3 localCorner = transform.InverseTransformPoint(corner);
                if (!found)
                    localBounds = new Bounds(localCorner, Vector3.zero);
                else
                    localBounds.Encapsulate(localCorner);
                found = true;
            }
        }

        if (!found)
            return;

        // Bake the underlying floor, then carve a temporary hole. Removing this
        // obstacle restores that floor without rebuilding the whole loaded map.
        NavMeshModifier modifier = GetComponent<NavMeshModifier>();
        if (modifier == null)
            modifier = gameObject.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
        modifier.applyToChildren = true;
        modifier.enabled = true;

        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = localBounds.center;
        obstacle.size = localBounds.size;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        obstacle.enabled = true;
    }
}
