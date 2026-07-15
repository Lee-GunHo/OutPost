using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlacedBlockSaveManager : MonoBehaviour
{
    private static PlacedBlockSaveManager instance;
    public static PlacedBlockSaveManager Instance => instance;

    [SerializeField] private bool showDebugLog = true;

    private int currentWorldSeed;
    private bool isInitialized;

    private readonly Dictionary<Vector2Int, int> placedBlocks =
        new Dictionary<Vector2Int, int>();

    private PlacedBlockSaveData saveData = new PlacedBlockSaveData();

    [Serializable]
    private class PlacedBlockSaveData
    {
        public int worldSeed;
        public List<PlacedBlockRecord> blocks = new List<PlacedBlockRecord>();
    }

    [Serializable]
    private class PlacedBlockRecord
    {
        public int globalCellX;
        public int globalCellZ;
        public int itemId;

        public PlacedBlockRecord() { }

        public PlacedBlockRecord(Vector2Int globalCell, int itemId)
        {
            globalCellX = globalCell.x;
            globalCellZ = globalCell.y;
            this.itemId = itemId;
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

    public static PlacedBlockSaveManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<PlacedBlockSaveManager>();

        if (instance != null)
            return instance;

        GameObject managerObject = new GameObject("PlacedBlockSaveManager");
        instance = managerObject.AddComponent<PlacedBlockSaveManager>();
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

    public bool TryGetPlacedBlock(
        int worldSeed,
        Vector2Int globalCell,
        out int itemId)
    {
        EnsureWorldInitialized(worldSeed);
        return placedBlocks.TryGetValue(globalCell, out itemId);
    }

    public bool RegisterPlacedBlock(
        int worldSeed,
        Vector2Int globalCell,
        int itemId)
    {
        EnsureWorldInitialized(worldSeed);

        if (placedBlocks.ContainsKey(globalCell))
            return false;

        placedBlocks.Add(globalCell, itemId);
        saveData.blocks.Add(new PlacedBlockRecord(globalCell, itemId));
        SaveNow();

        if (showDebugLog)
        {
            Debug.Log(
                $"설치 블록 저장: Cell({globalCell.x}, {globalCell.y}), itemID={itemId}"
            );
        }

        return true;
    }

    public bool RemovePlacedBlock(int worldSeed, Vector2Int globalCell)
    {
        EnsureWorldInitialized(worldSeed);

        if (!placedBlocks.Remove(globalCell))
            return false;

        saveData.blocks.RemoveAll(record =>
            record.globalCellX == globalCell.x &&
            record.globalCellZ == globalCell.y
        );

        SaveNow();

        if (showDebugLog)
        {
            Debug.Log(
                $"설치 블록 제거 저장: Cell({globalCell.x}, {globalCell.y})"
            );
        }

        return true;
    }

    public void SaveNow()
    {
        if (!isInitialized)
            return;

        try
        {
            saveData.worldSeed = currentWorldSeed;
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetSaveFilePath(currentWorldSeed), json);
        }
        catch (Exception exception)
        {
            Debug.LogError("설치 블록 저장 실패: " + exception);
        }
    }

    private void LoadCurrentWorld()
    {
        placedBlocks.Clear();

        string savePath = GetSaveFilePath(currentWorldSeed);

        if (!File.Exists(savePath))
        {
            saveData = CreateEmptySaveData();
            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            PlacedBlockSaveData loadedData =
                JsonUtility.FromJson<PlacedBlockSaveData>(json);

            if (loadedData == null || loadedData.worldSeed != currentWorldSeed)
            {
                saveData = CreateEmptySaveData();
                return;
            }

            if (loadedData.blocks == null)
                loadedData.blocks = new List<PlacedBlockRecord>();

            saveData = loadedData;

            foreach (PlacedBlockRecord record in saveData.blocks)
            {
                Vector2Int globalCell = new Vector2Int(
                    record.globalCellX,
                    record.globalCellZ
                );

                placedBlocks[globalCell] = record.itemId;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("설치 블록 불러오기 실패: " + exception);
            saveData = CreateEmptySaveData();
        }
    }

    private PlacedBlockSaveData CreateEmptySaveData()
    {
        placedBlocks.Clear();

        return new PlacedBlockSaveData
        {
            worldSeed = currentWorldSeed,
            blocks = new List<PlacedBlockRecord>()
        };
    }

    private void EnsureWorldInitialized(int worldSeed)
    {
        if (!isInitialized || currentWorldSeed != worldSeed)
            InitializeWorld(worldSeed);
    }

    private string GetSaveFilePath(int worldSeed)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"placed_blocks_{worldSeed}.json"
        );
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }
}