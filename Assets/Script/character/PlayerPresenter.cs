using UnityEngine;

public class PlayerPresenter : MonoBehaviour
{
    private PlayerInputManager inputManager;
    private PlayerModel playerModel;
    private PlayerStateManager stateManager;
    private PlayerView playerView;
    private Rigidbody rigid;

    private Vector3 lastMoveDirection = Vector3.forward;

    public Vector2 MoveInput => inputManager.MoveInput;
    public bool IsDashPressed => inputManager.IsDashPressed;

    public float MoveSpeed => playerModel.MoveSpeed;
    public float DashSpeed => playerModel.DashSpeed;
    public float DashDuration => playerModel.DashDuration;

    public PlayerStateManager StateManager => stateManager;

    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
        playerModel = GetComponent<PlayerModel>();
        stateManager = GetComponent<PlayerStateManager>();
        playerView = GetComponent<PlayerView>();
        rigid = GetComponent<Rigidbody>();
    }

    public void Move()
    {
        Vector3 moveDirection = GetMoveDirection();

        if (moveDirection != Vector3.zero)
        {
            lastMoveDirection = moveDirection;
        }

        Vector3 moveVelocity = moveDirection * MoveSpeed;
        rigid.linearVelocity = moveVelocity;
    }

    public void DashMove(Vector3 dashDirection)
    {
        Vector3 dashVelocity = dashDirection.normalized * DashSpeed;
        rigid.linearVelocity = dashVelocity;
    }

    public void StopMove()
    {
        rigid.linearVelocity = Vector3.zero;
    }

    public Vector3 GetMoveDirection()
    {
        return new Vector3(MoveInput.x, 0f, MoveInput.y).normalized;
    }

    public Vector3 GetDashDirection()
    {
        Vector3 moveDirection = GetMoveDirection();

        if (moveDirection != Vector3.zero)
        {
            return moveDirection;
        }

        return lastMoveDirection;
    }

    public void PlayDashBlinkEffect(float duration)
    {
        playerView.PlayDashBlinkEffect(duration);
    }

    public void ReturnToIdleOrMove()
    {
        if (MoveInput != Vector2.zero)
        {
            stateManager.ChangeState(stateManager.MoveState);
        }
        else
        {
            stateManager.ChangeState(stateManager.IdleState);
        }
    }
}