using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Header("Move Data")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash Data")]
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 0.35f;

    public float MoveSpeed => moveSpeed;
    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
}