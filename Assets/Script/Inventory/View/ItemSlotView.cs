using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// (경민) 0707 TempItemSlotView.cs의 상점 슬롯 기능 추가(이하 (경민) 0707 추가)
public enum ShopSlotMode
{
    Buy,
    Sell
}

public class ItemSlotView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    // (경민) 0707 추가
    [Header("상점 슬롯 수량 버튼")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("상점 슬롯 선택 표시")]
    [SerializeField] private GameObject selectedMark;

    private SlotReference slotReference;
    private ItemStack currentItem;

    // (경민) 0707 추가
    private ShopUI shopUI;
    private int shopSlotIndex;
    private ShopSlotMode shopSlotMode;
    private ItemData currentItemData;
    private bool isShopSlot;

    public SlotReference SlotReference => slotReference;
    public ItemStack CurrentItem => currentItem;

    public event Action<SlotReference> OnSlotClicked;
    public event Action<SlotReference> OnSlotHovered;
    public event Action<SlotReference> OnSlotUnhovered;

    public void Initialize(SlotType slotType, int slotIndex)
    {
        // (경민) 0707 추가
        isShopSlot = false;

        slotReference = new SlotReference(slotType, slotIndex);
        Clear();
    }

    /// <summary>
    /// (경민) 0707 추가
    /// 상점 슬롯 초기화
    /// </summary>
    /// <param name="shopUI"></param>
    /// <param name="slotIndex"></param>
    /// <param name="slotMode"></param>
    public void InitializeShopSlot(ShopUI shopUI, int slotIndex, ShopSlotMode slotMode)
    {
        isShopSlot = true;

        this.shopUI = shopUI;
        this.shopSlotIndex = slotIndex;
        this.shopSlotMode = slotMode;

        Clear();
    }

    public void SetItem(ItemStack itemStack)
    {
        currentItem = itemStack;

        // (경민) 0707 추가
        currentItemData = itemStack != null ? itemStack.item : null;

        if (itemStack == null || itemStack.item == null)
        {
            Clear();
            return;
        }

        itemIcon.sprite = itemStack.item.icon;
        itemIcon.enabled = itemStack.item.icon != null;

        if (countText != null)
            countText.text = itemStack.amount > 1 ? itemStack.amount.ToString() : "";
    }

    /// <summary>
    /// (경민) 상점 슬롯에 ItemData를 직접 넣기 위해 만든 함수
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="amount"></param>
    public void SetItem(ItemData itemData, int amount)
    {
        currentItem = null;
        currentItemData = itemData;

        if(itemData == null)
        {
            Clear();
            return;
        }

        if(itemIcon !=  null)
        {
            itemIcon.sprite = itemData.icon;
            itemIcon.enabled = itemData.icon != null;
        }

        if(countText != null)
        {
            countText.text = amount > 1 ? amount.ToString() : "";
        }
    }

    public void Clear()
    {
        currentItem = null;

        // (경민) 0707 추가
        currentItemData = null;

        if(itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (countText != null)
            countText.text = "";

        // (경민) 0707 추가
        SetSelected(false);
    }

    // (경민) 0707 추가
    public void SetSelected(bool isSelected)
    {
        if(selectedMark != null)
        {
            selectedMark.SetActive(isSelected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // (경민) 0707 추가
        if(isShopSlot)
        {
            if (shopUI == null || currentItemData == null)
                return;

            shopUI.SelectSlot(shopSlotMode, shopSlotIndex);
            return;
        }

        OnSlotClicked?.Invoke(slotReference);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // (경민) 0707 추가
        if (isShopSlot)
            return;

        OnSlotHovered?.Invoke(slotReference);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // (경민) 0707 추가
        if (isShopSlot)
            return;

        OnSlotUnhovered?.Invoke(slotReference);
    }
}