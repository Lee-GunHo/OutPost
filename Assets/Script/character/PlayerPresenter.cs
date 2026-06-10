using UnityEngine;

public class PlayerPresenter : MonoBehaviour
{
    private PlayerInputManager inputManager;
    private PlayerModel playerModel;
    private PlayerStateManager stateManager;
    private PlayerView playerView;
    private Rigidbody rigid;
    private EquipmentModel equipmentModel;
    private StatusEffectModel statusEffectModel;

    private Vector3 lastMoveDirection = Vector3.forward;

    public Vector2 MoveInput => inputManager.MoveInput;
    public bool IsDashPressed => inputManager.IsDashPressed;

    public float MoveSpeed => playerModel.MoveSpeed;
    public float DashSpeed => playerModel.DashSpeed;
    public float DashDuration => playerModel.DashDuration;

    public bool CanDash => playerModel.CanDash;

    public PlayerStateManager StateManager => stateManager;

    public bool IsInteractPressed => inputManager.IsInteractPressed;
    public float InteractionRange => playerModel.InteractionRange;

    public int TotalAttackPower => playerModel.AttackPower  ;
    public int TotalDefensePower => playerModel.DefensePower ;

    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
        playerModel = GetComponent<PlayerModel>();
        stateManager = GetComponent<PlayerStateManager>();
        playerView = GetComponent<PlayerView>();
        rigid = GetComponent<Rigidbody>();
        equipmentModel = GetComponent<EquipmentModel>();
        statusEffectModel = GetComponent<StatusEffectModel>();
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

    public void StartDashCooldown()
    {
        playerModel.StartDashCooldown();
    }

    public void PlayDashEffect(float duration)
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

    public void TryInteract()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, InteractionRange);

        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact(this);
                return;
            }
        }

        Debug.Log("상호작용 가능한 대상이 없습니다.");
    }

    private void OnDrawGizmosSelected()
    {
        PlayerModel model = GetComponent<PlayerModel>();

        if (model == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, model.InteractionRange);
    }

    public void AddStatusEffect(StatusEffectData effectData)
    {
        statusEffectModel.AddEffect(effectData);
    }

    public void RemoveStatusEffect(StatusEffectType effectType)
    {
        statusEffectModel.RemoveEffect(effectType);
    }

    public bool HasStatusEffect(StatusEffectType effectType)
    {
        return statusEffectModel.HasEffect(effectType);
    }
}