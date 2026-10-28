using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 물물교환 상점 UI. 돈 대신 몬스터 드랍 재료(Small Mushroom, Poison Mushroom, Spore 등)를 내고 아이템을 받음.
/// 왼쪽 = 교환 목록(받는 아이템 + 필요한 재료), 오른쪽 = 이 상점에서 쓰는 재료의 보유량(인벤토리 + 핫바)
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("전체 상점 패널")]
    [SerializeField] private GameObject panel;

    [Header("상점 이름")]
    [SerializeField] private TMP_Text shopTitleText;

    [Header("상점 아이템 스크롤 Content")]
    [SerializeField] private Transform shopContent;

    [Header("플레이어 교환 재료 스크롤 Content (예전 인벤토리 칸)")]
    [SerializeField] private Transform playerContent;

    [Header("슬롯 프리팹")]
    [SerializeField] private ShopSlotView slotPrefab;

    [Header("슬롯 생성 수량")]
    [SerializeField] private int ShopSlotCount;
    [SerializeField] private int playerSlotCount;

    [Header("교환 팝업")]
    [SerializeField] private ShopQuantityPopup quantityBuyPopup;
    [SerializeField] private ShopConfirmPopup confirmBuyPopup;

    [Header("교환 설정")]
    [Tooltip("한 번에 교환할 수 있는 최대 횟수")]
    [Min(1)]
    [SerializeField] private int maxBuyAmount = 99;

    [Header("버튼")]
    [SerializeField] private Button closeButton;

    private ShopData currentShopData;
    private PlayerPresenter currentPlayer;
    private NPCPresenter currentNPC;
    private InventoryModel currentInventory;

    private readonly List<ShopSlotView> shopSlotViews = new List<ShopSlotView>();
    private readonly List<ShopSlotView> playerSlotViews = new List<ShopSlotView>();
    // 오른쪽 칸에 보여줄 재료(이 상점 교환 목록에 나오는 재료, 처음 나온 순서)
    private readonly List<ItemData> materialItems = new List<ItemData>();

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
        if (!UIState.CanOpenWindow || panel == null)
            return;

        currentShopData = shopData;
        currentPlayer = player;
        currentNPC = npc;
        currentInventory = currentPlayer != null ? currentPlayer.PlayerInventory : null;
        CollectMaterialItems();

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

        if (currentShopData.TradeItems == null)
        {
            Debug.LogWarning("ShopUI 오류: ShopData.TradeItems가 없습니다.");
            return;
        }

        int createCount = Mathf.Max(ShopSlotCount, currentShopData.TradeItems.Count);

        List<ShopSlotView> createdSlots = CreateSlot(shopContent, createCount);

        for (int i = 0; i < createdSlots.Count; i++)
        {
            ShopSlotView slotView = createdSlots[i];

            slotView.Initialize(this, ShopSlotMode.Buy, i);

            if(i >= currentShopData.TradeItems.Count)
            {
                slotView.Clear();
                shopSlotViews.Add(slotView);
                continue;
            }

            ShopItemData shopItem = currentShopData.TradeItems[i];

            if(shopItem == null || shopItem.ItemData == null)
            {
                slotView.Clear();
            }
            else
            {
                // 수량 칸 = 교환 1회에 받는 개수, 아래 줄 = 필요한 재료
                slotView.SetItem(shopItem.ItemData, shopItem.GiveAmount);
                slotView.SetTradeInfo(BuildCostLine(shopItem), GetMaxTradeCount(shopItem) > 0);
            }

            shopSlotViews.Add(slotView);
        }
    }

    private string BuildCostLine(ShopItemData shopItem)
    {
        string line = ShopTradeRules.FormatCosts(shopItem.Costs);
        int remaining = GetRemainingGiveAmount(shopItem);

        if (remaining != ShopGiveLimit.Unlimited)
        {
            line += remaining >= shopItem.GiveAmount ? "  (남은 " + remaining + ")" : "  (품절)";
        }

        return line;
    }

    /// <summary>
    /// 이 상점 아이템을 앞으로 더 받을 수 있는 개수. 한도가 없으면 ShopGiveLimit.Unlimited.
    /// </summary>
    private int GetRemainingGiveAmount(ShopItemData shopItem)
    {
        if (shopItem == null || shopItem.ItemData == null)
            return 0;

        if (shopItem.MaxTotalGive <= 0)
            return ShopGiveLimit.Unlimited;

        return ShopGiveLimit.GetRemaining(
            shopItem.MaxTotalGive,
            ProgressTracker.GetOrCreate().GetShopReceivedAmount(shopItem.ItemData.itemID));
    }

    /// <summary>
    /// 재료 보유량(인벤토리 + 핫바)과 지급 한도로 지금 몇 번 교환할 수 있는지
    /// </summary>
    private int GetMaxTradeCount(ShopItemData shopItem)
    {
        if (shopItem == null || shopItem.ItemData == null)
            return 0;

        return ShopTradeRules.GetMaxTradeCount(
            shopItem.Costs,
            GetOwnedAmount,
            shopItem.GiveAmount,
            GetRemainingGiveAmount(shopItem),
            maxBuyAmount);
    }

    private int GetOwnedAmount(ItemData item)
    {
        // 주운 아이템은 핫바부터 들어가므로 인벤토리와 핫바를 합쳐서 셈
        return PlayerItemReceiver.GetOwnedAmount(currentInventory, PlayerItemReceiver.CurrentHotbar, item);
    }

    private void CollectMaterialItems()
    {
        materialItems.Clear();

        if (currentShopData == null || currentShopData.TradeItems == null)
            return;

        foreach (ShopItemData shopItem in currentShopData.TradeItems)
        {
            if (shopItem == null)
                continue;

            foreach (ShopCost cost in ShopTradeRules.MergeCosts(shopItem.Costs))
            {
                if (!materialItems.Contains(cost.Item))
                {
                    materialItems.Add(cost.Item);
                }
            }
        }
    }

    /// <summary>
    /// 오른쪽 칸: 이 상점에서 쓰는 교환 재료와 현재 보유량(인벤토리 + 핫바 합계)
    /// </summary>
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

        int createCount = Mathf.Max(playerSlotCount, materialItems.Count);

        List<ShopSlotView> createdSlots = CreateSlot(playerContent, createCount);

        for(int i = 0; i < createdSlots.Count; i++)
        {
            ShopSlotView slotView = createdSlots[i];

            slotView.Initialize(this, ShopSlotMode.Sell, i);

            if(i >= materialItems.Count)
            {
                slotView.Clear();
                playerSlotViews.Add(slotView);
                continue;
            }

            ItemData material = materialItems[i];
            int owned = GetOwnedAmount(material);

            // 0개인 재료도 보여줘야 무엇을 모아야 하는지 알 수 있음
            slotView.SetItem(material, owned);
            slotView.SetTradeInfo("보유 " + owned + "개", owned > 0);

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
            return createdSlots;
        }

        if(parent == null)
        {
            Debug.LogError("ShopUI 오류: Content Transform이 연결되지 않았습니다. shopContent 또는 playerContent를 연결해주세요.");
            return createdSlots;
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
        ShopItemData shopItem = GetShopItem(slotIndex);

        if (shopItem == null || shopItem.ItemData == null)
            return;

        selectedItem = shopItem.ItemData;
        selectedAmount = shopItem.GiveAmount;
    }

    private void SelectPlayerSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= materialItems.Count)
            return;

        selectedItem = materialItems[slotIndex];
        selectedAmount = GetOwnedAmount(selectedItem);
    }

    public void OnShopItemDoubleClicked(int slotIndex)
    {
        ShopItemData shopItem = GetShopItem(slotIndex);

        if(shopItem == null || shopItem.ItemData == null)
            return;

        if(quantityBuyPopup == null)
        {
            Debug.LogWarning("교환 수량 팝업이 ShopUI에 연결되지 않았습니다.");
            return;
        }

        CloseAllPopups();

        ItemData item = shopItem.ItemData;

        if (GetRemainingGiveAmount(shopItem) < shopItem.GiveAmount)
        {
            Debug.Log("교환 실패 : " + item.itemName + "은(는) 더 받을 수 없습니다. (총 수령 한도 도달)");
            confirmBuyPopup?.Open(item.itemName + "은(는) 더 이상 받을 수 없습니다.", null);
            return;
        }

        if (GetMaxTradeCount(shopItem) <= 0)
        {
            confirmBuyPopup?.Open(
                "재료가 부족합니다.\n필요 : " + ShopTradeRules.FormatCosts(shopItem.Costs) + "\n" + BuildOwnedLine(shopItem),
                null);
            return;
        }

        quantityBuyPopup.Open(
            item.itemName + " 몇 번 교환하시겠습니까?\n(1회 : " + ShopTradeRules.FormatCosts(shopItem.Costs) + ")",
            GetMaxTradeCount(shopItem),
            tradeCount => OpenBuyConfirmPopup(slotIndex, tradeCount)
        );
    }

    private string BuildOwnedLine(ShopItemData shopItem)
    {
        List<string> parts = new List<string>();

        foreach (ShopCost cost in ShopTradeRules.MergeCosts(shopItem.Costs))
        {
            parts.Add(cost.Item.itemName + " " + GetOwnedAmount(cost.Item));
        }

        return "보유 : " + string.Join(" + ", parts);
    }

    private void OpenBuyConfirmPopup(int slotIndex, int tradeCount)
    {
        ShopItemData shopItem = GetShopItem(slotIndex);

        if(shopItem == null || shopItem.ItemData == null)
            return;

        if(confirmBuyPopup == null)
        {
            Debug.LogWarning("교환 확인 팝업이 ShopUI에 연결되지 않았습니다.");
            return;
        }

        int receiveAmount = shopItem.GiveAmount * tradeCount;
        string message = shopItem.ItemData.itemName + " " + receiveAmount + "개로 교환하시겠습니까?"
            + "\n내는 재료 : " + ShopTradeRules.FormatCosts(shopItem.Costs, tradeCount);

        confirmBuyPopup.Open(
            message,
            () => TradeShopItem(slotIndex, tradeCount)
        );
    }

    private void TradeShopItem(int slotIndex, int tradeCount)
    {
        ShopItemData shopItem = GetShopItem(slotIndex);

        if (shopItem == null || shopItem.ItemData == null)
            return;

        if(currentInventory == null)
        {
            Debug.LogWarning("교환 실패 : 플레이어 InventoryModel이 없습니다.");
            return;
        }

        if (tradeCount <= 0)
            return;

        // 팝업이 열려 있는 사이 재료/한도가 바뀌었을 수 있으니 다시 확인
        if (tradeCount > GetMaxTradeCount(shopItem))
        {
            Debug.Log("교환 실패 : 재료가 부족하거나 총 수령 한도를 넘는 수량입니다.");
            confirmBuyPopup?.Open("재료가 부족합니다.\n" + BuildOwnedLine(shopItem), null);
            return;
        }

        ItemData item = shopItem.ItemData;
        int receiveAmount = shopItem.GiveAmount * tradeCount;
        HotbarModel hotbar = PlayerItemReceiver.CurrentHotbar;

        // 재료를 빼기 전에 확인 (재료가 빠지며 생기는 빈 칸은 계산에 넣지 않음 - 보수적으로 판정)
        if(!PlayerItemReceiver.CanReceive(currentInventory, hotbar, item, receiveAmount))
        {
            Debug.Log("교환 실패 : 인벤토리 공간이 부족합니다.");
            confirmBuyPopup?.Open("인벤토리 공간이 부족합니다.", null);
            return;
        }

        // GetMaxTradeCount로 모든 재료가 충분한지 확인했으므로 중간에 실패하지 않음
        foreach (ShopCost cost in ShopTradeRules.MergeCosts(shopItem.Costs))
        {
            PlayerItemReceiver.RemoveOwned(currentInventory, hotbar, cost.Item, cost.Amount * tradeCount);
        }

        // 핫바 먼저, 남으면 인벤토리
        bool success = PlayerItemReceiver.Give(currentInventory, hotbar, item, receiveAmount);

        if(!success)
        {
            Debug.LogWarning("교환 아이템 지급에 실패했습니다.");
            return;
        }

        Debug.Log(item.itemName + " " + receiveAmount + "개를 교환했습니다. 낸 재료 : "
            + ShopTradeRules.FormatCosts(shopItem.Costs, tradeCount));

        if (shopItem.MaxTotalGive > 0)
        {
            ProgressTracker.GetOrCreate().AddShopReceivedAmount(item.itemID, receiveAmount);
        }

        RefreshShopItems();
        RefreshPlayerItems();
        RefreshSelectedMarks();
    }

    private ShopItemData GetShopItem(int slotIndex)
    {
        if(currentShopData == null || currentShopData.TradeItems == null)
            return null;

        if(slotIndex < 0 || slotIndex >= currentShopData.TradeItems.Count)
            return null;

        return currentShopData.TradeItems[slotIndex];
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

    private void CloseAllPopups()
    {
        quantityBuyPopup?.Close();
        confirmBuyPopup?.Close();
    }

    public void Close()
    {
        NPCPresenter closedNPC = currentNPC;

        if(panel != null)
        {
            panel.SetActive(false);
        }

        UIState.SetShopOpen(false);

        currentShopData = null;
        currentPlayer = null;
        currentNPC = null;
        currentInventory = null;
        materialItems.Clear();

        CloseAllPopups();
        ClearSelectedItem();
        ClearSlotViews(shopSlotViews, shopContent);
        ClearSlotViews(playerSlotViews, playerContent);

        if(closedNPC != null)
        {
            closedNPC.ShowInteractionMarkIfPossible();
        }

        Debug.Log("상점 닫힘: UIState.SetShopOpen(false)");
        UIState.DebugLogState("ShopUI.Close 이후");
    }
}
