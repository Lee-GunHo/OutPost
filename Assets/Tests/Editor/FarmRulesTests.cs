using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 농사 규칙(FarmRules)을 플레이 없이 검증. 성장 단계 경계와 수확량 범위.
/// </summary>
public class FarmRulesTests
{
    [TestCase(0f, 0)]
    [TestCase(10f, 0)]    // 1/3 미만
    [TestCase(30f, 1)]    // 정확히 1/3 지점
    [TestCase(60f, 2)]
    [TestCase(89.9f, 2)]  // 다 자라기 직전에도 마지막 단계는 아님
    [TestCase(90f, 3)]    // 다 자람
    [TestCase(500f, 3)]   // 넘쳐도 마지막 단계
    public void GetStage_SplitsGrowTimeEvenly(float growth, int expectedStage)
    {
        Assert.AreEqual(expectedStage, FarmRules.GetStage(growth, 90f, 4));
    }

    [Test]
    public void GetStage_NegativeGrowth_IsFirstStage()
    {
        Assert.AreEqual(0, FarmRules.GetStage(-5f, 90f, 4));
    }

    [Test]
    public void IsMature_OnlyWhenGrowthReachesGrowSeconds()
    {
        Assert.IsFalse(FarmRules.IsMature(119.9f, 120f));
        Assert.IsTrue(FarmRules.IsMature(120f, 120f));
    }

    [TestCase(3f, 1f, 1, 0)]
    [TestCase(-3f, 1f, -1, 0)]
    [TestCase(1f, -4f, 0, -1)]
    [TestCase(2f, 2f, 1, 0)]   // 대각선 동률은 x축
    [TestCase(0f, 0f, 0, 1)]   // 같은 칸이면 +z
    public void GetFacing_PicksDominantAxis(float dx, float dz, int expectedX, int expectedY)
    {
        Assert.AreEqual(new Vector2Int(expectedX, expectedY), FarmRules.GetFacing(dx, dz));
    }

    [Test]
    public void GetAreaCells_OneByOne_IsOnlyTarget()
    {
        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(4, 7) },
            FarmRules.GetAreaCells(new Vector2Int(4, 7), new Vector2Int(1, 0), 1, 1));
    }

    [Test]
    public void GetAreaCells_LineOfThree_FollowsFacing()
    {
        // 오른쪽(+x)을 보면 x축으로 일자, 위(+z)를 보면 z축으로 일자
        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(5, 0) },
            FarmRules.GetAreaCells(new Vector2Int(4, 0), new Vector2Int(1, 0), 1, 3));

        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(0, 3), new Vector2Int(0, 4), new Vector2Int(0, 5) },
            FarmRules.GetAreaCells(new Vector2Int(0, 4), new Vector2Int(0, 1), 1, 3));
    }

    [TestCase(3, 9)]
    [TestCase(5, 25)]
    public void GetAreaCells_Square_IsCenteredOnTarget(int size, int expectedCount)
    {
        var cells = FarmRules.GetAreaCells(new Vector2Int(10, 10), new Vector2Int(0, 1), size, size);
        int half = size / 2;

        Assert.AreEqual(expectedCount, cells.Count);
        Assert.AreEqual(expectedCount, new System.Collections.Generic.HashSet<Vector2Int>(cells).Count, "중복 칸 없음");

        foreach (Vector2Int cell in cells)
        {
            Assert.That(cell.x, Is.InRange(10 - half, 10 + half));
            Assert.That(cell.y, Is.InRange(10 - half, 10 + half));
        }
    }

    [Test]
    public void RollAmount_StaysWithinInclusiveRange()
    {
        System.Random random = new System.Random(1234);
        bool sawMin = false;
        bool sawMax = false;

        for (int i = 0; i < 500; i++)
        {
            int amount = FarmRules.RollAmount(random, 1, 3);
            Assert.That(amount, Is.InRange(1, 3));
            sawMin |= amount == 1;
            sawMax |= amount == 3;
        }

        Assert.IsTrue(sawMin && sawMax, "양 끝 값이 모두 나와야 함");
    }

    [Test]
    public void RollAmount_MaxBelowMin_ReturnsMin()
    {
        Assert.AreEqual(2, FarmRules.RollAmount(new System.Random(1), 2, 1));
    }

    [Test]
    public void RollHarvest_Immature_ReturnsOnlyOneSeed()
    {
        (int crop, int seed) = FarmRules.RollHarvest(new System.Random(1), false, 1, 3, 1, 2);

        Assert.AreEqual(0, crop);
        Assert.AreEqual(1, seed);
    }

    [Test]
    public void RollHarvest_Mature_UsesBothRanges()
    {
        System.Random random = new System.Random(42);

        for (int i = 0; i < 200; i++)
        {
            (int crop, int seed) = FarmRules.RollHarvest(random, true, 1, 3, 1, 2);
            Assert.That(crop, Is.InRange(1, 3));
            Assert.That(seed, Is.InRange(1, 2));
        }
    }
}
