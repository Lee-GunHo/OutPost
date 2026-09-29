using System.Collections;
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

    [Header("Chunk Streaming")]
    [Tooltip("청크 생성에 한 프레임당 쓸 수 있는 최대 시간(밀리초). " +
        "칸 개수가 아니라 실제 걸린 시간 기준이라 기기 성능과 무관하게 프레임당 부담이 일정함. " +
        "낮을수록 버벅임은 줄지만 청크가 채워지는 속도는 느려짐")]
    [Min(0.1f)]
    [SerializeField] private float frameTimeBudgetMs = 4f;

    [Header("Nexus")]
    [Tooltip("게임 시작 시 청크 시스템과 연동되어 설치될 넥서스 프리팹")]
    [SerializeField] private GameObject nexusPrefab;

    [Tooltip("넥서스가 설치될 월드 좌표")]
    [SerializeField] private Vector3 nexusSpawnPosition = new Vector3(0f, 1f, 0f);

    private GameObject spawnedNexus;

    // 아직 생성되지 않은 청크 좌표 대기열. RefreshChunks는 여기에 쌓기만 하고,
    // 실제 셀 생성은 ProcessChunkQueue 코루틴이 프레임 예산만큼만 나눠서 처리함.
    private readonly Queue<Vector2Int> pendingChunkCoords = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> queuedChunkCoords = new HashSet<Vector2Int>();

    // 제거할 청크 좌표 대기열. 대각선 이동 등으로 여러 청크가 한 번에 범위를 벗어나면
    // RemoveChunk를 그 프레임에 전부 몰아서 부르는 대신 한 프레임에 하나씩만 처리함.
    private readonly Queue<Vector2Int> pendingRemovalCoords = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> queuedRemovalCoords = new HashSet<Vector2Int>();

    // 플레이어가 지금 서 있는 청크. 대기열 순서를 무시하고 다음 차례에 가장 먼저 처리됨
    // (동기 생성은 아니라서 여전히 frameTimeBudgetMs 예산만큼 프레임에 걸쳐 채워짐).
    private Vector2Int? priorityChunkCoord;

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

        StartCoroutine(ProcessChunkQueue());

        RefreshChunks();

        Debug.Log("5. RefreshChunks 완료(대기열 등록, 실제 생성은 프레임에 걸쳐 진행)");

        model.ConfirmCurrentChunk();

        SpawnNexus();
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

    /// <summary>
    /// worldPosition이 속한 청크가 아직 없으면 대기열을 거치지 않고 그 자리에서 즉시 생성함.
    /// 순간이동/세이브 위치 복구처럼 "지금 당장 바닥이 있어야 하는" 상황에서
    /// ChunkPresenter의 매 프레임 감지를 기다리지 않고 직접 호출하기 위한 API.
    /// </summary>
    public void EnsureChunkLoaded(Vector3 worldPosition)
    {
        if (!ValidateReferences())
            return;

        Vector2Int chunkCoord = model.WorldToChunkCoord(worldPosition, CellSize);

        if (!view.ContainsChunk(chunkCoord))
            BuildChunkImmediately(chunkCoord);
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
        // 플레이어가 지금 서 있는 청크는 여전히 프레임 예산만큼씩 나눠 만들지만,
        // 대기열의 다른 청크보다 먼저 처리되도록 우선순위로 등록함.
        // (매 프레임 통째로 동기 생성하면 청크 경계를 넘을 때마다 버벅임이 재발함)
        Vector2Int currentChunkCoord = model.CurrentChunkCoord;

        if (!view.ContainsChunk(currentChunkCoord))
            priorityChunkCoord = currentChunkCoord;

        foreach (Vector2Int chunkCoord in model.GetRequiredChunkCoordinates())
        {
            if (view.ContainsChunk(chunkCoord) || queuedChunkCoords.Contains(chunkCoord))
                continue;

            queuedChunkCoords.Add(chunkCoord);
            pendingChunkCoords.Enqueue(chunkCoord);
        }

        List<Vector2Int> chunksToRemove =
            new List<Vector2Int>(view.LoadedChunkCoordinates);

        foreach (Vector2Int chunkCoord in chunksToRemove)
        {
            if (model.ShouldKeepChunk(chunkCoord) || queuedRemovalCoords.Contains(chunkCoord))
                continue;

            queuedRemovalCoords.Add(chunkCoord);
            pendingRemovalCoords.Enqueue(chunkCoord);
        }
    }

    /// <summary>
    /// 프레임 분산 없이 셀 생성을 한 번에 끝까지 진행함. 매 프레임 도는 RefreshChunks에서는
    /// 쓰지 않고(버벅임의 원인이 됨), 세이브 위치 복구처럼 일회성으로 즉시 바닥이 있어야 하는
    /// EnsureChunkLoaded 호출에서만 사용함.
    /// </summary>
    private void BuildChunkImmediately(Vector2Int chunkCoord)
    {
        GameObject chunkObject = null;

        RunToCompletion(mapPresenter.GenerateChunkRoutine(
            chunkCoord,
            ChunkSize,
            GlobalSeed,
            frameTimeBudgetMs,
            result => chunkObject = result
        ));

        if (chunkObject == null)
        {
            Debug.LogError($"청크 생성 실패: {chunkCoord}", this);
            return;
        }

        if (!view.AddChunk(chunkCoord, chunkObject))
        {
            Debug.LogWarning($"청크 등록 실패 또는 중복: {chunkCoord}", this);
            ChunkView.ReleaseChunkObject(chunkObject);
        }
    }

    /// <summary>
    /// 코루틴을 프레임 대기 없이 한 번에 끝까지 실행함.
    /// GenerateChunkRoutine처럼 내부에서 다른 IEnumerator를 다시 yield하는 경우,
    /// StartCoroutine을 거치지 않으면 Unity가 그 안쪽까지 자동으로 실행해주지 않으므로
    /// 스택을 이용해 중첩된 IEnumerator까지 직접 끝까지 진행시킴.
    /// </summary>
    private static void RunToCompletion(IEnumerator routine)
    {
        Stack<IEnumerator> stack = new Stack<IEnumerator>();
        stack.Push(routine);

        while (stack.Count > 0)
        {
            IEnumerator current = stack.Peek();

            if (!current.MoveNext())
            {
                stack.Pop();
                continue;
            }

            if (current.Current is IEnumerator nested)
                stack.Push(nested);
        }
    }

    /// <summary>
    /// 대기열에 쌓인 청크를 한 번에 하나씩 처리함: 제거는 프레임당 1개씩,
    /// 생성은 frameTimeBudgetMs만큼 시간을 쓰면 한 프레임을 양보하며 진행함.
    /// 대각선 이동 등으로 여러 청크가 한 번에 범위를 벗어나거나 새로 필요해져도
    /// 한 프레임에 몰아서 처리하지 않도록 이 코루틴이 여러 프레임으로 펼쳐서 처리함.
    /// </summary>
    private IEnumerator ProcessChunkQueue()
    {
        while (true)
        {
            if (pendingRemovalCoords.Count > 0)
            {
                Vector2Int removeCoord = pendingRemovalCoords.Dequeue();
                queuedRemovalCoords.Remove(removeCoord);

                // 대기하는 동안 플레이어가 되돌아와서 다시 필요해진 청크는 지우지 않음
                if (view.ContainsChunk(removeCoord) && !model.ShouldKeepChunk(removeCoord))
                    view.RemoveChunk(removeCoord);

                yield return null;
                continue;
            }

            Vector2Int chunkCoord;

            if (priorityChunkCoord.HasValue)
            {
                chunkCoord = priorityChunkCoord.Value;
                priorityChunkCoord = null;
                queuedChunkCoords.Remove(chunkCoord);
            }
            else if (pendingChunkCoords.Count > 0)
            {
                chunkCoord = pendingChunkCoords.Dequeue();
                queuedChunkCoords.Remove(chunkCoord);
            }
            else
            {
                yield return null;
                continue;
            }

            // 대기하는 동안 플레이어가 이미 지나갔거나 다른 경로로 먼저 로드된 좌표는 건너뜀
            if (view.ContainsChunk(chunkCoord) || !model.ShouldKeepChunk(chunkCoord))
                continue;

            Debug.Log($"청크 생성 시작 : {chunkCoord}");

            GameObject chunkObject = null;

            yield return mapPresenter.GenerateChunkRoutine(
                chunkCoord,
                ChunkSize,
                GlobalSeed,
                frameTimeBudgetMs,
                result => chunkObject = result
            );

            if (chunkObject == null)
            {
                Debug.LogError($"청크 생성 실패: {chunkCoord}", this);
                continue;
            }

            if (!view.AddChunk(chunkCoord, chunkObject))
            {
                Debug.LogWarning($"청크 등록 실패 또는 중복: {chunkCoord}", this);
                ChunkView.ReleaseChunkObject(chunkObject);
                continue;
            }

            Debug.Log($"청크 생성 완료 : {chunkCoord}");
        }
    }

    /// <summary>
    /// 청크 시스템 초기화에 맞춰 넥서스를 지정된 월드 좌표에 설치
    /// </summary>
    private void SpawnNexus()
    {
        if (spawnedNexus != null)
            return;

        if (nexusPrefab == null)
        {
            Debug.LogWarning("ChunkPresenter 경고: NexusPrefab이 연결되지 않아 넥서스를 설치할 수 없습니다.", this);
            return;
        }

        spawnedNexus = Instantiate(nexusPrefab, nexusSpawnPosition, Quaternion.identity);
        spawnedNexus.name = "Nexus";

        Debug.Log($"6. 넥서스 설치 완료 / 위치: {nexusSpawnPosition}");
    }
}