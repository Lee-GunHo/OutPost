using System.Collections.Generic;
using UnityEngine;

public class CraftingPresenter : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private InventoryModel inventoryModel;

    [Header("View")]
    [SerializeField] private CraftingView craftingView;

    [SerializeField] private CraftingTooltipView craftingTooltipView;

    [Header("Hand Crafting Recipes")]
    [SerializeField]
    private List<CraftingRecipe> handRecipes
        = new List<CraftingRecipe>();

    private CraftingModel craftingModel;

    private void Start()
    {
        if (inventoryModel == null || craftingView == null)
        {
            Debug.LogError(
                "CraftingPresenter의 참조가 연결되지 않았습니다.");
            return;
        }

        craftingModel = new CraftingModel(inventoryModel);

        craftingView.OnRecipeClicked += HandleRecipeClicked;
        craftingView.OnRecipePointerEntered += HandleRecipePointerEntered;
        craftingView.OnRecipePointerExited += HandleRecipePointerExited;
        inventoryModel.OnInventoryChanged += RefreshCraftableState;

        craftingView.ShowRecipes(handRecipes);
        RefreshCraftableState();
    }

    private void OnDestroy()
    {
        if (craftingView != null)
        {
            craftingView.OnRecipeClicked -= HandleRecipeClicked;
            craftingView.OnRecipePointerEntered -= HandleRecipePointerEntered;
            craftingView.OnRecipePointerExited -= HandleRecipePointerExited;
        }

        if (inventoryModel != null)
        {
            inventoryModel.OnInventoryChanged -= RefreshCraftableState;
        }
    }

    private void HandleRecipeClicked(CraftingRecipe recipe)
    {
        if (craftingModel == null)
        {
            return;
        }

        CraftingResult result = craftingModel.Craft(recipe);

        switch (result)
        {
            case CraftingResult.Success:
                Debug.Log($"{recipe.RecipeName} 제작 성공");
                break;

            case CraftingResult.NotEnoughIngredients:
                Debug.Log("제작 재료가 부족합니다.");
                break;

            case CraftingResult.InventoryFull:
                Debug.Log("인벤토리 공간이 부족합니다.");
                break;

            default:
                Debug.LogWarning("올바르지 않은 제작법입니다.");
                break;
        }

        RefreshCraftableState();
        if (result == CraftingResult.Success)
        {
            HandleRecipePointerEntered(recipe);
        }
    }
    private void HandleRecipePointerEntered(CraftingRecipe recipe)
    {
        if (recipe == null ||
            inventoryModel == null ||
            craftingTooltipView == null)
        {
            return;
        }

        List<int> ownedAmounts = new List<int>();

        foreach (CraftingIngredient ingredient in recipe.Ingredients)
        {
            int ownedAmount =
                inventoryModel.GetItemCount(ingredient.Item);

            ownedAmounts.Add(ownedAmount);
        }

        craftingTooltipView.Show(recipe, ownedAmounts);
    }

    private void HandleRecipePointerExited()
    {
        if (craftingTooltipView != null)
        {
            craftingTooltipView.Hide();
        }
    }

    private void RefreshCraftableState()
    {
        if (craftingModel == null || craftingView == null)
        {
            return;
        }

        for (int i = 0; i < handRecipes.Count; i++)
        {
            bool canCraft = craftingModel.CanCraft(handRecipes[i]);
            craftingView.SetRecipeCraftable(i, canCraft);
        }
    }
}