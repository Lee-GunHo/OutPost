using NUnit.Framework;

public class MiningRulesTests
{
    [TestCase(0, 0)]
    [TestCase(9, 0)]
    [TestCase(10, 1)]
    [TestCase(25, 2)]
    [TestCase(29, 2)]
    [TestCase(30, 3)]
    [TestCase(999, 3)]
    public void GetClearedStageCount_SplitsDurabilityEvenly(int damageTaken, int expected)
    {
        Assert.AreEqual(expected, MiningRules.GetClearedStageCount(damageTaken, 30, 3));
    }

    [Test]
    public void GetClearedStageCount_SingleStage_ClearsOnlyWhenBroken()
    {
        Assert.AreEqual(0, MiningRules.GetClearedStageCount(9, 10, 1));
        Assert.AreEqual(1, MiningRules.GetClearedStageCount(10, 10, 1));
    }
}
