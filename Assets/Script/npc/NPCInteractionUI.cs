using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// NPC 상호작용 했을 때 열리는 UI
/// </summary>
public class NPCInteractionUI : MonoBehaviour
{
    public static NPCInteractionUI Instance { get; private set; }

    [Header("전체 UI 패널")]
    [SerializeField] private GameObject panel;

    [Header("텍스트 UI")]
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("버튼 UI")]
    [SerializeField] private Button dialogueButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button questButton;
    [SerializeField] private Button closeButton;

    private NPCPresenter currentNPC;
    private PlayerPresenter currentPlayer;

    private int lastDialogueIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Start()
    {
        if (dialogueButton != null)
        {
            dialogueButton.onClick.AddListener(OnDialogueButtonClicked);
        }

        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClicked);
        }

        if (questButton != null)
        {
            questButton.onClick.AddListener(OnQuestButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    /// <summary>
    /// NPCPresenter에서 호출하는 함수
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="player"></param>
    public void Open(NPCPresenter npc, PlayerPresenter player)
    {
        currentNPC = npc;
        currentPlayer = player;

        lastDialogueIndex = -1;

        UIState.SetNPCInteractionOpen(true);

        if(currentNPC != null)
        {
            currentNPC.HideInteractionMark();
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }
        
        if(npcNameText != null && currentNPC != null)
        {
            npcNameText.text = currentNPC.GetNPCName();
        }

        ShowDefaultDialogue();

        if (shopButton != null && currentNPC != null)
        {
            shopButton.gameObject.SetActive(currentNPC.CanTrade());
        }

        if (questButton != null && currentNPC != null)
        {
            questButton.gameObject.SetActive(currentNPC.CanGiveQuest());
        }
    }

    /// <summary>
    /// 대화창이 처음 열렸을 때 기본 문구 표시
    /// </summary>
    private void ShowDefaultDialogue()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "무엇을 하시겠습니까?";
        }
    }

    /// <summary>
    /// 대화 버튼을 눌렀을 때 실행되는 함수
    /// </summary>
    private void OnDialogueButtonClicked()
    {
        if (currentNPC == null)
        {
            return;
        }

        string[] lines = currentNPC.GetDialogueLines();

        if (lines == null || lines.Length == 0)
        {
            if(dialogueText != null)
            {
                dialogueText.text = "대화 내용이 없습니다.";
            }

            return;
        }

        int randomIndex;

        if(lines.Length == 1)
        {
            randomIndex = 0;
        }
        else
        {
            do
            {
                randomIndex = Random.Range(0, lines.Length);
            }
            while (randomIndex == lastDialogueIndex);
        }

        lastDialogueIndex = randomIndex;

        if(dialogueText != null)
        {
            dialogueText.text = lines[randomIndex];
        }
    }

    /// <summary>
    /// 상점 버튼을 눌렀을 때 실행되는 함수
    /// </summary>
    private void OnShopButtonClicked()
    {
        if (currentNPC == null)
        {
            return;
        }

        if (!currentNPC.CanTrade())
        {
            return;
        }

        ShopData shopData = currentNPC.GetShopData();

        if (shopData == null)
        {
            Debug.LogWarning("상점 데이터가 없습니다.");
            return;
        }

        if(ShopUI.Instance == null)
        {
            Debug.LogWarning("씬에 ShopUI가 없습니다.");
            return;
        }
        
        if(panel != null)
        {
            panel.SetActive(false);
        }

        UIState.SetNPCInteractionOpen(false);

        ShopUI.Instance.Open(shopData, currentPlayer, currentNPC);

        Debug.Log(currentNPC.GetNPCName() + "의 상점을 엽니다.");

        if(dialogueText != null)
        {
            dialogueText.text = "상점을 엽니다.";
        }
    }

    /// <summary>
    /// 퀘스트 버튼을 눌렀을 때 실행되는 함수
    /// </summary>
    private void OnQuestButtonClicked()
    {
        if (currentNPC == null)
        {
            return;
        }

        if (!currentNPC.CanGiveQuest())
        {
            return;
        }

        QuestData questData = currentNPC.GetQuestData();

        if (questData == null)
        {
            Debug.LogWarning("퀘스트 데이터가 없습니다.");
            return;
        }

        if (currentNPC.IsQuestCompleted())
        {
            Debug.Log("이미 완료한 퀘스트입니다.");

            if(dialogueText != null)
            {
                dialogueText.text = "이미 완료한 퀘스트 입니다.";
            }

            return;
        }

        if (!currentNPC.IsQuestAccepted())
        {
            currentNPC.AcceptQuest();

            Debug.Log("퀘스트 수락 : " + questData.QuestTitle);

            if(dialogueText != null)
            {
                dialogueText.text = "퀘스트를 수락했습니다.\n" + questData.QuestTitle;
            }

            return;
        }

        Debug.Log("이미 진행 중인 퀘스트입니다. : " + questData.QuestTitle);

        if(dialogueText != null)
        {
            dialogueText.text = "이미 진행 중인 퀘스트입니다. : " + questData.QuestTitle;
        }
    }

    /// <summary>
    /// UI 닫는 함수
    /// </summary>
    public void Close()
    {
        NPCPresenter closedNPC = currentNPC;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        UIState.SetNPCInteractionOpen(false);

        // 현재 상호작용 중인 NPC 정보 비우기
        currentNPC = null;

        // 현재 상호작용 중인 플레이어 정보 비우기
        currentPlayer = null;

        // 대화창을 닫았는데 아직 NPC 범위 안이면 F키 안내 UI 다시 표시
        if(closedNPC != null)
        {
            closedNPC.ShowInteractionMarkIfPossible();
        }
    }
}
