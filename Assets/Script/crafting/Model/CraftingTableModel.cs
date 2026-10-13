using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작대 오브젝트에 부착되는 모델
/// 이 제작대 등급의 전용 레시피 목록과, 바로 아래 등급 제작대 링크를 보관.
/// 제작 UI에서 화살표로 lowerTierTable을 따라 내려가며 하위 제작대 레시피도 제작 가능(코어키퍼 방식).
/// 예) 금 → 은 → 구리 → 나무
/// </summary>
public class CraftingTableModel : MonoBehaviour
{
    [Header("제작대 이름 (표시용)")]
    [SerializeField] private string tableName = "제작대";

    [Header("이 제작대 등급의 레시피")]
    [SerializeField]
    private List<CraftingRecipe> tableRecipes = new List<CraftingRecipe>();

    [Header("바로 아래 등급 제작대 (프리팹 참조 가능, 없으면 최하위)")]
    [SerializeField] private CraftingTableModel lowerTierTable;

    public string TableName => tableName;
    public IReadOnlyList<CraftingRecipe> TableRecipes => tableRecipes;
    public CraftingTableModel LowerTierTable => lowerTierTable;

    /// <summary>
    /// 자기 자신부터 최하위까지의 제작대 목록. 잘못 연결된 순환 링크는 한 번만 포함.
    /// </summary>
    public List<CraftingTableModel> GetTierChain()
    {
        List<CraftingTableModel> chain = new List<CraftingTableModel>();

        for (CraftingTableModel table = this;
             table != null && !chain.Contains(table);
             table = table.lowerTierTable)
        {
            chain.Add(table);
        }

        return chain;
    }
}
