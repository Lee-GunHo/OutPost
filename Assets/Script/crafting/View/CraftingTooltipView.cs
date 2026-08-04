using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingTooltipView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private TMP_Text ingredientTitleText;
    [SerializeField] private Transform ingredientContainer;

    [Header("Prefab")]
    [SerializeField]
    private CraftingIngredientRowView ingredientRowPrefab;

    [Header("Position")]
    [SerializeField]
    private Vector2 offset = new Vector2(20f, -20f);

    private readonly List<CraftingIngredientRowView> ingredientRows
        = new List<CraftingIngredientRowView>();

    private void Awake()
    {
        DisableRaycast();
        Hide();
    }

    private void Update()
    {
        if (gameObject.activeSelf)
        {
            transform.position =
                (Vector2)Input.mousePosition + offset;
        }
    }

    public void Show(
        CraftingRecipe recipe,
        IReadOnlyList<int> ownedAmounts)
    {
        if (recipe == null || !recipe.IsValid())
        {
            Hide();
            return;
        }

        ClearIngredientRows();

        if (itemNameText != null)
        {
            itemNameText.text = recipe.RecipeName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = recipe.Description;
        }

        if (ingredientTitleText != null)
        {
            ingredientTitleText.text = "필요 재료";
        }

        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            CraftingIngredient ingredient =
                recipe.Ingredients[i];

            if (ingredient == null || ingredient.Item == null)
            {
                continue;
            }

            CraftingIngredientRowView row = Instantiate(
                ingredientRowPrefab,
                ingredientContainer);

            int ownedAmount =
                ownedAmounts != null && i < ownedAmounts.Count
                    ? ownedAmounts[i]
                    : 0;

            row.SetIngredient(
                ingredient.Item.icon,
                ownedAmount,
                ingredient.Amount);

            ingredientRows.Add(row);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        ClearIngredientRows();
        gameObject.SetActive(false);
    }

    private void ClearIngredientRows()
    {
        foreach (CraftingIngredientRowView row in ingredientRows)
        {
            if (row != null)
            {
                Destroy(row.gameObject);
            }
        }

        ingredientRows.Clear();
    }

    private void DisableRaycast()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Graphic[] graphics =
            GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }
}