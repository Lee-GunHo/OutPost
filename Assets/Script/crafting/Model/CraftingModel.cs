using UnityEngine;

public enum CraftingResult
{
    Success,
    InvalidRecipe,
    NotEnoughIngredients,
    InventoryFull
}

public class CraftingModel
{
    private readonly InventoryModel inventoryModel;
    private readonly HotbarModel hotbarModel;

    public CraftingModel(InventoryModel inventoryModel, HotbarModel hotbarModel = null)
    {
        this.inventoryModel = inventoryModel;
        this.hotbarModel = hotbarModel;
    }

    // 인벤토리 + 핫바에 들고 있는 재료를 합산해서 계산한다.
    public int GetOwnedAmount(ItemData item)
    {
        int amount = inventoryModel.GetItemCount(item);

        if (hotbarModel != null)
            amount += hotbarModel.GetItemCount(item);

        return amount;
    }

    /// <summary>
    /// 해당 제작법으로 제작할 수 있는지 검사합니다.
    /// </summary>
    public bool CanCraft(CraftingRecipe recipe)
    {
        if (inventoryModel == null ||
            recipe == null ||
            !recipe.IsValid())
        {
            return false;
        }

        foreach (CraftingIngredient ingredient in recipe.Ingredients)
        {
            if (GetOwnedAmount(ingredient.Item) < ingredient.Amount)
            {
                return false;
            }
        }

        return inventoryModel.CanAddItem(
            recipe.ResultItem,
            recipe.ResultAmount);
    }

    /// <summary>
    /// 재료를 차감하고 결과 아이템을 지급합니다.
    /// </summary>
    public CraftingResult Craft(CraftingRecipe recipe)
    {
        if (inventoryModel == null ||
            recipe == null ||
            !recipe.IsValid())
        {
            return CraftingResult.InvalidRecipe;
        }

        // 재료가 충분한지 검사
        foreach (CraftingIngredient ingredient in recipe.Ingredients)
        {
            if (GetOwnedAmount(ingredient.Item) < ingredient.Amount)
            {
                return CraftingResult.NotEnoughIngredients;
            }
        }

        // 결과 아이템이 들어갈 공간 검사
        if (!inventoryModel.CanAddItem(
                recipe.ResultItem,
                recipe.ResultAmount))
        {
            return CraftingResult.InventoryFull;
        }

        // 모든 검사가 끝난 뒤 재료 차감 (인벤토리 우선, 부족분은 핫바에서 차감)
        foreach (CraftingIngredient ingredient in recipe.Ingredients)
        {
            int remaining = ingredient.Amount;
            remaining -= inventoryModel.RemoveUpTo(ingredient.Item, remaining);

            if (remaining > 0 && hotbarModel != null)
                hotbarModel.RemoveUpTo(ingredient.Item, remaining);
        }

        // 제작 결과 지급
        inventoryModel.AddItem(
            recipe.ResultItem,
            recipe.ResultAmount);

        return CraftingResult.Success;
    }
}