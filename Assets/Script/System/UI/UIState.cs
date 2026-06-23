public static class UIState
{
    public static bool IsInventoryOpen { get; private set; }
    public static bool IsPauseOpen { get; private set; }

    public static bool IsAnyUIOpen => IsInventoryOpen || IsPauseOpen;

    public static void SetInventoryOpen(bool isOpen)
    {
        IsInventoryOpen = isOpen;
    }

    public static void SetPauseOpen(bool isOpen)
    {
        IsPauseOpen = isOpen;
    }
}