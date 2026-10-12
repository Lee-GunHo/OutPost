using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 조언 팁 선택 규칙(AdviceTipSelector)과 그 주변 순수 규칙들을 플레이 없이 검증.
/// 기본 팁 목록(AdviceTipDefaults)을 그대로 대상으로 삼으므로, 기본 priority/조건을 바꾸면 여기서 드러남.
/// </summary>
public class AdviceTipSelectorTests
{
    private const string SampleHint = "안전구역에서 북쪽 가까운 곳";
    private const string DefaultText = "기본 대사";

    private static readonly List<AdviceTip> DefaultTips = AdviceTipDefaults.CreateDefaultTips();

    /// <summary>
    /// questNumber번 퀘스트가 state이고, 그 앞 퀘스트는 모두 완료, 뒤 퀘스트는 시작 전인 스냅샷.
    /// 도구 등급은 기본으로 최고 등급을 줘서 도구 관련 팁이 끼어들지 않게 함.
    /// </summary>
    private static ProgressSnapshot Snapshot(
        int questNumber,
        QuestProgressState state,
        int recentDeath = 0,
        bool combatDiscovered = false,
        string combatNpcHint = SampleHint,
        int toolTier = 4)
    {
        ProgressSnapshot snapshot = new ProgressSnapshot
        {
            DeathCount = recentDeath,
            RecentDeathCount = recentDeath,
            ToolTier = toolTier,
            CombatNpcHint = combatNpcHint
        };

        for (int number = 1; number <= 3; number++)
        {
            QuestProgressState questState =
                number < questNumber ? QuestProgressState.Completed :
                number == questNumber ? state :
                QuestProgressState.NotStarted;

            snapshot.QuestStates[number.ToString()] = questState;
        }

        if (combatDiscovered)
        {
            snapshot.DiscoveredNpcIds.Add(AdviceTipSelector.CombatNpcId);
        }

        return snapshot;
    }

    private static TipSelectionResult Select(ProgressSnapshot snapshot)
    {
        return AdviceTipSelector.Select(snapshot, DefaultTips, DefaultText);
    }

    // ---- 퀘스트별 4개 상태 ----
    // 2·3번 퀘스트의 NotStarted는 "앞 퀘스트를 방금 완료"와 같은 상황이라 그 팁(다음 퀘스트를 받으라는 안내)이 나옴.

    [TestCase(1, QuestProgressState.NotStarted, "quest1_notstarted")]
    [TestCase(1, QuestProgressState.InProgress, "quest1_inprogress")]
    [TestCase(1, QuestProgressState.ReadyToComplete, "quest1_ready")]
    [TestCase(1, QuestProgressState.Completed, "quest1_completed")]
    [TestCase(2, QuestProgressState.NotStarted, "quest1_completed")]
    [TestCase(2, QuestProgressState.InProgress, "quest2_inprogress")]
    [TestCase(2, QuestProgressState.ReadyToComplete, "quest2_ready")]
    [TestCase(2, QuestProgressState.Completed, "quest2_completed")]
    [TestCase(3, QuestProgressState.NotStarted, "quest2_completed")]
    [TestCase(3, QuestProgressState.InProgress, "quest3_inprogress")]
    [TestCase(3, QuestProgressState.ReadyToComplete, "quest3_ready")]
    [TestCase(3, QuestProgressState.Completed, "ending")]
    public void QuestState_SelectsMatchingTip(int questNumber, QuestProgressState state, string expectedTipId)
    {
        TipSelectionResult result = Select(Snapshot(questNumber, state));

        Assert.IsFalse(result.IsDefault);
        Assert.AreEqual(expectedTipId, result.TipId);
        Assert.IsFalse(string.IsNullOrEmpty(result.Reason));
    }

    [Test]
    public void DefaultPriorities_FollowRequestedOrder()
    {
        Assert.AreEqual(100, AdviceTipDefaults.ReadyToCompletePriority);
        Assert.AreEqual(90, AdviceTipDefaults.CombatNpcHintPriority);
        Assert.AreEqual(50, AdviceTipDefaults.InProgressPriority);
        Assert.AreEqual(40, AdviceTipDefaults.JustCompletedPriority);
        Assert.AreEqual(30, AdviceTipDefaults.NotStartedPriority);
        Assert.AreEqual(2, AdviceTipDefaults.CombatNpcHintRecentDeathThreshold);
    }

    // ---- 완료 가능 + 사망 많음의 우선순위 ----

    [Test]
    public void ReadyToComplete_BeatsCombatNpcHint_WhenManyDeaths()
    {
        TipSelectionResult result = Select(Snapshot(1, QuestProgressState.ReadyToComplete, recentDeath: 10));

        Assert.AreEqual("quest1_ready", result.TipId);
        Assert.AreEqual(100, result.Priority);
        Assert.IsFalse(result.UsesCombatNpcHint);
    }

    // ---- 전투 NPC 위치 힌트 ----

    [TestCase(QuestProgressState.NotStarted)]
    [TestCase(QuestProgressState.InProgress)]
    [TestCase(QuestProgressState.Completed)]
    public void CombatNpcHint_Selected_WhenRecentDeathAtThresholdAndUndiscovered(QuestProgressState state)
    {
        TipSelectionResult result = Select(Snapshot(1, state, recentDeath: 2));

        Assert.AreEqual("combat_npc_hint", result.TipId);
        Assert.IsTrue(result.UsesCombatNpcHint);
        Assert.AreEqual(90, result.Priority);
    }

    [Test]
    public void CombatNpcHint_NotSelected_BelowThreshold()
    {
        TipSelectionResult result = Select(Snapshot(1, QuestProgressState.InProgress, recentDeath: 1));

        Assert.AreEqual("quest1_inprogress", result.TipId);
    }

    [TestCase(2)]
    [TestCase(99)]
    public void CombatNpcHint_NeverSelected_AfterDiscovery(int recentDeath)
    {
        for (int questNumber = 1; questNumber <= 3; questNumber++)
        {
            foreach (QuestProgressState state in System.Enum.GetValues(typeof(QuestProgressState)))
            {
                TipSelectionResult result = Select(
                    Snapshot(questNumber, state, recentDeath, combatDiscovered: true));

                Assert.IsFalse(result.UsesCombatNpcHint, $"quest {questNumber} {state}");
                Assert.AreNotEqual("combat_npc_hint", result.TipId);
            }
        }
    }

    [Test]
    public void CombatNpcHint_NeverSelected_AfterDiscovery_EvenIfTipHasNoDiscoveryCondition()
    {
        // 기획자가 실수로 "미발견" 조건을 지운 팁이라도 코드에서 막아야 함
        List<AdviceTip> tips = new List<AdviceTip>
        {
            new AdviceTip
            {
                tipId = "broken_hint",
                priority = 999,
                text = "동료는 " + AdviceTipSelector.CombatNpcHintToken + "에 있어!"
            }
        };

        TipSelectionResult result = AdviceTipSelector.Select(
            Snapshot(1, QuestProgressState.InProgress, recentDeath: 5, combatDiscovered: true),
            tips,
            DefaultText);

        Assert.IsTrue(result.IsDefault);
        Assert.AreEqual(DefaultText, result.Text);
    }

    [TestCase(null)]
    [TestCase("")]
    public void CombatNpcHint_Skipped_WhenHintUnavailable(string unavailableHint)
    {
        // 위치를 구할 수 없으면 오류 없이 위치를 언급하지 않는 다음 팁으로 넘어감
        TipSelectionResult result = Select(
            Snapshot(1, QuestProgressState.InProgress, recentDeath: 5, combatNpcHint: unavailableHint));

        Assert.AreEqual("quest1_inprogress", result.TipId);
        Assert.IsFalse(result.Text.Contains(AdviceTipSelector.CombatNpcHintToken));
    }

    // ---- 힌트 후 RecentDeath 초기화 ----

    [Test]
    public void RecentDeath_ResetAfterHint_ThenHintDoesNotRepeat()
    {
        ProgressSnapshot snapshot = Snapshot(1, QuestProgressState.InProgress, recentDeath: 3);

        TipSelectionResult first = Select(snapshot);
        Assert.AreEqual("combat_npc_hint", first.TipId);
        Assert.IsTrue(AdviceTipSelector.ShouldResetRecentDeath(first, resetOnAnyAdviceTalk: false));

        snapshot.RecentDeathCount = 0; // ProgressTracker.ResetRecentDeath()가 하는 일

        TipSelectionResult second = Select(snapshot);
        Assert.AreEqual("quest1_inprogress", second.TipId);
        Assert.AreEqual(3, snapshot.DeathCount, "누적 사망 횟수는 초기화되면 안 됨");
    }

    [Test]
    public void RecentDeath_NotReset_AfterNonHintTip_UnlessConfigured()
    {
        TipSelectionResult result = Select(Snapshot(1, QuestProgressState.ReadyToComplete, recentDeath: 3));

        Assert.IsFalse(AdviceTipSelector.ShouldResetRecentDeath(result, resetOnAnyAdviceTalk: false));
        Assert.IsTrue(AdviceTipSelector.ShouldResetRecentDeath(result, resetOnAnyAdviceTalk: true));
    }

    // ---- 퀘스트 3 완료 후 엔딩 ----

    [TestCase(0, false)]
    [TestCase(50, false)]
    [TestCase(50, true)]
    public void Ending_Selected_AfterQuest3Completed(int recentDeath, bool combatDiscovered)
    {
        TipSelectionResult result = Select(
            Snapshot(3, QuestProgressState.Completed, recentDeath, combatDiscovered));

        Assert.AreEqual("ending", result.TipId);
    }

    // ---- 맞는 팁이 없을 때 기본 대사 ----

    [Test]
    public void Default_WhenNoTipMatches()
    {
        List<AdviceTip> tips = new List<AdviceTip>
        {
            new AdviceTip
            {
                tipId = "never",
                priority = 10,
                text = "나오면 안 되는 대사",
                conditions = new List<TipCondition>
                {
                    new TipCondition
                    {
                        kind = TipConditionKind.DeathCount,
                        op = TipCompareOp.Greater,
                        value = 1000
                    }
                }
            }
        };

        TipSelectionResult result = AdviceTipSelector.Select(
            Snapshot(1, QuestProgressState.NotStarted), tips, DefaultText);

        Assert.IsTrue(result.IsDefault);
        Assert.IsNull(result.Tip);
        Assert.AreEqual(0, result.Priority);
        Assert.AreEqual(DefaultText, result.Text);
        Assert.AreEqual(AdviceTipSelector.DefaultTipId, result.TipId);
    }

    [Test]
    public void Default_WhenTipListIsEmptyOrNull()
    {
        ProgressSnapshot snapshot = Snapshot(1, QuestProgressState.InProgress);

        Assert.IsTrue(AdviceTipSelector.Select(snapshot, new List<AdviceTip>(), DefaultText).IsDefault);
        Assert.IsTrue(AdviceTipSelector.Select(snapshot, null, DefaultText).IsDefault);
        Assert.IsTrue(AdviceTipSelector.Select(null, DefaultTips, DefaultText).IsDefault);
    }

    // ---- 선택 규칙 ----

    [Test]
    public void SamePriority_FirstInListWins()
    {
        List<AdviceTip> tips = new List<AdviceTip>
        {
            new AdviceTip { tipId = "low", priority = 1, text = "낮음" },
            new AdviceTip { tipId = "first", priority = 5, text = "먼저" },
            new AdviceTip { tipId = "second", priority = 5, text = "나중" }
        };

        TipSelectionResult result = AdviceTipSelector.Select(new ProgressSnapshot(), tips, DefaultText);

        Assert.AreEqual("first", result.TipId);
    }

    [Test]
    public void AllConditionsMustBeSatisfied()
    {
        AdviceTip tip = DefaultTips.First(t => t.tipId == "quest2_lowtool");

        Assert.IsTrue(AdviceTipSelector.AreAllSatisfied(
            tip.conditions, Snapshot(2, QuestProgressState.InProgress, toolTier: 1)));
        Assert.IsFalse(AdviceTipSelector.AreAllSatisfied(
            tip.conditions, Snapshot(2, QuestProgressState.InProgress, toolTier: 2)));
        Assert.IsFalse(AdviceTipSelector.AreAllSatisfied(
            tip.conditions, Snapshot(1, QuestProgressState.InProgress, toolTier: 1)));
    }

    [Test]
    public void ToolTier_LowToolTipBeatsPlainInProgress()
    {
        Assert.AreEqual("quest2_lowtool",
            Select(Snapshot(2, QuestProgressState.InProgress, toolTier: 1)).TipId);
        Assert.AreEqual("quest2_inprogress",
            Select(Snapshot(2, QuestProgressState.InProgress, toolTier: 2)).TipId);
    }

    [TestCase(TipCompareOp.Equal, 3, true)]
    [TestCase(TipCompareOp.NotEqual, 3, false)]
    [TestCase(TipCompareOp.Greater, 2, true)]
    [TestCase(TipCompareOp.Greater, 3, false)]
    [TestCase(TipCompareOp.GreaterOrEqual, 3, true)]
    [TestCase(TipCompareOp.Less, 4, true)]
    [TestCase(TipCompareOp.Less, 3, false)]
    [TestCase(TipCompareOp.LessOrEqual, 3, true)]
    public void CompareOps_WorkForEveryKind(TipCompareOp op, int value, bool expected)
    {
        ProgressSnapshot snapshot = new ProgressSnapshot
        {
            DeathCount = 3,
            RecentDeathCount = 3,
            KillCount = 3,
            ToolTier = 3
        };
        snapshot.QuestStates["q"] = QuestProgressState.Completed; // 3

        TipConditionKind[] kinds =
        {
            TipConditionKind.QuestState,
            TipConditionKind.DeathCount,
            TipConditionKind.RecentDeathCount,
            TipConditionKind.KillCount,
            TipConditionKind.ToolTier
        };

        foreach (TipConditionKind kind in kinds)
        {
            TipCondition condition = new TipCondition { kind = kind, op = op, targetId = "q", value = value };

            Assert.AreEqual(expected, AdviceTipSelector.IsSatisfied(condition, snapshot), kind.ToString());
        }
    }

    [Test]
    public void NpcDiscoveredCondition_UsesTargetId()
    {
        ProgressSnapshot snapshot = new ProgressSnapshot();
        snapshot.DiscoveredNpcIds.Add("Shop");

        TipCondition shop = new TipCondition
        {
            kind = TipConditionKind.NpcDiscovered, op = TipCompareOp.Equal, targetId = "Shop", value = 1
        };
        TipCondition combat = new TipCondition
        {
            kind = TipConditionKind.NpcDiscovered, op = TipCompareOp.Equal, targetId = "Combat", value = 1
        };

        Assert.IsTrue(AdviceTipSelector.IsSatisfied(shop, snapshot));
        Assert.IsFalse(AdviceTipSelector.IsSatisfied(combat, snapshot));
    }

    [Test]
    public void DefaultTips_DoNotUseKillCount_AndHaveUniqueIds()
    {
        Assert.IsFalse(DefaultTips.Any(
            tip => tip.conditions.Any(condition => condition.kind == TipConditionKind.KillCount)));
        Assert.AreEqual(DefaultTips.Count, DefaultTips.Select(tip => tip.tipId).Distinct().Count());
        Assert.IsTrue(DefaultTips.All(tip => !string.IsNullOrEmpty(tip.text)));
    }

    [Test]
    public void CombatNpcId_MatchesNpcRoleName()
    {
        Assert.AreEqual(NPCRole.Combat.ToString(), AdviceTipSelector.CombatNpcId);
    }

    // ---- 전투 NPC 위치 힌트 문구 ----

    [TestCase(0f, 100f, "북")]
    [TestCase(100f, 100f, "북동")]
    [TestCase(100f, 0f, "동")]
    [TestCase(100f, -100f, "남동")]
    [TestCase(0f, -100f, "남")]
    [TestCase(-100f, -100f, "남서")]
    [TestCase(-100f, 0f, "서")]
    [TestCase(-100f, 100f, "북서")]
    [TestCase(10f, 100f, "북")] // 살짝 비껴도 가장 가까운 방위로
    public void HintBuilder_ConvertsToEightDirections(float x, float z, string expectedDirection)
    {
        CombatNpcHintSettings settings = new CombatNpcHintSettings();
        Vector3 origin = new Vector3(5f, 0f, -3f);

        bool built = CombatNpcHintBuilder.TryBuild(
            origin, origin + new Vector3(x, 7f, z), settings, out string hint);

        Assert.IsTrue(built);
        StringAssert.Contains(expectedDirection + "쪽", hint);
    }

    [TestCase(10f, "안전구역 바로 근처")]
    [TestCase(50f, "가까운 곳")]
    [TestCase(200f, "조금 떨어진 곳")]
    [TestCase(400f, "꽤 먼 곳")]
    [TestCase(5000f, "아주 먼 곳")]
    public void HintBuilder_ConvertsToDistanceSteps(float distance, string expectedLabel)
    {
        bool built = CombatNpcHintBuilder.TryBuild(
            Vector3.zero, new Vector3(0f, 0f, distance), new CombatNpcHintSettings(), out string hint);

        Assert.IsTrue(built);
        StringAssert.Contains(expectedLabel, hint);
    }

    [Test]
    public void HintBuilder_NeverExposesCoordinates()
    {
        CombatNpcHintSettings settings = new CombatNpcHintSettings();
        Vector3[] targets =
        {
            new Vector3(123.45f, 6f, -78.9f),
            new Vector3(-301f, 0f, 44f),
            new Vector3(3f, 0f, 2f)
        };

        foreach (Vector3 target in targets)
        {
            Assert.IsTrue(CombatNpcHintBuilder.TryBuild(Vector3.zero, target, settings, out string hint));
            Assert.IsFalse(hint.Any(char.IsDigit), hint);
        }
    }

    [Test]
    public void HintBuilder_FailsQuietly_OnBadInput()
    {
        CombatNpcHintSettings settings = new CombatNpcHintSettings();

        Assert.IsFalse(CombatNpcHintBuilder.TryBuild(Vector3.zero, Vector3.one, null, out string hint));
        Assert.IsNull(hint);

        Assert.IsFalse(CombatNpcHintBuilder.TryBuild(
            Vector3.zero, new Vector3(float.NaN, 0f, 1f), settings, out hint));
        Assert.IsFalse(CombatNpcHintBuilder.TryBuild(
            Vector3.zero, new Vector3(float.PositiveInfinity, 0f, 1f), settings, out hint));

        settings.directionNames = new[] { "북", "남" };
        Assert.IsFalse(CombatNpcHintBuilder.TryBuild(
            Vector3.zero, new Vector3(0f, 0f, 100f), settings, out hint));
    }

    // ---- 진행도 규칙 ----

    [TestCase(false, false, 99, 20, QuestProgressState.NotStarted)]
    [TestCase(true, false, 19, 20, QuestProgressState.InProgress)]
    [TestCase(true, false, 20, 20, QuestProgressState.ReadyToComplete)]
    [TestCase(true, true, 0, 20, QuestProgressState.Completed)]
    public void QuestProgressRules_Resolve(
        bool accepted, bool completed, int owned, int required, QuestProgressState expected)
    {
        Assert.AreEqual(expected, QuestProgressRules.Resolve(accepted, completed, owned, required));
    }

    [Test]
    public void ToolTierRules_ReturnsHighestOwnedTier()
    {
        int[] tierItemIds = AdviceTipDefaults.CreateDefaultToolTierItemIds();

        Assert.AreEqual(0, ToolTierRules.Resolve(tierItemIds, new HashSet<int>()));
        Assert.AreEqual(1, ToolTierRules.Resolve(tierItemIds, new HashSet<int> { tierItemIds[0] }));
        Assert.AreEqual(3, ToolTierRules.Resolve(tierItemIds, new HashSet<int> { tierItemIds[0], tierItemIds[2] }));
        Assert.AreEqual(0, ToolTierRules.Resolve(null, new HashSet<int> { 1 }));
    }

    [TestCase(0, 50, ShopGiveLimit.Unlimited)]
    [TestCase(10, 0, 10)]
    [TestCase(10, 4, 6)]
    [TestCase(10, 10, 0)]
    [TestCase(10, 15, 0)]
    public void ShopGiveLimit_GetRemaining(int maxTotalGive, int received, int expected)
    {
        Assert.AreEqual(expected, ShopGiveLimit.GetRemaining(maxTotalGive, received));
    }

    // ---- 전투 동료 상태 분류 ----

    private static readonly CompanionRuleValues CompanionRules = new CompanionRuleValues
    {
        FollowStartDistance = 4f,
        FollowStopDistance = 2f,
        AttackRange = 1.8f,
        LeashDistance = 14f
    };

    // current, recruited, distToPlayer, hasTarget, distToTarget, expected
    [TestCase(CompanionStateId.Idle, false, 50f, true, 1f, CompanionStateId.Idle)]      // 미합류면 항상 Idle
    [TestCase(CompanionStateId.Idle, true, 3f, false, 0f, CompanionStateId.Idle)]       // 가까우면 대기
    [TestCase(CompanionStateId.Idle, true, 5f, false, 0f, CompanionStateId.Follow)]     // 멀어지면 따라감
    [TestCase(CompanionStateId.Follow, true, 3f, false, 0f, CompanionStateId.Follow)]   // 정지 거리까지 계속
    [TestCase(CompanionStateId.Follow, true, 1.5f, false, 0f, CompanionStateId.Idle)]   // 도착
    [TestCase(CompanionStateId.Idle, true, 3f, true, 5f, CompanionStateId.Chase)]       // 몬스터 발견
    [TestCase(CompanionStateId.Chase, true, 6f, true, 1.5f, CompanionStateId.Attack)]   // 사거리 진입
    [TestCase(CompanionStateId.Attack, true, 6f, true, 3f, CompanionStateId.Chase)]     // 사거리 이탈
    [TestCase(CompanionStateId.Attack, true, 6f, false, 0f, CompanionStateId.Follow)]   // 대상 없어지면 복귀
    [TestCase(CompanionStateId.Attack, true, 14f, true, 1f, CompanionStateId.Follow)]   // 너무 멀어지면 전투 포기
    [TestCase(CompanionStateId.Follow, true, 8f, true, 1f, CompanionStateId.Follow)]    // 복귀 중에는 전투 무시
    [TestCase(CompanionStateId.Follow, true, 3.5f, true, 1f, CompanionStateId.Attack)]  // 곁에 붙으면 다시 전투
    public void CompanionDecision_Decide(
        CompanionStateId current,
        bool recruited,
        float distanceToPlayer,
        bool hasTarget,
        float distanceToTarget,
        CompanionStateId expected)
    {
        CompanionSense sense = new CompanionSense
        {
            IsRecruited = recruited,
            DistanceToPlayer = distanceToPlayer,
            HasTarget = hasTarget,
            DistanceToTarget = distanceToTarget
        };

        Assert.AreEqual(expected, CompanionDecision.Decide(current, sense, CompanionRules));
    }
}
