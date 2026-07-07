using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPresenter : MonoBehaviour, IDamageable
{
    private PlayerInputManager inputManager;
    private PlayerModel playerModel;
    private PlayerStateManager stateManager;
    private PlayerView playerView;
    private Rigidbody rigid;
    private EquipmentModel equipmentModel;
    private StatusEffectModel statusEffectModel;


    private Vector3 lastMoveDirection = Vector3.forward;

    public Vector2 MoveInput => UIState.IsAnyUIOpen ? Vector2.zero : inputManager.MoveInput;
    public bool IsDashPressed => !UIState.IsAnyUIOpen && inputManager.IsDashPressed;
    public bool IsInteractPressed => !UIState.IsAnyUIOpen && inputManager.IsInteractPressed;
    public bool IsAttackPressed => !UIState.IsAnyUIOpen && inputManager.IsAttackPressed;

    public float MoveSpeed => playerModel.MoveSpeed;
    public float DashSpeed => playerModel.DashSpeed;
    public float DashDuration => playerModel.DashDuration;

    public bool CanDash => playerModel.CanDash;

    public PlayerStateManager StateManager => stateManager;

    public float InteractionRange => playerModel.InteractionRange;

    public int TotalAttackPower => playerModel.AttackPower;
    public int TotalDefensePower => playerModel.DefensePower;
    public float AttackDuration => playerModel.AttackDuration;
    public float HitDuration => playerModel.HitDuration;

    public Vector3 AttackBoxHalfSize => playerModel.AttackBoxHalfSize;
    public float AttackBoxDistance => playerModel.AttackBoxDistance;

    public bool IsPickaxeMode => playerModel.IsPickaxeMode;
    public bool IsWeaponMode => playerModel.IsWeaponMode;
    public ToolType CurrentToolType => playerModel.CurrentToolType;

    public bool IsDead => playerModel.IsDead;

    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private HotbarPresenter hotbarPresenter;
    private Camera mainCamera;

    // (경민) 0707 NPC 퀘스트 관련 InventoryModel 연결 추가
    [SerializeField] private InventoryModel inventoryModel;

    public InventoryModel PlayerInventory => inventoryModel;


    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
        playerModel = GetComponent<PlayerModel>();
        stateManager = GetComponent<PlayerStateManager>();
        playerView = GetComponent<PlayerView>();
        rigid = GetComponent<Rigidbody>();
        equipmentModel = GetComponent<EquipmentModel>();
        statusEffectModel = GetComponent<StatusEffectModel>();
        mainCamera = Camera.main;

        // (경민) 0707 NPC 퀘스트 관련 InventoryModel 연결 추가
        if(inventoryModel == null)
        {
            inventoryModel = GetComponent<InventoryModel>();
        }

        if(inventoryModel == null)
        {
            inventoryModel = GetComponentInChildren<InventoryModel>();
        }

        if(inventoryModel == null)
        {
#pragma warning disable CS0618
            inventoryModel = FindObjectOfType<InventoryModel>();
#pragma warning restore CS0618
        }
    }

    private void Update()
    {
        if (UIState.IsAnyUIOpen)
        {
            return;
        }

        if (inputManager.IsToggleToolPressed)
        {
            ToggleTool();
        }
    }

    private void RotateToDirection(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
    }

    public void Move()
    {
        if (UIState.IsAnyUIOpen)
        {
            Debug.Log("UI 열림 판정 때문에 이동 정지");
            StopMove();
            return;
        }

        Vector3 moveDirection = GetMoveDirection();

        if (moveDirection != Vector3.zero)
        {
            lastMoveDirection = moveDirection;
            RotateToDirection(moveDirection);
        }

        Vector3 moveVelocity = moveDirection * MoveSpeed;
        rigid.linearVelocity = moveVelocity;
    }

    public void DashMove(Vector3 dashDirection)
    {
        if (UIState.IsAnyUIOpen)
        {
            StopMove();
            return;
        }

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

    public void PlayDashEffect()
    {
        playerView.PlayDashEffect();
    }

    public void PlayHitEffect(float duration)
    {
        playerView.PlayHitBlinkEffect(duration);
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
        if (UIState.IsAnyUIOpen)
        {
            StopMove();
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, InteractionRange);

        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            IInteractable interactable = collider.GetComponentInParent<IInteractable>();

            if (interactable == null)
            {
                continue;
            }

            MonoBehaviour interactableObject = interactable as MonoBehaviour;

            if (interactableObject == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, interactableObject.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        if (closestInteractable != null)
        {
            closestInteractable.Interact(this);
            return;
        }

        Debug.Log("상호작용 가능한 대상이 없습니다.");
    }

    public void TakeDamage(int damage)
    {
        if (playerModel.IsDead)
        {
            return;
        }

        playerModel.TakeDamage(damage);

        Debug.Log("플레이어 피격, 현재 체력: " + playerModel.CurrentHp);

        if (playerModel.IsDead)
        {
            Debug.Log("플레이어 사망");
            stateManager.ChangeState(stateManager.DeadState);
            return;
        }

        stateManager.ChangeState(stateManager.HitState);
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

        Gizmos.color = Color.red;

        Vector3 attackDirection = transform.forward;
        Vector3 attackCenter = transform.position + attackDirection * model.AttackBoxDistance;

        Gizmos.matrix = Matrix4x4.TRS(
            attackCenter,
            Quaternion.LookRotation(attackDirection),
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, model.AttackBoxHalfSize * 2f);

        Gizmos.matrix = Matrix4x4.identity;
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

    private void ToggleTool()
    {
        playerModel.ToggleTool();
    }

    public void ExecuteAttackAction()
    {
        if (UIState.IsAnyUIOpen)
        {
            return;
        }

        ItemStack selectedItem = hotbarPresenter != null
            ? hotbarPresenter.GetSelectedItem()
            : null;

        ItemData item = selectedItem != null
            ? selectedItem.item
            : null;

        // 아직 아이템 시스템 테스트 전이면 item이 null이어도 테스트 공격 허용
        if (item == null)
        {
            Debug.Log("손에 든 아이템 없음 - 테스트용 기본 공격/채굴 실행");

            bool testHitTarget = Attack(null);

            if (!testHitTarget)
            {
                TryBreakWall(null);
            }

            return;
        }

        if (!IsUsableTool(item))
        {
            Debug.Log("공격/채굴 가능한 도구가 아님: " + item.itemName);
            return;
        }

        bool hitTarget = Attack(item);

        if (!hitTarget)
        {
            TryBreakWall(item);
        }
    }
    public void TryBreakWall(ItemData item)
    {
        if (UIState.IsAnyUIOpen)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                Debug.LogWarning("MainCamera를 찾지 못했습니다.");
                return;
            }
        }

        if (Mouse.current == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, wallLayer))
        {
            BreakableWall wall = hit.collider.GetComponent<BreakableWall>();

            if (wall == null)
            {
                wall = hit.collider.GetComponentInParent<BreakableWall>();
            }

            if (wall == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, wall.transform.position);

            if (distance <= playerModel.BreakRange)
            {
                wall.Break();
                Debug.Log("벽 부수기 성공");
            }
            else
            {
                Debug.Log("벽이 너무 멀다.");
            }
        }
    }

    private Vector3 GetMouseDirectionFromPlayer()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                return transform.forward;
            }
        }

        if (Mouse.current == null)
        {
            return transform.forward;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 mouseWorldPosition = ray.GetPoint(enter);

            Vector3 direction = mouseWorldPosition - transform.position;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                return direction.normalized;
            }
        }

        return transform.forward;
    }

    public bool Attack(ItemData item)
    {
        if (UIState.IsAnyUIOpen)
        {
            return false;
        }

        Vector3 attackDirection = GetMouseDirectionFromPlayer();

        if (attackDirection == Vector3.zero)
        {
            return false;
        }

        transform.rotation = Quaternion.LookRotation(attackDirection);

        Vector3 attackCenter = transform.position + attackDirection * AttackBoxDistance;

        Collider[] colliders = Physics.OverlapBox(
            attackCenter,
            AttackBoxHalfSize,
            Quaternion.LookRotation(attackDirection)
        );

        foreach (Collider collider in colliders)
        {
            if (collider.GetComponentInParent<PlayerPresenter>() != null)
            {
                continue;
            }

            IDamageable damageable = collider.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                continue;
            }

            damageable.TakeDamage(TotalAttackPower);
            Debug.Log("플레이어 공격 성공: " + collider.name);

            return true;
        }

        Debug.Log("공격 범위 안에 대상이 없습니다.");
        return false;
    }

    private bool IsUsableTool(ItemData item)
    {
        if (item == null)
            return false;

        return item.toolType == ToolType.Sword
            || item.toolType == ToolType.Axe
            || item.toolType == ToolType.Pickaxe;
    }
}