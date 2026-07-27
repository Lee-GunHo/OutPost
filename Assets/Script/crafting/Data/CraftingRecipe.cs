using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftingIngredient
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int amount = 1;

    public ItemData Item => item;
    public int Amount => amount;
}

[CreateAssetMenu(
    fileName = "New Crafting Recipe",
    menuName = "Crafting/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("제작법 정보")]
    [SerializeField] private string recipeName;

    [TextArea]
    [SerializeField] private string description;

    [Header("결과 아이템")]
    [SerializeField] private ItemData resultItem;

    [SerializeField, Min(1)]
    private int resultAmount = 1;

    [Header("필요 재료")]
    [SerializeField]
    private List<CraftingIngredient> ingredients = new();

    public string RecipeName => recipeName;
    public string Description => description;
    public ItemData ResultItem => resultItem;
    public int ResultAmount => resultAmount;
    public IReadOnlyList<CraftingIngredient> Ingredients => ingredients;

    public bool IsValid()
    {
        if (resultItem == null || resultAmount <= 0)
            return false;

        if (ingredients == null || ingredients.Count == 0)
            return false;

        foreach (CraftingIngredient ingredient in ingredients)
        {
            if (ingredient == null ||
                ingredient.Item == null ||
                ingredient.Amount <= 0)
            {
                return false;
            }
        }

        return true;
    }
}