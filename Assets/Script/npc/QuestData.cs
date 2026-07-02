using UnityEngine;

/// <summary>
/// 퀘스트 하나의 고정 정보를 저장하는 데이터
/// </summary>
[CreateAssetMenu(fileName = "QuestData", menuName = "NPC/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("퀘스트 기본 정보")]
    [SerializeField] private string questId;
    [SerializeField] private string questTitle;
    [TextArea]
    [SerializeField] private string questDescription;

    [Header("퀘스트 완료 조건")]
    [SerializeField] private ItemData requiredItem;
    [SerializeField] private int requiredAmount;

    [Header("퀘스트 보상")]
    [SerializeField] private ItemData rewardItem;
    [SerializeField] private int rewardAmount;
    [SerializeField] private int rewardGold;

    public string QuestId => questId;
    public string QuestTitle => questTitle;
    public string QuestDescription => questDescription;
    public ItemData RequiredItem => requiredItem;
    public int RequiredAmount => requiredAmount;
    public ItemData RewardItem => rewardItem;
    public int RewardAmount => rewardAmount;
    public int RewardGold => rewardGold;
}
