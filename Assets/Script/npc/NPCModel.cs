using UnityEngine;

/// <summary>
/// NPC의 데이터를 보관하고
/// 퀘스트 수락 여부, 완료 여부처럼 게임 도중 변하는 상태를 관리
/// </summary>
public class NPCModel : MonoBehaviour
{
    [Header("NPC 데이터")]
    [SerializeField] private NPCData npcData;

    [Header("퀘스트 진행 상태")]
    private bool isQuestAccepted;
    private bool isQuestCompleted;

    public NPCData NPCData => npcData;
    public string NPCName => npcData.NPCName;
    public string[] DialogueLines => npcData.DialogueLines;
    public bool CanTrade => npcData.CanTrade;
    public ShopData ShopData => npcData.ShopData;
    public bool CanGiveQuest => npcData.CanGiveQuest;
    public QuestData QuestData => npcData.QuestData;
    public bool IsQuestAccepted => isQuestAccepted;
    public bool IsQuestCompleted => isQuestCompleted;

    private void Awake()
    {
        if (npcData == null)
        {
            Debug.LogWarning(gameObject.name + "에 NPCData가 연결되지 않았습니다.");
        }
    }

    private void Start()
    {
        LoadQuestStateFromSave();
    }

    /// <summary>
    /// 퀘스트를 수락했을 때 호출하는 함수
    /// </summary>
    public void AcceptQuest()
    {
        if (isQuestCompleted)
        {
            return;
        }

        isQuestAccepted = true;

        SaveQuestState();
    }

    public void CompleteQuest()
    {
        if (!isQuestAccepted)
        {
            return;
        }

        isQuestCompleted = true;

        SaveQuestState();
    }

    /// <summary>
    /// 저장된 퀘스트 상태(수락/완료 여부)를 불러와서 복원
    /// 저장된 데이터가 없으면 초기 상태(false, false)를 그대로 사용
    /// </summary>
    private void LoadQuestStateFromSave()
    {
        if (npcData == null || npcData.QuestData == null)
        {
            return;
        }

        QuestSaveManager saveManager = QuestSaveManager.GetOrCreate();

        if (saveManager.TryLoadQuestState(
            npcData.QuestData.QuestId,
            out bool savedAccepted,
            out bool savedCompleted))
        {
            isQuestAccepted = savedAccepted;
            isQuestCompleted = savedCompleted;
        }
    }

    /// <summary>
    /// 현재 퀘스트 상태(수락/완료 여부)를 즉시 파일에 저장
    /// </summary>
    private void SaveQuestState()
    {
        if (npcData == null || npcData.QuestData == null)
        {
            return;
        }

        QuestSaveManager.GetOrCreate().SaveQuestState(
            npcData.QuestData.QuestId,
            isQuestAccepted,
            isQuestCompleted
        );
    }
}
