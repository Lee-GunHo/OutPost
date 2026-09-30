using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CraftingRecipeDatabase",
    menuName = "Crafting/Crafting Recipe Database")]
public class CraftingRecipeDatabase : ScriptableObject
{
    [SerializeField]
    private List<CraftingRecipe> recipes = new();

    public IReadOnlyList<CraftingRecipe> Recipes => recipes;

    public IEnumerable<CraftingRecipe> GetRecipes(CraftingTableType tableType)
    {
        foreach (CraftingRecipe recipe in recipes)
        {
            if (recipe != null && recipe.TableType == tableType)
                yield return recipe;
        }
    }
}
