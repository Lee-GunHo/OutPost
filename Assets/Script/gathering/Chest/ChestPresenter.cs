using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// ChestModel과 ChestView를 연결하고 아이템 이동, 한 개씩 배치,
/// 삭제 대기 및 삭제 확정을 처리
///
/// 조작 방식
/// - 아이템 슬롯을 우클릭한 채 드래그: 해당 스택 전체를 커서로 듬.
/// - 커서에 아이템이 있을 때 일반 슬롯 좌클릭: 1개를 놓음
/// - 커서에 아이템이 있을 때 쓰레기통 좌클릭: 1개를 삭제 대기로 옮김
/// - 쓰레기통 버튼 클릭: 삭제 대기 아이템을 영구 삭제
/// - 커서에 아이템이 있을 때 슬롯 우클릭: 남은 아이템을 원래 슬롯으로 돌려놓음
/// - 커서가 비어 있을 때 Shift + 좌클릭: 기존 전체 빠른 이동을 수행
/// </summary>
public class ChestPresenter : MonoBehaviour
{
    public static ChestPresenter Instance { get; private set; }

    [Header("View")]
    [SerializeField] private ChestView chestView;

    [Header("플레이어 Hotbar")]
    [SerializeField] private HotbarPresenter hotbarPresenter;

    private ChestModel currentChest;
    private InventoryModel currentInventory;
    private PlayerPresenter currentPlayer;
    private ChestInteractable currentInteractable;

    private bool isOpen;
    private CarriedStack carriedStack;
    private PendingDiscard pendingDiscard;

    public bool IsOpen => isOpen;
    public ChestModel CurrentChest => currentChest;
    public bool IsCarryingItem => carriedStack != null;

    private sealed class SlotAddress
    {
        public ChestSlotArea Area { get; }
        public int Index { get; }

        public SlotAddress(ChestSlotArea area, int index)
        {
            Area = area;
            Index = index;
        }
    }

    private sealed class CarriedStack
    {
        public SlotAddress Origin { get; }
        public ItemData Item { get; }
        public int Amount { get; set; }

        public CarriedStack(
            ChestSlotArea area,
            int index,
            ItemData item,
            int amount)
        {
            Origin = new SlotAddress(area, index);
            Item = item;
            Amount = amount;
        }
    }

    private sealed class PendingDiscard
    {
        public SlotAddress Origin { get; }
        public ItemData Item { get; }
        public int Amount { get; set; }

        public PendingDiscard(
            SlotAddress origin,
            ItemData item)
        {
            Origin = origin;
            Item = item;
            Amount = 0;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (chestView == null)
        {
            chestView = GetComponent<ChestView>();
        }

        if (chestView == null)
        {
            Debug.LogError("ChestPresenter 오류: ChestView가 없습니다.");
            enabled = false;
            return;
        }

        chestView.Init(this);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        bool escapePressed =
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;

        if (escapePressed)
        {
            Close();
        }
    }

    private void OnApplicationQuit()
    {
        if (!isOpen)
            return;

        RestoreTransientItems();

        if (currentChest != null)
        {
            currentChest.ForceSave();
        }
    }

    private void OnDestroy()
    {
        if (isOpen)
        {
            RestoreTransientItems();
        }

        UnsubscribeModels();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Open(
        ChestModel chestModel,
        PlayerPresenter player,
        ChestInteractable interactable)
    {
        if (chestModel == null)
        {
            Debug.LogWarning("창고를 열 수 없습니다: ChestModel이 없습니다.");
            return;
        }

        if (player == null || player.PlayerInventory == null)
        {
            Debug.LogWarning(
                "창고를 열 수 없습니다: 플레이어 InventoryModel이 없습니다."
            );
            return;
        }

        if (isOpen)
        {
            Close();
        }

        currentChest = chestModel;
        currentPlayer = player;
        currentInventory = player.PlayerInventory;
        currentInteractable = interactable;

        ClearCarriedState();
        ClearPendingDiscard();
        SubscribeModels();

        isOpen = true;

        UIState.SetInventoryOpen(true);
        currentPlayer.StopMove();

        chestView.Show();
        RefreshView();
    }

    public void Close()
    {
        Debug.Log(
            $"[닫기 버튼] ChestPresenter.Close 호출 / isOpen : {isOpen}"
        );

        if (!isOpen)
            return;

        ChestInteractable closedInteractable = currentInteractable;

        RestoreTransientItems();
        UnsubscribeModels();

        isOpen = false;

        chestView.Hide();

        Debug.Log(
            $"[닫기 버튼] ChestView.Hide 실행 완료"
        );

        UIState.SetInventoryOpen(false);

        currentChest = null;
        currentInventory = null;
        currentPlayer = null;
        currentInteractable = null;

        if (closedInteractable != null)
        {
            closedInteractable.ShowInteractionMarkIfPossible();
        }
    }

    /// <summary>
    /// 일반 슬롯 클릭 입력
    /// 아이템을 들고 있다면 좌클릭으로 1개 배치
    /// </summary>
    public void OnSlotClicked(
        ChestSlotArea area,
        int slotIndex,
        PointerEventData.InputButton button,
        bool isShiftPressed)
    {
        if (!isOpen)
            return;

        if (carriedStack != null)
        {
            if (button == PointerEventData.InputButton.Left)
            {
                PlaceOneIntoSlot(area, slotIndex);
                return;
            }

            if (button == PointerEventData.InputButton.Right)
            {
                ReturnCarriedToOrigin();
                return;
            }

            return;
        }

        // 삭제 대기 아이템이 있으면 삭제 또는 창 닫기로 먼저 처리해야 함
        // 이를 통해 삭제 취소 시 원래 슬롯으로 안전하게 복원할 수 있음
        if (pendingDiscard != null)
        {
            Debug.Log(
                "삭제 대기 아이템이 있습니다. 삭제 버튼을 누르거나 창고를 닫아 취소하세요."
            );
            return;
        }

        if (button == PointerEventData.InputButton.Left && isShiftPressed)
        {
            ItemStack sourceItem = GetItem(area, slotIndex);

            if (!IsEmpty(sourceItem))
            {
                Transfer(area, slotIndex, sourceItem.amount);
            }
        }
    }

    public void OnSlotHovered(ChestSlotArea area, int slotIndex)
    {
        if (!isOpen || carriedStack != null)
            return;

        chestView.ShowTooltip(GetItem(area, slotIndex));
    }

    public void OnSlotUnhovered()
    {
        chestView.HideTooltip();
    }

    /// <summary>
    /// 아이템이 있는 슬롯에서 우클릭 드래그를 시작하면
    /// 원본 스택 전체를 커서로 옮김
    /// </summary>
    public void OnRightDragStarted(ChestSlotArea area, int slotIndex)
    {
        if (!isOpen || carriedStack != null)
            return;

        if (pendingDiscard != null)
        {
            Debug.Log(
                "삭제 대기 아이템을 먼저 삭제하거나 창고를 닫아 취소해야 합니다."
            );
            return;
        }

        ItemStack sourceItem = GetItem(area, slotIndex);

        if (IsEmpty(sourceItem))
            return;

        carriedStack = new CarriedStack(
            area,
            slotIndex,
            sourceItem.item,
            sourceItem.amount
        );

        // 원본 슬롯에서 스택을 제거하고 커서가 실제 수량을 보관
        SetSlotItem(area, slotIndex, null);

        chestView.HideTooltip();
        RefreshCarriedItemView();
        RefreshView();
    }

    /// <summary>
    /// 같은 아이템 스택을 합치고 빈 슬롯을 뒤로 이동
    /// </summary>
    public void OnGatherItemsClicked()
    {
        if(!CanSortChest())
            return;

        currentChest.GartherItems();
        chestView.CloseSortPopup();

        Debug.Log("창고 아이템을 위로 모았습니다.");
    }

    /// <summary>
    /// 타입, 희귀도, 아이템 ID 순으로 정렬
    /// </summary>
    public void OnSortByTypeClicked()
    {
        if(!CanSortChest())
            return;

        currentChest.SortItemsByType();
        chestView.CloseSortPopup();

        Debug.Log("창고 아이템을 종류별로 정렬했습니다.");
    }

    private bool CanSortChest()
    {
        if(!isOpen || currentChest == null)
            return false;

        if(carriedStack != null)
        {
            Debug.Log(
                "커서에 들고 있는 아이템을 먼저 놓아야 정렬할 수 있습니다."
            );

            return false;
        }

        if(pendingDiscard != null)
        {
            Debug.Log(
                "삭제 대기 아이템을 먼저 삭제하거나 취소해야 정렬할 수 있습니다."
            );

            return false;
        }

        return true;
    }

    /// <summary>
    /// 쓰레기통 슬롯 좌클릭 시 커서 아이템 1개를 삭제 대기로 이동
    /// </summary>
    public void OnDiscardSlotClicked(
        PointerEventData.InputButton button)
    {
        if (!isOpen ||
            button != PointerEventData.InputButton.Left ||
            carriedStack == null)
        {
            return;
        }

        if (pendingDiscard == null)
        {
            pendingDiscard = new PendingDiscard(
                carriedStack.Origin,
                carriedStack.Item
            );
        }

        if (pendingDiscard.Item != carriedStack.Item)
        {
            Debug.Log(
                "쓰레기통 슬롯에는 한 종류의 아이템만 넣을 수 있습니다."
            );
            return;
        }

        pendingDiscard.Amount++;
        carriedStack.Amount--;

        RefreshPendingDiscardView();
        FinishCarriedStackIfEmpty();
    }

    /// <summary>
    /// 쓰레기통 버튼을 누르면 삭제 대기 아이템을 영구 삭제
    /// 삭제 대기 수량은 이미 원본 저장소에서 빠진 상태이므로
    /// 여기서는 복원 정보를 제거하는 것으로 삭제가 확정
    /// </summary>
    public void OnDiscardButtonClicked()
    {
        if (!isOpen || pendingDiscard == null)
            return;

        ItemData discardedItem = pendingDiscard.Item;
        int discardedAmount = pendingDiscard.Amount;

        ClearPendingDiscard();

        Debug.Log(
            $"아이템 삭제: {discardedItem.itemName} x{discardedAmount}"
        );

        if (currentChest != null)
        {
            currentChest.ForceSave();
        }
    }

    public bool IsOpenedChest(ChestModel chestModel)
    {
        return isOpen && currentChest == chestModel;
    }

    private void PlaceOneIntoSlot(
        ChestSlotArea targetArea,
        int targetIndex)
    {
        if (carriedStack == null || carriedStack.Amount <= 0)
            return;

        ItemStack targetItem = GetItem(targetArea, targetIndex);

        if (IsEmpty(targetItem))
        {
            SetSlotItem(
                targetArea,
                targetIndex,
                new ItemStack(carriedStack.Item, 1)
            );
        }
        else
        {
            if (targetItem.item != carriedStack.Item)
            {
                Debug.Log("다른 종류의 아이템 위에는 놓을 수 없습니다.");
                return;
            }

            int maxStack = Mathf.Max(1, carriedStack.Item.maxStack);

            if (targetItem.amount >= maxStack)
            {
                Debug.Log("해당 슬롯의 아이템 스택이 가득 찼습니다.");
                return;
            }

            SetSlotItem(
                targetArea,
                targetIndex,
                new ItemStack(
                    targetItem.item,
                    targetItem.amount + 1
                )
            );
        }

        carriedStack.Amount--;
        FinishCarriedStackIfEmpty();
        RefreshView();
    }

    private void FinishCarriedStackIfEmpty()
    {
        if (carriedStack == null)
            return;

        if (carriedStack.Amount <= 0)
        {
            ClearCarriedState();
        }
        else
        {
            RefreshCarriedItemView();
        }
    }

    private void RefreshCarriedItemView()
    {
        if (carriedStack == null || carriedStack.Amount <= 0)
        {
            chestView.HideCarriedItem();
            return;
        }

        chestView.ShowCarriedItem(
            new ItemStack(
                carriedStack.Item,
                carriedStack.Amount
            )
        );
    }

    private void RefreshPendingDiscardView()
    {
        if (pendingDiscard == null || pendingDiscard.Amount <= 0)
        {
            chestView.ClearDiscardItem();
            return;
        }

        chestView.SetDiscardItem(
            new ItemStack(
                pendingDiscard.Item,
                pendingDiscard.Amount
            )
        );
    }

    private void ReturnCarriedToOrigin()
    {
        if (carriedStack == null)
            return;

        bool restored = AddToOriginSlot(
            carriedStack.Origin,
            carriedStack.Item,
            carriedStack.Amount
        );

        if (!restored)
        {
            Debug.LogError(
                "커서 아이템을 원래 슬롯으로 되돌리지 못했습니다."
            );
            return;
        }

        ClearCarriedState();
        RefreshView();
    }

    private void RestoreTransientItems()
    {
        if (carriedStack != null)
        {
            bool restored = AddToOriginSlot(
                carriedStack.Origin,
                carriedStack.Item,
                carriedStack.Amount
            );

            if (!restored)
            {
                Debug.LogError(
                    "창고를 닫는 중 커서 아이템 복원에 실패했습니다."
                );
            }
        }

        if (pendingDiscard != null)
        {
            bool restored = AddToOriginSlot(
                pendingDiscard.Origin,
                pendingDiscard.Item,
                pendingDiscard.Amount
            );

            if (!restored)
            {
                Debug.LogError(
                    "창고를 닫는 중 삭제 대기 아이템 복원에 실패했습니다."
                );
            }
        }

        ClearCarriedState();
        ClearPendingDiscard();
        RefreshView();
    }

    /// <summary>
    /// 우클릭 드래그를 시작한 원래 슬롯에 수량을 복원
    /// 이 기능이 동작하는 동안 다른 아이템 스택을 새로 들 수 없으므로
    /// 원래 슬롯은 항상 비어 있거나 같은 아이템만 포함
    /// </summary>
    private bool AddToOriginSlot(
        SlotAddress origin,
        ItemData item,
        int amount)
    {
        if (origin == null || item == null || amount <= 0)
            return true;

        ItemStack originItem = GetItem(origin.Area, origin.Index);

        if (IsEmpty(originItem))
        {
            SetSlotItem(
                origin.Area,
                origin.Index,
                new ItemStack(item, amount)
            );
            return true;
        }

        if (originItem.item != item)
            return false;

        int maxStack = Mathf.Max(1, item.maxStack);

        if (originItem.amount + amount > maxStack)
            return false;

        SetSlotItem(
            origin.Area,
            origin.Index,
            new ItemStack(
                item,
                originItem.amount + amount
            )
        );

        return true;
    }

    private void ClearCarriedState()
    {
        carriedStack = null;

        if (chestView != null)
        {
            chestView.HideCarriedItem();
        }
    }

    private void ClearPendingDiscard()
    {
        pendingDiscard = null;

        if (chestView != null)
        {
            chestView.ClearDiscardItem();
        }
    }

    // 기존 Shift + 좌클릭 전체 이동 기능
    private void Transfer(
        ChestSlotArea sourceArea,
        int sourceIndex,
        int requestedAmount)
    {
        ItemStack sourceItem = GetItem(sourceArea, sourceIndex);

        if (IsEmpty(sourceItem) || requestedAmount <= 0)
            return;

        int moveRequest = Mathf.Min(
            sourceItem.amount,
            requestedAmount
        );

        int remainingAmount;

        if (sourceArea == ChestSlotArea.Chest)
        {
            remainingAmount = AddToPlayer(
                sourceItem.item,
                moveRequest
            );
        }
        else
        {
            remainingAmount = currentChest.AddItemAndGetRemaining(
                sourceItem.item,
                moveRequest
            );
        }

        int movedAmount = moveRequest - remainingAmount;

        if (movedAmount <= 0)
        {
            Debug.Log("아이템을 이동할 빈 공간이 없습니다.");
            return;
        }

        RemoveFromSource(
            sourceArea,
            sourceIndex,
            movedAmount
        );

        RefreshView();
    }

    private int AddToPlayer(ItemData item, int amount)
    {
        int remainingAmount = AddToInventory(item, amount);

        if (remainingAmount > 0)
        {
            remainingAmount = AddToHotbar(item, remainingAmount);
        }

        return remainingAmount;
    }

    private int AddToInventory(ItemData item, int amount)
    {
        if (currentInventory == null || item == null || amount <= 0)
            return amount;

        int remainingAmount = amount;
        int maxStack = Mathf.Max(1, item.maxStack);

        for (int i = 0;
             i < currentInventory.Items.Count && remainingAmount > 0;
             i++)
        {
            ItemStack stack = currentInventory.Items[i];

            if (IsEmpty(stack) ||
                stack.item != item ||
                stack.amount >= maxStack)
            {
                continue;
            }

            int addAmount = Mathf.Min(
                maxStack - stack.amount,
                remainingAmount
            );

            currentInventory.SetItemAt(
                i,
                new ItemStack(item, stack.amount + addAmount)
            );

            remainingAmount -= addAmount;
        }

        for (int i = 0;
             i < currentInventory.Items.Count && remainingAmount > 0;
             i++)
        {
            if (!IsEmpty(currentInventory.Items[i]))
                continue;

            int addAmount = Mathf.Min(maxStack, remainingAmount);

            currentInventory.SetItemAt(
                i,
                new ItemStack(item, addAmount)
            );

            remainingAmount -= addAmount;
        }

        return remainingAmount;
    }

    private int AddToHotbar(ItemData item, int amount)
    {
        if (hotbarPresenter == null || item == null || amount <= 0)
            return amount;

        int remainingAmount = amount;
        int maxStack = Mathf.Max(1, item.maxStack);
        int slotCount = chestView.HotbarSlotCount;

        for (int i = 0; i < slotCount && remainingAmount > 0; i++)
        {
            ItemStack stack = hotbarPresenter.GetItem(i);

            if (IsEmpty(stack) ||
                stack.item != item ||
                stack.amount >= maxStack)
            {
                continue;
            }

            int addAmount = Mathf.Min(
                maxStack - stack.amount,
                remainingAmount
            );

            hotbarPresenter.SetItemToSlot(
                i,
                new ItemStack(item, stack.amount + addAmount)
            );

            remainingAmount -= addAmount;
        }

        for (int i = 0; i < slotCount && remainingAmount > 0; i++)
        {
            if (!IsEmpty(hotbarPresenter.GetItem(i)))
                continue;

            int addAmount = Mathf.Min(maxStack, remainingAmount);

            hotbarPresenter.SetItemToSlot(
                i,
                new ItemStack(item, addAmount)
            );

            remainingAmount -= addAmount;
        }

        return remainingAmount;
    }

    private void RemoveFromSource(
        ChestSlotArea sourceArea,
        int sourceIndex,
        int amount)
    {
        if (amount <= 0)
            return;

        switch (sourceArea)
        {
            case ChestSlotArea.Hotbar:
                RemoveFromHotbar(sourceIndex, amount);
                break;

            case ChestSlotArea.Inventory:
                RemoveFromInventory(sourceIndex, amount);
                break;

            case ChestSlotArea.Chest:
                if (currentChest != null)
                {
                    currentChest.RemoveItemAt(sourceIndex, amount);
                }
                break;
        }
    }

    private void RemoveFromInventory(int slotIndex, int amount)
    {
        if (currentInventory == null ||
            slotIndex < 0 ||
            slotIndex >= currentInventory.Items.Count)
        {
            return;
        }

        ItemStack stack = currentInventory.Items[slotIndex];

        if (IsEmpty(stack))
            return;

        int remainingAmount = stack.amount - amount;

        currentInventory.SetItemAt(
            slotIndex,
            remainingAmount > 0
                ? new ItemStack(stack.item, remainingAmount)
                : null
        );
    }

    private void RemoveFromHotbar(int slotIndex, int amount)
    {
        if (hotbarPresenter == null ||
            slotIndex < 0 ||
            slotIndex >= chestView.HotbarSlotCount)
        {
            return;
        }

        ItemStack stack = hotbarPresenter.GetItem(slotIndex);

        if (IsEmpty(stack))
            return;

        int remainingAmount = stack.amount - amount;

        hotbarPresenter.SetItemToSlot(
            slotIndex,
            remainingAmount > 0
                ? new ItemStack(stack.item, remainingAmount)
                : null
        );
    }

    private ItemStack GetItem(ChestSlotArea area, int slotIndex)
    {
        switch (area)
        {
            case ChestSlotArea.Hotbar:
                if (hotbarPresenter == null ||
                    slotIndex < 0 ||
                    slotIndex >= chestView.HotbarSlotCount)
                {
                    return null;
                }

                return hotbarPresenter.GetItem(slotIndex);

            case ChestSlotArea.Inventory:
                if (currentInventory == null ||
                    slotIndex < 0 ||
                    slotIndex >= currentInventory.Items.Count)
                {
                    return null;
                }

                return currentInventory.Items[slotIndex];

            case ChestSlotArea.Chest:
                return currentChest != null
                    ? currentChest.GetItem(slotIndex)
                    : null;

            default:
                return null;
        }
    }

    private void SetSlotItem(
        ChestSlotArea area,
        int slotIndex,
        ItemStack itemStack)
    {
        ItemStack copiedStack = CloneStack(itemStack);

        switch (area)
        {
            case ChestSlotArea.Hotbar:
                if (hotbarPresenter != null &&
                    slotIndex >= 0 &&
                    slotIndex < chestView.HotbarSlotCount)
                {
                    hotbarPresenter.SetItemToSlot(slotIndex, copiedStack);
                }
                break;

            case ChestSlotArea.Inventory:
                if (currentInventory != null &&
                    slotIndex >= 0 &&
                    slotIndex < currentInventory.Items.Count)
                {
                    currentInventory.SetItemAt(slotIndex, copiedStack);
                }
                break;

            case ChestSlotArea.Chest:
                if (currentChest != null)
                {
                    currentChest.SetItemAt(slotIndex, copiedStack);
                }
                break;
        }
    }

    private void RefreshView()
    {
        if (!isOpen || currentInventory == null || currentChest == null)
            return;

        List<ItemStack> hotbarItems = new List<ItemStack>();

        for (int i = 0; i < chestView.HotbarSlotCount; i++)
        {
            hotbarItems.Add(
                hotbarPresenter != null
                    ? hotbarPresenter.GetItem(i)
                    : null
            );
        }

        chestView.Refresh(
            hotbarItems,
            currentInventory.Items,
            currentChest.Items
        );

        RefreshCarriedItemView();
        RefreshPendingDiscardView();
    }

    private void SubscribeModels()
    {
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged += RefreshView;
        }

        if (currentChest != null)
        {
            currentChest.OnChestChanged += RefreshView;
        }
    }

    private void UnsubscribeModels()
    {
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged -= RefreshView;
        }

        if (currentChest != null)
        {
            currentChest.OnChestChanged -= RefreshView;
        }
    }

    private static bool IsEmpty(ItemStack itemStack)
    {
        return itemStack == null ||
               itemStack.item == null ||
               itemStack.amount <= 0;
    }

    private static ItemStack CloneStack(ItemStack itemStack)
    {
        if (IsEmpty(itemStack))
            return null;

        return new ItemStack(
            itemStack.item,
            itemStack.amount
        );
    }

    /// <summary>
    /// 인벤토리 아이템 중 창고에 이미 같은 종류가 존재하는 아이템을
    /// 가능한 수량만큼 모두 창고로 이동
    ///
    /// 같은 아이템이 창고에 하나라도 존재하면 자동 보관 대상이 되며,
    /// 기존 스택을 채운 후 창고의 빈 슬롯에도 새로운 스택을 생성
    /// </summary>
    public void OnAutoStoreClicked()
    {
        if (!CanUseAutoStore())
            return;

        int totalMovedAmount = 0;

        for (int inventoryIndex = 0;
             inventoryIndex < currentInventory.Items.Count;
             inventoryIndex++)
        {
            ItemStack inventoryStack =
                currentInventory.Items[inventoryIndex];

            if (IsEmpty(inventoryStack))
                continue;

            ItemData item = inventoryStack.item;

            // 창고에 같은 종류의 아이템이 없으면 자동 보관하지 않음
            if (!ChestContainsItem(item))
                continue;

            int originalAmount = inventoryStack.amount;

            // 기존 스택을 채운 뒤 빈 슬롯까지 사용하여 전부 보관 시도
            int remainingAmount =
                currentChest.AddItemAndGetRemaining(
                    item,
                    originalAmount
                );

            int movedAmount =
                originalAmount - remainingAmount;

            if (movedAmount <= 0)
                continue;

            totalMovedAmount += movedAmount;

            currentInventory.SetItemAt(
                inventoryIndex,
                remainingAmount > 0
                    ? new ItemStack(item, remainingAmount)
                    : null
            );
        }

        RefreshView();

        if (totalMovedAmount > 0)
        {
            Debug.Log(
                $"자동 보관 완료: 총 {totalMovedAmount}개의 아이템을 이동했습니다."
            );
        }
        else
        {
            Debug.Log(
                "자동 보관할 아이템이 없거나 창고 공간이 부족합니다."
            );
        }
    }

    /// <summary>
    /// 창고에 해당 아이템이 하나라도 존재하는지 확인
    /// </summary>
    private bool ChestContainsItem(ItemData targetItem)
    {
        if (currentChest == null || targetItem == null)
            return false;

        for (int i = 0; i < currentChest.Items.Count; i++)
        {
            ItemStack chestStack =
                currentChest.Items[i];

            if (IsEmpty(chestStack))
                continue;

            if (chestStack.item == targetItem)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 자동 보관을 실행할 수 있는 상태인지 검사
    /// </summary>
    private bool CanUseAutoStore()
    {
        if (!isOpen ||
            currentChest == null ||
            currentInventory == null)
        {
            return false;
        }

        if (carriedStack != null)
        {
            Debug.Log(
                "커서에 들고 있는 아이템을 먼저 놓아야 자동 보관할 수 있습니다."
            );

            return false;
        }

        if (pendingDiscard != null)
        {
            Debug.Log(
                "삭제 대기 아이템을 먼저 삭제하거나 취소해야 자동 보관할 수 있습니다."
            );

            return false;
        }

        return true;
    }
}