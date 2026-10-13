using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 테스트용: 플레이 중에 모든 레시피 재료와 씨앗을 99개씩, 괭이를 1개(최대 스택만큼) 지급.
/// 재료 목록은 CraftingRecipe 에셋에서 자동으로 모으므로 엑셀에 새 재료가 생겨도 수정 불필요.
/// 단축키 Ctrl+Shift+9
/// </summary>
public static class TestMaterialGiver
{
    private const int GiveAmount = 99;
    private const string MenuPath = "Tools/Debug/Give Test Materials (99) %#9";

    [MenuItem(MenuPath)]
    public static void GiveTestMaterials()
    {
        PlayerPresenter player = Object.FindFirstObjectByType<PlayerPresenter>();
        InventoryModel inventory = player != null ? player.PlayerInventory : null;

        if (inventory == null)
        {
            Debug.LogWarning("[TestMaterialGiver] 플레이어 인벤토리를 찾지 못했습니다.");
            return;
        }

        List<string> given = new List<string>();
        List<string> failed = new List<string>();

        HotbarModel hotbar = PlayerItemReceiver.CurrentHotbar;

        foreach (ItemData item in CollectTestItems())
        {
            int amount = Mathf.Min(GiveAmount, item.maxStack);

            // 줍기와 같은 규칙(핫바 먼저). 인벤토리 뒤쪽 칸은 화면에 안 보일 수 있어서 괭이/씨앗을 먼저 지급
            if (PlayerItemReceiver.Give(inventory, hotbar, item, amount))
                given.Add($"{item.itemName} x{amount}");
            else
                failed.Add(item.itemName);
        }

        Debug.Log($"[TestMaterialGiver] 지급: {string.Join(", ", given)}");

        if (failed.Count > 0)
            Debug.LogWarning($"[TestMaterialGiver] 인벤토리 공간 부족으로 못 넣음: {string.Join(", ", failed)}");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanGiveTestMaterials()
    {
        return EditorApplication.isPlaying;
    }

    // 괭이 → 씨앗 → 레시피 재료 순서
    private static IEnumerable<ItemData> CollectTestItems()
    {
        IEnumerable<ItemData> ingredients = AssetDatabase.FindAssets("t:CraftingRecipe", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<CraftingRecipe>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(recipe => recipe != null && recipe.Ingredients != null)
            .SelectMany(recipe => recipe.Ingredients)
            .Where(ingredient => ingredient != null && ingredient.Item != null)
            .Select(ingredient => ingredient.Item);

        IEnumerable<ItemData> seeds = AssetDatabase.FindAssets("t:CropData", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(crop => crop != null && crop.seedItem != null)
            .Select(crop => crop.seedItem);

        IEnumerable<ItemData> hoes = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(item => item != null && item.toolType == ToolType.Hoe);

        return hoes.OrderBy(item => item.itemID)
            .Concat(seeds.OrderBy(item => item.itemID))
            .Concat(ingredients.OrderBy(item => item.itemID))
            .Distinct();
    }
}
