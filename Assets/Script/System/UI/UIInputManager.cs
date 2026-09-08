using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputManager : MonoBehaviour
{
    private PlayerInputAction playerInputAction;

    public event Action OnInventoryPressed;
    public event Action OnPausePressed;

    private void Awake()
    {
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
        OnInventoryPressed?.Invoke();
    }

    private void HandlePause(InputAction.CallbackContext context)
    {
        // Closing the stat window consumes ESC before any pause listeners run.
        if (UIState.IsStatWindowOpen)
        {
            PlayerStatusUI statusUI = FindFirstObjectByType<PlayerStatusUI>();
            if (statusUI != null)
            {
                statusUI.CloseStatPanel();
            }

            return;
        }

        OnPausePressed?.Invoke();
    }
}
