using System;
using UnityEngine;

/// <summary>
/// NPC의 데이터를 보관하고
/// 퀘스트 수락 여부, 완료 여부처럼 게임 도중 변하는 상태를 관리
/// </summary>
public class NPCModel : MonoBehaviour
{
    public event Action<string> OnQuestAccepted;
    public event Action<string> OnQuestCompleted;

    [Header("NPC 데이터")]
    [SerializeField] private NPCData npcData;

    public NPCData NPCData => npcData;
    public string NPCName => npcData.NPCName;
    public NPCRole Role => npcData != null ? npcData.Role : NPCRole.General;
    public string[] DialogueLines => npcData.DialogueLines;
    public bool CanTrade => npcData.CanTrade;
    public ShopData ShopData => npcData.ShopData;
    public bool CanGiveQuest => npcData.CanGiveQuest;

    // 퀘스트 목록 중 지금 진행할 차례인 퀘스트(아직 완료하지 않은 첫 퀘스트, 전부 완료했으면 마지막 퀘스트)
    public QuestData QuestData => GetCurrentQuest();
    public QuestData[] QuestChain => npcData != null ? npcData.QuestChain : new QuestData[0];

    // 퀘스트 상태는 QuestSaveManager의 저장 데이터(questId별)를 그대로 읽음
    public bool IsQuestAccepted => LoadQuestState(GetCurrentQuest(), out bool isAccepted, out _) && isAccepted;
    public bool IsQuestCompleted => LoadQuestState(GetCurrentQuest(), out _, out bool isCompleted) && isCompleted;

    private void Awake()
    {
        if (npcData == null)
        {
            Debug.LogWarning(gameObject.name + "에 NPCData가 연결되지 않았습니다.");
        }
    }

    /// <summary>
    /// 퀘스트를 수락했을 때 호출하는 함수
    /// </summary>
    public void AcceptQuest()
    {
        QuestData quest = GetCurrentQuest();

        if (quest == null)
        {
            return;
        }

        LoadQuestState(quest, out _, out bool isCompleted);

        if (isCompleted)
        {
            return;
        }

        QuestSaveManager.GetOrCreate().SaveQuestState(quest.QuestId, true, false);
        OnQuestAccepted?.Invoke(quest.QuestId);
    }

    public void CompleteQuest()
    {
        QuestData quest = GetCurrentQuest();

        if (quest == null)
        {
            return;
        }

        LoadQuestState(quest, out bool isAccepted, out _);

        if (!isAccepted)
        {
            return;
        }

        // 저장하면 '현재 퀘스트'가 다음 퀘스트로 넘어가므로 완료한 퀘스트 ID를 먼저 잡아둠
        string completedQuestId = quest.QuestId;

        QuestSaveManager.GetOrCreate().SaveQuestState(completedQuestId, true, true);
        OnQuestCompleted?.Invoke(completedQuestId);
    }

    private QuestData GetCurrentQuest()
    {
        if (npcData == null)
        {
            return null;
        }

        QuestData lastQuest = null;

        foreach (QuestData quest in npcData.QuestChain)
        {
            if (quest == null)
            {
                continue;
            }

            lastQuest = quest;

            LoadQuestState(quest, out _, out bool isCompleted);

            if (!isCompleted)
            {
                return quest;
            }
        }

        return lastQuest;
    }

    /// <summary>
    /// 저장된 퀘스트 상태(수락/완료 여부)를 불러옴. 저장된 데이터가 없으면 false, false.
    /// </summary>
    private static bool LoadQuestState(QuestData quest, out bool isAccepted, out bool isCompleted)
    {
        isAccepted = false;
        isCompleted = false;

        if (quest == null)
        {
            return false;
        }

        return QuestSaveManager.GetOrCreate().TryLoadQuestState(
            quest.QuestId,
            out isAccepted,
            out isCompleted);
    }
}
