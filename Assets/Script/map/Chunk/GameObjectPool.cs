using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 청크 생성/해제마다 반복되는 프리팹 Instantiate/Destroy를 대체하는 프리팹별 오브젝트 풀.
/// 청크를 벗어난 오브젝트는 파괴 대신 비활성화되어 이 풀에 보관되고,
/// 같은 프리팹이 다시 필요할 때 재사용됨.
///
/// Despawn에서는 절대 Transform.SetParent를 호출하지 않음.
/// 여러 오브젝트에 대해 SetParent를 반복 호출하는 것 자체가 비용이 커서
/// (계층 변경 콜백 + 로컬 좌표 재계산), 호출부(ChunkView)가 청크 루트에서
/// Transform.DetachChildren()로 한 번에 떼어낸 뒤 이미 부모 없는 오브젝트를 넘겨줌.
/// </summary>
public class GameObjectPool : MonoBehaviour
{
    private static GameObjectPool instance;

    // GameObject를 Dictionary 키로 쓰면 비교/해시 때마다 네이티브 objet가 살아있는지
    // 확인하는 오버헤드가 붙으므로, 미리 뽑아둔 InstanceID(int)를 키로 사용함.
    private readonly Dictionary<int, Stack<GameObject>> pools =
        new Dictionary<int, Stack<GameObject>>();

    public static GameObjectPool GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<GameObjectPool>();

        if (instance != null)
            return instance;

        GameObject poolObject = new GameObject("GameObjectPool");
        instance = poolObject.AddComponent<GameObjectPool>();
        DontDestroyOnLoad(poolObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
    {
        if (prefab == null)
            return null;

        int prefabId = prefab.GetInstanceID();

        if (pools.TryGetValue(prefabId, out Stack<GameObject> pool) && pool.Count > 0)
        {
            GameObject reused = pool.Pop();
            Transform reusedTransform = reused.transform;
            reusedTransform.SetParent(parent, false);
            reusedTransform.SetPositionAndRotation(position, rotation);
            reused.SetActive(true);
            return reused;
        }

        GameObject created = Instantiate(prefab, position, rotation, parent);
        PooledObject pooledObject = created.AddComponent<PooledObject>();
        pooledObject.SourcePrefabId = prefabId;
        return created;
    }

    /// <summary>
    /// GameObjectPool.Spawn으로 만들어지지 않은 오브젝트는 그냥 파괴함.
    /// 호출 전에 부모에서 이미 떼어져 있어야 함(SetParent를 여기서 하지 않음).
    /// </summary>
    public void Despawn(GameObject instance)
    {
        if (instance == null)
            return;

        PooledObject pooledObject = instance.GetComponent<PooledObject>();

        if (pooledObject == null)
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);

        int prefabId = pooledObject.SourcePrefabId;

        if (!pools.TryGetValue(prefabId, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            pools[prefabId] = pool;
        }

        pool.Push(instance);
    }
}
