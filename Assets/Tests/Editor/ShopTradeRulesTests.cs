using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 물물교환 계산 규칙(ShopTradeRules)을 플레이 없이 검증. 재료 합치기, 교환 가능 횟수, 표시 문구.
/// </summary>
public class ShopTradeRulesTests
{
    private ItemData mushroom;
    private ItemData spore;

    [SetUp]
    public void SetUp()
    {
        mushroom = ScriptableObject.CreateInstance<ItemData>();
        mushroom.itemName = "Small Mushroom";
        spore = ScriptableObject.CreateInstance<ItemData>();
        spore.itemName = "Spore";
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(mushroom);
        Object.DestroyImmediate(spore);
    }

    private System.Func<ItemData, int> Owned(int mushroomCount, int sporeCount)
    {
        return item => item == mushroom ? mushroomCount : item == spore ? sporeCount : 0;
    }

    [Test]
    public void MergeCosts_SameItemOnSeveralLines_IsSummed()
    {
        List<ShopCost> merged = ShopTradeRules.MergeCosts(new List<ShopCost>
        {
            new ShopCost(mushroom, 2),
            new ShopCost(spore, 1),
            new ShopCost(mushroom, 3),
            new ShopCost(null, 5),
        });

        Assert.AreEqual(2, merged.Count);
        Assert.AreEqual(mushroom, merged[0].Item);
        Assert.AreEqual(5, merged[0].Amount);
        Assert.AreEqual(1, merged[1].Amount);
    }

    [Test]
    public void GetMaxTradeCount_LimitedByScarcestMaterial()
    {
        var costs = new List<ShopCost> { new ShopCost(mushroom, 3), new ShopCost(spore, 2) };

        // 버섯 10개 → 3번, 포자 5개 → 2번
        Assert.AreEqual(2, ShopTradeRules.GetMaxTradeCount(costs, Owned(10, 5), 1, ShopGiveLimit.Unlimited, 99));
    }

    [Test]
    public void GetMaxTradeCount_NotEnoughMaterial_IsZero()
    {
        var costs = new List<ShopCost> { new ShopCost(mushroom, 3) };

        Assert.AreEqual(0, ShopTradeRules.GetMaxTradeCount(costs, Owned(2, 0), 1, ShopGiveLimit.Unlimited, 99));
    }

    [Test]
    public void GetMaxTradeCount_RespectsGiveLimitAndGiveAmount()
    {
        var costs = new List<ShopCost> { new ShopCost(mushroom, 1) };

        // 남은 한도 5개, 1회에 2개씩 → 2번
        Assert.AreEqual(2, ShopTradeRules.GetMaxTradeCount(costs, Owned(99, 0), 2, 5, 99));
        // 남은 한도가 1회 지급량보다 적으면 교환 불가
        Assert.AreEqual(0, ShopTradeRules.GetMaxTradeCount(costs, Owned(99, 0), 2, 1, 99));
    }

    [Test]
    public void GetMaxTradeCount_NoCosts_IsFreeUpToMax()
    {
        Assert.AreEqual(7, ShopTradeRules.GetMaxTradeCount(new List<ShopCost>(), Owned(0, 0), 1, ShopGiveLimit.Unlimited, 7));
    }

    [Test]
    public void FormatCosts_MultipliesByTradeCount()
    {
        var costs = new List<ShopCost> { new ShopCost(mushroom, 3), new ShopCost(spore, 1) };

        Assert.AreEqual("Small Mushroom 3 + Spore 1", ShopTradeRules.FormatCosts(costs));
        Assert.AreEqual("Small Mushroom 6 + Spore 2", ShopTradeRules.FormatCosts(costs, 2));
        Assert.AreEqual("무료", ShopTradeRules.FormatCosts(null));
    }
}
