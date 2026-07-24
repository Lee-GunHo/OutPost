using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 삭제 대기 아이템을 표시하고 좌클릭 입력을 Presenter에 전달
/// 커서에 아이템을 들고 있을 때 좌클릭하면 1개씩 들어감
/// </summary>
[RequireComponent(typeof(Image))]
public class ChestDiscardSlotView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text countText;

    public event Action<PointerEventData.InputButton> OnSlotClicked;

    private void Awake()
    {
        Image backgroundImage = GetComponent<Image>();
        backgroundImage.raycastTarget = true;

        if (itemIcon != null)
        {
            itemIcon.raycastTarget = false;
        }

        if (countText != null)
        {
            countText.raycastTarget = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(eventData.button);
    }

    public void SetItem(ItemStack itemStack)
    {
        if (itemStack == null ||
            itemStack.item == null ||
            itemStack.amount <= 0)
        {
            Clear();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = itemStack.item.icon;
            itemIcon.enabled = itemStack.item.icon != null;
        }

        if (countText != null)
        {
            countText.text = itemStack.amount > 1
                ? itemStack.amount.ToString()
                : string.Empty;
        }
    }

    public void Clear()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (countText != null)
        {
            countText.text = string.Empty;
        }
    }
}