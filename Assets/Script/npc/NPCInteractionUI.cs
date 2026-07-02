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
    private int dialogueIndex;

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
        dialogueIndex = 0;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        ShowCurrentDialogue();

        if (shopButton != null)
        {
            shopButton.gameObject.SetActive(currentNPC.CanTrade());
        }

        if (questButton != null)
        {
            questButton.gameObject.SetActive(currentNPC.CanGiveQuest());
        }
    }

    /// <summary>
    /// 현재 dialogueIndex에 맞는 대화 문장을 표시하는 함수
    /// </summary>
    private void ShowCurrentDialogue()
    {
        if (currentNPC == null)
        {
            return;
        }

        string[] lines = currentNPC.GetDialogueLines();

        if (lines == null || lines.Length == 0)
        {
            if (dialogueText != null)
            {
                dialogueText.text = "대화 내용이 없습니다.";
            }

            return;
        }

        if (dialogueIndex >= lines.Length)
        {
            dialogueIndex = lines.Length - 1;
        }

        if (dialogueText != null)
        {
            dialogueText.text = lines[dialogueIndex];
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
            return;
        }

        dialogueIndex++;

        if (dialogueIndex >= lines.Length)
        {
            dialogueIndex = 0;
        }

        ShowCurrentDialogue();
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

        Debug.Log(currentNPC.GetNPCName() + "의 상점을 엽니다.");
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
            return;
        }

        if (!currentNPC.IsQuestAccepted())
        {
            currentNPC.AcceptQuest();

            Debug.Log("퀘스트 수락 : " + questData.QuestTitle);
            return;
        }

        Debug.Log("이미 진행 중인 퀘스트입니다. : " + questData.QuestTitle);
    }

    /// <summary>
    /// UI 닫는 함수
    /// </summary>
    public void Close()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // 현재 상호작용 중인 NPC 정보 비우기
        currentNPC = null;

        // 현재 상호작용 중인 플레이어 정보 비우기
        currentPlayer = null;

        // 대화 번호 초기화
        dialogueIndex = 0;
    }
}
