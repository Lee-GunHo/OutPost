using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingIngredientRowView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image ingredientIcon;
    [SerializeField] private TMP_Text ingredientCountText;

    [Header("Colors")]
    [SerializeField] private Color enoughColor = Color.white;
    [SerializeField] private Color notEnoughColor = Color.red;

    public void SetIngredient(
        Sprite icon,
        int ownedAmount,
        int requiredAmount)
    {
        if (ingredientIcon != null)
        {
            ingredientIcon.sprite = icon;
            ingredientIcon.enabled = icon != null;
        }

        if (ingredientCountText != null)
        {
            ingredientCountText.text =
                $"{ownedAmount} / {requiredAmount}";

            ingredientCountText.color =
                ownedAmount >= requiredAmount
                    ? enoughColor
                    : notEnoughColor;
        }
    }
}