using UnityEngine;

public class HotbarPresenter : MonoBehaviour
{
    [SerializeField] private HotbarModel hotbarModel;
    [SerializeField] private HotbarView hotbarView;
    [SerializeField] private InventoryPresenter inventoryPresenter;


    private void Awake()
    {
        hotbarModel.Initialize(hotbarView.SlotCount);

        hotbarView.CreateSlots();

        hotbarView.OnNumberKeyPressed += SelectSlot;
        hotbarView.OnSlotClicked += SelectSlot;
        hotbarView.OnMouseWheelUp += SelectPreviousSlot;
        hotbarView.OnMouseWheelDown += SelectNextSlot;

        hotbarModel.OnHotbarChanged += RefreshView;
        hotbarModel.OnSelectedSlotChanged += hotbarView.MoveSelectedFrame;

        RefreshView();
        hotbarModel.SelectSlot(0);
    }

    private void OnDestroy()
    {
        hotbarView.OnNumberKeyPressed -= SelectSlot;
        hotbarView.OnSlotClicked -= SelectSlot;
        hotbarView.OnMouseWheelUp -= SelectPreviousSlot;
        hotbarView.OnMouseWheelDown -= SelectNextSlot;

        hotbarModel.OnHotbarChanged -= RefreshView;
        hotbarModel.OnSelectedSlotChanged -= hotbarView.MoveSelectedFrame;
    }

    public void SetItem(int index, ItemStack itemStack)
    {
        hotbarModel.SetItem(index, itemStack);
    }

    public ItemStack GetSelectedItem()
    {
        return hotbarModel.GetSelectedItem();
    }

    private void SelectSlot(int index)
    {
        if (inventoryPresenter != null)
        {
            inventoryPresenter.OnHotbarSlotClicked(index);
        }

        hotbarModel.SelectSlot(index);
    }

    private void SelectNextSlot()
    {
        hotbarModel.SelectNext();
    }

    private void SelectPreviousSlot()
    {
        hotbarModel.SelectPrevious();
    }

    private void RefreshView()
    {
        hotbarView.Refresh(hotbarModel.Items);
    }

    public bool AddItemToHotbar(ItemStack itemStack)
    {
        if (itemStack == null || itemStack.item == null)
            return false;

        return hotbarModel.AddItem(itemStack);
    }
    public void SetItemToSlot(int slotIndex, ItemStack itemStack)
    {
        hotbarModel.SetItem(slotIndex, itemStack);
    }
    public ItemStack GetItem(int index)
    {
        return hotbarModel.GetItem(index);
    }
}