using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 절차적으로 생성된 벽과 나무의 파괴 상태를 저장
///
/// 핵심 식별자는 "월드 타일 좌표"
/// 청크가 언로드되거나 다시 생성되어도 같은 타일은 항상 같은 키를 사용
/// 기존 청크 좌표 + 로컬 좌표 방식도 함께 보관하여 이전 저장 파일과 호환
/// </summary>
public class ChunkModificationSaveManager : MonoBehaviour
{
    public const string ObjectTypeWall = "Wall";
    public const string ObjectTypeTree = "Tree";

    private const int CurrentSaveVersion = 2;

    private static ChunkModificationSaveManager instance;
    public static ChunkModificationSaveManager Instance => instance;

    [Header("Save Settings")]
    [Tooltip("저장, 조회, 불러오기 과정을 Console에 출력합니다.")]
    [SerializeField] private bool showDebugLog = true;

    private int currentWorldSeed;
    private bool isInitialized;

    private WorldModificationSaveData saveData = new WorldModificationSaveData();

    // 새 저장 방식: 월드 타일 좌표 기반 키
    private readonly HashSet<string> destroyedGlobalKeys = new HashSet<string>();

    // 이전 저장 방식과의 호환용: 청크 + 로컬 좌표 기반 키
    private readonly HashSet<string> destroyedLegacyKeys = new HashSet<string>();

    [Serializable]
    private class WorldModificationSaveData
    {
        public int saveVersion = CurrentSaveVersion;
        public int worldSeed;
        public List<DestroyedObjectRecord> destroyedObjects = new List<DestroyedObjectRecord>();
    }

    [Serializable]
    private class DestroyedObjectRecord
    {
        public string objectType;

        // 버전 2 식별자
        public bool hasGlobalCell;
        public int globalCellX;
        public int globalCellZ;

        // 버전 1 호환 식별자
        public int chunkX;
        public int chunkZ;
        public int localX;
        public int localZ;

        public DestroyedObjectRecord() { }

        public DestroyedObjectRecord(
            Vector2Int chunkCoord,
            Vector2Int localCellCoord,
            Vector2Int globalCellCoord,
            string objectType)
        {
            this.objectType = objectType;

            hasGlobalCell = true;
            globalCellX = globalCellCoord.x;
            globalCellZ = globalCellCoord.y;

            chunkX = chunkCoord.x;
            chunkZ = chunkCoord.y;
            localX = localCellCoord.x;
            localZ = localCellCoord.y;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static ChunkModificationSaveManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<ChunkModificationSaveManager>();

        if (instance != null)
            return instance;

        GameObject managerObject = new GameObject("ChunkModificationSaveManager");
        instance = managerObject.AddComponent<ChunkModificationSaveManager>();
        return instance;
    }

    public void InitializeWorld(int worldSeed)
    {
        if (isInitialized && currentWorldSeed == worldSeed)
            return;

        if (isInitialized)
            SaveNow();

        currentWorldSeed = worldSeed;
        isInitialized = true;
        LoadCurrentWorld();
    }

    public bool IsWallDestroyed(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord)
    {
        return IsObjectDestroyed(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ObjectTypeWall
        );
    }

    public bool IsTreeDestroyed(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord)
    {
        return IsObjectDestroyed(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ObjectTypeTree
        );
    }

    public void RegisterDestroyedWall(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord)
    {
        RegisterDestroyedObject(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ObjectTypeWall
        );
    }

    public void RegisterDestroyedTree(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord)
    {
        RegisterDestroyedObject(
            worldSeed,
            chunkCoord,
            localCellCoord,
            globalCellCoord,
            ObjectTypeTree
        );
    }

    // 이전 코드 호환용 오버로드
    public bool IsWallDestroyed(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord)
    {
        EnsureWorldInitialized(worldSeed);
        return destroyedLegacyKeys.Contains(
            CreateLegacyKey(chunkCoord, localCellCoord, ObjectTypeWall)
        );
    }

    public bool IsTreeDestroyed(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord)
    {
        EnsureWorldInitialized(worldSeed);
        return destroyedLegacyKeys.Contains(
            CreateLegacyKey(chunkCoord, localCellCoord, ObjectTypeTree)
        );
    }

    public bool IsObjectDestroyed(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        string objectType)
    {
        EnsureWorldInitialized(worldSeed);

        string normalizedType = NormalizeObjectType(objectType);
        string globalKey = CreateGlobalKey(globalCellCoord, normalizedType);
        string legacyKey = CreateLegacyKey(chunkCoord, localCellCoord, normalizedType);

        bool destroyed =
            destroyedGlobalKeys.Contains(globalKey) ||
            destroyedLegacyKeys.Contains(legacyKey);

        if (showDebugLog && destroyed)
        {
            Debug.Log(
                $"[{normalizedType} 생성 생략] " +
                $"GlobalCell({globalCellCoord.x}, {globalCellCoord.y}) " +
                $"Chunk({chunkCoord.x}, {chunkCoord.y}) " +
                $"Cell({localCellCoord.x}, {localCellCoord.y})"
            );
        }

        return destroyed;
    }

    public void RegisterDestroyedObject(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        Vector2Int globalCellCoord,
        string objectType)
    {
        EnsureWorldInitialized(worldSeed);

        string normalizedType = NormalizeObjectType(objectType);
        string globalKey = CreateGlobalKey(globalCellCoord, normalizedType);
        string legacyKey = CreateLegacyKey(chunkCoord, localCellCoord, normalizedType);

        bool alreadyRegistered =
            destroyedGlobalKeys.Contains(globalKey) ||
            destroyedLegacyKeys.Contains(legacyKey);

        if (!alreadyRegistered)
        {
            destroyedGlobalKeys.Add(globalKey);
            destroyedLegacyKeys.Add(legacyKey);

            saveData.destroyedObjects.Add(
                new DestroyedObjectRecord(
                    chunkCoord,
                    localCellCoord,
                    globalCellCoord,
                    normalizedType
                )
            );
        }

        // 메모리에 실제 등록되었는지 즉시 검증
        bool registered =
            destroyedGlobalKeys.Contains(globalKey) &&
            destroyedLegacyKeys.Contains(legacyKey);

        if (!registered)
        {
            Debug.LogError(
                $"{normalizedType} 파괴 상태 등록 실패: " +
                $"GlobalKey={globalKey}, LegacyKey={legacyKey}"
            );
            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                $"[{normalizedType} 파괴 상태 등록 완료] " +
                $"GlobalCell({globalCellCoord.x}, {globalCellCoord.y}) " +
                $"Chunk({chunkCoord.x}, {chunkCoord.y}) " +
                $"Cell({localCellCoord.x}, {localCellCoord.y})"
            );
        }

        // 영구 저장 기능이므로 파괴 즉시 파일에 기록
        // Inspector의 이전 직렬화 값 때문에 저장이 꺼지는 문제를 막기 위해 항상 저장
        SaveNow();
    }

    // 이전 코드 호환용 새 SeedMapGenerator에서는 사용하지 않음
    public void RegisterDestroyedObject(
        int worldSeed,
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        string objectType)
    {
        EnsureWorldInitialized(worldSeed);

        string normalizedType = NormalizeObjectType(objectType);
        string legacyKey = CreateLegacyKey(chunkCoord, localCellCoord, normalizedType);

        if (!destroyedLegacyKeys.Add(legacyKey))
            return;

        saveData.destroyedObjects.Add(
            new DestroyedObjectRecord
            {
                objectType = normalizedType,
                hasGlobalCell = false,
                chunkX = chunkCoord.x,
                chunkZ = chunkCoord.y,
                localX = localCellCoord.x,
                localZ = localCellCoord.y
            }
        );

        // 영구 저장 기능이므로 파괴 즉시 파일에 기록
        // Inspector의 이전 직렬화 값 때문에 저장이 꺼지는 문제를 막기 위해 항상 저장
        SaveNow();
    }

    public void SaveNow()
    {
        if (!isInitialized)
            return;

        try
        {
            saveData.saveVersion = CurrentSaveVersion;
            saveData.worldSeed = currentWorldSeed;

            string savePath = GetSaveFilePath(currentWorldSeed);
            string tempPath = savePath + ".tmp";
            string json = JsonUtility.ToJson(saveData, true);

            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            File.WriteAllText(tempPath, json);

            if (File.Exists(savePath))
                File.Delete(savePath);

            File.Move(tempPath, savePath);

            if (showDebugLog)
            {
                Debug.Log(
                    $"청크 변경사항 JSON 저장 완료: " +
                    $"{saveData.destroyedObjects.Count}개\n{savePath}"
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("청크 변경사항 저장 실패: " + exception);
        }
    }

    [ContextMenu("Print Current Save Information")]
    private void PrintCurrentSaveInformation()
    {
        if (!isInitialized)
        {
            Debug.Log("저장 관리자가 아직 초기화되지 않았습니다.");
            return;
        }

        Debug.Log(
            $"현재 월드 시드: {currentWorldSeed}\n" +
            $"파괴 기록 수: {saveData.destroyedObjects.Count}\n" +
            $"저장 경로: {GetSaveFilePath(currentWorldSeed)}"
        );
    }

    [ContextMenu("Delete Current World Save")]
    public void DeleteCurrentWorldSave()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("삭제할 월드가 아직 초기화되지 않았습니다.");
            return;
        }

        destroyedGlobalKeys.Clear();
        destroyedLegacyKeys.Clear();

        saveData = new WorldModificationSaveData
        {
            saveVersion = CurrentSaveVersion,
            worldSeed = currentWorldSeed,
            destroyedObjects = new List<DestroyedObjectRecord>()
        };

        string savePath = GetSaveFilePath(currentWorldSeed);
        string tempPath = savePath + ".tmp";

        try
        {
            if (File.Exists(savePath))
                File.Delete(savePath);

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            Debug.Log("현재 월드의 청크 변경사항을 초기화했습니다: " + savePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("청크 변경사항 파일 삭제 실패: " + exception);
        }
    }

    private void EnsureWorldInitialized(int worldSeed)
    {
        if (!isInitialized || currentWorldSeed != worldSeed)
            InitializeWorld(worldSeed);
    }

    private void LoadCurrentWorld()
    {
        destroyedGlobalKeys.Clear();
        destroyedLegacyKeys.Clear();

        string savePath = GetSaveFilePath(currentWorldSeed);

        if (!File.Exists(savePath))
        {
            saveData = new WorldModificationSaveData
            {
                saveVersion = CurrentSaveVersion,
                worldSeed = currentWorldSeed,
                destroyedObjects = new List<DestroyedObjectRecord>()
            };

            if (showDebugLog)
                Debug.Log("기존 저장 파일이 없어 새 데이터를 사용합니다: " + savePath);

            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            WorldModificationSaveData loadedData =
                JsonUtility.FromJson<WorldModificationSaveData>(json);

            if (loadedData == null || loadedData.worldSeed != currentWorldSeed)
            {
                Debug.LogWarning("저장 파일의 월드 시드가 현재 시드와 달라 새 데이터를 사용합니다.");
                CreateEmptySaveData();
                return;
            }

            if (loadedData.destroyedObjects == null)
                loadedData.destroyedObjects = new List<DestroyedObjectRecord>();

            saveData = loadedData;

            foreach (DestroyedObjectRecord record in saveData.destroyedObjects)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.objectType))
                    continue;

                string normalizedType = NormalizeObjectType(record.objectType);

                Vector2Int chunkCoord = new Vector2Int(record.chunkX, record.chunkZ);
                Vector2Int localCellCoord = new Vector2Int(record.localX, record.localZ);

                destroyedLegacyKeys.Add(
                    CreateLegacyKey(chunkCoord, localCellCoord, normalizedType)
                );

                if (record.hasGlobalCell)
                {
                    Vector2Int globalCellCoord =
                        new Vector2Int(record.globalCellX, record.globalCellZ);

                    destroyedGlobalKeys.Add(
                        CreateGlobalKey(globalCellCoord, normalizedType)
                    );
                }
            }

            if (showDebugLog)
            {
                Debug.Log(
                    $"청크 변경사항 불러오기 완료: " +
                    $"{saveData.destroyedObjects.Count}개\n{savePath}"
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("청크 변경사항 불러오기 실패: " + exception);
            CreateEmptySaveData();
        }
    }

    private void CreateEmptySaveData()
    {
        destroyedGlobalKeys.Clear();
        destroyedLegacyKeys.Clear();

        saveData = new WorldModificationSaveData
        {
            saveVersion = CurrentSaveVersion,
            worldSeed = currentWorldSeed,
            destroyedObjects = new List<DestroyedObjectRecord>()
        };
    }

    private string GetSaveFilePath(int worldSeed)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"chunk_modifications_{worldSeed}.json"
        );
    }

    private static string NormalizeObjectType(string objectType)
    {
        if (string.Equals(objectType, ObjectTypeTree, StringComparison.OrdinalIgnoreCase))
            return ObjectTypeTree;

        return ObjectTypeWall;
    }

    private static string CreateGlobalKey(
        Vector2Int globalCellCoord,
        string objectType)
    {
        return $"{objectType}:{globalCellCoord.x}:{globalCellCoord.y}";
    }

    private static string CreateLegacyKey(
        Vector2Int chunkCoord,
        Vector2Int localCellCoord,
        string objectType)
    {
        return
            $"{objectType}:" +
            $"{chunkCoord.x}:{chunkCoord.y}:" +
            $"{localCellCoord.x}:{localCellCoord.y}";
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }
}