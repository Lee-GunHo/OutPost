using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 괭이/씨앗을 들었을 때의 미리보기와 씨앗 심기(우클릭).
/// 밭 갈기와 수확은 좌클릭(공격) 경로에서 FarmManager.TryHandlePrimaryAction이 처리.
/// 괭이는 등급별 범위(FarmSettings.hoeAreas)만큼 칸마다 파랑(갈 수 있음)/빨강(불가)으로 표시.
/// FarmManager가 자동으로 붙여줌.
/// </summary>
[RequireComponent(typeof(FarmManager))]
public class FarmInputController : MonoBehaviour
{
    private const float GhostHeight = 0.3f;

    private FarmManager farm;
    private PlayerPresenter player;
    private GameObject ghostShape;
    private readonly List<BlockGhostPreview> ghosts = new List<BlockGhostPreview>();
    private int shownGhostCount;

    private void Awake()
    {
        farm = GetComponent<FarmManager>();
    }

    private void Start()
    {
        // 미리보기 모양 = 밭 한 칸 크기의 낮은 판
        ghostShape = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ghostShape.name = "FarmGhostShape";
        Destroy(ghostShape.GetComponent<Collider>());
        ghostShape.transform.SetParent(transform, false);
        ghostShape.transform.localScale = new Vector3(0.96f, GhostHeight, 0.96f);
        ghostShape.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (BlockGhostPreview ghost in ghosts)
            ghost.Dispose();
    }

    private void Update()
    {
        shownGhostCount = 0;
        UpdatePreviewAndPlanting();

        for (int i = shownGhostCount; i < ghosts.Count; i++)
            ghosts[i].Hide();
    }

    private void UpdatePreviewAndPlanting()
    {
        if (ghostShape == null || !TryGetContext(out ItemData heldItem, out Vector2Int cell))
            return;

        bool isHoe = heldItem.toolType == ToolType.Hoe;
        CropData seedCrop = farm.Settings.FindBySeed(heldItem);

        if (!isHoe && seedCrop == null)
            return;

        if (!farm.IsInRange(cell, player.transform.position, player.BreakRange))
            return;

        if (seedCrop != null)
        {
            bool canPlant = farm.CanPlant(cell);
            ShowGhost(cell, canPlant);

            if (canPlant && Mouse.current.rightButton.wasPressedThisFrame)
                TryPlant(cell, seedCrop, heldItem);

            return;
        }

        // 괭이: 빈 밭을 가리키면 그 한 칸 되돌리기, 아니면 범위만큼 갈기
        if (farm.CanPlant(cell))
        {
            ShowGhost(cell, true);
            return;
        }

        foreach (Vector2Int areaCell in farm.GetHoeCells(heldItem, cell, player.transform.position))
            ShowGhost(areaCell, farm.CanTill(areaCell));
    }

    private void ShowGhost(Vector2Int cell, bool valid)
    {
        if (shownGhostCount >= ghosts.Count)
            ghosts.Add(new BlockGhostPreview(null, 0.02f));

        // ghostShape는 중심 기준 큐브라 높이 절반만큼 올려 바닥에 붙임
        ghosts[shownGhostCount].Show(ghostShape,
            farm.CellToWorld(cell) + Vector3.up * (GhostHeight * 0.5f),
            valid ? farm.Settings.ghostValidColor : farm.Settings.ghostInvalidColor);

        shownGhostCount++;
    }

    private void TryPlant(Vector2Int cell, CropData crop, ItemData seed)
    {
        HotbarPresenter hotbar = HotbarPresenter.Instance;

        if (hotbar == null)
            return;

        ItemStack selected = hotbar.GetSelectedItem();
        if (selected == null || selected.item != seed || selected.amount <= 0)
            return;

        if (!farm.Plant(cell, crop))
            return;

        hotbar.ConsumeSelectedItem(1);
        player.FaceActionTarget(farm.CellToWorld(cell));
    }

    private bool TryGetContext(out ItemData heldItem, out Vector2Int cell)
    {
        heldItem = null;
        cell = default;

        if (farm == null || farm.Chunks == null || farm.Settings == null)
            return false;

        if (UIState.IsAnyUIOpen || Mouse.current == null)
            return false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (player == null)
            player = FindFirstObjectByType<PlayerPresenter>();

        if (player == null || HotbarPresenter.Instance == null)
            return false;

        ItemStack selected = HotbarPresenter.Instance.GetSelectedItem();
        if (selected == null || selected.item == null || selected.amount <= 0)
            return false;

        heldItem = selected.item;
        return farm.TryGetMouseCell(out cell);
    }
}
