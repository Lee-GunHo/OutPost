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

    [Header("교환 재료 표시 (비우면 이름 텍스트를 복제해 아래 줄에 자동 생성)")]
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Color unaffordableCostColor = new Color(1f, 0.45f, 0.45f);

    [Header("슬롯 배경")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Sprite defaultSlotSprite;
    [SerializeField] private Sprite selectedSlotSprite;

    private ShopUI owner;
    private ShopSlotMode mode;
    private int slotIndex;

    private ItemData currentItem;
    private int currentAmount;
    private Color defaultCostColor = Color.white;
    private bool costTextReady;

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

        if(costText != null)
        {
            costText.text = "";
        }

        SetIconDimmed(false);
        SetSelected(false);
    }

    /// <summary>
    /// 교환 슬롯 아래 줄에 필요한 재료를 표시. 재료가 모자라면 빨간 글씨 + 아이콘 흐리게
    /// </summary>
    public void SetTradeInfo(string costLine, bool affordable)
    {
        EnsureCostText();

        if (costText != null)
        {
            costText.text = costLine;
            costText.color = affordable ? defaultCostColor : unaffordableCostColor;
        }

        SetIconDimmed(!affordable);
    }

    private void SetIconDimmed(bool dimmed)
    {
        if (itemIcon == null)
            return;

        Color color = itemIcon.color;
        color.a = dimmed ? 0.4f : 1f;
        itemIcon.color = color;
    }

    // 기존 ShopSlot 프리팹에는 재료 줄이 없어서, 이름 텍스트를 복제해 바로 아래에 붙임
    private void EnsureCostText()
    {
        if (costTextReady)
            return;

        costTextReady = true;

        if (costText == null && itemNameText != null)
        {
            costText = Instantiate(itemNameText, itemNameText.transform.parent);
            costText.name = "CostText";
            costText.fontSize = itemNameText.fontSize * 0.7f;
            costText.textWrappingMode = TextWrappingModes.NoWrap;
            costText.raycastTarget = false;

            RectTransform nameRect = itemNameText.rectTransform;
            costText.rectTransform.anchoredPosition = nameRect.anchoredPosition + new Vector2(0f, -nameRect.sizeDelta.y * 0.85f);
        }

        if (costText != null)
        {
            defaultCostColor = costText.color;
        }
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

        // 판매(돈) 기능이 없어져서 오른쪽 재료 슬롯은 보기만 함
        if(mode == ShopSlotMode.Buy)
        {
            owner.OnShopItemDoubleClicked(slotIndex);
        }
    }
}
