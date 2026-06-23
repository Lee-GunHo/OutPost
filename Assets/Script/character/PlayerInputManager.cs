using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    private PlayerInputAction playerInputAction;

    public Vector2 MoveInput { get; private set; }

    public bool IsAttackPressed { get; private set; }
    public bool IsInteractPressed { get; private set; }
    public bool IsDashPressed { get; private set; }

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
    }

    private void OnDisable()
    {
        playerInputAction.Player.Move.performed -= OnMovePerformed;
        playerInputAction.Player.Move.canceled -= OnMoveCanceled;

        playerInputAction.Player.Attack.performed -= OnAttackPerformed;
        playerInputAction.Player.Interact.performed -= OnInteractPerformed;
        playerInputAction.Player.Dash.performed -= OnDashPerformed;

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



    private void ResetButtonInputs()
    {
        IsAttackPressed = false;
        IsInteractPressed = false;
        IsDashPressed = false;
    }
}