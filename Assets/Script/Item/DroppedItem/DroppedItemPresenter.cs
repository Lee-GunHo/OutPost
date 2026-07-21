using UnityEngine;

/// <summary>
/// DroppedItemModel과 DroppedItemView를 연결하고
/// 플레이어 인벤토리에 아이템을 추가
/// </summary>
[RequireComponent(typeof(DroppedItemModel))]
[RequireComponent(typeof(DroppedItemView))]
public class DroppedItemPresenter : MonoBehaviour
{
    [Header("MVP")]
    [SerializeField] private DroppedItemModel model;
    [SerializeField] private DroppedItemView view;

    protected DroppedItemModel Model => model;

    protected virtual void Awake()
    {
        ResolveComponents();
        ApplyLegacySerializedData();
    }

    protected virtual void OnEnable()
    {
        ResolveComponents();

        if (view != null)
            view.TriggerEntered += HandleTriggerEntered;
    }

    protected virtual void OnDisable()
    {
        if (view != null)
            view.TriggerEntered -= HandleTriggerEntered;
    }

    /// <summary>
    /// 기존 DroppedItem 클래스의 Inspector 데이터를 Model로 전달하기 위한 확장 지점
    /// </summary>
    protected virtual void ApplyLegacySerializedData()
    {
    }

    private void ResolveComponents()
    {
        if (model == null)
            model = GetComponent<DroppedItemModel>();

        if (model == null)
            model = gameObject.AddComponent<DroppedItemModel>();

        if (view == null)
            view = GetComponent<DroppedItemView>();

        if (view == null)
            view = gameObject.AddComponent<DroppedItemView>();
    }

    private void HandleTriggerEntered(Collider other)
    {
        if (model == null || view == null)
            return;

        if (model.IsPickedUp)
            return;

        PlayerPresenter player = other.GetComponentInParent<PlayerPresenter>();

        if (player == null)
            return;

        InventoryModel inventory = player.PlayerInventory;

        if (inventory == null)
        {
            Debug.LogWarning("플레이어에게 InventoryModel이 연결되어 있지 않습니다.");
            return;
        }

        if (model.ItemData == null)
        {
            Debug.LogWarning(gameObject.name + "에 ItemData가 연결되어 있지 않습니다.");
            return;
        }

        bool success = inventory.AddItem(model.ItemData, model.Amount);

        if (!success)
        {
            Debug.Log("인벤토리가 가득 차서 아이템을 획득하지 못했습니다.");
            return;
        }

        if (!model.TryMarkPickedUp())
            return;

        view.DestroyItem();
    }
}