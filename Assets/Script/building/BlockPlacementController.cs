using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 인벤토리에서 첫 번째 설치 가능 아이템을 찾아 블록을 설치
/// </summary>
public class BlockPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPresenter playerPresenter;
    [SerializeField] private HotbarPresenter hotbarPresenter;
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private Camera mainCamera;

    [Header("Grid Preview")]
    [SerializeField] private GameObject gridPreview;
    [SerializeField] private Renderer gridPreviewRenderer;
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.45f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.45f);
    [SerializeField] private float previewYOffset = 0.03f;
    [SerializeField] private bool autoScalePreview = true;

    [Header("Placement Check")]
    [Tooltip("Floor 레이어는 제외하고 Wall, Player, NPC, Building 등을 포함하세요.")]
    [SerializeField] private LayerMask placementBlockerMask;
    [SerializeField] private float checkCenterY = 1f;
    [SerializeField] private float checkHeight = 1.8f;

    [Range(0.1f, 1f)]
    [SerializeField] private float checkWidthRatio = 0.9f;

    [SerializeField] private float groundY = 0f;

    private PlacedBlockSaveManager placedBlockSaveManager;
    private Material previewMaterial;
    private Collider[] playerColliders;

    private Vector3 currentGridWorldPosition;
    private bool canPlaceCurrentCell;

    private void Awake()
    {
        if (playerPresenter == null)
            playerPresenter = GetComponent<PlayerPresenter>();

        if (playerPresenter != null)
            playerColliders = playerPresenter.GetComponentsInChildren<Collider>(true);

        if (mainCamera == null)
            mainCamera = Camera.main;

        placedBlockSaveManager = PlacedBlockSaveManager.GetOrCreate();

        if (gridPreviewRenderer != null)
            previewMaterial = gridPreviewRenderer.material;

        HidePreview();
    }

    private void Start()
    {
        if (chunkManager == null)
            return;

        placedBlockSaveManager.InitializeWorld(chunkManager.GlobalSeed);

        if (autoScalePreview && gridPreview != null)
        {
            float cellSize = chunkManager.CellSize;
            gridPreview.transform.localScale = new Vector3(cellSize, 1f, cellSize);
        }
    }

    private void Update()
    {
        if (!CanProcessPlacement())
        {
            HidePreview();
            return;
        }

        // 인벤토리 앞쪽 슬롯부터 설치 가능한 아이템을 자동 검색
        if (!TryGetPlaceableItem(out ItemData placeableItem))
        {
            //Debug.LogWarning("현재 Hotbar 선택 아이템은 설치할 수 없습니다.");
            HidePreview();
            return;
        }

        //Debug.Log("설치 아이템 감지: " + placeableItem.itemName);

        if (!TryGetMouseGlobalCell(out Vector2Int globalCell))
        {
            HidePreview();
            return;
        }

        currentGridWorldPosition = chunkManager.GlobalCellToWorldPosition(
            globalCell,
            previewYOffset
        );

        // 기존 벽을 부술 수 있는 거리와 동일한 범위에서만 표시 및 설치
        if (!IsWithinBreakRange(currentGridWorldPosition))
        {
            HidePreview();
            return;
        }

        canPlaceCurrentCell = IsCellPlaceable(globalCell);
        ShowPreview(currentGridWorldPosition, canPlaceCurrentCell);

        if (!canPlaceCurrentCell)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            PlaceBlock(placeableItem, globalCell);
    }

    private bool CanProcessPlacement()
    {
        if (UIState.IsAnyUIOpen)
            return false;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        if (playerPresenter == null ||
            hotbarPresenter == null ||
            chunkManager == null)
        {
            return false;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        return mainCamera != null && Mouse.current != null;
    }

    private bool TryGetPlaceableItem(out ItemData placeableItem)
    {
        placeableItem = null;

        if (hotbarPresenter == null)
            return false;

        ItemStack selectedStack = hotbarPresenter.GetSelectedItem();

        if (selectedStack == null ||
            selectedStack.item == null ||
            selectedStack.amount <= 0)
        {
            return false;
        }

        ItemData itemData = selectedStack.item;

        // 설치 가능한 아이템인지 확인
        if (itemData.toolType != ToolType.Placeable)
            return false;

        // 설치할 프리팹이 존재하는지 확인
        if (itemData.placeablePrefab == null)
            return false;

        placeableItem = itemData;
        return true;
    }

    private bool TryGetMouseGlobalCell(out Vector2Int globalCell)
    {
        globalCell = default;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane groundPlane = new Plane(
            Vector3.up,
            new Vector3(0f, groundY, 0f)
        );

        if (!groundPlane.Raycast(ray, out float enter))
            return false;

        Vector3 mouseWorldPosition = ray.GetPoint(enter);
        globalCell = chunkManager.WorldToGlobalCell(mouseWorldPosition);
        return true;
    }

    private bool IsWithinBreakRange(Vector3 targetPosition)
    {
        Vector3 playerPosition = playerPresenter.transform.position;
        playerPosition.y = 0f;

        targetPosition.y = 0f;

        return Vector3.Distance(playerPosition, targetPosition) <=
               playerPresenter.BreakRange;
    }

    private bool IsCellPlaceable(Vector2Int globalCell)
    {
        Vector2Int chunkCoord =
            chunkManager.GlobalCellToChunkCoord(globalCell);

        if (!chunkManager.TryGetLoadedChunk(chunkCoord, out _))
            return false;

        if (placedBlockSaveManager.TryGetPlacedBlock(
            chunkManager.GlobalSeed,
            globalCell,
            out _))
        {
            return false;
        }

        // 플레이어가 현재 서 있는 셀 또는 실제 몸체와 겹치는 셀에는 설치하지 않음
        if(IsPlayerOccupyingCell(globalCell)) 
            return false;

        float cellSize = chunkManager.CellSize;
        float width = cellSize * checkWidthRatio;

        Vector3 checkCenter = chunkManager.GlobalCellToWorldPosition(
            globalCell,
            checkCenterY
        );

        Vector3 halfExtents = new Vector3(
            width * 0.5f,
            checkHeight * 0.5f,
            width * 0.5f
        );

        return !Physics.CheckBox(
            checkCenter,
            halfExtents,
            Quaternion.identity,
            placementBlockerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    /// <summary>
    /// 플레이어가 서 있는 그리드 칸과 플레이어의 실제 비-Trigger 콜라이더가
    /// 겹치는 칸을 설치 불가로 판정
    /// </summary>
    private bool IsPlayerOccupyingCell(Vector2Int globalCell)
    {
        if (playerPresenter == null || chunkManager == null)
            return false;

        // 콜라이더 설정과 관계없이 플레이어 기준 좌표가 속한 셀은 항상 차단
        Vector2Int playerCell =
            chunkManager.WorldToGlobalCell(playerPresenter.transform.position);

        if (playerCell == globalCell)
            return true;

        if (playerColliders == null || playerColliders.Length == 0)
            return false;

        float cellSize = chunkManager.CellSize;
        float width = cellSize * checkWidthRatio;

        Vector3 cellCenter = chunkManager.GlobalCellToWorldPosition(
            globalCell,
            checkCenterY
        );

        Bounds cellBounds = new Bounds(
            cellCenter,
            new Vector3(width, checkHeight, width)
        );

        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == null ||
                !playerCollider.enabled ||
                playerCollider.isTrigger ||
                !playerCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (playerCollider.bounds.Intersects(cellBounds))
                return true;
        }

        return false;
    }

    private void PlaceBlock(ItemData itemData, Vector2Int globalCell)
    {
        if (itemData == null || itemData.placeablePrefab == null)
            return;

        // 설치 직전에 아이템이 아직 존재하는지 다시 확인
        ItemStack selectedStack = hotbarPresenter.GetSelectedItem();

        if (selectedStack == null ||
            selectedStack.item != itemData ||
            selectedStack.amount <= 0)
        {
            HidePreview();
            return;
        }

        Vector2Int chunkCoord =
            chunkManager.GlobalCellToChunkCoord(globalCell);

        Vector2Int localCellCoord =
            chunkManager.GlobalCellToLocalCell(globalCell);

        if (!chunkManager.TryGetLoadedChunk(
            chunkCoord,
            out Transform chunkTransform))
        {
            return;
        }

        bool registered = placedBlockSaveManager.RegisterPlacedBlock(
            chunkManager.GlobalSeed,
            globalCell,
            itemData.itemID
        );

        if (!registered)
            return;

        Vector3 blockPosition = chunkManager.GlobalCellToWorldPosition(
            globalCell,
            itemData.placeableYOffset
        );

        GameObject blockObject = Instantiate(
            itemData.placeablePrefab,
            blockPosition,
            Quaternion.identity,
            chunkTransform
        );

        blockObject.name = itemData.placeablePrefab.name;

        BreakableWall[] breakableWalls =
            blockObject.GetComponentsInChildren<BreakableWall>(true);

        foreach (BreakableWall breakableWall in breakableWalls)
        {
            breakableWall.InitializePlacedWall(
                chunkManager.GlobalSeed,
                chunkCoord,
                localCellCoord,
                globalCell,
                blockObject
            );
        }

        // 인벤토리에서 블록 1개를 차감
        bool consumed = hotbarPresenter.ConsumeSelectedItem(1);

        if (!consumed)
        {
            placedBlockSaveManager.RemovePlacedBlock(
                chunkManager.GlobalSeed,
                globalCell
            );

            Destroy(blockObject);
            return;
        }

        canPlaceCurrentCell = false;
        ShowPreview(currentGridWorldPosition, false);
    }

    private void ShowPreview(Vector3 position, bool isValid)
    {
        if (gridPreview == null)
            return;

        gridPreview.transform.position = position;
        gridPreview.SetActive(true);

        if (previewMaterial != null)
            previewMaterial.color = isValid ? validColor : invalidColor;
    }

    private void HidePreview()
    {
        canPlaceCurrentCell = false;

        if (gridPreview != null)
            gridPreview.SetActive(false);
    }
}