using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조언 NPC의 "조건 → 대사" 팁 목록과 관련 수치. 기획자가 코드 수정 없이 인스펙터에서 조정.
/// NPCTipDialoguePresenter 인스펙터에 연결하거나, Resources/NpcTip/AdviceTipSet 경로에 두면 자동으로 읽음.
/// 둘 다 없으면 코드 기본값(AdviceTipDefaults)으로 동작.
/// </summary>
[CreateAssetMenu(fileName = "AdviceTipSet", menuName = "NPC/Advice Tip Set")]
public class AdviceTipSet : ScriptableObject
{
    public const string ResourcesPath = "NpcTip/AdviceTipSet";

    [Header("팁 목록 (priority 동률이면 위에 있는 팁 우선)")]
    [SerializeField] private List<AdviceTip> tips = AdviceTipDefaults.CreateDefaultTips();

    [Header("기본 대사 (조건을 만족하는 팁이 없을 때)")]
    [TextArea]
    [SerializeField] private string defaultText = AdviceTipDefaults.DefaultText;

    [Header("RecentDeath 초기화")]
    [Tooltip("꺼짐: 전투 NPC 위치 힌트를 말했을 때만 초기화 / 켜짐: 조언 NPC와 대화할 때마다 초기화")]
    [SerializeField] private bool resetRecentDeathOnAnyAdviceTalk;

    [Header("도구 등급 (ToolTier)")]
    [Tooltip("1등급부터 순서대로 도구의 itemID. 인벤토리/핫바에 가진 것 중 가장 높은 등급이 ToolTier(없으면 0)")]
    [SerializeField] private int[] toolTierItemIds = AdviceTipDefaults.CreateDefaultToolTierItemIds();

    [Header("전투 NPC 위치 힌트 ({combatNpcHint})")]
    [SerializeField] private CombatNpcHintSettings combatNpcHint = new CombatNpcHintSettings();

    public IReadOnlyList<AdviceTip> Tips => tips;
    public string DefaultText => defaultText;
    public bool ResetRecentDeathOnAnyAdviceTalk => resetRecentDeathOnAnyAdviceTalk;
    public IReadOnlyList<int> ToolTierItemIds => toolTierItemIds;
    public CombatNpcHintSettings CombatNpcHint => combatNpcHint;
}
