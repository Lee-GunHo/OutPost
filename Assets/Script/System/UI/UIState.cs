using System;
using UnityEngine;

public static class UIState
{
    public static bool IsInventoryOpen { get; private set; }
    public static bool IsPauseOpen { get; private set; }
    public static bool IsNPCInteractionOpen { get; private set; }
    public static bool IsShopOpen { get; private set; }
    public static bool IsStatWindowOpen { get; private set; }
    public static bool IsChestOpen { get; private set; }

    private static int escapeHandledFrame = -1;
    public static bool WasEscapeHandledThisFrame => escapeHandledFrame == Time.frameCount;
    public static bool CanOpenWindow => !IsAnyUIOpen && !WasEscapeHandledThisFrame;

    public static bool TryConsumeEscape()
    {
        if (WasEscapeHandledThisFrame)
            return false;

        escapeHandledFrame = Time.frameCount;
        return true;
    }

    // 제작대 UI 열림 상태 추가
    public static bool IsCraftingOpen { get; private set; }

    public static bool IsAnyUIOpen =>
        IsInventoryOpen ||
        IsPauseOpen ||
        IsNPCInteractionOpen ||
        IsShopOpen ||
        IsStatWindowOpen ||
        IsChestOpen ||
        IsCraftingOpen;

    public static event Action OnStateChanged;

    // UI가 하나라도 열려 있으면 게임을 멈춤(Time.timeScale = 0) - 몬스터 AI/물리도 같이
    // 멈추므로, 이전까지는 플레이어 입력만 막히고 몬스터는 계속 움직여서 무방비로 맞는
    // 문제가 있었음. UI(Canvas)는 timeScale 영향을 안 받아서 계속 조작 가능.
    private static void NotifyChanged()
    {
        Time.timeScale = IsAnyUIOpen ? 0f : 1f;
        OnStateChanged?.Invoke();
    }

    public static void ResetAll()
    {
        IsInventoryOpen = false;
        IsPauseOpen = false;
        IsNPCInteractionOpen = false;
        IsShopOpen = false;
        IsStatWindowOpen = false;
        IsCraftingOpen = false;
        IsChestOpen = false;
        escapeHandledFrame = -1;
        NotifyChanged();
    }

    public static void SetInventoryOpen(bool isOpen)
    {
        IsInventoryOpen = isOpen;
        NotifyChanged();
    }

    public static void SetChestOpen(bool isOpen)
    {
        IsChestOpen = isOpen;
        NotifyChanged();
    }

    public static void SetPauseOpen(bool isOpen)
    {
        IsPauseOpen = isOpen;
        NotifyChanged();
    }

    public static void SetNPCInteractionOpen(bool isOpen)
    {
        IsNPCInteractionOpen = isOpen;
        NotifyChanged();
    }

    public static void SetShopOpen(bool isOpen)
    {
        IsShopOpen = isOpen;
        NotifyChanged();
    }

    public static void SetStatWindowOpen(bool isOpen)
    {
        IsStatWindowOpen = isOpen;
        NotifyChanged();
    }

    // 제작대 UI 열림 상태 변경 함수 추가
    public static void SetCraftingOpen(bool isOpen)
    {
        IsCraftingOpen = isOpen;
        NotifyChanged();
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
