using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlotView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text amountText;

    [Header("슬롯 배경")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Sprite defaultSlotSprite;
    [SerializeField] private Sprite selectedSlotSprite;

    private ShopUI owner;
    private ShopSlotMode mode;
    private int slotIndex;

    private ItemData currentItem;
    private int currentAmount;

    public ItemData CurrentItem => currentItem;
    public int CurrentAmount => currentAmount;
    public int SlotIndex => slotIndex;
    public ShopSlotMode Mode => mode;

    public void Initialize(ShopUI owner, ShopSlotMode mode, int slotIndex)
    {
        this.owner = owner;
        this.mode = mode;
        this.slotIndex = slotIndex;

        SetSelected(false);
        Clear();
    }

    public void SetItem(ItemData item, int amount)
    {
        currentItem = item;
        currentAmount = amount;

        if(item == null)
        {
            Clear();
            return;
        }

        if(itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = item.icon != null;
        }

        if(itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }

        if(amountText != null)
        {
            amountText.text = amount > 0 ? "x" + amount : "";
        }

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        currentItem = null;
        currentAmount = 0;
        
        if(itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if(itemNameText != null)
        {
            itemNameText.text = "";
        }

        if(amountText != null)
        {
            amountText.text = "";
        }

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (slotBackground == null)
            return;

        slotBackground.sprite = isSelected
            ? selectedSlotSprite
            : defaultSlotSprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null)
            return;

        owner.SelectSlot(mode, slotIndex);

        if(eventData.clickCount < 2)
        {
            return;
        }

        if(mode == ShopSlotMode.Sell)
        {
            owner.OnPlayerItemDoubleClicked(slotIndex);
        }
        else if(mode == ShopSlotMode.Buy)
        {
            owner.OnShopItemDoubleClicked(slotIndex);
        }
    }
}
