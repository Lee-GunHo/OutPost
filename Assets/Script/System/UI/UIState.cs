using System;
using UnityEngine;

public static class UIState
{
    public static bool IsInventoryOpen { get; private set; }
    public static bool IsPauseOpen { get; private set; }
    public static bool IsNPCInteractionOpen { get; private set; }
    public static bool IsShopOpen { get; private set; }
    public static bool IsStatWindowOpen { get; private set; }

    // 제작대 UI 열림 상태 추가
    public static bool IsCraftingOpen { get; private set; }

    public static bool IsAnyUIOpen =>
        IsInventoryOpen ||
        IsPauseOpen ||
        IsNPCInteractionOpen ||
        IsShopOpen ||
        IsStatWindowOpen ||
        IsCraftingOpen;

    public static event Action OnStateChanged;

    public static void SetInventoryOpen(bool isOpen)
    {
        IsInventoryOpen = isOpen;
        OnStateChanged?.Invoke();
    }

    public static void SetPauseOpen(bool isOpen)
    {
        IsPauseOpen = isOpen;
        OnStateChanged?.Invoke();
    }

    public static void SetNPCInteractionOpen(bool isOpen)
    {
        IsNPCInteractionOpen = isOpen;
        OnStateChanged?.Invoke();
    }

    public static void SetShopOpen(bool isOpen)
    {
        IsShopOpen = isOpen;
        OnStateChanged?.Invoke();
    }

    public static void SetStatWindowOpen(bool isOpen)
    {
        IsStatWindowOpen = isOpen;
        OnStateChanged?.Invoke();
    }

    // 제작대 UI 열림 상태 변경 함수 추가
    public static void SetCraftingOpen(bool isOpen)
    {
        IsCraftingOpen = isOpen;
        OnStateChanged?.Invoke();
    }

    public static void DebugLogState(string where)
    {
        Debug.Log(
            where +
            " / IsAnyUIOpen : " + IsAnyUIOpen +
            " / Inventory : " + IsInventoryOpen +
            " / Pause : " + IsPauseOpen +
            " / NPC : " + IsNPCInteractionOpen +
            " / Shop : " + IsShopOpen +
            " / StatWindow : " + IsStatWindowOpen +
            " / Crafting : " + IsCraftingOpen
        );
    }
}