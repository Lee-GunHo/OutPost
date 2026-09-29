using UnityEngine;

/// <summary>
/// GameObjectPool이 생성한 인스턴스에 자동으로 붙어 원본 프리팹의 InstanceID를 기억함.
/// Despawn 시 어느 풀로 되돌아가야 하는지 판단하는 용도로만 사용.
/// </summary>
public class PooledObject : MonoBehaviour
{
    public int SourcePrefabId { get; set; }
}
