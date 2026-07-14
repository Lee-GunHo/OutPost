using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class RuntimeNavMeshBuilder : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private float buildDelay = 2f;

    private IEnumerator Start()
    {
        Debug.Log("RuntimeNavMeshBuilder Start 호출됨");

        yield return new WaitForSeconds(buildDelay);

        Build();
    }

    [ContextMenu("Build NavMesh Now")]
    public void Build()
    {
        ResolveNavMeshSurface();

        if (navMeshSurface == null)
        {
            Debug.LogWarning("NavMeshSurface가 연결되지 않았습니다. 현재 오브젝트: " + gameObject.name);
            return;
        }

        Debug.Log("NavMeshSurface 찾음: " + navMeshSurface.gameObject.name);

        navMeshSurface.BuildNavMesh();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        Debug.Log("런타임 NavMesh 빌드 완료");
        Debug.Log("현재 NavMesh 정점 수: " + triangulation.vertices.Length);
    }

    private void ResolveNavMeshSurface()
    {
        if (navMeshSurface != null)
        {
            return;
        }

        navMeshSurface = GetComponent<NavMeshSurface>();

        if (navMeshSurface != null)
        {
            return;
        }

        navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
    }
}