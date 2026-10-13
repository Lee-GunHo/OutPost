using UnityEngine;

/// <summary>
/// 설치한 블록(제작대, 항아리 등)이 부서지면 그 블록의 아이템을 바닥에 떨어뜨려 돌려줌.
/// 내용물이 있는 상자는 BreakableChest를 사용.
/// </summary>
[RequireComponent(typeof(BreakableWall))]
public class PlacedBlockItemDrop : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int amount = 1;

    [Tooltip("인벤토리에서 버릴 때와 같은 범용 드롭 프리팹")]
    [SerializeField] private DroppedItemPresenter droppedItemPrefab;

    private BreakableWall breakableWall;

    private void Awake()
    {
        breakableWall = GetComponent<BreakableWall>();
        breakableWall.Broken += HandleBroken;
    }

    private void OnDestroy()
    {
        if (breakableWall != null)
            breakableWall.Broken -= HandleBroken;
    }

    private void HandleBroken(BreakableWallPresenter _)
    {
        if (item == null || droppedItemPrefab == null)
        {
            Debug.LogWarning($"{name}: 드롭할 아이템 또는 드롭 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        DroppedItemPresenter dropped = Instantiate(droppedItemPrefab, transform.position, Quaternion.identity);
        dropped.Configure(item, amount);
    }
}
