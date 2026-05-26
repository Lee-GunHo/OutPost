using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    Rigidbody m_Rigidbody;
    Vector3 m_Movement;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        m_Movement = new Vector3(horizontal, 0, vertical).normalized;

        transform.position += m_Movement * moveSpeed * Time.deltaTime; ;
    }
}
