using UnityEngine;

/// <summary>
/// NPC의 역할. 조언 팁 대화(Advice), 발견 여부 추적(Shop/Combat)에 사용.
/// </summary>
public enum NPCRole
{
    General,
    Advice,
    Shop,
    Combat
}

/// <summary>
///  NPC 각각의 고정 정보를 저장하는 데이터 파일
/// </summary>
[CreateAssetMenu(fileName = "NPCData", menuName = "NPC/NPC Data")]
public class NPCData : ScriptableObject
{
    [Header("NPC 기본 정보")]
    [SerializeField] private string npcName;
    [SerializeField] private NPCRole role;

    [Header("대화 정보")]
    [TextArea]
    [SerializeField] private string[] dialogueLines;

    [Header("상점 정보")]
    [SerializeField] private bool canTrade;
    [SerializeField] private ShopData shopData;

    [Header("퀘스트 정보")]
    [SerializeField] private bool canGiveQuest;
    [SerializeField] private QuestData questData;
    [Tooltip("순서대로 주는 퀘스트 목록. 앞 퀘스트를 완료하면 다음 퀘스트를 줌. 비어 있으면 questData 하나만 사용")]
    [SerializeField] private QuestData[] questChain;

    public string NPCName => npcName;
    public NPCRole Role => role;
    public string[] DialogueLines => dialogueLines;

    public bool CanTrade => canTrade;
    public ShopData ShopData => shopData;

    public bool CanGiveQuest => canGiveQuest;
    public QuestData QuestData => questData;

    public QuestData[] QuestChain
    {
        get
        {
            if (questChain != null && questChain.Length > 0)
            {
                return questChain;
            }

            return questData != null ? new[] { questData } : new QuestData[0];
        }
    }
}
