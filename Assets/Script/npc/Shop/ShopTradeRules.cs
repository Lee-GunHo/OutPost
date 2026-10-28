using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 물물교환 계산 규칙(순수 함수). UI 없이 테스트할 수 있게 ShopUI에서 분리.
/// </summary>
public static class ShopTradeRules
{
    /// <summary>
    /// 같은 재료가 여러 줄에 나뉘어 있어도 하나로 합침(처음 나온 순서 유지). 아이템이 없거나 개수가 0 이하인 줄은 무시
    /// </summary>
    public static List<ShopCost> MergeCosts(IReadOnlyList<ShopCost> costs)
    {
        List<ShopCost> merged = new List<ShopCost>();

        if (costs == null)
            return merged;

        foreach (ShopCost cost in costs)
        {
            if (cost == null || cost.Item == null || cost.Amount <= 0)
                continue;

            int index = merged.FindIndex(c => c.Item == cost.Item);

            if (index < 0)
                merged.Add(new ShopCost(cost.Item, cost.Amount));
            else
                merged[index] = new ShopCost(cost.Item, merged[index].Amount + cost.Amount);
        }

        return merged;
    }

    /// <summary>
    /// 지금 몇 번 교환할 수 있는지. 재료 보유량, 남은 지급 한도(ShopGiveLimit), 한 번에 교환할 최대 횟수 중 가장 작은 값
    /// </summary>
    public static int GetMaxTradeCount(
        IReadOnlyList<ShopCost> costs,
        Func<ItemData, int> getOwnedAmount,
        int giveAmount,
        int remainingGive,
        int maxTradeCount)
    {
        int max = Math.Max(1, maxTradeCount);

        if (remainingGive != ShopGiveLimit.Unlimited)
            max = Math.Min(max, Math.Max(0, remainingGive) / Math.Max(1, giveAmount));

        foreach (ShopCost cost in MergeCosts(costs))
        {
            int owned = getOwnedAmount != null ? getOwnedAmount(cost.Item) : 0;
            max = Math.Min(max, Math.Max(0, owned) / cost.Amount);
        }

        return Math.Max(0, max);
    }

    /// <summary>
    /// "Small Mushroom 3 + Spore 1" 형태. tradeCount만큼 곱해서 표시. 재료가 없으면 "무료"
    /// </summary>
    public static string FormatCosts(IReadOnlyList<ShopCost> costs, int tradeCount = 1)
    {
        List<ShopCost> merged = MergeCosts(costs);

        if (merged.Count == 0)
            return "무료";

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < merged.Count; i++)
        {
            if (i > 0)
                builder.Append(" + ");

            builder.Append(merged[i].Item.itemName).Append(' ').Append(merged[i].Amount * Math.Max(1, tradeCount));
        }

        return builder.ToString();
    }
}
