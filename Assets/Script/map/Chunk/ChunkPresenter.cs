using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 위치를 확인하고 Model의 계산 결과에 따라
/// View와 SeedMapPresenter를 제어함.
/// </summary>
public class ChunkPresenter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("플레이어 위치를 추적하기 위해 필요")]
    [SerializeField] private Transform player;

    [Header("MVP")]
    [SerializeField] private ChunkModel model;
    [SerializeField] private ChunkView view;
    [SerializeField] private SeedMapPresenter mapPresenter;

    public int ChunkSize => model != null ? model.ChunkSize : 16;
    public int ViewDistance => model != null ? model.ViewDistance : 0;
    public int GlobalSeed => model != null ? model.GlobalSeed : 0;
    public float CellSize => mapPresenter != null ? mapPresenter.CellSize : 1f;

    // 기존 코드에서 소문자 필드를 읽던 경우를 위한 읽기 전용 호환 프로퍼티
    public int chunkSize => ChunkSize;
    public int viewDistance => ViewDistance;
    public int globalSeed => GlobalSeed;

    protected virtual void Start()
    {
        Debug.Log("1. ChunkPresenter Start 실행");

        if (!ValidateReferences())
        {
            Debug.LogError("2. ChunkPresenter 참조 검사 실패");
            enabled = false;
            return;
        }

        Debug.Log("2. ChunkPresenter 참조 검사 성공");

        ChunkModificationSaveManager saveManager =
            ChunkModificationSaveManager.GetOrCreate();

        saveManager.InitializeWorld(GlobalSeed);

        Debug.Log("3. 저장 시스템 초기화 완료");

        model.Initialize(player.position, CellSize);

        Debug.Log(
            $"4. ChunkModel 초기화 완료 / 현재 청크: {model.CurrentChunkCoord}"
        );

        RefreshChunks();

        Debug.Log("5. RefreshChunks 완료");

        model.ConfirmCurrentChunk();
    }

    protected virtual void Update()
    {
        if (!model.UpdatePlayerPosition(player.position, CellSize))
            return;

        RefreshChunks();
        model.ConfirmCurrentChunk();
    }

    public void ForceRefresh()
    {
        if (!ValidateReferences())
            return;

        model.UpdatePlayerPosition(player.position, CellSize);
        RefreshChunks();
        model.ConfirmCurrentChunk();
    }

    public Vector2Int WorldToChunkCoord(Vector3 worldPosition)
    {
        return model.WorldToChunkCoord(worldPosition, CellSize);
    }

    public Vector2Int WorldToGlobalCell(Vector3 worldPosition)
    {
        return model.WorldToGlobalCell(worldPosition, CellSize);
    }

    public Vector3 GlobalCellToWorldPosition(
        Vector2Int globalCell,
        float yOffset = 0f)
    {
        return model.GlobalCellToWorldPosition(globalCell, CellSize, yOffset);
    }

    public Vector2Int GlobalCellToChunkCoord(Vector2Int globalCell)
    {
        return model.GlobalCellToChunkCoord(globalCell);
    }

    public Vector2Int GlobalCellToLocalCell(Vector2Int globalCell)
    {
        return model.GlobalCellToLocalCell(globalCell);
    }

    public bool TryGetLoadedChunk(
        Vector2Int chunkCoord,
        out Transform chunkTransform)
    {
        chunkTransform = null;
        return view != null && view.TryGetLoadedChunk(chunkCoord, out chunkTransform);
    }

    private bool ValidateReferences()
    {
        if (player == null)
        {
            Debug.LogError("ChunkPresenter 오류: Player가 연결되지 않았습니다.", this);
            return false;
        }

        if (model == null)
        {
            Debug.LogError("ChunkPresenter 오류: ChunkModel이 연결되지 않았습니다.", this);
            return false;
        }

        if (view == null)
        {
            Debug.LogError("ChunkPresenter 오류: ChunkView가 연결되지 않았습니다.", this);
            return false;
        }

        if (mapPresenter == null)
        {
            Debug.LogError("ChunkPresenter 오류: SeedMapPresenter가 연결되지 않았습니다.", this);
            return false;
        }

        return true;
    }

    private void RefreshChunks()
    {
        foreach (Vector2Int chunkCoord in model.GetRequiredChunkCoordinates())
        {
            if (!view.ContainsChunk(chunkCoord))
                CreateChunk(chunkCoord);
        }

        List<Vector2Int> chunksToRemove =
            new List<Vector2Int>(view.LoadedChunkCoordinates);

        foreach (Vector2Int chunkCoord in chunksToRemove)
        {
            if (!model.ShouldKeepChunk(chunkCoord))
                view.RemoveChunk(chunkCoord);
        }
    }

    private void CreateChunk(Vector2Int chunkCoord)
    {
        Debug.Log($"청크 생성 시작 : {chunkCoord}");

        GameObject chunkObject = mapPresenter.GenerateChunk(
            chunkCoord,
            ChunkSize,
            GlobalSeed
        );

        Debug.Log($"GenerateChunk 반환 : {chunkCoord}");

        if (chunkObject == null)
        {
            Debug.LogError($"청크 생성 실패: {chunkCoord}", this);
            return;
        }

        if (!view.AddChunk(chunkCoord, chunkObject))
        {
            Debug.LogWarning($"청크 등록 실패 또는 중복: {chunkCoord}", this);
            Destroy(chunkObject);
            return;
        }

        Debug.Log($"청크 생성 완료 : {chunkCoord}");
    }
}