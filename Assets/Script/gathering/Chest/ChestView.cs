using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// 창고 UI 표시와 UI 이벤트 연결만 담당
/// </summary>
public class ChestView : MonoBehaviour
{
    [Header("전체 패널")]
    [SerializeField] private GameObject panel;

    [Header("슬롯 Content")]
    [SerializeField] private Transform hotbarContent;
    [SerializeField] private Transform inventoryContent;
    [SerializeField] private Transform chestContent;

    [Header("일반 슬롯 프리팹")]
    [SerializeField] private ChestSlotView slotPrefab;

    [Header("슬롯 개수")]
    [SerializeField, Min(1)] private int hotbarSlotCount = 10;
    [SerializeField, Min(1)] private int inventorySlotCount = 40;
    [SerializeField, Min(1)] private int chestSlotCount = 40;

    [Header("Canvas")]
    [SerializeField] private Canvas parentCanvas;

    [Header("정렬 UI")]
    [SerializeField] private Button sortButton;
    [SerializeField] private ChestSortPopup sortPopup;

    [Header("버리기 UI")]
    [SerializeField] private ChestDiscardSlotView discardSlotView;
    [SerializeField] private Button discardButton;

    [Header("커서에 들고 있는 아이템 UI")]
    [FormerlySerializedAs("dragIcon")]
    [SerializeField] private Image carriedItemIcon;
    [FormerlySerializedAs("dragAmountText")]
    [SerializeField] private TMP_Text carriedItemAmountText;

    [Header("기타 UI")]
    [SerializeField] private Button closeButton;
    [SerializeField] private ItemTooltipView tooltipView;

    private readonly List<ChestSlotView> hotbarSlots =
        new List<ChestSlotView>();

    private readonly List<ChestSlotView> inventorySlots =
        new List<ChestSlotView>();

    private readonly List<ChestSlotView> chestSlots =
        new List<ChestSlotView>();

    private ChestPresenter presenter;
    private bool isInitialized;

    public int HotbarSlotCount => hotbarSlotCount;

    private void Update()
    {
        if (carriedItemIcon == null ||
            !carriedItemIcon.gameObject.activeSelf)
        {
            return;
        }

        if (Mouse.current != null)
        {
            carriedItemIcon.transform.position =
                Mouse.current.position.ReadValue();
        }

        carriedItemIcon.transform.SetAsLastSibling();
    }

    public void Init(ChestPresenter chestPresenter)
    {
        if (isInitialized)
            return;

        presenter = chestPresenter;

        CreateOrBindSlots(
            hotbarContent,
            hotbarSlotCount,
            ChestSlotArea.Hotbar,
            hotbarSlots
        );

        CreateOrBindSlots(
            inventoryContent,
            inventorySlotCount,
            ChestSlotArea.Inventory,
            inventorySlots
        );

        CreateOrBindSlots(
            chestContent,
            chestSlotCount,
            ChestSlotArea.Chest,
            chestSlots
        );

        if (discardSlotView != null)
        {
            discardSlotView.OnSlotClicked -=
                presenter.OnDiscardSlotClicked;

            discardSlotView.OnSlotClicked +=
                presenter.OnDiscardSlotClicked;

            discardSlotView.Clear();
        }

        if (discardButton != null)
        {
            discardButton.onClick.RemoveListener(
                presenter.OnDiscardButtonClicked
            );

            discardButton.onClick.AddListener(
                presenter.OnDiscardButtonClicked
            );

            discardButton.interactable = false;
        }

        if(parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        if(sortPopup != null)
        {
            sortPopup.Initialize(
                presenter.OnGatherItemsClicked,
                presenter.OnSortByTypeClicked
            );

            sortPopup.Close();
        }

        if(sortButton != null)
        {
            sortButton.onClick.RemoveListener(OpenSortPopup);
            sortButton.onClick.AddListener(OpenSortPopup);
        }

        ConfigureCarriedIconRaycast();
        HideCarriedItem();
        ClearDiscardItem();
        HideTooltip();
        Hide();

        isInitialized = true;
    }

    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    public void Hide()
    {
        HideTooltip();
        HideCarriedItem();
        CloseSortPopup();

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void Refresh(
        IReadOnlyList<ItemStack> hotbarItems,
        IReadOnlyList<ItemStack> inventoryItems,
        IReadOnlyList<ItemStack> chestItems)
    {
        RefreshSlots(hotbarSlots, hotbarItems);
        RefreshSlots(inventorySlots, inventoryItems);
        RefreshSlots(chestSlots, chestItems);
    }

    public void ShowTooltip(ItemStack itemStack)
    {
        if (tooltipView == null)
            return;

        if (itemStack == null || itemStack.item == null)
        {
            tooltipView.Hide();
            return;
        }

        tooltipView.Show(itemStack);
    }

    public void HideTooltip()
    {
        if (tooltipView != null)
        {
            tooltipView.Hide();
        }
    }

    public void ShowCarriedItem(ItemStack itemStack)
    {
        if (itemStack == null ||
            itemStack.item == null ||
            itemStack.amount <= 0)
        {
            HideCarriedItem();
            return;
        }

        if (carriedItemIcon != null)
        {
            carriedItemIcon.sprite = itemStack.item.icon;
            carriedItemIcon.enabled = itemStack.item.icon != null;
            carriedItemIcon.gameObject.SetActive(true);
            carriedItemIcon.transform.SetAsLastSibling();
        }

        if (carriedItemAmountText != null)
        {
            carriedItemAmountText.text = itemStack.amount > 1
                ? itemStack.amount.ToString()
                : string.Empty;
        }
    }

    public void HideCarriedItem()
    {
        if (carriedItemIcon != null)
        {
            carriedItemIcon.sprite = null;
            carriedItemIcon.gameObject.SetActive(false);
        }

        if (carriedItemAmountText != null)
        {
            carriedItemAmountText.text = string.Empty;
        }
    }

    public void OpenSortPopup()
    {
        HideTooltip();

        if (sortPopup == null)
            return;

        Vector2 mousePosition = Vector2.zero;

        if(Mouse.current != null)
        {
            mousePosition =
                Mouse.current.position.ReadValue();
        }
        else
        {
            mousePosition = Input.mousePosition;
        }

        sortPopup.Open(
            mousePosition,
            parentCanvas
        );
    }

    public void CloseSortPopup()
    {
        if(sortPopup != null)
        {
            sortPopup.Close();
        }
    }

    public void SetDiscardItem(ItemStack itemStack)
    {
        if (discardSlotView != null)
        {
            discardSlotView.SetItem(itemStack);
        }

        if (discardButton != null)
        {
            discardButton.interactable =
                itemStack != null &&
                itemStack.item != null &&
                itemStack.amount > 0;
        }
    }

    public void ClearDiscardItem()
    {
        if (discardSlotView != null)
        {
            discardSlotView.Clear();
        }

        if (discardButton != null)
        {
            discardButton.interactable = false;
        }
    }

    private void ConfigureCarriedIconRaycast()
    {
        if (carriedItemIcon == null)
            return;

        Graphic[] graphics =
            carriedItemIcon.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup canvasGroup =
            carriedItemIcon.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                carriedItemIcon.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void CreateOrBindSlots(
        Transform content,
        int requiredCount,
        ChestSlotArea area,
        List<ChestSlotView> result)
    {
        result.Clear();

        if (content == null)
        {
            Debug.LogError(
                $"ChestView 오류: {area} Content가 연결되지 않았습니다."
            );
            return;
        }

        ChestSlotView[] existingSlots =
            content.GetComponentsInChildren<ChestSlotView>(true);

        for (int i = 0;
             i < existingSlots.Length && result.Count < requiredCount;
             i++)
        {
            BindSlot(
                existingSlots[i],
                area,
                result.Count,
                result
            );
        }

        while (result.Count < requiredCount)
        {
            if (slotPrefab == null)
            {
                Debug.LogError(
                    $"ChestView 오류: {area} 슬롯이 {requiredCount}개 필요하지만 " +
                    $"현재 {result.Count}개이며 Slot Prefab이 없습니다."
                );
                break;
            }

            ChestSlotView createdSlot = Instantiate(slotPrefab, content);

            BindSlot(
                createdSlot,
                area,
                result.Count,
                result
            );
        }
    }

    private void BindSlot(
        ChestSlotView slotView,
        ChestSlotArea area,
        int index,
        List<ChestSlotView> result)
    {
        if (slotView == null)
            return;

        slotView.Initialize(area, index);

        slotView.OnSlotClicked -= presenter.OnSlotClicked;
        slotView.OnSlotClicked += presenter.OnSlotClicked;

        slotView.OnSlotHovered -= presenter.OnSlotHovered;
        slotView.OnSlotHovered += presenter.OnSlotHovered;

        slotView.OnSlotUnhovered -= presenter.OnSlotUnhovered;
        slotView.OnSlotUnhovered += presenter.OnSlotUnhovered;

        slotView.OnRightDragStarted -= presenter.OnRightDragStarted;
        slotView.OnRightDragStarted += presenter.OnRightDragStarted;

        slotView.gameObject.SetActive(true);
        result.Add(slotView);
    }

    private void RefreshSlots(
        List<ChestSlotView> slotViews,
        IReadOnlyList<ItemStack> items)
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            ChestSlotView slotView = slotViews[i];

            if (slotView == null)
                continue;

            if (items != null && i < items.Count)
            {
                slotView.SetItem(items[i]);
            }
            else
            {
                slotView.Clear();
            }
        }
    }
}