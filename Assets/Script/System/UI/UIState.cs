using System;
using UnityEngine;

public static class UIState
{
    public static bool IsInventoryOpen { get; private set; }
    public static bool IsPauseOpen { get; private set; }

    // (경민) 0703 NPC 대화 UI 및 상점 UI 추가
    public static bool IsNPCInteractionOpen { get; private set; }
    public static bool IsShopOpen { get; private set; }

    // (민상) 0806 스탯창 UI 상태 추가
    public static bool IsStatWindowOpen { get; private set; }

    // (민상) 0806 스탯창도 UI 열림 판정에 포함
    public static bool IsAnyUIOpen =>
        IsInventoryOpen ||
        IsPauseOpen ||
        IsNPCInteractionOpen ||
        IsShopOpen ||
        IsStatWindowOpen;

    public static event Action OnStateChanged;

    public static void SetInventoryOpen(bool isOpen)
    {
        IsInventoryOpen = isOpen;
        OnStateChanged?.Invoke(); // (민상) 0806 상태 변경 이벤트 호출 추가
    }

    public static void SetPauseOpen(bool isOpen)
    {
        IsPauseOpen = isOpen;
        OnStateChanged?.Invoke(); // (민상) 0806 상태 변경 이벤트 호출 추가
    }

    // (경민) 0703 NPC 대화 UI 및 상점 UI 추가
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

    // (민상) 0806 스탯창 UI 상태 변경 함수 추가
    public static void SetStatWindowOpen(bool isOpen)
    {
        IsStatWindowOpen = isOpen;
        OnStateChanged?.Invoke();
    }

    // (경민) 0709 상점 UI 오류 해결 위해 추가
    public static void DebugLogState(string where)
    {
        Debug.Log(
            where +
            " / IsAnyUIOpen : " + IsAnyUIOpen +
            " / Inventory : " + IsInventoryOpen +
            " / Pause : " + IsPauseOpen +
            " / NPC : " + IsNPCInteractionOpen +
            " / Shop : " + IsShopOpen +
            " / StatWindow : " + IsStatWindowOpen // (민상) 0806 스탯창 상태 로그 추가
        );
    }
}