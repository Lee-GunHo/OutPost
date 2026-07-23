using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ChestModel과 ChestView를 연결
/// </summary>
public class ChestPresenter : MonoBehaviour
{
    public static ChestPresenter Instance
    {
        get;
        private set;
    }

    [Header("View")]
    [SerializeField] private ChestView chestView;

    [Header("Hotbar")]
    [SerializeField] private HotbarPresenter hotbarPresenter;

    private ChestModel currentChest;
    private InventoryModel currentInventory;
    private PlayerPresenter currentPlayer;
    private ChestInteractable currentInteractable;

    private bool isOpen;

    public bool IsOpen => isOpen;
    public ChestModel CurrentChest => currentChest;

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
            Debug.LogError(
                "ChestPresenter 오류: ChestView가 없습니다."
            );

            enabled = false;
            return;
        }

        chestView.Init(this);
    }

    private void OnDestroy()
    {
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
            Debug.LogWarning(
                "창고를 열 수 없습니다: ChestModel이 없습니다."
            );

            return;
        }

        if (player == null ||
            player.PlayerInventory == null)
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

        SubscribeModels();

        isOpen = true;

        /*
         * 기존 UIState에 Chest 상태가 없으므로
         * Inventory 상태를 사용하여 이동과 다른 UI 입력을 차단합니다.
         */
        UIState.SetInventoryOpen(true);

        currentPlayer.StopMove();

        chestView.Show();
        RefreshView();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        ChestInteractable closedInteractable =
            currentInteractable;

        UnsubscribeModels();

        isOpen = false;

        chestView.Hide();

        UIState.SetInventoryOpen(false);

        currentChest = null;
        currentInventory = null;
        currentPlayer = null;
        currentInteractable = null;

        if (closedInteractable != null)
        {
            closedInteractable
                .ShowInteractionMarkIfPossible();
        }
    }

    public void OnSlotClicked(
        ChestSlotArea area,
        int slotIndex,
        PointerEventData.InputButton button,
        bool isShiftPressed)
    {
        if (!isOpen)
            return;

        ItemStack sourceItem =
            GetItem(area, slotIndex);

        if (IsEmpty(sourceItem))
            return;

        // 우클릭: 1개 이동
        if (button ==
            PointerEventData.InputButton.Right)
        {
            Transfer(
                area,
                slotIndex,
                1
            );

            return;
        }

        // Shift + 좌클릭: 전체 이동
        if (button ==
                PointerEventData.InputButton.Left &&
            isShiftPressed)
        {
            Transfer(
                area,
                slotIndex,
                sourceItem.amount
            );
        }
    }

    public void OnSlotHovered(
        ChestSlotArea area,
        int slotIndex)
    {
        if (!isOpen)
            return;

        chestView.ShowTooltip(
            GetItem(area, slotIndex)
        );
    }

    public void OnSlotUnhovered()
    {
        chestView.HideTooltip();
    }

    public bool IsOpenedChest(
        ChestModel chestModel)
    {
        return isOpen &&
               currentChest == chestModel;
    }

    private void Transfer(
        ChestSlotArea sourceArea,
        int sourceIndex,
        int requestedAmount)
    {
        ItemStack sourceItem =
            GetItem(
                sourceArea,
                sourceIndex
            );

        if (IsEmpty(sourceItem))
            return;

        if (requestedAmount <= 0)
            return;

        int moveRequest = Mathf.Min(
            sourceItem.amount,
            requestedAmount
        );

        int remainingAmount;

        if (sourceArea == ChestSlotArea.Chest)
        {
            // 창고에서 플레이어 쪽으로 이동
            remainingAmount = AddToPlayer(
                sourceItem.item,
                moveRequest
            );
        }
        else
        {
            // 인벤토리 또는 핫바에서 창고로 이동
            remainingAmount =
                currentChest.AddItemAndGetRemaining(
                    sourceItem.item,
                    moveRequest
                );
        }

        int movedAmount =
            moveRequest - remainingAmount;

        if (movedAmount <= 0)
        {
            Debug.Log(
                "아이템을 이동할 빈 공간이 없습니다."
            );

            return;
        }

        RemoveFromSource(
            sourceArea,
            sourceIndex,
            movedAmount
        );

        RefreshView();
    }

    private int AddToPlayer(
        ItemData item,
        int amount)
    {
        // 먼저 인벤토리에 넣음
        int remainingAmount =
            AddToInventory(item, amount);

        // 인벤토리가 가득 차면 핫바에 넣음
        if (remainingAmount > 0)
        {
            remainingAmount =
                AddToHotbar(
                    item,
                    remainingAmount
                );
        }

        return remainingAmount;
    }

    private int AddToInventory(
        ItemData item,
        int amount)
    {
        if (currentInventory == null ||
            item == null ||
            amount <= 0)
        {
            return amount;
        }

        int remainingAmount = amount;
        int maxStack = Mathf.Max(
            1,
            item.maxStack
        );

        // 같은 아이템 스택부터 채움
        for (int i = 0;
             i < currentInventory.Items.Count &&
             remainingAmount > 0;
             i++)
        {
            ItemStack stack =
                currentInventory.Items[i];

            if (IsEmpty(stack))
                continue;

            if (stack.item != item)
                continue;

            if (stack.amount >= maxStack)
                continue;

            int addAmount = Mathf.Min(
                maxStack - stack.amount,
                remainingAmount
            );

            currentInventory.SetItemAt(
                i,
                new ItemStack(
                    item,
                    stack.amount + addAmount
                )
            );

            remainingAmount -= addAmount;
        }

        // 빈 슬롯에 추가
        for (int i = 0;
             i < currentInventory.Items.Count &&
             remainingAmount > 0;
             i++)
        {
            if (!IsEmpty(
                currentInventory.Items[i]))
            {
                continue;
            }

            int addAmount = Mathf.Min(
                maxStack,
                remainingAmount
            );

            currentInventory.SetItemAt(
                i,
                new ItemStack(
                    item,
                    addAmount
                )
            );

            remainingAmount -= addAmount;
        }

        return remainingAmount;
    }

    private int AddToHotbar(
        ItemData item,
        int amount)
    {
        if (hotbarPresenter == null ||
            item == null ||
            amount <= 0)
        {
            return amount;
        }

        int remainingAmount = amount;
        int maxStack = Mathf.Max(
            1,
            item.maxStack
        );

        int slotCount =
            chestView.HotbarSlotCount;

        // 기존 스택부터 채움
        for (int i = 0;
             i < slotCount &&
             remainingAmount > 0;
             i++)
        {
            ItemStack stack =
                hotbarPresenter.GetItem(i);

            if (IsEmpty(stack))
                continue;

            if (stack.item != item)
                continue;

            if (stack.amount >= maxStack)
                continue;

            int addAmount = Mathf.Min(
                maxStack - stack.amount,
                remainingAmount
            );

            hotbarPresenter.SetItemToSlot(
                i,
                new ItemStack(
                    item,
                    stack.amount + addAmount
                )
            );

            remainingAmount -= addAmount;
        }

        // 빈 슬롯에 추가
        for (int i = 0;
             i < slotCount &&
             remainingAmount > 0;
             i++)
        {
            if (!IsEmpty(
                hotbarPresenter.GetItem(i)))
            {
                continue;
            }

            int addAmount = Mathf.Min(
                maxStack,
                remainingAmount
            );

            hotbarPresenter.SetItemToSlot(
                i,
                new ItemStack(
                    item,
                    addAmount
                )
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
        switch (sourceArea)
        {
            case ChestSlotArea.Hotbar:
                RemoveFromHotbar(
                    sourceIndex,
                    amount
                );
                break;

            case ChestSlotArea.Inventory:
                RemoveFromInventory(
                    sourceIndex,
                    amount
                );
                break;

            case ChestSlotArea.Chest:
                currentChest.RemoveItemAt(
                    sourceIndex,
                    amount
                );
                break;
        }
    }

    private void RemoveFromInventory(
        int slotIndex,
        int amount)
    {
        if (currentInventory == null)
            return;

        if (slotIndex < 0 ||
            slotIndex >= currentInventory.Items.Count)
        {
            return;
        }

        ItemStack stack =
            currentInventory.Items[slotIndex];

        if (IsEmpty(stack))
            return;

        int remainingAmount =
            stack.amount - amount;

        currentInventory.SetItemAt(
            slotIndex,
            remainingAmount > 0
                ? new ItemStack(
                    stack.item,
                    remainingAmount
                )
                : null
        );
    }

    private void RemoveFromHotbar(
        int slotIndex,
        int amount)
    {
        if (hotbarPresenter == null)
            return;

        if (slotIndex < 0 ||
            slotIndex >= chestView.HotbarSlotCount)
        {
            return;
        }

        ItemStack stack =
            hotbarPresenter.GetItem(slotIndex);

        if (IsEmpty(stack))
            return;

        int remainingAmount =
            stack.amount - amount;

        hotbarPresenter.SetItemToSlot(
            slotIndex,
            remainingAmount > 0
                ? new ItemStack(
                    stack.item,
                    remainingAmount
                )
                : null
        );
    }

    private ItemStack GetItem(
        ChestSlotArea area,
        int slotIndex)
    {
        switch (area)
        {
            case ChestSlotArea.Hotbar:
                if (hotbarPresenter == null)
                    return null;

                if (slotIndex < 0 ||
                    slotIndex >=
                    chestView.HotbarSlotCount)
                {
                    return null;
                }

                return hotbarPresenter
                    .GetItem(slotIndex);

            case ChestSlotArea.Inventory:
                if (currentInventory == null)
                    return null;

                if (slotIndex < 0 ||
                    slotIndex >=
                    currentInventory.Items.Count)
                {
                    return null;
                }

                return currentInventory
                    .Items[slotIndex];

            case ChestSlotArea.Chest:
                if (currentChest == null)
                    return null;

                return currentChest
                    .GetItem(slotIndex);

            default:
                return null;
        }
    }

    private void RefreshView()
    {
        if (!isOpen)
            return;

        if (currentInventory == null ||
            currentChest == null)
        {
            return;
        }

        List<ItemStack> hotbarItems =
            new List<ItemStack>();

        for (int i = 0;
             i < chestView.HotbarSlotCount;
             i++)
        {
            ItemStack hotbarItem =
                hotbarPresenter != null
                    ? hotbarPresenter.GetItem(i)
                    : null;

            hotbarItems.Add(hotbarItem);
        }

        chestView.Refresh(
            hotbarItems,
            currentInventory.Items,
            currentChest.Items
        );
    }

    private void SubscribeModels()
    {
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged +=
                RefreshView;
        }

        if (currentChest != null)
        {
            currentChest.OnChestChanged +=
                RefreshView;
        }
    }

    private void UnsubscribeModels()
    {
        if (currentInventory != null)
        {
            currentInventory.OnInventoryChanged -=
                RefreshView;
        }

        if (currentChest != null)
        {
            currentChest.OnChestChanged -=
                RefreshView;
        }
    }

    private static bool IsEmpty(
        ItemStack itemStack)
    {
        return itemStack == null ||
               itemStack.item == null ||
               itemStack.amount <= 0;
    }
}