using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("전체 상점 패널")]
    [SerializeField] private GameObject panel;

    [Header("상점 이름")]
    [SerializeField] private TMP_Text shopTitleText;

    [Header("상점 아이템 스크롤 Content")]
    [SerializeField] private Transform shopContent;

    [Header("플레이어 인벤토리 스크롤 Content")]
    [SerializeField] private Transform playerContent;

    [Header("슬롯 프리팹")]
    [SerializeField] private ShopSlotView slotPrefab;

    [Header("슬롯 생성 수량")]
    [SerializeField] private int ShopSlotCount;
    [SerializeField] private int playerSlotCount;

    [Header("팝업")]
    [SerializeField] private ShopQuantityPopup quantityPopup;
    [SerializeField] private ShopConfirmPopup confirmPopup;

    [Header("버튼")]
    [SerializeField] private Button closeButton;

    private ShopData currentShopData;
    private PlayerPresenter currentPlayer;
    private NPCPresenter currentNPC;
    private InventoryModel currentInventory;

    private readonly List<ShopSlotView> shopSlotViews = new List<ShopSlotView>();
    private readonly List<ShopSlotView> playerSlotViews = new List<ShopSlotView>();

    private ShopSlotMode selectedMode;
    private int selectedSlotIndex = -1;
    private ItemData selectedItem;
    private int selectedAmount;

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
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        ClearSelectedItem();
    }

    public void Open(ShopData shopData, PlayerPresenter player, NPCPresenter npc)
    {
        currentShopData = shopData;
        currentPlayer = player;
        currentNPC = npc;
        currentInventory = currentPlayer != null ? currentPlayer.PlayerInventory : null;

        UIState.SetInventoryOpen(false);
        UIState.SetNPCInteractionOpen(false);

        UIState.SetShopOpen(true);

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (shopTitleText != null)
        {
            string npcName = currentNPC != null ? currentNPC.GetNPCName() : "상점";
            shopTitleText.text = npcName + " 상점";
        }

        ClearSelectedItem();
        RefreshShopItems();
        RefreshPlayerItems();
    }

    private void RefreshShopItems()
    {
        ClearSlotViews(shopSlotViews, shopContent);

        if (currentShopData == null)
        {
            Debug.LogWarning("ShopUI 오류: currentShopData가 없습니다.");
            return;
        }

        if (currentShopData.SellItems == null)
        {
            Debug.LogWarning("ShopUI 오류: ShopData.SellItems가 없습니다.");
            return;
        }

        int createCount = Mathf.Max(ShopSlotCount, currentShopData.SellItems.Count);

        List<ShopSlotView> createdSlots = CreateSlot(shopContent, createCount);
        
        for (int i = 0; i < createdSlots.Count; i++)
        {
            ShopSlotView slotView = createdSlots[i];

            slotView.Initialize(this, ShopSlotMode.Buy, i);

            if(i >= currentShopData.SellItems.Count)
            {
                slotView.Clear();
                shopSlotViews.Add(slotView);
                continue;
            }

            ShopItemData shopItem = currentShopData.SellItems[i];

            if(shopItem == null || shopItem.ItemData == null)
            {
                slotView.Clear();
            }
            else
            {
                slotView.SetItem(shopItem.ItemData, 1);
            }

            shopSlotViews.Add(slotView);
        }
    }

    private void RefreshPlayerItems()
    {
        ClearSlotViews(playerSlotViews, playerContent);

        if (currentPlayer == null)
        {
            Debug.LogWarning("ShopUI 오류: currentPlayer가 없습니다.");
            return;
        }

        if(currentInventory == null)
        {
            Debug.LogWarning("ShopUI 오류: 플레이어에게 InventoryModel이 없습니다.");
            return;
        }

        if (currentInventory.Items == null)
        {
            Debug.LogWarning("ShopUI 오류: InventoryModel.Items가 없습니다.");
            return;
        }

        List<ShopSlotView> createdSlots = CreateSlot(playerContent, currentInventory.Items.Count);

        for(int i = 0; i < createdSlots.Count; i++)
        {
            ShopSlotView slotView = createdSlots[i];

            slotView.Initialize(this, ShopSlotMode.Sell, i);

            if(i >= currentInventory.Items.Count)
            {
                slotView.Clear();
                playerSlotViews.Add(slotView);
                continue;
            }

            ItemStack itemStack = currentInventory.Items[i];

            if(itemStack == null || itemStack.item == null)
            {
                slotView.Clear();
            }
            else
            {
                slotView.SetItem(itemStack.item, itemStack.amount);
            }

            playerSlotViews.Add(slotView);
        }
    }

    private List<ShopSlotView> CreateSlot(Transform parent, int count)
    {
        List<ShopSlotView> createdSlots = new List<ShopSlotView>();

        if(count <= 0)
        {
            Debug.LogWarning("ShopUI 경고 : 생성할 슬롯 수량이 0 이하입니다.");
            return createdSlots;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("ShopUI 오류 : SlotPrefab이 연결되지 않았습니다. Inspector에서 Slot Prefab에 ShopSlotView 프리팹을 넣어주세요.");
            return null;
        }

        if(parent == null)
        {
            Debug.LogError("ShopUI 오류: Content Transform이 연결되지 않았습니다. shopContent 또는 playerContent를 연결해주세요.");
            return null;
        }

        for(int i = 0; i < count; i++)
        {
            ShopSlotView slotView = Instantiate(slotPrefab, parent);

            if(slotView == null)
            {
                Debug.LogError("ShopUI 오류: 생성된 슬롯에 ShopSlotView 컴포넌트가 없습니다.");
                continue;
            }

            slotView.gameObject.SetActive(true);
            createdSlots.Add(slotView);
        }

        return createdSlots;
    }

    private void ClearSlotViews(List<ShopSlotView> slotViews, Transform content)
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            if (slotViews[i] != null)
            {
                Destroy(slotViews[i].gameObject);
            }
        }

        slotViews.Clear();

        if (content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    public void SelectSlot(ShopSlotMode mode, int slotIndex)
    {
        selectedMode = mode;
        selectedSlotIndex = slotIndex;
        selectedItem = null;
        selectedAmount = 0;

        if (mode == ShopSlotMode.Buy)
        {
            SelectShopSlot(slotIndex);
        }
        else
        {
            SelectPlayerSlot(slotIndex);
        }

        RefreshSelectedMarks();
    }

    private void SelectShopSlot(int slotIndex)
    {
        if (currentShopData == null || currentShopData.SellItems == null)
            return;

        if (slotIndex < 0 || slotIndex >= currentShopData.SellItems.Count)
            return;

        ShopItemData shopItem = currentShopData.SellItems[slotIndex];

        if (shopItem == null || shopItem.ItemData == null)
            return;

        selectedItem = shopItem.ItemData;
        selectedAmount = 1;
    }

    private void SelectPlayerSlot(int slotIndex)
    {
        if (currentInventory == null || currentInventory.Items == null)
            return;

        if (slotIndex < 0 || slotIndex >= currentInventory.Items.Count)
            return;

        ItemStack itemStack = currentInventory.Items[slotIndex];

        if (itemStack == null || itemStack.item == null)
            return;

        selectedItem = itemStack.item;
        selectedAmount = GetStackAmount(itemStack);
    }

    public void OnPlayerItemDoubleClicked(int slotIndex)
    {
        if (currentInventory == null || currentInventory.Items == null)
            return;

        if (slotIndex < 0 || slotIndex >= currentInventory.Items.Count)
            return;

        ItemStack itemStack = currentInventory.Items[slotIndex];

        if (itemStack == null || itemStack.item == null)
            return;

        ItemData item = itemStack.item;
        int amount = GetStackAmount(itemStack);

        if (amount <= 1)
        {
            OpenSellConfirmPopup(slotIndex, 1);
            return;
        }

        if (quantityPopup == null)
        {
            Debug.LogWarning("수량 선택 팝업이 연결되지 않았습니다.");
            return;
        }

        quantityPopup.Open(
            item.itemName,
            amount,
            selectedSellAmount =>
            {
                OpenSellConfirmPopup(slotIndex, selectedSellAmount);
            }
        );
    }

    private void OpenSellConfirmPopup(int slotIndex, int amount)
    {
        if (confirmPopup == null)
        {
            Debug.LogWarning("확인 팝업이 연결되지 않았습니다.");
            return;
        }

        ItemStack itemStack = currentInventory.Items[slotIndex];

        if (itemStack == null || itemStack.item == null)
            return;

        ItemData item = itemStack.item;

        string message = item.itemName + " " + amount + "개를 판매하시겠습니까?";

        confirmPopup.Open(
            message,
            () =>
            {
                SellPlayerItem(slotIndex, amount);
            }
        );
    }

    private void SellPlayerItem(int slotIndex, int amount)
    {
        if (currentInventory == null || currentInventory.Items == null)
            return;

        if (slotIndex < 0 || slotIndex >= currentInventory.Items.Count)
            return;

        ItemStack itemStack = currentInventory.Items[slotIndex];

        if (itemStack == null || itemStack.item == null)
            return;

        ItemData item = itemStack.item;
        int currentAmount = GetStackAmount(itemStack);

        if (amount <= 0)
            return;

        if (amount > currentAmount)
        {
            Debug.Log("판매할 아이템 수량이 부족합니다.");
            return;
        }

        bool success = currentInventory.RemoveItem(item, amount);

        if (!success)
        {
            Debug.LogWarning("아이템 판매에 실패했습니다.");
            return;
        }

        Debug.Log(item.itemName + " " + amount + "개를 판매했습니다.");

        RefreshPlayerItems();
    }

    private int GetStackAmount(ItemStack itemStack)
    {
        return itemStack.amount;
    }

    private void RefreshSelectedMarks()
    {
        for (int i = 0; i < shopSlotViews.Count; i++)
        {
            if (shopSlotViews[i] != null)
            {
                shopSlotViews[i].SetSelected(selectedMode == ShopSlotMode.Buy && i == selectedSlotIndex);
            }
        }

        for (int i = 0; i < playerSlotViews.Count; i++)
        {
            if (playerSlotViews[i] != null)
            {
                playerSlotViews[i].SetSelected(selectedMode == ShopSlotMode.Sell && i == selectedSlotIndex);
            }
        }
    }

    private void ClearSelectedItem()
    {
        selectedSlotIndex = -1;
        selectedItem = null;
        selectedAmount = 0;

        RefreshSelectedMarks();
    }

    public void Close()
    {
        NPCPresenter closedNPC = currentNPC;

        if(panel != null)
        {
            panel.SetActive(false);
        }

        UIState.SetShopOpen(false);
        UIState.SetNPCInteractionOpen(false);
        UIState.SetInventoryOpen(false);

        currentShopData = null;
        currentPlayer = null;
        currentNPC = null;
        currentInventory = null;

        ClearSelectedItem();
        ClearSlotViews(shopSlotViews, shopContent);
        ClearSlotViews(playerSlotViews, playerContent);

        if(quantityPopup != null)
        {
            quantityPopup.Close();
        }

        if(confirmPopup != null)
        {
            confirmPopup.Close();
        }

        if(closedNPC != null)
        {
            closedNPC.ShowInteractionMarkIfPossible();
        }

        Debug.Log("상점 닫힘: UIState.SetShopOpen(false)");
        UIState.DebugLogState("ShopUI.Close 이후");
    }
}