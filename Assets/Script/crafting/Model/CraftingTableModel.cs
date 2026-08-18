using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작대 오브젝트에 부착되는 모델
/// 이 제작대에서만 제작 가능한 전용 레시피 목록을 보관
/// </summary>
public class CraftingTableModel : MonoBehaviour
{
    [Header("제작대 이름 (표시용)")]
    [SerializeField] private string tableName = "제작대";

    [Header("이 제작대에서만 제작 가능한 레시피")]
    [SerializeField]
    private List<CraftingRecipe> tableRecipes = new List<CraftingRecipe>();

    public string TableName => tableName;
    public IReadOnlyList<CraftingRecipe> TableRecipes => tableRecipes;
}