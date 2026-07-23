using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 창고 UI 표시를 담당
/// </summary>
public class ChestView : MonoBehaviour
{
    [Header("전체 패널")]
    [SerializeField] private GameObject panel;

    [Header("슬롯 Content")]
    [SerializeField] private Transform hotbarContent;

    [Tooltip("Inventory Scroll View의 Content")]
    [SerializeField] private Transform inventoryContent;

    [Tooltip("Chest Scroll View의 Content")]
    [SerializeField] private Transform chestContent;

    [Header("슬롯 프리팹")]
    [SerializeField] private ChestSlotView slotPrefab;

    [Header("슬롯 개수")]
    [SerializeField] private int hotbarSlotCount = 10;
    [SerializeField] private int inventorySlotCount = 40;
    [SerializeField] private int chestSlotCount = 40;

    [Header("기타 UI")]
    [SerializeField] private Button closeButton;
    [SerializeField] private ItemTooltipView tooltipView;

    private readonly List<ChestSlotView> hotbarSlots
        = new List<ChestSlotView>();

    private readonly List<ChestSlotView> inventorySlots
        = new List<ChestSlotView>();

    private readonly List<ChestSlotView> chestSlots
        = new List<ChestSlotView>();

    private ChestPresenter presenter;
    private bool isInitialized;

    public int HotbarSlotCount => hotbarSlotCount;

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

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                presenter.Close
            );
        }

        if (tooltipView != null)
        {
            tooltipView.Hide();
        }

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
        if (tooltipView != null)
        {
            tooltipView.Hide();
        }

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
        RefreshSlots(
            hotbarSlots,
            hotbarItems
        );

        RefreshSlots(
            inventorySlots,
            inventoryItems
        );

        RefreshSlots(
            chestSlots,
            chestItems
        );
    }

    public void ShowTooltip(ItemStack itemStack)
    {
        if (tooltipView == null)
            return;

        if (itemStack == null ||
            itemStack.item == null)
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

        // 이미 만들어진 슬롯이 있다면 먼저 사용
        ChestSlotView[] existingSlots =
            content.GetComponentsInChildren<ChestSlotView>(
                true
            );

        for (int i = 0;
             i < existingSlots.Length &&
             result.Count < requiredCount;
             i++)
        {
            BindSlot(
                existingSlots[i],
                area,
                result.Count,
                result
            );
        }

        // 부족한 슬롯은 프리팹으로 생성
        while (result.Count < requiredCount)
        {
            if (slotPrefab == null)
            {
                Debug.LogError(
                    $"ChestView 오류: {area} 슬롯 프리팹이 없습니다."
                );

                break;
            }

            ChestSlotView createdSlot =
                Instantiate(
                    slotPrefab,
                    content
                );

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

        slotView.OnSlotClicked +=
            presenter.OnSlotClicked;

        slotView.OnSlotHovered +=
            presenter.OnSlotHovered;

        slotView.OnSlotUnhovered +=
            presenter.OnSlotUnhovered;

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