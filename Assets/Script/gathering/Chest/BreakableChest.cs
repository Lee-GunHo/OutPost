using UnityEngine;

/// <summary>
/// 설치한 상자를 벽처럼 부술 수 있게 함. 부서지면 내용물과 상자 아이템을 바닥에 떨어뜨리고 상자 저장을 삭제.
///
/// 설치된 상자는 칸 좌표로 저장 ID를 정해서 상자마다 내용물이 따로 저장되게 한다
/// (프리팹에 고정된 ID를 그대로 쓰면 모든 설치 상자가 같은 내용물을 공유함).
/// ChestModel.Start에서 저장을 불러오기 전에 ID를 정해야 해서 실행 순서를 앞당김.
/// </summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(ChestModel))]
[RequireComponent(typeof(BreakableWall))]
public class BreakableChest : MonoBehaviour
{
    [Header("Drop")]
    [Tooltip("부서질 때 돌려줄 상자 아이템")]
    [SerializeField] private ItemData chestItem;

    [Tooltip("인벤토리에서 버릴 때와 같은 범용 드롭 프리팹")]
    [SerializeField] private DroppedItemPresenter droppedItemPrefab;

    [SerializeField] private float dropSpread = 0.6f;

    private ChestModel chestModel;
    private BreakableWall breakableWall;

    private void Awake()
    {
        chestModel = GetComponent<ChestModel>();
        breakableWall = GetComponent<BreakableWall>();
        breakableWall.Broken += HandleBroken;
    }

    private void Start()
    {
        // BlockPlacementController/SeedMapView가 생성 직후(Start 전) InitializePlacedWall로 칸 좌표를 넣어줌
        BreakableWallModel wallModel = GetComponent<BreakableWallModel>();

        if (wallModel != null && wallModel.IsPlayerPlaced)
        {
            chestModel.SetPersistentChestId(
                $"PlacedChest_{wallModel.WorldSeed}_{wallModel.GlobalCellCoord.x}_{wallModel.GlobalCellCoord.y}");
        }
    }

    private void OnDestroy()
    {
        if (breakableWall != null)
            breakableWall.Broken -= HandleBroken;
    }

    private void HandleBroken(BreakableWallPresenter _)
    {
        // 열려 있던 창을 먼저 닫아야 닫힐 때의 ForceSave가 아래 저장 삭제 뒤에 다시 저장하지 않음
        if (ChestPresenter.Instance != null && ChestPresenter.Instance.IsOpenedChest(chestModel))
            ChestPresenter.Instance.Close();

        if (chestItem != null)
            SpawnDrop(chestItem, 1);

        foreach (ItemStack stack in chestModel.Items)
        {
            if (stack != null && stack.item != null && stack.amount > 0)
                SpawnDrop(stack.item, stack.amount);
        }

        // 같은 칸에 다시 상자를 설치하면 빈 상자로 시작하도록 저장 삭제
        chestModel.DeleteThisChestSave();
    }

    private void SpawnDrop(ItemData item, int amount)
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogWarning($"{name}: droppedItemPrefab이 없어 {item.itemName} x{amount}을 떨어뜨리지 못했습니다.", this);
            return;
        }

        Vector2 offset = Random.insideUnitCircle * dropSpread;
        Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);

        DroppedItemPresenter dropped = Instantiate(droppedItemPrefab, position, Quaternion.identity);
        dropped.Configure(item, amount);
    }
}
