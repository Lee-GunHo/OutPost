using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 모든 창고 데이터를 JSON 파일로 영구 저장하는 관리자
/// 저장 파일은 Application.persistentDataPath에 생성됨.
///
/// 창고 내용이 변경될 때마다 즉시 저장하므로 게임을 정상 종료하지 않아도
/// 마지막 변경 내용이 파일에 남움.
/// </summary>
public class ChestSaveManager : MonoBehaviour
{
    public static ChestSaveManager Instance { get; private set; }

    private const int CurrentSaveVersion = 1;
    private const string SaveFileName = "chest_save.json";

    [Header("아이템 데이터베이스")]
    [Tooltip("저장된 itemID를 ItemData로 복원하기 위해 필요합니다.")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private ChestSaveFileData saveData = new ChestSaveFileData();

    private readonly Dictionary<string, ChestSaveRecord> chestById =
        new Dictionary<string, ChestSaveRecord>();

    private bool isLoaded;

    public bool IsReady => itemDatabase != null;

    [Serializable]
    private class ChestSaveFileData
    {
        public int saveVersion = CurrentSaveVersion;
        public List<ChestSaveRecord> chests = new List<ChestSaveRecord>();
    }

    [Serializable]
    private class ChestSaveRecord
    {
        public string chestId;
        public List<ChestSlotSaveRecord> slots =
            new List<ChestSlotSaveRecord>();
    }

    [Serializable]
    private class ChestSlotSaveRecord
    {
        public int slotIndex;
        public int itemId;
        public int amount;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFromDisk();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static ChestSaveManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<ChestSaveManager>();

        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("ChestSaveManager");
        Instance = managerObject.AddComponent<ChestSaveManager>();

        Debug.LogError(
            "ChestSaveManager가 씬에 없어 자동 생성했지만 ItemDatabase가 없습니다. " +
            "현재 실행에서는 창고 저장과 불러오기를 중단합니다. " +
            "씬에 ChestSaveManager를 직접 배치하고 ItemDatabase를 연결해주세요."
        );

        return Instance;
    }

    /// <summary>
    /// 특정 창고의 저장 데이터를 불러옴.
    /// 저장 데이터가 없으면 false를 반환
    /// </summary>
    public bool TryLoadChest(
        string chestId,
        int slotCount,
        out List<ItemStack> loadedItems)
    {
        EnsureLoaded();

        loadedItems = CreateEmptySlots(slotCount);

        if (string.IsNullOrWhiteSpace(chestId))
        {
            Debug.LogError("창고 데이터를 불러올 수 없습니다: Chest ID가 비어 있습니다.");
            return false;
        }

        if (!chestById.TryGetValue(chestId, out ChestSaveRecord chestRecord))
        {
            return false;
        }

        if (itemDatabase == null)
        {
            Debug.LogError(
                "창고 데이터를 복원할 수 없습니다: ChestSaveManager에 " +
                "ItemDatabase가 연결되지 않았습니다."
            );

            return false;
        }

        if (chestRecord.slots == null)
            return true;

        foreach (ChestSlotSaveRecord slotRecord in chestRecord.slots)
        {
            if (slotRecord == null)
                continue;

            if (slotRecord.slotIndex < 0 ||
                slotRecord.slotIndex >= loadedItems.Count)
            {
                Debug.LogWarning(
                    $"창고 {chestId}의 잘못된 슬롯 인덱스를 무시했습니다: " +
                    slotRecord.slotIndex
                );

                continue;
            }

            if (slotRecord.amount <= 0)
                continue;

            ItemData itemData =
                 itemDatabase.GetItemByID(slotRecord.itemId);

            if (itemData == null)
                continue;

            int amount = Mathf.Clamp(
                slotRecord.amount,
                1,
                Mathf.Max(1, itemData.maxStack)
            );

            loadedItems[slotRecord.slotIndex] =
                new ItemStack(itemData, amount);
        }

        if (showDebugLog)
        {
            Debug.Log(
                $"창고 불러오기 완료: ID={chestId}, " +
                $"저장 슬롯 수={chestRecord.slots.Count}"
            );
        }

        return true;
    }

    /// <summary>
    /// 특정 창고의 현재 슬롯 상태를 즉시 파일에 저장
    /// 빈 슬롯은 JSON에 기록하지 않음.
    /// </summary>
    public void SaveChest(
        string chestId,
        IReadOnlyList<ItemStack> items)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(chestId))
        {
            Debug.LogError("창고를 저장할 수 없습니다: Chest ID가 비어 있습니다.");
            return;
        }

        if (items == null)
        {
            Debug.LogError($"창고 {chestId}를 저장할 수 없습니다: 슬롯 데이터가 없습니다.");
            return;
        }

        if (!chestById.TryGetValue(chestId, out ChestSaveRecord chestRecord))
        {
            chestRecord = new ChestSaveRecord
            {
                chestId = chestId,
                slots = new List<ChestSlotSaveRecord>()
            };

            chestById.Add(chestId, chestRecord);
            saveData.chests.Add(chestRecord);
        }

        chestRecord.slots.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            ItemStack stack = items[i];

            if (stack == null ||
                stack.item == null ||
                stack.amount <= 0)
            {
                continue;
            }

            chestRecord.slots.Add(
                new ChestSlotSaveRecord
                {
                    slotIndex = i,
                    itemId = stack.item.itemID,
                    amount = stack.amount
                }
            );
        }

        SaveNow();

        if (showDebugLog)
        {
            Debug.Log(
                $"창고 저장 완료: ID={chestId}, " +
                $"저장 슬롯 수={chestRecord.slots.Count}"
            );
        }
    }

    /// <summary>
    /// 메모리에 있는 모든 창고 데이터를 JSON 파일에 기록
    /// 임시 파일을 거쳐 교체하여 저장 중 파일 손상을 줄임.
    /// </summary>
    public void SaveNow()
    {
        EnsureLoaded();

        try
        {
            saveData.saveVersion = CurrentSaveVersion;

            if (saveData.chests == null)
            {
                saveData.chests = new List<ChestSaveRecord>();
            }

            string savePath = GetSaveFilePath();
            string temporaryPath = savePath + ".tmp";
            string json = JsonUtility.ToJson(saveData, true);

            string directory = Path.GetDirectoryName(savePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(temporaryPath, json);

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            File.Move(temporaryPath, savePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("창고 저장 실패: " + exception);
        }
    }

    /// <summary>
    /// 특정 창고의 저장 데이터만 강제로 삭제합니다.
    /// </summary>
    public bool DeleteChestSave(string chestId)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(chestId))
            return false;

        if (!chestById.TryGetValue(chestId, out ChestSaveRecord record))
            return false;

        chestById.Remove(chestId);
        saveData.chests.Remove(record);
        SaveNow();

        Debug.Log($"창고 저장 데이터 삭제 완료: ID={chestId}");
        return true;
    }

    /// <summary>
    /// 모든 창고 저장 데이터를 강제로 삭제
    /// 이 함수를 호출하지 않는 한 저장 파일은 계속 유지
    /// </summary>
    [ContextMenu("Delete All Chest Saves")]
    public void DeleteAllChestSaves()
    {
        chestById.Clear();

        saveData = new ChestSaveFileData
        {
            saveVersion = CurrentSaveVersion,
            chests = new List<ChestSaveRecord>()
        };

        string savePath = GetSaveFilePath();
        string temporaryPath = savePath + ".tmp";

        try
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            isLoaded = true;

            Debug.Log("모든 창고 저장 데이터를 강제로 삭제했습니다: " + savePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("창고 저장 파일 삭제 실패: " + exception);
        }
    }

    [ContextMenu("Print Chest Save Path")]
    private void PrintChestSavePath()
    {
        Debug.Log("창고 저장 경로: " + GetSaveFilePath());
    }

    private void EnsureLoaded()
    {
        if (!isLoaded)
        {
            LoadFromDisk();
        }
    }

    private void LoadFromDisk()
    {
        chestById.Clear();

        string savePath = GetSaveFilePath();

        if (!File.Exists(savePath))
        {
            saveData = new ChestSaveFileData
            {
                saveVersion = CurrentSaveVersion,
                chests = new List<ChestSaveRecord>()
            };

            isLoaded = true;

            if (showDebugLog)
            {
                Debug.Log("기존 창고 저장 파일이 없어 새 데이터를 사용합니다: " + savePath);
            }

            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            ChestSaveFileData loadedData =
                JsonUtility.FromJson<ChestSaveFileData>(json);

            if (loadedData == null)
            {
                CreateEmptySaveData();
                Debug.LogWarning("창고 저장 파일이 비어 있어 새 데이터를 사용합니다.");
                return;
            }

            if (loadedData.chests == null)
            {
                loadedData.chests = new List<ChestSaveRecord>();
            }

            saveData = loadedData;

            foreach (ChestSaveRecord chestRecord in saveData.chests)
            {
                if (chestRecord == null ||
                    string.IsNullOrWhiteSpace(chestRecord.chestId))
                {
                    continue;
                }

                if (chestRecord.slots == null)
                {
                    chestRecord.slots = new List<ChestSlotSaveRecord>();
                }

                if (chestById.ContainsKey(chestRecord.chestId))
                {
                    Debug.LogWarning(
                        "중복된 창고 ID 저장 데이터를 무시했습니다: " +
                        chestRecord.chestId
                    );

                    continue;
                }

                chestById.Add(chestRecord.chestId, chestRecord);
            }

            isLoaded = true;

            if (showDebugLog)
            {
                Debug.Log(
                    $"창고 저장 데이터 불러오기 완료: " +
                    $"창고 {chestById.Count}개\n{savePath}"
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("창고 저장 데이터 불러오기 실패: " + exception);
            CreateEmptySaveData();
        }
    }

    private void CreateEmptySaveData()
    {
        chestById.Clear();

        saveData = new ChestSaveFileData
        {
            saveVersion = CurrentSaveVersion,
            chests = new List<ChestSaveRecord>()
        };

        isLoaded = true;
    }

    private static List<ItemStack> CreateEmptySlots(int slotCount)
    {
        int safeSlotCount = Mathf.Max(1, slotCount);
        List<ItemStack> result = new List<ItemStack>(safeSlotCount);

        for (int i = 0; i < safeSlotCount; i++)
        {
            result.Add(null);
        }

        return result;
    }

    private static string GetSaveFilePath()
    {
        return Path.Combine(
            Application.persistentDataPath,
            SaveFileName
        );
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }
}