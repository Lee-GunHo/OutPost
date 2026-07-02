using UnityEngine;

/// <summary>
///  NPC 각각의 고정 정보를 저장하는 데이터 파일
/// </summary>
[CreateAssetMenu(fileName = "NPCData", menuName = "NPC/NPC Data")]
public class NPCData : ScriptableObject
{
    [Header("NPC 기본 정보")]
    [SerializeField] private string npcName;

    [Header("대화 정보")]
    [TextArea]
    [SerializeField] private string[] dialogueLines;

    [Header("상점 정보")]
    [SerializeField] private bool canTrade;
    [SerializeField] private ShopData shopData;

    [Header("퀘스트 정보")]
    [SerializeField] private bool canGiveQuest;
    [SerializeField] private QuestData questData;

    public string NPCName => npcName;
    public string[] DialogueLines => dialogueLines;

    public bool CanTrade => canTrade;
    public ShopData ShopData => shopData;

    public bool CanGiveQuest => canGiveQuest;
    public QuestData QuestData => questData;
}
