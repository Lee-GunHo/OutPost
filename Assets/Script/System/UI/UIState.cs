public static class UIState
{
    public static bool IsInventoryOpen { get; private set; }
    public static bool IsPauseOpen { get; private set; }

    // (경민) 0703 NPC 대화 UI 및 상점 UI 추가
    public static bool IsNPCInteractionOpen { get; private set; }
    public static bool IsShopOpen { get; private set; }

    public static bool IsAnyUIOpen => IsInventoryOpen || IsPauseOpen || IsNPCInteractionOpen || IsShopOpen;

    public static void SetInventoryOpen(bool isOpen)
    {
        IsInventoryOpen = isOpen;
    }

    public static void SetPauseOpen(bool isOpen)
    {
        IsPauseOpen = isOpen;
    }

    // (경민) 0703 NPC 대화 UI 및 상점 UI 추가
    public static void SetNPCInteractionOpen(bool isOpen)
    {
        IsNPCInteractionOpen = isOpen;
    }

    public static void SetShopOpen(bool isOpen)
    {
        IsShopOpen = isOpen;
    }
}