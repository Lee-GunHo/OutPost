using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPresenter : MonoBehaviour, IDamageable
{
    private PlayerInputManager inputManager;
    private PlayerModel playerModel;
    private PlayerStateManager stateManager;
    private PlayerView playerView;
    private PlayerLevelModel levelModel;
    private Rigidbody rigid;
    private EquipmentModel equipmentModel;
    private StatusEffectModel statusEffectModel;

    [Header("Mouse Action Ray")]
    [SerializeField] private float mouseActionRayRadius = 0.25f;
    [SerializeField] private float mouseActionHeightOffset = 0.8f;

    [Header("Invincible Data")]
    [SerializeField] private float reviveInvincibleDuration = 2f;

    private bool isDashInvincible;
    private bool isReviveInvincible;
    private Coroutine reviveInvincibleCoroutine;

    public bool IsDamageInvincible => isDashInvincible || isReviveInvincible;
    public bool IsStatusDamageInvincible => isReviveInvincible;

    public event Action OnPlayerStatusChanged;
    public event Action<string, Vector3> OnPlayerDied;
    public event Action<string> OnPlayerUpgraded;

    private string pendingDamageSource = "Unknown";

    private Vector3 lastMoveDirection = Vector3.forward;
    private int actionFacingFrame = -1;

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

    public int AttackPower => playerModel.AttackPower;
    public int DefensePower => playerModel.DefensePower;
    public float AttackDuration => playerModel.AttackDuration;
    public float WallBreakDuration => playerModel.WallBreakDuration;
    public float HitDuration => playerModel.HitDuration;

    public Vector3 AttackBoxHalfSize => playerModel.AttackBoxHalfSize;
    public float AttackBoxDistance => playerModel.AttackBoxDistance;

    public bool IsPickaxeMode => playerModel.IsPickaxeMode;
    public bool IsWeaponMode => playerModel.IsWeaponMode;
    public ToolType CurrentToolType => playerModel.CurrentToolType;

    public bool IsDead => playerModel.IsDead;

    public int MaxHp => playerModel.MaxHp;
    public int CurrentHp => playerModel.CurrentHp;

    public int MaxMp => playerModel.MaxMp;
    public int CurrentMp => playerModel.CurrentMp;

    public int CurrentExp => levelModel != null ? levelModel.CurrentExp : 0;
    public int RequiredExp => levelModel != null ? levelModel.RequiredExp : 0;
    public int Level => levelModel != null ? levelModel.Level : 1;
    public int StatPoint => levelModel != null ? levelModel.StatPoint : 0;

    public int HpUpgradeLevel => playerModel.HpUpgradeLevel;
    public int MpUpgradeLevel => playerModel.MpUpgradeLevel;
    public int AttackUpgradeLevel => playerModel.AttackUpgradeLevel;
    public int DefenseUpgradeLevel => playerModel.DefenseUpgradeLevel;
    public int MoveSpeedUpgradeLevel => playerModel.MoveSpeedUpgradeLevel;

    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private HotbarPresenter hotbarPresenter;
    private Camera mainCamera;

    [Header("Save Position Safety")]
    [Tooltip("청크 생성 상태를 확인하기 위한 참조. 비워두면 자동으로 찾음")]
    [SerializeField] private ChunkManager chunkManager;

    [Tooltip("저장 위치의 X/Z에서 바닥을 찾기 위해 위쪽에서 시작할 높이")]
    [SerializeField] private float loadGroundProbeHeight = 50f;

    [Tooltip("저장 위치에서 아래 방향으로 바닥을 찾는 최대 거리")]
    [SerializeField] private float loadGroundProbeDistance = 200f;

    [Tooltip("바닥과 플레이어 발 사이에 둘 여유 높이")]
    [SerializeField] private float loadGroundPadding = 0.05f;

    [Tooltip("ChunkManager 참조를 찾지 못했을 때만 쓰는 예비용 대기 프레임 수. " +
        "평소에는 ChunkManager.EnsureChunkLoaded를 직접 호출해 즉시 청크를 만들기 때문에 사용되지 않음")]
    [Min(1)]
    [SerializeField] private int loadGroundWaitFrames = 30;

    private Vector3 initialSpawnPosition;
    private Coroutine restorePositionCoroutine;
    private bool restorePreviousUseGravity;

    // (경민) 0707 NPC 퀘스트 관련 InventoryModel 연결 추가
    [SerializeField] private InventoryModel inventoryModel;

    public InventoryModel PlayerInventory => inventoryModel;

    // (경민) 0715 블럭 설치 범위 관련 프로퍼티 추가
    public float BreakRange => playerModel.BreakRange;




    [ContextMenu("Test Use MP")]
    private void TestUseMp()
    {
        UseMp(20);
    }

    [ContextMenu("Test Recover MP")]
    private void TestRecoverMp()
    {
        RecoverMp(20);
    }

    [ContextMenu("Test Level UP")]
    private void TestAddExp()
    {
        AddExp(2000);
    }


    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
        playerModel = GetComponent<PlayerModel>();
        stateManager = GetComponent<PlayerStateManager>();
        playerView = GetComponent<PlayerView>();
        levelModel = GetComponent<PlayerLevelModel>();
        rigid = GetComponent<Rigidbody>();
        equipmentModel = GetComponent<EquipmentModel>();
        statusEffectModel = GetComponent<StatusEffectModel>();
        mainCamera = Camera.main;
        initialSpawnPosition = playerModel.SpawnPosition;
        SetPhysicsPosition(initialSpawnPosition);

        // (경민) 0707 NPC 퀘스트 관련 InventoryModel 연결 추가
        if (inventoryModel == null)
        {
            inventoryModel = GetComponent<InventoryModel>();
        }

        if (inventoryModel == null)
        {
            inventoryModel = GetComponentInChildren<InventoryModel>();
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

    private void LateUpdate()
    {
        if (playerView == null)
            return;

        Vector3 velocity = rigid != null ? rigid.linearVelocity : Vector3.zero;
        velocity.y = 0f;
        playerView.SetWalking(!IsDead && !UIState.IsAnyUIOpen &&
            stateManager.IsMoving && MoveInput.sqrMagnitude > 0f && velocity.sqrMagnitude > 0.01f);
    }

    public void NotifyStatusChanged()
    {
        OnPlayerStatusChanged?.Invoke();
    }

    // 몬스터/보스 공격 코드가 TakeDamage 직전에 호출해 사망 원인을 남길 수 있게 함.
    public void SetPendingDamageSource(string sourceName)
    {
        pendingDamageSource = string.IsNullOrEmpty(sourceName) ? "Unknown" : sourceName;
    }

    private void RotateToDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        // Keep the interpolated physics body and visible transform in agreement.
        if (rigid != null)
            rigid.rotation = rotation;
        transform.rotation = rotation;
    }

    public void FaceActionTarget(Vector3 worldPosition)
    {
        if (UIState.IsAnyUIOpen || IsDead)
            return;

        RotateToDirection(worldPosition - transform.position);
        actionFacingFrame = Time.frameCount;
    }

    public void FaceMouseAction()
    {
        FaceActionTarget(transform.position + GetMouseDirectionFromPlayer());
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
            if (actionFacingFrame != Time.frameCount)
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
        if (playerView != null)
            playerView.SetWalking(false);
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
            Debug.Log("다른 UI가 열려 있어서 상호작용할 수 없습니다.");
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

        if (IsDamageInvincible)
        {
            Debug.Log("무적 상태라 데미지를 받지 않았습니다.");
            return;
        }

        playerModel.TakeDamage(damage);
        NotifyStatusChanged();

        Debug.Log("플레이어 피격, 현재 체력: " + playerModel.CurrentHp);

        if (playerModel.IsDead)
        {
            Debug.Log("플레이어 사망");
            OnPlayerDied?.Invoke(pendingDamageSource, transform.position);
            pendingDamageSource = "Unknown";
            stateManager.ChangeState(stateManager.DeadState);
            return;
        }

        stateManager.ChangeState(stateManager.HitState);
    }

    public void ReviveAtNexusFront()
    {
        Vector3 revivePosition = playerModel.SpawnPosition;

        int reviveHp = Mathf.Max(1, MaxHp / 2);
        int reviveMp = Mathf.Max(0, MaxMp / 2);

        // 체력 회복 전에 먼저 부활 무적을 켠다.
        StartReviveInvincible();

        playerModel.LoadSavedHealth(reviveHp);
        playerModel.LoadSavedMp(reviveMp);

        SetPhysicsPosition(revivePosition);

        NotifyStatusChanged();

        Debug.Log(
            $"플레이어 부활 완료. 위치: {revivePosition}, " +
            $"HP: {CurrentHp}/{MaxHp}, MP: {CurrentMp}/{MaxMp}"
        );

        stateManager.ChangeState(stateManager.IdleState);
    }

    public void SetDashInvincible(bool value)
    {
        isDashInvincible = value;

        Debug.Log("대시 무적 상태: " + isDashInvincible);
    }

    public void StartReviveInvincible()
    {
        if (reviveInvincibleCoroutine != null)
        {
            StopCoroutine(reviveInvincibleCoroutine);
        }

        isReviveInvincible = true;
        reviveInvincibleCoroutine = StartCoroutine(ReviveInvincibleRoutine());
    }

    private IEnumerator ReviveInvincibleRoutine()
    {
        Debug.Log("부활 무적 시작");

        yield return new WaitForSeconds(reviveInvincibleDuration);

        isReviveInvincible = false;
        reviveInvincibleCoroutine = null;

        Debug.Log("부활 무적 종료");
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
        // Invincibility blocks new effects; existing damage-over-time has its own rule.
        if (IsDamageInvincible)
        {
            return;
        }

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

    public float ExecuteAttackAction()
    {
        if (UIState.IsAnyUIOpen)
        {
            return 0f;
        }

        ItemStack selectedItem = hotbarPresenter != null
            ? hotbarPresenter.GetSelectedItem()
            : null;

        ItemData item = selectedItem != null
            ? selectedItem.item
            : null;

        return ExecuteMousePriorityAction(item);
    }

    private float GetAttackRecoveryDuration(ItemData item)
    {
        if (item != null && item.itemType == ItemType.Weapon && item.attackRecoveryDuration > 0f)
            return item.attackRecoveryDuration;

        return Mathf.Max(0f, AttackDuration);
    }

    private float ExecuteMousePriorityAction(ItemData item)
    {
        float attackRecoveryDuration = GetAttackRecoveryDuration(item);

        if (!TryGetMouseWorldPosition(out Vector3 mouseWorldPosition))
        {
            Attack(item);
            return attackRecoveryDuration;
        }

        Vector3 direction = mouseWorldPosition - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
        {
            Attack(item);
            return attackRecoveryDuration;
        }

        direction.Normalize();

        FaceActionTarget(mouseWorldPosition);

        float attackReach = AttackBoxDistance + Mathf.Max(AttackBoxHalfSize.x, AttackBoxHalfSize.z);
        float actionRange = Mathf.Max(attackReach, BreakRange);

        Vector3 rayOrigin = transform.position + Vector3.up * mouseActionHeightOffset;

        RaycastHit[] hits = Physics.SphereCastAll(
            rayOrigin,
            mouseActionRayRadius,
            direction,
            actionRange,
            ~0,
            QueryTriggerInteraction.Collide
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.GetComponentInParent<PlayerPresenter>() != null)
            {
                continue;
            }

            // The nexus blocks this attack without taking friendly damage.
            if (IsNexusTarget(hit.collider))
            {
                return attackRecoveryDuration;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                int finalDamage = GetAttackDamage(item);
                damageable.TakeDamage(finalDamage);

                Debug.Log("마우스 방향 선 판정: 적 공격 / 데미지 " + finalDamage);
                return attackRecoveryDuration;
            }

            BreakableWall wall = hit.collider.GetComponent<BreakableWall>();

            if (wall == null)
            {
                wall = hit.collider.GetComponentInParent<BreakableWall>();
            }

            if (wall != null)
            {
                float distanceToWall = Vector3.Distance(transform.position, wall.transform.position);

                if (distanceToWall <= BreakRange)
                {
                    wall.Break();
                    Debug.Log("마우스 방향 선 판정: 벽 파괴");
                    return Mathf.Max(0f, WallBreakDuration);
                }

                Debug.Log("벽이 너무 멀다.");
                return Mathf.Max(0f, WallBreakDuration);
            }
        }

        Attack(item);
        return attackRecoveryDuration;
    }

    private bool TryGetMouseWorldPosition(out Vector3 mouseWorldPosition)
    {
        mouseWorldPosition = Vector3.zero;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                return false;
            }
        }

        if (Mouse.current == null)
        {
            return false;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (!groundPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        mouseWorldPosition = ray.GetPoint(enter);
        return true;
    }

    private int GetAttackDamage(ItemData item)
    {
        int finalDamage = AttackPower;

        if (item == null)
        {
            return Mathf.Max(1, finalDamage);
        }

        // 나중에 ItemData에 공격력 필드가 있으면 여기서 더하면 됨.
        // 예시:
        // finalDamage += item.attackDamage;
        // finalDamage += item.weaponDamage;

        return Mathf.Max(1, finalDamage);
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
                FaceActionTarget(hit.point);
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

    public bool Attack(ItemData item = null)
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

        FaceActionTarget(transform.position + attackDirection);

        Vector3 attackCenter = transform.position + attackDirection * AttackBoxDistance;

        Collider[] colliders = Physics.OverlapBox(
            attackCenter,
            AttackBoxHalfSize,
            Quaternion.LookRotation(attackDirection)
        );

        foreach (Collider collider in colliders)
        {
            if (collider.GetComponentInParent<PlayerPresenter>() != null || IsNexusTarget(collider))
            {
                continue;
            }

            IDamageable damageable = collider.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                continue;
            }

            int finalDamage = GetAttackDamage(item);
            damageable.TakeDamage(finalDamage);

            return true;
        }

        Debug.Log("공격 범위 안에 대상이 없습니다.");
        return false;
    }

    private static bool IsNexusTarget(Collider collider)
    {
        // Colliders on child objects inherit protection from a tagged parent.
        for (Transform current = collider.transform; current != null; current = current.parent)
        {
            if (current.CompareTag("Nexus"))
            {
                return true;
            }
        }

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

    public void AddExp(int amount)
    {
        if (levelModel == null)
        {
            Debug.LogWarning("PlayerLevelModel이 없습니다.");
            return;
        }

        levelModel.AddExp(amount);
        NotifyStatusChanged();
    }

    public void TakeStatusDamage(int damage)
    {
        if (playerModel.IsDead)
        {
            return;
        }

        if (IsStatusDamageInvincible)
        {
            Debug.Log("무적 상태라 상태효과 데미지를 받지 않았습니다.");
            return;
        }

        playerModel.TakeStatusDamage(damage);
        NotifyStatusChanged();

        Debug.Log("상태효과 데미지, 현재 체력: " + playerModel.CurrentHp);

        if (playerModel.IsDead)
        {
            Debug.Log("플레이어 사망");
            OnPlayerDied?.Invoke("StatusEffect", transform.position);
            stateManager.ChangeState(stateManager.DeadState);
        }
    }

    public void AddStatusStats(int attackModifier, int defenseModifier)
    {
        playerModel.AddStatusStats(attackModifier, defenseModifier);
    }

    public void RemoveStatusStats(int attackModifier, int defenseModifier)
    {
        playerModel.RemoveStatusStats(attackModifier, defenseModifier);
    }

    public void LoadPlayerData(PlayerSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        StopMove();

        // 스탯 데이터는 즉시 복구한다.
        playerModel.LoadStatUpgradeLevels(
            saveData.hpUpgradeLevel,
            saveData.mpUpgradeLevel,
            saveData.attackUpgradeLevel,
            saveData.defenseUpgradeLevel,
            saveData.moveSpeedUpgradeLevel
        );

        playerModel.LoadSavedHealth(saveData.currentHp);
        playerModel.LoadSavedMp(saveData.currentMp);

        if (levelModel != null)
        {
            levelModel.LoadSavedLevelData(
                saveData.level,
                saveData.currentExp,
                saveData.statPoint
            );
        }

        NotifyStatusChanged();

        // 저장된 Y를 그대로 적용하지 않고, 해당 X/Z의 실제 바닥을 찾아 위치를 복구한다.
        if (restorePositionCoroutine != null)
        {
            StopCoroutine(restorePositionCoroutine);

            if (rigid != null)
            {
                rigid.useGravity = restorePreviousUseGravity;
            }
        }

        restorePositionCoroutine =
            StartCoroutine(RestorePlayerPositionSafely(saveData.playerPosition));
    }

    private IEnumerator RestorePlayerPositionSafely(Vector3 savedPosition)
    {
        // 우선 저장된 X/Z 위의 높은 위치로 옮긴다.
        // 아래에서 ChunkManager.EnsureChunkLoaded를 직접 호출해 그 자리의 청크를 즉시 생성한다.
        float temporaryY = Mathf.Max(
            initialSpawnPosition.y,
            savedPosition.y,
            0f
        ) + loadGroundProbeHeight;

        Vector3 temporaryPosition = new Vector3(
            savedPosition.x,
            temporaryY,
            savedPosition.z
        );

        if (rigid != null)
        {
            restorePreviousUseGravity = rigid.useGravity;
            rigid.useGravity = false;
        }

        SetPhysicsPosition(temporaryPosition);

        // ChunkPresenter의 매 프레임 감지(Update)를 기다리는 대신, 청크 생성을
        // 여기서 직접(동기적으로) 요청해서 프레임 타이밍에 의존하지 않게 함.
        if (chunkManager == null)
            chunkManager = FindFirstObjectByType<ChunkManager>();

        if (chunkManager != null)
        {
            chunkManager.EnsureChunkLoaded(temporaryPosition);
        }
        else
        {
            // 참조를 못 찾은 경우에만 예전처럼 프레임 수 기반으로 최대한 기다림.
            int fallbackWaitFrames = Mathf.Max(1, loadGroundWaitFrames);

            for (int i = 0; i < fallbackWaitFrames; i++)
                yield return null;
        }

        Physics.SyncTransforms();

        if (TryFindGroundBelow(temporaryPosition, out RaycastHit groundHit))
        {
            float feetOffset = GetPlayerFeetOffset();

            Vector3 safePosition = new Vector3(
                savedPosition.x,
                groundHit.point.y + feetOffset + loadGroundPadding,
                savedPosition.z
            );

            SetPhysicsPosition(safePosition);

            Debug.Log(
                $"플레이어 위치 안전 복구 완료. " +
                $"저장 위치={savedPosition}, 복구 위치={safePosition}, " +
                $"바닥={groundHit.collider.name}"
            );
        }
        else
        {
            // 저장 위치에 바닥을 찾지 못하면 씬에 배치된 최초 시작 위치로 되돌린다.
            SetPhysicsPosition(initialSpawnPosition);

            Debug.LogWarning(
                $"저장 위치 X/Z({savedPosition.x:F2}, {savedPosition.z:F2})에서 " +
                "바닥을 찾지 못해 초기 시작 위치로 복구했습니다."
            );
        }

        if (rigid != null)
        {
            rigid.useGravity = restorePreviousUseGravity;
            rigid.linearVelocity = Vector3.zero;
        }

        restorePositionCoroutine = null;
    }

    private bool TryFindGroundBelow(
        Vector3 probePosition,
        out RaycastHit selectedHit)
    {
        selectedHit = default;

        float distance = Mathf.Max(1f, loadGroundProbeDistance);

        RaycastHit[] hits = Physics.RaycastAll(
            probePosition,
            Vector3.down,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        bool foundGround = false;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            // 자기 자신의 몸 Collider는 바닥 후보에서 제외한다.
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            // Old saves may be on the nexus roof. Do not restore onto its top or
            // inside the new blocking collider; return to the default spawn.
            if (IsNexusTarget(hit.collider))
                return false;

            // 아래를 향한 Ray가 윗면을 맞은 경우만 바닥으로 취급한다.
            if (hit.normal.y <= 0.5f)
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            selectedHit = hit;
            foundGround = true;
        }

        return foundGround;
    }

    private float GetPlayerFeetOffset()
    {
        Physics.SyncTransforms();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        float lowestPoint = float.PositiveInfinity;
        bool foundBodyCollider = false;

        foreach (Collider col in colliders)
        {
            if (col == null || !col.enabled || col.isTrigger)
                continue;

            // Player Rigidbody에 속한 물리 몸체만 사용한다.
            if (rigid != null && col.attachedRigidbody != rigid)
                continue;

            lowestPoint = Mathf.Min(lowestPoint, col.bounds.min.y);
            foundBodyCollider = true;
        }

        if (!foundBodyCollider)
        {
            Debug.LogWarning(
                "플레이어의 비-Trigger 몸 Collider를 찾지 못했습니다. " +
                "기본 발 높이 0.5를 사용합니다."
            );
            return 0.5f;
        }

        return Mathf.Max(0f, transform.position.y - lowestPoint);
    }

    private void SetPhysicsPosition(Vector3 position)
    {
        if (rigid != null)
        {
            rigid.position = position;
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.position = position;
        }

        Physics.SyncTransforms();
    }

    public bool UseMp(int amount)
    {
        bool success = playerModel.UseMp(amount);

        if (success)
        {
            NotifyStatusChanged();
        }

        return success;
    }

    public void RecoverMp(int amount)
    {
        playerModel.RecoverMp(amount);
        NotifyStatusChanged();
    }

    public bool UpgradeHp()
    {
        if (levelModel == null)
        {
            Debug.LogWarning("PlayerLevelModel이 없습니다.");
            return false;
        }

        if (!levelModel.UseStatPoint(1))
        {
            return false;
        }

        playerModel.UpgradeHp();
        NotifyStatusChanged();
        OnPlayerUpgraded?.Invoke("Hp");

        return true;
    }

    public bool UpgradeMp()
    {
        if (levelModel == null)
        {
            Debug.LogWarning("PlayerLevelModel이 없습니다.");
            return false;
        }

        if (!levelModel.UseStatPoint(1))
        {
            return false;
        }

        playerModel.UpgradeMp();
        NotifyStatusChanged();
        OnPlayerUpgraded?.Invoke("Mp");

        return true;
    }

    public bool UpgradeAttack()
    {
        if (levelModel == null)
        {
            Debug.LogWarning("PlayerLevelModel이 없습니다.");
            return false;
        }

        if (!levelModel.UseStatPoint(1))
        {
            return false;
        }

        playerModel.UpgradeAttack();
        NotifyStatusChanged();
        OnPlayerUpgraded?.Invoke("Attack");

        return true;
    }

    public bool UpgradeDefense()
    {
        if (levelModel == null)
        {
            Debug.LogWarning("PlayerLevelModel이 없습니다.");
            return false;
        }

        if (!levelModel.UseStatPoint(1))
        {
            return false;
        }

        playerModel.UpgradeDefense();
        NotifyStatusChanged();
        OnPlayerUpgraded?.Invoke("Defense");

        return true;
    }

    public bool UpgradeMoveSpeed()
    {
        if (levelModel == null)
        {
            Debug.LogWarning("PlayerLevelModel이 없습니다.");
            return false;
        }

        if (!levelModel.UseStatPoint(1))
        {
            return false;
        }

        playerModel.UpgradeMoveSpeed();
        NotifyStatusChanged();
        OnPlayerUpgraded?.Invoke("MoveSpeed");

        return true;
    }
}
