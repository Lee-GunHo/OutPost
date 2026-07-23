using UnityEngine;

/// <summary>
/// 월드 창고와 플레이어 F키 상호작용 연결
/// </summary>
[RequireComponent(typeof(ChestModel))]
public class ChestInteractable :
    MonoBehaviour,
    IInteractable
{
    [Header("Model")]
    [SerializeField] private ChestModel chestModel;

    [Header("상호작용 표시")]
    [SerializeField] private GameObject interactionMark;

    private PlayerPresenter currentPlayer;
    private bool isPlayerInRange;

    private void Awake()
    {
        if (chestModel == null)
        {
            chestModel =
                GetComponent<ChestModel>();
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

        if (ChestPresenter.Instance != null &&
            ChestPresenter.Instance
                .IsOpenedChest(chestModel))
        {
            ChestPresenter.Instance.Close();
        }
    }

    public void Interact(PlayerPresenter player)
    {
        if (!isPlayerInRange ||
            player == null ||
            player != currentPlayer)
        {
            Debug.Log(
                "플레이어가 창고 상호작용 범위 밖에 있습니다."
            );

            return;
        }

        if (chestModel == null)
        {
            Debug.LogWarning(
                gameObject.name +
                "에 ChestModel이 없습니다."
            );

            return;
        }

        if (ChestPresenter.Instance == null)
        {
            Debug.LogWarning(
                "씬에 ChestPresenter가 없습니다."
            );

            return;
        }

        HideInteractionMark();

        ChestPresenter.Instance.Open(
            chestModel,
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