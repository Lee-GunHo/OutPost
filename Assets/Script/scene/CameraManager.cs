using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -7f);

    [Header("Camera Rotation")]
    [SerializeField] private Vector3 rotation = new Vector3(55f, 0f, 0f);

    [Header("Follow")]
    [SerializeField] private float followSpeed = 10f;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(rotation);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(rotation);
    }
}