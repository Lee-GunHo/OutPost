using UnityEngine;

/// <summary>
/// NPCModel, NPCView, UI, 퀘스트, 상점 연결하는 스크립트
/// </summary>
public class NPCPresenter : MonoBehaviour, IInteractable, IInteractionPromptSource
{
    private NPCModel npcModel;

    private NPCView npcView;

    // 이 NPC 프리팹 안에 있는 상호작용 UI (역할별로 버튼 구성이 다름). 없으면 씬의 공용 UI 사용
    private NPCInteractionUI interactionUI;

    private PlayerPresenter currentPlayer;
    private bool isPlayerInRange;

    public bool CanInteract => isPlayerInRange && npcModel != null && npcView != null;
    public string InteractionDisplayName => npcModel != null && npcModel.NPCData != null ? npcModel.NPCName : gameObject.name;
    public string InteractionActionText => "대화하기";

    private void Awake()
    {
        npcModel = GetComponent<NPCModel>();
        npcView = GetComponent<NPCView>();
        interactionUI = GetComponentInChildren<NPCInteractionUI>(true);

        if (npcModel == null)
        {
            Debug.LogWarning(gameObject.name + "에 NPCModel이 없습니다.");
        }

        if (npcView == null)
        {
            Debug.LogWarning(gameObject.name + "에 NPCView가 없습니다.");
        }

        // 전투 NPC는 동료 FSM이 있어야 따라다니므로, 씬에서 붙이는 걸 잊어도 여기서 붙여줌
        if (npcModel != null && npcModel.Role == NPCRole.Combat &&
            GetComponent<CompanionPresenter>() == null)
        {
            gameObject.AddComponent<CompanionPresenter>();
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

        // 상호작용 범위에 처음 들어온 순간을 "발견"으로 기록 (전투/상점 NPC 발견 여부 추적용)
        if (npcModel != null)
        {
            ProgressTracker.GetOrCreate().MarkNpcDiscovered(npcModel.Role.ToString());
        }

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

        NPCInteractionUI ui = interactionUI != null ? interactionUI : NPCInteractionUI.Instance;

        if (ui == null)
        {
            Debug.LogWarning("NPCInteractionUI가 씬에 없습니다.");
            return;
        }

        ui.Open(this, player);
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

    public NPCRole GetRole()
    {
        return npcModel != null ? npcModel.Role : NPCRole.General;
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
