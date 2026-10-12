using System;
using System.Collections.Generic;
using UnityEngine;

public enum TipConditionKind
{
    QuestState,
    DeathCount,
    RecentDeathCount,
    KillCount,
    ToolTier,
    NpcDiscovered
}

public enum TipCompareOp
{
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual
}

/// <summary>
/// 조언 팁의 조건 하나. 종류에 상관없이 "현재 값 (비교 연산) value" 한 가지 모양만 사용.
/// </summary>
[Serializable]
public class TipCondition
{
    public TipConditionKind kind;
    public TipCompareOp op;

    [Tooltip("QuestState: questId / NpcDiscovered: NPCRole 이름(Combat, Shop) / 그 외: 비워둠")]
    public string targetId;

    [Tooltip("QuestState: 0=NotStarted 1=InProgress 2=ReadyToComplete 3=Completed / " +
        "NpcDiscovered: 1=발견 0=미발견 / 그 외: 횟수 또는 등급")]
    public int value;
}

/// <summary>
/// 조언 팁 하나. conditions를 모두 만족하면 후보가 되고, 후보 중 priority가 가장 큰 것이 선택됨.
/// </summary>
[Serializable]
public class AdviceTip
{
    public string tipId;
    public int priority;
    public List<TipCondition> conditions = new List<TipCondition>();

    [TextArea]
    public string text;
}
