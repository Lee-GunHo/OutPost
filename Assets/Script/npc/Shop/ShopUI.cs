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

    [Header("선택 정보 패널")]
    [SerializeField] private TMP_Text selectedModeText;
    [SerializeField] private TMP_Text selectedItemNameText;
    [SerializeField] private TMP_Text selectedAmountText;

    [Header("구매 슬롯 목록")]
    [SerializeField] private TempItemSlotView[] buySlots;

    [Header("판매 슬롯 목록")]
    [SerializeField] private TempItemSlotView[] sellSlots;

    [Header("수량 설정")]
    [SerializeField] private int minAmount = 1;
    [SerializeField] private int maxAmount = 99;

    [Header("버튼")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button closeButton;

    private ShopData currentShopData;
    private PlayerPresenter currentPlayer;
    private NPCPresenter currentNPC;

    private List<ShopItemData> currentBuyItems = new List<ShopItemData>();
    private List<ShopItemData> currentSellItems = new List<ShopItemData>();

    private int[] buyAmounts;
    private int[] sellAmounts;

    private ShopSlotMode selectedMode;
    private int selectedSlotIndex = -1;
    private ShopItemData selectedShopItem;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if(panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Start()
    {
        if(buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        if(sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        InitializeSlots();
        InitializeAmountArrays();
        ClearSelectedItem();
        ClearAllSlots();
    }

    private void InitializeSlots()
    {
        if(buySlots != null)
        {
            for(int i = 0; i < buySlots.Length; i++)
            {
                if (buySlots[i] != null)
                {
                    buySlots[i].InitializeShopSlot(this, i, ShopSlotMode.Buy);
                }
            }
        }

        if(sellSlots != null)
        {
            for(int i = 0; i< sellSlots.Length; i++)
            {
                if (sellSlots[i] != null)
                {
                    sellSlots[i].InitializeShopSlot(this, i, ShopSlotMode.Sell);
                }
            }
        }
    }

    private void InitializeAmountArrays()
    {
        buyAmounts = new int[buySlots != null ? buySlots.Length : 0];
        sellAmounts = new int[sellSlots != null ? sellSlots.Length : 0];

        for(int i = 0; i < buyAmounts.Length; i++)
        {
            buyAmounts[i] = 1;
        }

        for(int i = 0; i < sellAmounts.Length; i++)
        {
            sellAmounts[i] = 1;
        }
    }

    public void Open(ShopData shopData, PlayerPresenter player, NPCPresenter npc)
    {
        currentShopData = shopData;
        currentPlayer = player;
        currentNPC = npc;

        UIState.SetShopOpen(true);

        if(panel != null)
        {
            panel.SetActive(true);
        }

        if(shopTitleText != null && currentNPC != null)
        {
            shopTitleText.text = currentNPC.GetNPCName() + "상점";
        }

        InitializeAmountArrays();
        ClearSelectedItem();
        RefreshBuySlots();
        RefreshSellSlots();
    }

    private void RefreshBuySlots()
    {
        ClearBuySlots();

        currentBuyItems.Clear();

        if (currentShopData == null || currentShopData.ShopSellItems == null)
            return;

        currentBuyItems = currentShopData.ShopSellItems;

        for(int i = 0; i < buySlots.Length; i++)
        {
            if (i >= currentBuyItems.Count)
                break;

            ShopItemData shopItem = currentBuyItems[i];

            if(shopItem == null || shopItem.ItemData == null)
                continue;

            buySlots[i].SetItem(shopItem.ItemData, 1);
            buySlots[i].SetSelected(false);
        }
    }

    private void RefreshSellSlots()
    {
        ClearSellSlots();

        currentSellItems.Clear();

        if (currentShopData == null || currentShopData.ShopSellItems == null)
            return;

        currentSellItems = currentShopData.ShopSellItems;

        for(int i = 0; i < sellSlots.Length; i++)
        {
            if(i >= currentSellItems.Count)
                break;

            ShopItemData shopItem = currentSellItems[i];

            if(shopItem == null || shopItem.ItemData == null)
                continue;

            sellSlots[i].SetItem(shopItem.ItemData, 1);
            sellSlots[i].SetSelected(false);
        }
    }

    public void SelectSlot(ShopSlotMode mode, int slotIndex)
    {
        List<ShopItemData> targetList = GetTargetList(mode);

        if (targetList == null)
            return;

        if (slotIndex < 0 || slotIndex >= targetList.Count)
            return;

        ShopItemData shopItem = targetList[slotIndex];

        if (shopItem == null || shopItem.ItemData == null)
            return;

        selectedMode = mode;
        selectedSlotIndex = slotIndex;
        selectedShopItem = shopItem;

        RefreshSelectedMark();
        RefreshSelectedPanel();
    }

    private void ClearBuySlots()
    {
        if(buySlots == null) 
            return;

        for(int i = 0; i < buySlots.Length; i++)
        {
            if (buySlots[i] != null)
            {
                buySlots[i].Clear();
            }
        }
    }

    private void ClearSellSlots()
    {
        if(sellSlots == null)
            return;

        for(int i = 0; i < sellSlots.Length; i++)
        {
            if(sellSlots[i] != null)
            {
                sellSlots[i].Clear();
            }
        }
    }

    private void ClearAllSlots()
    {
        ClearBuySlots();
        ClearSellSlots();
    }


    public void IncreaseAmountFromSlot(ShopSlotMode mode, int slotIndex)
    {
        if (!IsValidSlot(mode, slotIndex))
            return;

        SelectSlot(mode, slotIndex);

        int currentAmount = GetSlotAmount(mode, slotIndex);
        SetSlotAmount(mode, slotIndex, currentAmount + 1);
    }

    public void DecreaseAmountFromSlot(ShopSlotMode mode, int slotIndex)
    {
        if (!IsValidSlot(mode, slotIndex))
            return;

        SelectSlot(mode, slotIndex);

        int currentAmount = GetSlotAmount(mode, slotIndex);
        SetSlotAmount(mode, slotIndex, currentAmount - 1);
    }

    private void SetSlotAmount(ShopSlotMode mode, int slotIndex, int amount)
    {
        int clampedAmount = Mathf.Clamp(amount, minAmount, maxAmount);

        if(mode == ShopSlotMode.Buy)
        {
            buyAmounts[slotIndex] = clampedAmount;
        }
        else
        {
            sellAmounts[slotIndex] = clampedAmount;
        }

        RefreshSlotAmount(mode, slotIndex);
        RefreshSelectedPanel();
    }

    private int GetSlotAmount(ShopSlotMode mode, int slotIndex)
    {
        if(mode == ShopSlotMode.Buy)
        {
            return buyAmounts[slotIndex];
        }

        return sellAmounts[slotIndex];
    }

    private void RefreshSlotAmount(ShopSlotMode mode, int slotIndex)
    {
        List<ShopItemData> targetList = GetTargetList(mode);
        TempItemSlotView[] targetSlots = GetTargetSlots(mode);

        if (targetList == null || targetSlots == null)
            return;

        if (slotIndex < 0 || slotIndex >= targetList.Count || slotIndex >= targetSlots.Length)
            return;

        ShopItemData shopItem = targetList[slotIndex];

        if (shopItem == null || shopItem.ItemData == null)
            return;

        int amount = GetSlotAmount(mode, slotIndex);

        if (targetSlots[slotIndex] != null)
        {
            targetSlots[slotIndex].SetItem(shopItem.ItemData, amount);
        }
    }

    /*
    private void SetSelectedAmount(int amount)
    {
        selectedAmount = Mathf.Clamp(amount, minAmount, maxAmount);

        RefreshSelectedPanel();
        RefreshSelectedSlotAmount();
    }
    */
    private void RefreshSelectedPanel()
    {
        if(selectedShopItem == null || selectedShopItem.ItemData == null)
        {
            if(selectedModeText != null)
            {
                selectedModeText.text = "선택 없음";
            }
            if(selectedItemNameText != null)
            {
                selectedItemNameText.text = "선택한 아이템 없음";
            }

            if(selectedAmountText != null)
            {
                selectedAmountText.text = "0";
            }

            return;
        }

        if(selectedModeText != null)
        {
            selectedModeText.text = selectedMode == ShopSlotMode.Buy ? "구매" : "판매";
        }

        if(selectedItemNameText != null)
        {
            selectedItemNameText.text = selectedShopItem.ItemData.itemName;
        }

        if(selectedAmountText != null)
        {
            selectedAmountText.text = GetSlotAmount(selectedMode, selectedSlotIndex).ToString();
        }
    }

    /*
    private void RefreshSelectedSlotAmount()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= buySlots.Length)
            return;

        TempItemSlotView[] targetSlots = GetTargetSlots(selectedMode);

        if (targetSlots == null)
            return;

        if(selectedShopItem == null || selectedShopItem.ItemData == null)
            return;

        if (buySlots[selectedSlotIndex] != null)
        {
            buySlots[selectedSlotIndex].SetItem(selectedShopItem.ItemData, selectedAmount);
        }
    }
    */

    private void RefreshSelectedMark()
    {
        if(buySlots != null)
        {
            for(int i = 0; i < buySlots.Length; i++)
            {
                if (buySlots[i] != null)
                {
                    buySlots[i].SetSelected(selectedMode == ShopSlotMode.Buy && i == selectedSlotIndex);
                }
            }
        }

        if(sellSlots != null)
        {
            for(int i = 0; i < sellSlots.Length; i++)
            {
                if (sellSlots[i] != null)
                {
                    sellSlots[i].SetSelected(selectedMode == ShopSlotMode.Sell && i == selectedSlotIndex);
                }
            }
        }
    }

    public void OnBuyButtonClicked()
    {
        if(selectedShopItem == null || selectedShopItem.ItemData == null)
        {
            Debug.Log("구매할 아이템을 선택하세요.");
            return;
        }

        if(selectedMode != ShopSlotMode.Buy)
        {
            Debug.Log("구매 슬롯에서 아이템을 선택하세요.");
            return;
        }

        int amount = GetSlotAmount(selectedMode, selectedSlotIndex);

        Debug.Log(selectedShopItem.ItemData.itemName + " " + amount + "개를 구매했습니다.");
    }

    public void OnSellButtonClicked()
    {
        if(selectedShopItem == null || selectedShopItem.ItemData == null)
        {
            Debug.Log("판매할 아이템을 선택하세요.");
            return;
        }

        if(selectedMode != ShopSlotMode.Sell)
        {
            Debug.Log("판매 슬롯에서 아이템을 선택하세요.");
            return;
        }

        int amount = GetSlotAmount(selectedMode, selectedSlotIndex);

        Debug.Log(selectedShopItem.ItemData.itemName + " " + amount + "개를 판매했습니다.");
    }

    private bool IsValidSlot(ShopSlotMode mode, int slotIndex)
    {
        List<ShopItemData> targetList = GetTargetList(mode);
        TempItemSlotView[] targetSlots = GetTargetSlots(mode);

        if (targetList == null || targetSlots == null)
            return false;

        if(slotIndex < 0 || slotIndex >= targetList.Count || slotIndex >= targetSlots.Length)
            return false;

        return true;
    }

    private List<ShopItemData> GetTargetList(ShopSlotMode mode)
    {
        return mode == ShopSlotMode.Buy ? currentBuyItems : currentSellItems;
    }

    private TempItemSlotView[] GetTargetSlots(ShopSlotMode mode)
    {
        return mode == ShopSlotMode.Buy ? buySlots : sellSlots;
    }

    private void ClearSelectedItem()
    {
        selectedSlotIndex = -1;
        selectedShopItem = null;

        RefreshSelectedMark();
        RefreshSelectedPanel();
    }

    public void Close()
    {
        if(panel != null)
        {
            panel.SetActive(false);
        }

        UIState.SetShopOpen(false);

        currentShopData = null;
        currentPlayer = null;

        if(currentNPC != null)
        {
            currentNPC.ShowInteractionMarkIfPossible();
        }

        currentNPC = null;

        ClearSelectedItem();
        ClearAllSlots();
    }
}
