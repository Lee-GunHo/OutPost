using UnityEngine;

/// <summary>
/// NPCModel, NPCView, UI, 퀘스트, 상점 연결하는 스크립트
/// </summary>
public class NPCPresenter : MonoBehaviour, IInteractable
{
    private NPCModel npcModel;

    private NPCView npcView;

    private void Awake()
    {
        npcModel = GetComponent<NPCModel>();

        npcView = GetComponent<NPCView>();

        if (npcModel == null)
        {
            Debug.LogWarning(gameObject.name + "에 NPCModel이 없습니다.");
        }

        if (npcView == null)
        {
            Debug.LogWarning(gameObject.name + "에 NPCView가 없습니다.");
        }
    }

    /// <summary>
    /// 플레이어가 이 NPC와 상호작용하면 호출되는 함수
    /// </summary>
    /// <param name="player"></param>
    public void Interact(PlayerPresenter player)
    {
        if (npcModel == null || npcView == null)
        {
            return;
        }

        npcView.LookAtPlayer(player.transform);

        npcView.PlayTalk();

        NPCInteractionUI.Instance.Open(this, player);
    }

    public string GetNPCName()
    {
        return npcModel.NPCName;
    }

    public string[] GetDialogueLines()
    {
        return npcModel.DialogueLines;
    }

    public bool CanTrade()
    {
        return npcModel.CanTrade;
    }

    public ShopData GetShopData()
    {
        return npcModel.ShopData;
    }

    public bool CanGiveQuest()
    {
        return npcModel.CanGiveQuest;
    }

    public QuestData GetQuestData()
    {
        return npcModel.QuestData;
    }

    public bool IsQuestAccepted()
    {
        return npcModel.IsQuestAccepted;
    }

    public bool IsQuestCompleted()
    {
        return npcModel.IsQuestCompleted;
    }

    public void AcceptQuest()
    {
        npcModel.AcceptQuest();
    }

    public void CompleteQuest()
    {
        npcModel.CompleteQuest();
    }
}
