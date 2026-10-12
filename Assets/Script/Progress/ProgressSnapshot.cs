using System.Collections.Generic;

/// <summary>
/// questId별 퀘스트 진행 상태. 팁 조건(TipCondition)의 value로 쓸 때는 이 순서의 정수값(0~3)을 그대로 사용.
/// </summary>
public enum QuestProgressState
{
    NotStarted = 0,
    InProgress = 1,
    ReadyToComplete = 2,
    Completed = 3
}

/// <summary>
/// 한 시점의 플레이어 진행도. 조언 팁 선택(AdviceTipSelector)의 입력으로 쓰는 순수 데이터라
/// MonoBehaviour/씬 없이도 테스트와 에디터 디버그 창에서 직접 만들어 쓸 수 있음.
/// </summary>
public class ProgressSnapshot
{
    public readonly Dictionary<string, QuestProgressState> QuestStates =
        new Dictionary<string, QuestProgressState>();

    public int DeathCount;

    // 전투 NPC 위치 힌트를 마지막으로 들은 이후의 사망 횟수(AdviceTipSet 설정에 따라 조언 대화마다 초기화 가능)
    public int RecentDeathCount;

    public int KillCount;
    public int ToolTier;

    // NPCRole 이름("Combat", "Shop" 등)
    public readonly HashSet<string> DiscoveredNpcIds = new HashSet<string>();

    // 전투 NPC의 대략적인 위치 문구. 계산에 실패했거나 NPC가 없으면 null.
    public string CombatNpcHint;

    public QuestProgressState GetQuestState(string questId)
    {
        if (string.IsNullOrEmpty(questId))
        {
            return QuestProgressState.NotStarted;
        }

        return QuestStates.TryGetValue(questId, out QuestProgressState state)
            ? state
            : QuestProgressState.NotStarted;
    }

    public bool IsNpcDiscovered(string npcId)
    {
        return !string.IsNullOrEmpty(npcId) && DiscoveredNpcIds.Contains(npcId);
    }
}

/// <summary>
/// 저장된 수락/완료 여부 + 인벤토리 보유량으로 퀘스트 상태를 판정하는 규칙.
/// </summary>
public static class QuestProgressRules
{
    public static QuestProgressState Resolve(
        bool isAccepted,
        bool isCompleted,
        int ownedAmount,
        int requiredAmount)
    {
        if (isCompleted)
        {
            return QuestProgressState.Completed;
        }

        if (!isAccepted)
        {
            return QuestProgressState.NotStarted;
        }

        return ownedAmount >= requiredAmount
            ? QuestProgressState.ReadyToComplete
            : QuestProgressState.InProgress;
    }
}

/// <summary>
/// 도구 등급 규칙. tierItemIds[i]가 (i+1)등급 도구의 itemID이고, 가진 것 중 가장 높은 등급을 반환(없으면 0).
/// ItemData.itemGrade가 모든 아이템에서 Normal이라 등급 구분에 쓸 수 없어서 itemID 목록으로 정의함.
/// </summary>
public static class ToolTierRules
{
    public static int Resolve(IReadOnlyList<int> tierItemIds, ICollection<int> ownedItemIds)
    {
        if (tierItemIds == null || ownedItemIds == null)
        {
            return 0;
        }

        for (int i = tierItemIds.Count - 1; i >= 0; i--)
        {
            if (ownedItemIds.Contains(tierItemIds[i]))
            {
                return i + 1;
            }
        }

        return 0;
    }
}

/// <summary>
/// 상점 아이템별 총 수령 한도 규칙. maxTotalGive가 0 이하면 무제한.
/// </summary>
public static class ShopGiveLimit
{
    public const int Unlimited = int.MaxValue;

    public static int GetRemaining(int maxTotalGive, int receivedSoFar)
    {
        if (maxTotalGive <= 0)
        {
            return Unlimited;
        }

        return System.Math.Max(0, maxTotalGive - System.Math.Max(0, receivedSoFar));
    }
}
