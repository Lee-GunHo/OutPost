using UnityEngine;

/// <summary>
/// 제작대 오브젝트의 플레이어 F키 상호작용 처리
/// ChestInteractable과 동일한 방식으로 동작
/// </summary>
[RequireComponent(typeof(CraftingTableModel))]
public class CraftingTableInteractable :
    MonoBehaviour,
    IInteractable
{
    [Header("Model")]
    [SerializeField] private CraftingTableModel craftingTableModel;

    [Header("상호작용 표시")]
    [SerializeField] private GameObject interactionMark;

    private PlayerPresenter currentPlayer;
    private bool isPlayerInRange;

    private void Awake()
    {
        if (craftingTableModel == null)
        {
            craftingTableModel = GetComponent<CraftingTableModel>();
        }

        HideInteractionMark();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerPresenter player =
            other.GetComponentInParent<PlayerPresenter>();

        if (player == null)
            return;

        currentPlayer = player;
        isPlayerInRange = true;

        ShowInteractionMarkIfPossible();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerPresenter player =
            other.GetComponentInParent<PlayerPresenter>();

        if (player == null ||
            player != currentPlayer)
        {
            return;
        }

        currentPlayer = null;
        isPlayerInRange = false;

        HideInteractionMark();

        if (CraftingPresenter.Instance != null &&
            CraftingPresenter.Instance.IsOpenedTable(craftingTableModel))
        {
            CraftingPresenter.Instance.Close();
        }
    }

    public void Interact(PlayerPresenter player)
    {
        if (!isPlayerInRange ||
            player == null ||
            player != currentPlayer)
        {
            Debug.Log("플레이어가 제작대와 상호작용 범위 밖에 있습니다.");
            return;
        }

        if (craftingTableModel == null)
        {
            Debug.LogWarning(
                gameObject.name + "에 CraftingTableModel이 없습니다."
            );
            return;
        }

        if (CraftingPresenter.Instance == null)
        {
            Debug.LogWarning("씬에 CraftingPresenter가 없습니다.");
            return;
        }

        // 이미 이 제작대가 열려있으면 F키로 다시 닫는다
        if (CraftingPresenter.Instance.IsOpenedTable(craftingTableModel))
        {
            CraftingPresenter.Instance.Close();
            return;
        }

        HideInteractionMark();

        CraftingPresenter.Instance.Open(
            craftingTableModel,
            player,
            this
        );
    }

    public void ShowInteractionMarkIfPossible()
    {
        if (!isPlayerInRange)
            return;

        if (UIState.IsAnyUIOpen)
            return;

        if (interactionMark != null)
        {
            interactionMark.SetActive(true);
        }
    }

    public void HideInteractionMark()
    {
        if (interactionMark != null)
        {
            interactionMark.SetActive(false);
        }
    }
}