using UnityEngine;

/// <summary>
/// NPCModel, NPCView, UI, 퀘스트, 상점 연결하는 스크립트
/// </summary>
public class NPCPresenter : MonoBehaviour, IInteractable
{
    private NPCModel npcModel;

    private NPCView npcView;

    private PlayerPresenter currentPlayer;
    private bool isPlayerInRange;

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

        HideInteractionMark();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerPresenter player = other.GetComponentInParent<PlayerPresenter>();

        if (player == null)
            return;

        currentPlayer = player;
        isPlayerInRange = true;

        ShowInteractionMarkIfPossible();

        Debug.Log("NPC 상호작용 범위 진입");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerPresenter player = other.GetComponentInParent<PlayerPresenter>();
        
        if(player == null) 
            return;

        if(currentPlayer == player)
        {
            currentPlayer = null;
            isPlayerInRange = false;
        }

        HideInteractionMark();

        Debug.Log("NPC 상호작용 범위 이탈");
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

        if (currentPlayer != player)
        {
            Debug.Log("플레이어가 NPC 상호작용 범위 밖에 있습니다.");
            return;
        }

        HideInteractionMark();

        npcView.LookAtPlayer(player.transform);
        //npcView.PlayTalk();

        if (NPCInteractionUI.Instance == null)
        {
            Debug.LogWarning("NPCInteractionUI가 씬에 없습니다.");
            return;
        }

        NPCInteractionUI.Instance.Open(this, player);
    }

    public void ShowInteractionMarkIfPossible()
    {
        if (!isPlayerInRange)
            return;

        if (UIState.IsAnyUIOpen)
            return;

        if (npcView != null)
        {
            npcView.ShowInteractionMark();
        }
    }

    public void HideInteractionMark()
    {
        if(npcView != null)
        {
            npcView.HideInteractionMark();
        }
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
