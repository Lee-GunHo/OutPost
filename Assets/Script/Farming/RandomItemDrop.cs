using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부서질 때(BreakableWall 완전 파괴) 확률적으로 목록 중 하나를 떨어뜨림. 예) 나무 → 씨앗 3종 중 하나 20%
/// 기존 드롭(BreakableWallModel.dropItemPrefab, 예: 나무 목재)과 별개로 추가로 떨어짐.
/// </summary>
[RequireComponent(typeof(BreakableWall))]
public class RandomItemDrop : MonoBehaviour
{
    [Tooltip("이 중 하나를 같은 확률로 고름")]
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 0.2f;

    [Min(1)]
    [SerializeField] private int amount = 1;

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
        if (items.Count == 0 || droppedItemPrefab == null || Random.value >= dropChance)
            return;

        ItemData item = items[Random.Range(0, items.Count)];
        if (item == null)
            return;

        Vector2 offset = Random.insideUnitCircle * 0.4f;
        DroppedItemPresenter dropped = Instantiate(droppedItemPrefab,
            transform.position + new Vector3(offset.x, 0.3f, offset.y), Quaternion.identity);
        dropped.Configure(item, amount);
    }
}
