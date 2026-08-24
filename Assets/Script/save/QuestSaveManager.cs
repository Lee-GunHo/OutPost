using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 모든 퀘스트의 수락/완료 상태를 JSON 파일로 영구 저장하는 관리자.
/// 저장 파일은 Application.persistentDataPath에 생성됨.
///
/// 퀘스트 상태가 바뀔 때마다 즉시 저장하므로 게임을 정상 종료하지 않아도
/// 마지막 변경 내용이 파일에 남음.
/// </summary>
public class QuestSaveManager : MonoBehaviour
{
    public static QuestSaveManager Instance { get; private set; }

    private const int CurrentSaveVersion = 1;
    private const string SaveFileName = "quest_save.json";

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private QuestSaveFileData saveData = new QuestSaveFileData();

    private readonly Dictionary<string, QuestSaveRecord> questById =
        new Dictionary<string, QuestSaveRecord>();

    private bool isLoaded;

    [Serializable]
    private class QuestSaveFileData
    {
        public int saveVersion = CurrentSaveVersion;
        public List<QuestSaveRecord> quests = new List<QuestSaveRecord>();
    }

    [Serializable]
    private class QuestSaveRecord
    {
        public string questId;
        public bool isAccepted;
        public bool isCompleted;
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

    public static QuestSaveManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<QuestSaveManager>();

        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("QuestSaveManager");
        Instance = managerObject.AddComponent<QuestSaveManager>();

        return Instance;
    }

    /// <summary>
    /// 특정 퀘스트의 저장된 상태를 불러옴.
    /// 저장 데이터가 없으면 false를 반환.
    /// </summary>
    public bool TryLoadQuestState(
        string questId,
        out bool isAccepted,
        out bool isCompleted)
    {
        EnsureLoaded();

        isAccepted = false;
        isCompleted = false;

        if (string.IsNullOrWhiteSpace(questId))
            return false;

        if (!questById.TryGetValue(questId, out QuestSaveRecord record))
            return false;

        isAccepted = record.isAccepted;
        isCompleted = record.isCompleted;

        return true;
    }

    /// <summary>
    /// 퀘스트의 현재 상태를 즉시 파일에 저장.
    /// </summary>
    public void SaveQuestState(
        string questId,
        bool isAccepted,
        bool isCompleted)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(questId))
        {
            Debug.LogError("퀘스트 상태를 저장할 수 없습니다: Quest ID가 비어 있습니다.");
            return;
        }

        if (!questById.TryGetValue(questId, out QuestSaveRecord record))
        {
            record = new QuestSaveRecord { questId = questId };
            questById.Add(questId, record);
            saveData.quests.Add(record);
        }

        record.isAccepted = isAccepted;
        record.isCompleted = isCompleted;

        SaveNow();

        if (showDebugLog)
        {
            Debug.Log(
                $"퀘스트 상태 저장 완료: ID={questId}, " +
                $"수락={isAccepted}, 완료={isCompleted}"
            );
        }
    }

    /// <summary>
    /// 메모리에 있는 모든 퀘스트 데이터를 JSON 파일에 기록.
    /// 임시 파일을 거쳐 교체하여 저장 중 파일 손상을 줄임.
    /// </summary>
    public void SaveNow()
    {
        EnsureLoaded();

        try
        {
            saveData.saveVersion = CurrentSaveVersion;

            if (saveData.quests == null)
            {
                saveData.quests = new List<QuestSaveRecord>();
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
            Debug.LogError("퀘스트 저장 실패: " + exception);
        }
    }

    /// <summary>
    /// 특정 퀘스트의 저장 데이터만 강제로 삭제.
    /// </summary>
    public bool DeleteQuestSave(string questId)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(questId))
            return false;

        if (!questById.TryGetValue(questId, out QuestSaveRecord record))
            return false;

        questById.Remove(questId);
        saveData.quests.Remove(record);
        SaveNow();

        Debug.Log($"퀘스트 저장 데이터 삭제 완료: ID={questId}");
        return true;
    }

    /// <summary>
    /// 모든 퀘스트 저장 데이터를 강제로 삭제.
    /// 이 함수를 호출하지 않는 한 저장 파일은 계속 유지.
    /// </summary>
    [ContextMenu("Delete All Quest Saves")]
    public void DeleteAllQuestSaves()
    {
        questById.Clear();

        saveData = new QuestSaveFileData
        {
            saveVersion = CurrentSaveVersion,
            quests = new List<QuestSaveRecord>()
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

            Debug.Log("모든 퀘스트 저장 데이터를 강제로 삭제했습니다: " + savePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("퀘스트 저장 파일 삭제 실패: " + exception);
        }
    }

    [ContextMenu("Print Quest Save Path")]
    private void PrintQuestSavePath()
    {
        Debug.Log("퀘스트 저장 경로: " + GetSaveFilePath());
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
        questById.Clear();

        string savePath = GetSaveFilePath();

        if (!File.Exists(savePath))
        {
            saveData = new QuestSaveFileData
            {
                saveVersion = CurrentSaveVersion,
                quests = new List<QuestSaveRecord>()
            };

            isLoaded = true;

            if (showDebugLog)
            {
                Debug.Log("기존 퀘스트 저장 파일이 없어 새 데이터를 사용합니다: " + savePath);
            }

            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            QuestSaveFileData loadedData =
                JsonUtility.FromJson<QuestSaveFileData>(json);

            if (loadedData == null)
            {
                CreateEmptySaveData();
                Debug.LogWarning("퀘스트 저장 파일이 비어 있어 새 데이터를 사용합니다.");
                return;
            }

            if (loadedData.quests == null)
            {
                loadedData.quests = new List<QuestSaveRecord>();
            }

            saveData = loadedData;

            foreach (QuestSaveRecord record in saveData.quests)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.questId))
                {
                    continue;
                }

                if (questById.ContainsKey(record.questId))
                {
                    Debug.LogWarning(
                        "중복된 퀘스트 ID 저장 데이터를 무시했습니다: " +
                        record.questId
                    );

                    continue;
                }

                questById.Add(record.questId, record);
            }

            isLoaded = true;

            if (showDebugLog)
            {
                Debug.Log(
                    $"퀘스트 저장 데이터 불러오기 완료: " +
                    $"퀘스트 {questById.Count}개\n{savePath}"
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("퀘스트 저장 데이터 불러오기 실패: " + exception);
            CreateEmptySaveData();
        }
    }

    private void CreateEmptySaveData()
    {
        questById.Clear();

        saveData = new QuestSaveFileData
        {
            saveVersion = CurrentSaveVersion,
            quests = new List<QuestSaveRecord>()
        };

        isLoaded = true;
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
