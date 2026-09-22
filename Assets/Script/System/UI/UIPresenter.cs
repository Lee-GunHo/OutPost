using UnityEngine;

public class UIPresenter : MonoBehaviour
{
    // UIState holds window state; existing presenters/views own their close and
    // cleanup routines. Consume ESC once before invoking any of those routines.
    public bool HandleEscape()
    {
        if (!UIState.TryConsumeEscape())
            return true;

        if (UIState.IsChestOpen)
            ChestPresenter.Instance?.Close();
        else if (UIState.IsShopOpen)
            ShopUI.Instance?.Close();
        else if (UIState.IsNPCInteractionOpen)
            NPCInteractionUI.Instance?.Close();
        else if (UIState.IsStatWindowOpen)
            FindFirstObjectByType<PlayerStatusUI>()?.CloseStatPanel();
        else if (UIState.IsInventoryOpen)
            FindFirstObjectByType<InventoryPresenter>()?.CloseInventory();
        else if (UIState.IsCraftingOpen)
            CraftingPresenter.Instance?.Close();
        else
            return false; // No gameplay window: let the pause menu handle ESC.

        return true;
    }
}
