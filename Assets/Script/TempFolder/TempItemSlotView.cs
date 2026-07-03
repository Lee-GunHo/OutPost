using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ShopSlotMode
{
    Buy,
    Sell
}

public class TempItemSlotView : MonoBehaviour, IPointerClickHandler
{
    [Header("슬롯 UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text countText;

    [Header("슬롯 안의 수량 버튼")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("선택 표시")]
    [SerializeField] private GameObject selectedMark;

    private ShopUI shopUI;
    private int slotIndex;
    private ShopSlotMode slotMode;
    private ItemData currentItemData;

    private void Awake()
    {
        if (plusButton != null)
        {
            plusButton.onClick.AddListener(OnPlusButtonClicked);
        }

        if (minusButton != null)
        {
            minusButton.onClick.AddListener(OnMinusButtonClicked);
        }

        SetSelected(false);
    }

    public void InitializeShopSlot(ShopUI shopUI, int slotIndex, ShopSlotMode slotMode)
    {
        this.shopUI = shopUI;
        this.slotIndex = slotIndex;
        this.slotMode = slotMode;
    }

    public void SetItem(ItemData itemData, int amount)
    {
        currentItemData = itemData;

        if (itemData == null)
        {
            Clear();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.icon;
            itemIcon.enabled = itemData.icon != null;
        }

        if (countText != null)
        {
            countText.text = amount.ToString();
        }
    }

    public void Clear()
    {
        currentItemData = null;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (countText != null)
        {
            countText.text = "";
        }

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedMark != null)
        {
            selectedMark.SetActive(isSelected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shopUI == null || currentItemData == null)
        {
            return;
        }

        shopUI.SelectSlot(slotMode, slotIndex);
    }

    private void OnPlusButtonClicked()
    {
        if (shopUI == null || currentItemData == null)
        {
            return;
        }

        shopUI.IncreaseAmountFromSlot(slotMode, slotIndex);
    }

    private void OnMinusButtonClicked()
    {
        if (shopUI == null || currentItemData == null)
        {
            return;
        }

        shopUI.DecreaseAmountFromSlot(slotMode, slotIndex);
    }
}