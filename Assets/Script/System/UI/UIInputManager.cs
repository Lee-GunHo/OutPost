using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(UIPresenter))]
public class UIInputManager : MonoBehaviour
{
    private PlayerInputAction playerInputAction;
    private UIPresenter uiPresenter;

    public event Action OnInventoryPressed;
    public event Action OnPausePressed;

    private void Awake()
    {
        uiPresenter = GetComponent<UIPresenter>();
        if (uiPresenter == null)
            uiPresenter = gameObject.AddComponent<UIPresenter>();
        playerInputAction = new PlayerInputAction();
    }

    private void OnEnable()
    {
        playerInputAction.UI.Enable();

        playerInputAction.UI.Inventory.performed += HandleInventory;
        playerInputAction.UI.Esc.performed += HandlePause;
    }

    private void OnDisable()
    {
        playerInputAction.UI.Inventory.performed -= HandleInventory;
        playerInputAction.UI.Esc.performed -= HandlePause;

        playerInputAction.UI.Disable();
    }

    private void HandleInventory(InputAction.CallbackContext context)
    {
        // Input action callback ordering must not turn ESC + inventory into pause.
        if (UIState.WasEscapeHandledThisFrame ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            return;

        OnInventoryPressed?.Invoke();
    }

    private void HandlePause(InputAction.CallbackContext context)
    {
        if (uiPresenter.HandleEscape())
            return;

        OnPausePressed?.Invoke();
    }
}
