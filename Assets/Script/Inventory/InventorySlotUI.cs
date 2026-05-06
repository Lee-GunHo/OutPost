using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text amountText;

    public void Set(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
        {
            iconImage.enabled = false;
            amountText.text = "";
            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = stack.item.icon;
        amountText.text = stack.amount > 1 ? stack.amount.ToString() : "";
    }
}
