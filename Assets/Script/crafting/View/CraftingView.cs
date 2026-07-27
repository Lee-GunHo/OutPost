using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingView : MonoBehaviour
{
    [Header("제작 UI")]
    [SerializeField] private GameObject craftingPanel;

    [Header("레시피 목록")]
    [SerializeField] private Transform recipeSlotParent;
    [SerializeField] private ItemSlotView itemSlotPrefab;

    [Header("선택한 제작법")]
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private ItemSlotView resultSlot;
    [SerializeField] private Transform ingredientSlotParent;

    [Header("제작")]
    [SerializeField] private Button craftButton;
    [SerializeField] private TMP_Text resultMessageText;

    private readonly List<ItemSlotView> recipeSlots = new();
    private readonly List<ItemSlotView> ingredientSlots = new();

    private CraftingRecipe selectedRecipe;

    public event Action<CraftingRecipe> OnRecipeSelected;
    public event Action<CraftingRecipe> OnCraftRequested;

    public bool IsOpen =>
        craftingPanel != null &&
        craftingPanel.activeSelf;

    public CraftingRecipe SelectedRecipe => selectedRecipe;

    private void Awake()
    {
        if (craftButton != null)
        {
            craftButton.onClick.AddListener(HandleCraftButtonClicked);
            craftButton.interactable = false;
        }

        if (resultSlot != null)
        {
            resultSlot.Initialize(SlotType.Crafting, 0);
        }

        ClearRecipeDetail();

        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (craftButton != null)
        {
            craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
        }

        ClearRecipeSlots();
        ClearIngredientSlots();
    }

    public void CreateRecipeSlots(
        IReadOnlyList<CraftingRecipe> recipes)
    {
        ClearRecipeSlots();

        if (recipes == null ||
            recipeSlotParent == null ||
            itemSlotPrefab == null)
        {
            return;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            CraftingRecipe recipe = recipes[i];

            if (recipe == null)
                continue;

            ItemSlotView slot = Instantiate(
                itemSlotPrefab,
                recipeSlotParent);

            slot.InitializeCraftingSlot(recipe, i);
            slot.OnCraftingRecipeClicked += HandleRecipeClicked;

            recipeSlots.Add(slot);
        }
    }

    public void ShowRecipeDetail(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        ClearIngredientSlots();

        if (recipe == null)
        {
            ClearRecipeDetail();
            return;
        }

        if (recipeNameText != null)
        {
            recipeNameText.text = recipe.RecipeName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = recipe.Description;
        }

        if (resultSlot != null)
        {
            resultSlot.SetItem(
                recipe.ResultItem,
                recipe.ResultAmount);
        }

        CreateIngredientSlots(recipe);

        if (craftButton != null)
        {
            craftButton.interactable = recipe.IsValid();
        }

        SetResultMessage(string.Empty);
    }

    private void CreateIngredientSlots(CraftingRecipe recipe)
    {
        if (recipe == null ||
            ingredientSlotParent == null ||
            itemSlotPrefab == null)
        {
            return;
        }

        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            CraftingIngredient ingredient = recipe.Ingredients[i];

            if (ingredient == null || ingredient.Item == null)
                continue;

            ItemSlotView slot = Instantiate(
                itemSlotPrefab,
                ingredientSlotParent);

            slot.Initialize(SlotType.Crafting, i);
            slot.SetItem(
                ingredient.Item,
                ingredient.Amount);

            ingredientSlots.Add(slot);
        }
    }

    public void ClearRecipeSlots()
    {
        foreach (ItemSlotView slot in recipeSlots)
        {
            if (slot == null)
                continue;

            slot.OnCraftingRecipeClicked -= HandleRecipeClicked;
            Destroy(slot.gameObject);
        }

        recipeSlots.Clear();
    }

    private void ClearIngredientSlots()
    {
        foreach (ItemSlotView slot in ingredientSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        ingredientSlots.Clear();
    }

    public void ClearRecipeDetail()
    {
        selectedRecipe = null;
        ClearIngredientSlots();

        if (recipeNameText != null)
        {
            recipeNameText.text = "제작법을 선택하세요";
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
        }

        if (resultSlot != null)
        {
            resultSlot.Clear();
        }

        if (craftButton != null)
        {
            craftButton.interactable = false;
        }

        SetResultMessage(string.Empty);
    }

    public void SetCraftButtonInteractable(bool interactable)
    {
        if (craftButton != null)
        {
            craftButton.interactable = interactable;
        }
    }

    public void SetResultMessage(string message)
    {
        if (resultMessageText != null)
        {
            resultMessageText.text = message;
        }
    }

    public void Open()
    {
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(true);
        }
    }

    public void Close()
    {
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
        }

        ClearRecipeDetail();
    }

    public void SetVisible(bool isVisible)
    {
        if (isVisible)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    private void HandleRecipeClicked(CraftingRecipe recipe)
    {
        if (recipe == null)
            return;

        ShowRecipeDetail(recipe);
        OnRecipeSelected?.Invoke(recipe);
    }

    private void HandleCraftButtonClicked()
    {
        if (selectedRecipe == null)
            return;

        OnCraftRequested?.Invoke(selectedRecipe);
    }
}
