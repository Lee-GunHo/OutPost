using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    private PlayerInputAction playerInputAction;

    public Vector2 MoveInput { get; private set; }

    public bool IsAttackPressed { get; private set; }
    public bool IsInteractPressed { get; private set; }
    public bool IsDashPressed { get; private set; }
    public bool IsToggleToolPressed { get; private set; }

    // (경민) 0806 스탯창 관련 입력 추가
    public bool IsStatWindowPressed { get; private set; }

    private void Awake()
    {
        playerInputAction = new PlayerInputAction();
    }

    private void OnEnable()
    {
        playerInputAction.Player.Enable();

        playerInputAction.Player.Move.performed += OnMovePerformed;
        playerInputAction.Player.Move.canceled += OnMoveCanceled;

        playerInputAction.Player.Attack.performed += OnAttackPerformed;
        playerInputAction.Player.Interact.performed += OnInteractPerformed;
        playerInputAction.Player.Dash.performed += OnDashPerformed;
        playerInputAction.Player.ToggleTool.performed += OnToggleToolPerformed;

        // (경민) 0806 스탯창 입력 이벤트 등록
        playerInputAction.Player.StatWindow.performed += OnStatWindowPerformed;
    }

    private void OnDisable()
    {
        playerInputAction.Player.Move.performed -= OnMovePerformed;
        playerInputAction.Player.Move.canceled -= OnMoveCanceled;

        playerInputAction.Player.Attack.performed -= OnAttackPerformed;
        playerInputAction.Player.Interact.performed -= OnInteractPerformed;
        playerInputAction.Player.Dash.performed -= OnDashPerformed;
        playerInputAction.Player.ToggleTool.performed -= OnToggleToolPerformed;

        // (경민) 0806 스탯창 입력 이벤트 해제
        playerInputAction.Player.StatWindow.performed -= OnStatWindowPerformed;

        playerInputAction.Player.Disable();
    }

    private void LateUpdate()
    {
        ResetButtonInputs();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        IsAttackPressed = true;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        IsInteractPressed = true;
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        IsDashPressed = true;
    }

    private void OnToggleToolPerformed(InputAction.CallbackContext context)
    {
        IsToggleToolPressed = true;
    }

    // (경민) 0806 스탯창 입력 처리
    private void OnStatWindowPerformed(InputAction.CallbackContext context)
    {
        IsStatWindowPressed = true;
    }

    private void ResetButtonInputs()
    {
        IsAttackPressed = false;
        IsInteractPressed = false;
        IsDashPressed = false;
        IsToggleToolPressed = false;

        // (경민) 0806 스탯창 입력 초기화
        IsStatWindowPressed = false;
    }
}