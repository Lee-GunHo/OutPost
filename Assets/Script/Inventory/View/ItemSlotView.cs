using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// (∞ÊπŒ) 0707 TempItemSlotView.cs¿« ªÛ¡° ΩΩ∑‘ ±‚¥… √ﬂ∞°(¿Ã«œ (∞ÊπŒ) 0707 √ﬂ∞°)
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

    // (∞ÊπŒ) 0707 √ﬂ∞°
    [Header("ªÛ¡° ΩΩ∑‘ ºˆ∑Æ πˆ∆∞")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("ªÛ¡° ΩΩ∑‘ º±≈√ «•Ω√")]
    [SerializeField] private GameObject selectedMark;

    private SlotReference slotReference;
    private ItemStack currentItem;

    // (∞ÊπŒ) 0707 √ﬂ∞°
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

    // (∞ÊπŒ) 0707 √ﬂ∞°
    private void Awake()
    {
        if(plusButton != null)
        {
            plusButton.onClick.AddListener(OnPlusButtonClicked);
        }

        if(minusButton != null)
        {
            minusButton.onClick.AddListener(OnMinusButtonClicked);
        }

        SetSelected(false);
    }

    // (∞ÊπŒ) 0707 √ﬂ∞°
    private void OnDestroy()
    {
        if(plusButton != null)
        {
            plusButton.onClick.RemoveListener(OnPlusButtonClicked);
        }

        if(minusButton != null)
        {
            minusButton.onClick.RemoveListener(OnMinusButtonClicked);
        }
    }

    public void Initialize(SlotType slotType, int slotIndex)
    {
        // (∞ÊπŒ) 0707 √ﬂ∞°
        isShopSlot = false;

        slotReference = new SlotReference(slotType, slotIndex);
        Clear();
    }

    /// <summary>
    /// (∞ÊπŒ) 0707 √ﬂ∞°
    /// ªÛ¡° ΩΩ∑‘ √ ±‚»≠
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

        // (∞ÊπŒ) 0707 √ﬂ∞°
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
    /// (∞ÊπŒ) ªÛ¡° ΩΩ∑‘ø° ItemData∏¶ ¡˜¡¢ ≥÷±‚ ¿ß«ÿ ∏∏µÁ «‘ºˆ
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

        // (∞ÊπŒ) 0707 √ﬂ∞°
        currentItemData = null;

        if(itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (countText != null)
            countText.text = "";

        // (∞ÊπŒ) 0707 √ﬂ∞°
        SetSelected(false);
    }

    // (∞ÊπŒ) 0707 √ﬂ∞°
    public void SetSelected(bool isSelected)
    {
        if(selectedMark != null)
        {
            selectedMark.SetActive(isSelected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // (∞ÊπŒ) 0707 √ﬂ∞°
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
        // (∞ÊπŒ) 0707 √ﬂ∞°
        if (isShopSlot)
            return;

        OnSlotHovered?.Invoke(slotReference);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // (∞ÊπŒ) 0707 √ﬂ∞°
        if (isShopSlot)
            return;

        OnSlotUnhovered?.Invoke(slotReference);
    }

    // (∞ÊπŒ) 0707 √ﬂ∞°
    private void OnPlusButtonClicked()
    {
        if (!isShopSlot)
            return;

        if (shopUI == null || currentItemData == null)
            return;

        shopUI.IncreaseAmountFromSlot(shopSlotMode, shopSlotIndex);
    }

    // (∞ÊπŒ) 0707 √ﬂ∞°
    private void OnMinusButtonClicked()
    {
        if (!isShopSlot)
            return;

        if (shopUI == null || currentItemData == null)
            return;

        shopUI.DecreaseAmountFromSlot(shopSlotMode, shopSlotIndex);
    }
}