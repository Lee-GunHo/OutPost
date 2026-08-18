using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingView : MonoBehaviour
{
    [Header("Recipe Slots")]
    [SerializeField] private Transform recipeSlotParent;
    [SerializeField] private CraftingRecipeSlotView recipeSlotPrefab;

    [Header("Close Button")]
    [SerializeField] private GameObject closeButton;

    private readonly List<CraftingRecipeSlotView> recipeSlots
        = new List<CraftingRecipeSlotView>();

    public event Action<CraftingRecipe> OnRecipeClicked;
    public event Action<CraftingRecipe> OnRecipePointerEntered;
    public event Action OnRecipePointerExited;

    public void ShowRecipes(IReadOnlyList<CraftingRecipe> recipes)
    {
        ClearRecipeSlots();

        if (recipes == null ||
            recipeSlotParent == null ||
            recipeSlotPrefab == null)
        {
            return;
        }

        foreach (CraftingRecipe recipe in recipes)
        {
            if (recipe == null)
            {
                continue;
            }

            CraftingRecipeSlotView slot = Instantiate(
                recipeSlotPrefab,
                recipeSlotParent);

            slot.Initialize(recipe);
            slot.OnClicked += HandleRecipeClicked;
            slot.OnPointerEntered += HandleRecipePointerEntered;
            slot.OnPointerExited += HandleRecipePointerExited;

            recipeSlots.Add(slot);
        }
    }
    public void SetRecipeCraftable(int index, bool canCraft)
    {
        if (index < 0 || index >= recipeSlots.Count)
        {
            return;
        }

        CraftingRecipeSlotView slot = recipeSlots[index];

        if (slot != null)
        {
            slot.SetCraftable(canCraft);
        }
    }
    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void SetCloseButtonVisible(bool isVisible)
    {
        if (closeButton != null)
        {
            closeButton.SetActive(isVisible);
        }
    }

    public void ClearRecipeSlots()
    {
        foreach (CraftingRecipeSlotView slot in recipeSlots)
        {
            if (slot == null)
            {
                continue;
            }

            slot.OnClicked -= HandleRecipeClicked;
            slot.OnPointerEntered -= HandleRecipePointerEntered;
            slot.OnPointerExited -= HandleRecipePointerExited;

            Destroy(slot.gameObject);
        }

        recipeSlots.Clear();
    }

    private void HandleRecipeClicked(CraftingRecipe recipe)
    {
        OnRecipeClicked?.Invoke(recipe);
    }

    private void OnDestroy()
    {
        ClearRecipeSlots();
    }
    private void HandleRecipePointerEntered(CraftingRecipe recipe)
    {
        OnRecipePointerEntered?.Invoke(recipe);
    }

    private void HandleRecipePointerExited()
    {
        OnRecipePointerExited?.Invoke();
    }
}