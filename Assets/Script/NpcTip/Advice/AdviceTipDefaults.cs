using System.Collections.Generic;

/// <summary>
/// AdviceTipSet의 초기값. 에셋을 새로 만들 때 이 값이 채워지고, 이후에는 인스펙터에서 자유롭게 수정.
/// EditMode 테스트와 에디터 디버그 창도 같은 기본값을 사용.
///
/// TODO(대사): 아래 문구는 전부 퀘스트 말투(반말, 느낌표)를 흉내 낸 임시 문구. 기획 확정 후 교체할 것.
/// </summary>
public static class AdviceTipDefaults
{
    public const int ReadyToCompletePriority = 100;
    public const int EndingPriority = 100;
    public const int CombatNpcHintPriority = 90;
    public const int LowToolTierPriority = 55;
    public const int InProgressPriority = 50;
    public const int JustCompletedPriority = 40;
    public const int NotStartedPriority = 30;

    // 짧은 플레이에서도 힌트가 나오도록 낮게 잡음
    public const int CombatNpcHintRecentDeathThreshold = 2;

    public const string Quest1Id = "1";
    public const string Quest2Id = "2";
    public const string Quest3Id = "3";

    // TODO(대사): 임시 문구
    public const string DefaultText = "오늘도 힘내! 모르는 게 있으면 언제든 물어봐!";

    // WoodPick, CopperPick, SilverPick, GoldPick의 itemID
    public static int[] CreateDefaultToolTierItemIds()
    {
        return new[] { 26, 37, 47, 57 };
    }

    public static List<AdviceTip> CreateDefaultTips()
    {
        return new List<AdviceTip>
        {
            // ---- 퀘스트 1: 흙 블럭 20개 ----
            // TODO(대사): 임시 문구
            Tip("quest1_notstarted", NotStartedPriority,
                "어서 와! 여기는 처음이지? 먼저 퀘스트를 받아서 블럭부터 부숴 봐!",
                Quest(Quest1Id, QuestProgressState.NotStarted)),
            // TODO(대사): 임시 문구
            Tip("quest1_inprogress", InProgressPriority,
                "흙 블럭은 안전구역 바로 바깥에 잔뜩 있어! 곡괭이로 부숴서 20개를 모아 와!",
                Quest(Quest1Id, QuestProgressState.InProgress)),
            // TODO(대사): 임시 문구
            Tip("quest1_ready", ReadyToCompletePriority,
                "흙 블럭을 다 모았네! 얼른 퀘스트를 완료하고 보상을 받아 가!",
                Quest(Quest1Id, QuestProgressState.ReadyToComplete)),
            // TODO(대사): 임시 문구
            Tip("quest1_completed", JustCompletedPriority,
                "첫 퀘스트를 해냈구나! 잘했어! 다음 퀘스트도 기다리고 있으니까 받아 가!",
                Quest(Quest1Id, QuestProgressState.Completed),
                Quest(Quest2Id, QuestProgressState.NotStarted)),

            // ---- 퀘스트 2: 돌 블럭 30개 ----
            // TODO(대사): 임시 문구
            Tip("quest2_inprogress", InProgressPriority,
                "돌 블럭은 흙 구역을 지나서 더 멀리 나가야 보여! 30개를 모아 와!",
                Quest(Quest2Id, QuestProgressState.InProgress)),
            // TODO(대사): 임시 문구
            Tip("quest2_lowtool", LowToolTierPriority,
                "그 곡괭이로는 돌이 너무 느리게 캐질 거야! 제작대에서 더 좋은 곡괭이부터 만들어 봐!",
                Quest(Quest2Id, QuestProgressState.InProgress),
                Condition(TipConditionKind.ToolTier, TipCompareOp.LessOrEqual, null, 1)),
            // TODO(대사): 임시 문구
            Tip("quest2_ready", ReadyToCompletePriority,
                "돌 블럭을 다 모았구나! 얼른 퀘스트를 완료하고 선물을 받아 가!",
                Quest(Quest2Id, QuestProgressState.ReadyToComplete)),
            // TODO(대사): 임시 문구
            Tip("quest2_completed", JustCompletedPriority,
                "벌써 두 번째 퀘스트까지 끝냈어? 대단해! 마지막 퀘스트도 받아 가!",
                Quest(Quest2Id, QuestProgressState.Completed),
                Quest(Quest3Id, QuestProgressState.NotStarted)),

            // ---- 퀘스트 3: 금 블럭 50개 ----
            // TODO(대사): 임시 문구
            Tip("quest3_inprogress", InProgressPriority,
                "금 블럭은 아주 먼 구역까지 가야 나와! 몬스터가 강하니까 장비를 단단히 챙겨서 50개를 모아 와!",
                Quest(Quest3Id, QuestProgressState.InProgress)),
            // TODO(대사): 임시 문구
            Tip("quest3_ready", ReadyToCompletePriority,
                "금 블럭을 전부 모았어!! 어서 마지막 퀘스트를 완료해!",
                Quest(Quest3Id, QuestProgressState.ReadyToComplete)),

            // ---- 엔딩: 퀘스트 3 완료 후 ----
            // TODO(대사): 임시 문구
            Tip("ending", EndingPriority,
                "금 블럭까지 전부 모으다니 정말 대단해!! 이제 네가 이 전초기지의 영웅이야! 지금까지 고마웠어!",
                Quest(Quest3Id, QuestProgressState.Completed)),

            // ---- 전투 NPC 위치 힌트: 최근 사망 N회 이상 + 전투 NPC 미발견 ----
            // TODO(대사): 임시 문구
            Tip("combat_npc_hint", CombatNpcHintPriority,
                "요즘 많이 쓰러지던데 괜찮아? 같이 싸워 줄 동료가 " +
                AdviceTipSelector.CombatNpcHintToken + "에 있다고 들었어! 한번 찾아가 봐!",
                Condition(TipConditionKind.RecentDeathCount, TipCompareOp.GreaterOrEqual, null,
                    CombatNpcHintRecentDeathThreshold),
                Condition(TipConditionKind.NpcDiscovered, TipCompareOp.Equal, AdviceTipSelector.CombatNpcId, 0))
        };
    }

    private static AdviceTip Tip(string tipId, int priority, string text, params TipCondition[] conditions)
    {
        return new AdviceTip
        {
            tipId = tipId,
            priority = priority,
            text = text,
            conditions = new List<TipCondition>(conditions)
        };
    }

    private static TipCondition Quest(string questId, QuestProgressState state)
    {
        return Condition(TipConditionKind.QuestState, TipCompareOp.Equal, questId, (int)state);
    }

    private static TipCondition Condition(TipConditionKind kind, TipCompareOp op, string targetId, int value)
    {
        return new TipCondition { kind = kind, op = op, targetId = targetId, value = value };
    }
}
