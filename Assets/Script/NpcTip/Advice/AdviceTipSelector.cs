using System.Collections.Generic;

/// <summary>
/// 팁 선택 결과. Tip이 null이면(IsDefault) 기본 대사.
/// </summary>
public class TipSelectionResult
{
    public AdviceTip Tip;
    public bool IsDefault;
    public int Priority;
    public string Text;
    public string Reason;
    public bool UsesCombatNpcHint;

    public string TipId => IsDefault || Tip == null ? AdviceTipSelector.DefaultTipId : Tip.tipId;
}

/// <summary>
/// 진행도 스냅샷 + 팁 목록 → 팁 하나를 고르는 순수 함수 모음. UI/MonoBehaviour/씬에 의존하지 않음.
/// 규칙: 조건을 모두 만족하는 팁 중 priority 최대, 동률이면 목록에서 앞에 있는 것, 없으면 기본 대사.
/// </summary>
public static class AdviceTipSelector
{
    public const string CombatNpcHintToken = "{combatNpcHint}";
    public const string CombatNpcId = "Combat"; // NPCRole.Combat.ToString()
    public const string DefaultTipId = "default";

    public static TipSelectionResult Select(
        ProgressSnapshot snapshot,
        IReadOnlyList<AdviceTip> tips,
        string defaultText)
    {
        AdviceTip best = null;
        int candidateCount = 0;
        string skippedNote = string.Empty;

        if (snapshot != null && tips != null)
        {
            for (int i = 0; i < tips.Count; i++)
            {
                AdviceTip tip = tips[i];

                if (tip == null || !AreAllSatisfied(tip.conditions, snapshot))
                {
                    continue;
                }

                if (UsesCombatNpcHint(tip))
                {
                    // 조건 데이터가 잘못 편집돼도 발견 후에는 위치 힌트가 절대 나오지 않게 코드로 막음
                    if (snapshot.IsNpcDiscovered(CombatNpcId))
                    {
                        skippedNote += $" [{tip.tipId} 제외: 전투 NPC 이미 발견]";
                        continue;
                    }

                    if (string.IsNullOrEmpty(snapshot.CombatNpcHint))
                    {
                        skippedNote += $" [{tip.tipId} 제외: 위치 힌트 계산 불가]";
                        continue;
                    }
                }

                candidateCount++;

                // '>'라서 동률이면 먼저 나온(목록 앞) 팁이 유지됨
                if (best == null || tip.priority > best.priority)
                {
                    best = tip;
                }
            }
        }

        if (best == null)
        {
            return new TipSelectionResult
            {
                Tip = null,
                IsDefault = true,
                Priority = 0,
                Text = defaultText,
                Reason = "조건을 만족하는 팁이 없어 기본 대사 사용" + skippedNote
            };
        }

        return new TipSelectionResult
        {
            Tip = best,
            IsDefault = false,
            Priority = best.priority,
            Text = best.text,
            UsesCombatNpcHint = UsesCombatNpcHint(best),
            Reason = $"조건 {(best.conditions != null ? best.conditions.Count : 0)}개 모두 만족, " +
                $"후보 {candidateCount}개 중 priority {best.priority} 최대" + skippedNote
        };
    }

    public static bool UsesCombatNpcHint(AdviceTip tip)
    {
        return tip != null && !string.IsNullOrEmpty(tip.text) && tip.text.Contains(CombatNpcHintToken);
    }

    /// <summary>
    /// 이 팁을 말한 뒤 RecentDeath를 0으로 돌려야 하는지. 위치 힌트를 말했으면 항상 초기화.
    /// </summary>
    public static bool ShouldResetRecentDeath(TipSelectionResult result, bool resetOnAnyAdviceTalk)
    {
        return result != null && (resetOnAnyAdviceTalk || result.UsesCombatNpcHint);
    }

    public static bool AreAllSatisfied(IReadOnlyList<TipCondition> conditions, ProgressSnapshot snapshot)
    {
        if (conditions == null)
        {
            return true;
        }

        for (int i = 0; i < conditions.Count; i++)
        {
            if (!IsSatisfied(conditions[i], snapshot))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsSatisfied(TipCondition condition, ProgressSnapshot snapshot)
    {
        if (condition == null || snapshot == null)
        {
            return false;
        }

        return Compare(GetCurrentValue(condition, snapshot), condition.op, condition.value);
    }

    public static int GetCurrentValue(TipCondition condition, ProgressSnapshot snapshot)
    {
        switch (condition.kind)
        {
            case TipConditionKind.QuestState:
                return (int)snapshot.GetQuestState(condition.targetId);
            case TipConditionKind.DeathCount:
                return snapshot.DeathCount;
            case TipConditionKind.RecentDeathCount:
                return snapshot.RecentDeathCount;
            case TipConditionKind.KillCount:
                return snapshot.KillCount;
            case TipConditionKind.ToolTier:
                return snapshot.ToolTier;
            case TipConditionKind.NpcDiscovered:
                return snapshot.IsNpcDiscovered(condition.targetId) ? 1 : 0;
            default:
                return 0;
        }
    }

    private static bool Compare(int current, TipCompareOp op, int value)
    {
        switch (op)
        {
            case TipCompareOp.Equal: return current == value;
            case TipCompareOp.NotEqual: return current != value;
            case TipCompareOp.Greater: return current > value;
            case TipCompareOp.GreaterOrEqual: return current >= value;
            case TipCompareOp.Less: return current < value;
            case TipCompareOp.LessOrEqual: return current <= value;
            default: return false;
        }
    }
}
