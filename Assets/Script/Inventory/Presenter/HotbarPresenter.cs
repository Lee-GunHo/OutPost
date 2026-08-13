using System.Collections;
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
        hotbarView.OnSlotClicked += OnHotbarSlotClicked;
        hotbarView.OnSlotHovered += OnHotbarSlotHovered;
        hotbarView.OnSlotUnhovered += OnHotbarSlotUnhovered;
        hotbarView.OnSlotSplitClicked += OnHotbarSlotSplitClicked;

        hotbarView.OnMouseWheelUp += SelectPreviousSlot;
        hotbarView.OnMouseWheelDown += SelectNextSlot;

        hotbarModel.OnHotbarChanged += RefreshView;
        hotbarModel.OnSelectedSlotChanged += hotbarView.MoveSelectedFrame;

        RefreshView();
        RefreshVisibility();
    }

    private IEnumerator Start()
    {
        yield return null;

        hotbarModel.SelectSlot(0);
        hotbarView.MoveSelectedFrame(0);
    }

    private void OnDestroy()
    {
        hotbarView.OnNumberKeyPressed -= SelectSlot;
        hotbarView.OnSlotClicked -= OnHotbarSlotClicked;
        hotbarView.OnSlotHovered -= OnHotbarSlotHovered;
        hotbarView.OnSlotSplitClicked -= OnHotbarSlotSplitClicked;
        hotbarView.OnSlotUnhovered -= OnHotbarSlotUnhovered;

        hotbarView.OnMouseWheelUp -= SelectPreviousSlot;
        hotbarView.OnMouseWheelDown -= SelectNextSlot;


        hotbarModel.OnHotbarChanged -= RefreshView;
        hotbarModel.OnSelectedSlotChanged -= hotbarView.MoveSelectedFrame;

        UIState.OnStateChanged -= RefreshVisibility;
    }

    private void OnHotbarSlotClicked(SlotReference slotReference)
    {
        // 인벤토리 상태와 관계없이 핫바 선택은 변경한다.
        SelectSlot(slotReference.SlotIndex);

        // 인벤토리가 열려 있을 때만 아이템 이동을 시작한다.
        if (inventoryPresenter != null &&
            inventoryPresenter.IsOpen)
        {
            inventoryPresenter.OnItemSlotClicked(slotReference);
        }
    }
    private void OnHotbarSlotHovered(SlotReference slotReference)
    {
        if (inventoryPresenter == null ||
            !inventoryPresenter.IsOpen)
        {
            return;
        }

        inventoryPresenter.OnItemSlotHovered(slotReference);
    }
    private void OnHotbarSlotUnhovered(SlotReference slotReference)
    {
        if (inventoryPresenter != null)
        {
            inventoryPresenter.OnItemSlotUnhovered(slotReference);
        }
    }
    private void OnHotbarSlotSplitClicked(
    SlotReference slotReference)
    {
        // 인벤토리가 열려 있을 때만 핫바 아이템을 나눈다.
        if (inventoryPresenter == null ||
            !inventoryPresenter.IsOpen)
        {
            return;
        }

        inventoryPresenter.OnItemSlotSplitClicked(slotReference);
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

    private void RefreshVisibility()
    {
        bool shouldShow =
            !UIState.IsNPCInteractionOpen &&
            !UIState.IsShopOpen;

        hotbarView.SetVisible(shouldShow);
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

    // 0813 경민 선택 아이템 1개 차감 기능 연결 함수 추가
    public bool ConsumeSelectedItem(int amount = 1)
    {
        return hotbarModel.ConsumeSelectedItem(amount);
    }
}