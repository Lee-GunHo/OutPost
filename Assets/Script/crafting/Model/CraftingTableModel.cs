using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작대 오브젝트에 부착되는 모델
/// 제작대 종류에 맞는 레시피는 CraftingRecipeDatabase에서 가져오고,
/// 이 제작대에만 추가로 보여줄 레시피를 보관
/// </summary>
public class CraftingTableModel : MonoBehaviour
{
    [Header("제작대 이름 (표시용)")]
    [SerializeField] private string tableName = "제작대";

    [Header("제작대 종류")]
    [SerializeField] private CraftingTableType tableType = CraftingTableType.Wood;

    [Header("이 제작대에만 추가로 제작 가능한 레시피")]
    [SerializeField]
    private List<CraftingRecipe> tableRecipes = new List<CraftingRecipe>();

    public string TableName => tableName;
    public CraftingTableType TableType => tableType;
    public IReadOnlyList<CraftingRecipe> TableRecipes => tableRecipes;
}
