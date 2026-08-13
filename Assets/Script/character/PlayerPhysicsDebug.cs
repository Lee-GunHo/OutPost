using UnityEngine;

public class PlayerPhysicsDebug : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        Debug.Log(
            $"[RB] " +
            $"UseGravity={rb.useGravity}, " +
            $"Kinematic={rb.isKinematic}, " +
            $"DetectCollisions={rb.detectCollisions}, " +
            $"Mode={rb.collisionDetectionMode}"
        );

        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);

        Debug.Log($"[PLAYER] 3D Collider 개수 = {colliders.Length}");

        foreach (Collider col in colliders)
        {
            Debug.Log(
                $"[COLLIDER] " +
                $"Name={col.gameObject.name}, " +
                $"Type={col.GetType().Name}, " +
                $"Enabled={col.enabled}, " +
                $"Trigger={col.isTrigger}, " +
                $"Layer={LayerMask.LayerToName(col.gameObject.layer)}, " +
                $"AttachedRB={(col.attachedRigidbody != null ? col.attachedRigidbody.gameObject.name : "NULL")}, " +
                $"Bounds={col.bounds}"
            );
        }
    }

    private void FixedUpdate()
    {
        Debug.Log(
            $"[PLAYER] PosY={transform.position.y:F3}, " +
            $"VelY={(rb != null ? rb.linearVelocity.y : 0f):F3}"
        );

        if (Physics.Raycast(
            transform.position,
            Vector3.down,
            out RaycastHit hit,
            10f))
        {
            Debug.Log(
                $"[DOWN RAY] {hit.collider.name}, " +
                $"Layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}, " +
                $"Distance={hit.distance:F3}, " +
                $"Trigger={hit.collider.isTrigger}"
            );
        }
        else
        {
            Debug.LogWarning("[DOWN RAY] 아래에 Collider 없음");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.LogError(
            $"[COLLISION ENTER] {collision.gameObject.name} / " +
            $"Layer={LayerMask.LayerToName(collision.gameObject.layer)}"
        );
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log(
            $"[COLLISION STAY] {collision.gameObject.name}"
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.LogWarning(
            $"[TRIGGER ENTER] {other.gameObject.name}"
        );
    }
}