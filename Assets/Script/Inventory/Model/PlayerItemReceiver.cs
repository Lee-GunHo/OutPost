using UnityEngine;

/// <summary>
/// 플레이어가 아이템을 받을 때(줍기, 제작 결과 등) 핫바를 먼저 채우고 남은 개수만 인벤토리로 넣는 규칙.
/// 핫바: 같은 아이템 칸에 먼저 쌓고 → 앞쪽 빈 칸. 전부 들어갈 자리가 있을 때만 받는다.
/// </summary>
public static class PlayerItemReceiver
{
    /// <summary>
    /// 씬의 플레이어 핫바. 핫바가 아직 없으면 null(그 경우 인벤토리만 사용)
    /// </summary>
    public static HotbarModel CurrentHotbar =>
        HotbarPresenter.Instance != null ? HotbarPresenter.Instance.Model : null;

    /// <summary>
    /// 인벤토리 + 핫바 보유량 합계. 아이템이 핫바부터 들어가므로 보유량 판정(퀘스트 등)은 둘 다 봐야 함
    /// </summary>
    public static int GetOwnedAmount(InventoryModel inventory, HotbarModel hotbar, ItemData item)
    {
        if (item == null)
            return 0;

        int amount = inventory != null ? inventory.GetItemCount(item) : 0;

        if (hotbar != null)
            amount += hotbar.GetItemCount(item);

        return amount;
    }

    /// <summary>
    /// 인벤토리에서 먼저 빼고 모자라면 핫바에서 뺌(제작 재료와 같은 순서). 합계가 부족하면 아무것도 빼지 않고 false
    /// </summary>
    public static bool RemoveOwned(InventoryModel inventory, HotbarModel hotbar, ItemData item, int amount)
    {
        if (item == null || amount <= 0 || GetOwnedAmount(inventory, hotbar, item) < amount)
            return false;

        int remaining = amount;

        if (inventory != null)
            remaining -= inventory.RemoveUpTo(item, remaining);

        if (remaining > 0 && hotbar != null)
            remaining -= hotbar.RemoveUpTo(item, remaining);

        return remaining <= 0;
    }

    public static bool CanReceive(InventoryModel inventory, HotbarModel hotbar, ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        int toInventory = amount - GetHotbarShare(hotbar, item, amount);
        return toInventory <= 0 || (inventory != null && inventory.CanAddItem(item, toInventory));
    }

    /// <summary>
    /// CanReceive로 확인한 뒤 호출. 공간이 부족하면 아무것도 넣지 않고 false
    /// </summary>
    public static bool Give(InventoryModel inventory, HotbarModel hotbar, ItemData item, int amount)
    {
        if (!CanReceive(inventory, hotbar, item, amount))
            return false;

        int toHotbar = GetHotbarShare(hotbar, item, amount);

        if (toHotbar > 0)
            hotbar.AddItemUpTo(item, toHotbar);

        int toInventory = amount - toHotbar;

        if (toInventory > 0)
            inventory.AddItem(item, toInventory);

        return true;
    }

    private static int GetHotbarShare(HotbarModel hotbar, ItemData item, int amount)
    {
        return hotbar != null ? Mathf.Min(amount, hotbar.GetAddableAmount(item)) : 0;
    }
}
