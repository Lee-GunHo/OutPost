using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 조언 팁/상점이 참고하는 진행도 값을 한곳에서 조회하게 해주는 관리자.
/// - 직접 세고 저장하는 값: 사망 횟수(누적/Recent), NPC 발견 여부, 상점 아이템별 수령 개수
/// - 기존 시스템에서 읽어오기만 하는 값: 퀘스트 상태(QuestSaveManager + 인벤토리),
///   처치 횟수(GameProgressManager), 도구 등급(인벤토리/핫바)
/// 씬에 배치할 필요 없이 게임 시작 시 자동 생성되고, QuestSaveManager와 같은 방식으로
/// 값이 바뀔 때마다 progress_save.json에 즉시 저장.
/// </summary>
public class ProgressTracker : MonoBehaviour
{
    public static ProgressTracker Instance { get; private set; }

    public const string SaveFileName = "progress_save.json";
    private const int CurrentSaveVersion = 1;

    private SaveFileData saveData = new SaveFileData();
    private bool isLoaded;

    private PlayerPresenter playerPresenter;

    [Serializable]
    private class SaveFileData
    {
        public int saveVersion = CurrentSaveVersion;
        public int deathCount;
        public int recentDeathCount;
        public List<string> discoveredNpcIds = new List<string>();
        public List<ShopReceivedRecord> shopReceived = new List<ShopReceivedRecord>();
    }

    [Serializable]
    private class ShopReceivedRecord
    {
        public int itemId;
        public int amount;
    }

    public int DeathCount
    {
        get
        {
            EnsureLoaded();
            return saveData.deathCount;
        }
    }

    public int RecentDeathCount
    {
        get
        {
            EnsureLoaded();
            return saveData.recentDeathCount;
        }
    }

    public int KillCount =>
        GameProgressManager.Instance != null ? GameProgressManager.Instance.NormalMonsterKillCount : 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnGameStart()
    {
        GetOrCreate();
    }

    public static ProgressTracker GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<ProgressTracker>();

        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("ProgressTracker");
        Instance = managerObject.AddComponent<ProgressTracker>();

        return Instance;
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

        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindPlayer();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindPlayer();
        Instance = null;
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindPlayer();
    }

    // 씬이 바뀌면 플레이어가 새로 만들어지므로 사망 이벤트를 다시 구독
    private void BindPlayer()
    {
        PlayerPresenter found = FindFirstObjectByType<PlayerPresenter>();

        if (found == playerPresenter)
        {
            return;
        }

        UnbindPlayer();
        playerPresenter = found;

        if (playerPresenter != null)
        {
            playerPresenter.OnPlayerDied += HandlePlayerDied;
        }
    }

    private void UnbindPlayer()
    {
        if (playerPresenter != null)
        {
            playerPresenter.OnPlayerDied -= HandlePlayerDied;
        }

        playerPresenter = null;
    }

    private void HandlePlayerDied(string cause, Vector3 position)
    {
        EnsureLoaded();

        saveData.deathCount++;
        saveData.recentDeathCount++;

        SaveNow();
    }

    public void ResetRecentDeath()
    {
        EnsureLoaded();

        if (saveData.recentDeathCount == 0)
        {
            return;
        }

        saveData.recentDeathCount = 0;
        SaveNow();
    }

    public bool IsNpcDiscovered(string npcId)
    {
        EnsureLoaded();
        return !string.IsNullOrEmpty(npcId) && saveData.discoveredNpcIds.Contains(npcId);
    }

    public void MarkNpcDiscovered(string npcId)
    {
        EnsureLoaded();

        if (string.IsNullOrEmpty(npcId) || saveData.discoveredNpcIds.Contains(npcId))
        {
            return;
        }

        saveData.discoveredNpcIds.Add(npcId);
        SaveNow();

        Debug.Log("NPC 발견 기록: " + npcId);
    }

    public int GetShopReceivedAmount(int itemId)
    {
        EnsureLoaded();

        ShopReceivedRecord record = FindShopRecord(itemId);
        return record != null ? record.amount : 0;
    }

    public void AddShopReceivedAmount(int itemId, int amount)
    {
        EnsureLoaded();

        if (amount <= 0)
        {
            return;
        }

        ShopReceivedRecord record = FindShopRecord(itemId);

        if (record == null)
        {
            record = new ShopReceivedRecord { itemId = itemId };
            saveData.shopReceived.Add(record);
        }

        record.amount += amount;
        SaveNow();
    }

    private ShopReceivedRecord FindShopRecord(int itemId)
    {
        foreach (ShopReceivedRecord record in saveData.shopReceived)
        {
            if (record != null && record.itemId == itemId)
            {
                return record;
            }
        }

        return null;
    }

    /// <summary>
    /// 현재 진행도를 스냅샷으로 만들어 반환. 조언 팁 선택(AdviceTipSelector)의 입력.
    /// CombatNpcHint는 여기서 채우지 않음(호출하는 쪽에서 계산).
    /// </summary>
    public ProgressSnapshot BuildSnapshot(IReadOnlyList<int> toolTierItemIds)
    {
        EnsureLoaded();
        BindPlayer();

        ProgressSnapshot snapshot = new ProgressSnapshot
        {
            DeathCount = saveData.deathCount,
            RecentDeathCount = saveData.recentDeathCount,
            KillCount = KillCount,
            ToolTier = ToolTierRules.Resolve(toolTierItemIds, CollectOwnedItemIds())
        };

        foreach (string npcId in saveData.discoveredNpcIds)
        {
            snapshot.DiscoveredNpcIds.Add(npcId);
        }

        InventoryModel inventory = playerPresenter != null ? playerPresenter.PlayerInventory : null;

        // 씬에 있는 NPC들이 가진 퀘스트 목록을 모아 questId별 상태를 채움
        foreach (NPCModel npcModel in FindObjectsByType<NPCModel>(FindObjectsSortMode.None))
        {
            foreach (QuestData quest in npcModel.QuestChain)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
                {
                    continue;
                }

                snapshot.QuestStates[quest.QuestId] = GetQuestState(quest, inventory);
            }
        }

        return snapshot;
    }

    public QuestProgressState GetQuestState(QuestData quest, InventoryModel inventory)
    {
        if (quest == null)
        {
            return QuestProgressState.NotStarted;
        }

        QuestSaveManager.GetOrCreate().TryLoadQuestState(
            quest.QuestId,
            out bool isAccepted,
            out bool isCompleted);

        // 퀘스트 완료 판정(NPCInteractionUI.TryCompleteQuest)과 똑같이 인벤토리 보유량만 봄
        int ownedAmount = inventory != null && quest.RequiredItem != null
            ? inventory.GetItemCount(quest.RequiredItem)
            : 0;

        return QuestProgressRules.Resolve(isAccepted, isCompleted, ownedAmount, quest.RequiredAmount);
    }

    private HashSet<int> CollectOwnedItemIds()
    {
        HashSet<int> ownedItemIds = new HashSet<int>();

        if (playerPresenter != null && playerPresenter.PlayerInventory != null)
        {
            AddItemIds(playerPresenter.PlayerInventory.Items, ownedItemIds);
        }

        if (HotbarPresenter.Instance != null && HotbarPresenter.Instance.Model != null)
        {
            AddItemIds(HotbarPresenter.Instance.Model.Items, ownedItemIds);
        }

        return ownedItemIds;
    }

    private static void AddItemIds(List<ItemStack> items, HashSet<int> ownedItemIds)
    {
        if (items == null)
        {
            return;
        }

        foreach (ItemStack itemStack in items)
        {
            if (itemStack != null && !itemStack.IsEmpty)
            {
                ownedItemIds.Add(itemStack.item.itemID);
            }
        }
    }

    public void SaveNow()
    {
        EnsureLoaded();

        try
        {
            saveData.saveVersion = CurrentSaveVersion;

            string savePath = GetSaveFilePath();
            string temporaryPath = savePath + ".tmp";
            string json = JsonUtility.ToJson(saveData, true);

            File.WriteAllText(temporaryPath, json);

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            File.Move(temporaryPath, savePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("진행도 저장 실패: " + exception);
        }
    }

    /// <summary>
    /// 새 게임/저장 삭제 시 GameSaveManager.ResetProgressCaches()에서 호출. 다음 조회 때 파일을 다시 읽음.
    /// </summary>
    public void ResetCache()
    {
        saveData = new SaveFileData();
        isLoaded = false;
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
        saveData = new SaveFileData();
        isLoaded = true;

        string savePath = GetSaveFilePath();

        if (!File.Exists(savePath))
        {
            return;
        }

        try
        {
            SaveFileData loadedData = JsonUtility.FromJson<SaveFileData>(File.ReadAllText(savePath));

            if (loadedData == null)
            {
                return;
            }

            if (loadedData.discoveredNpcIds == null)
            {
                loadedData.discoveredNpcIds = new List<string>();
            }

            if (loadedData.shopReceived == null)
            {
                loadedData.shopReceived = new List<ShopReceivedRecord>();
            }

            saveData = loadedData;
        }
        catch (Exception exception)
        {
            Debug.LogError("진행도 저장 데이터 불러오기 실패: " + exception);
        }
    }

    private static string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 디버그 창(Tools > NpcTip > Advice Tip Debugger)이 플레이 중에 값을 강제로 바꿀 때만 사용.
    /// </summary>
    public void EditorSetDebugValues(int deathCount, int recentDeathCount, bool combatDiscovered, bool shopDiscovered)
    {
        EnsureLoaded();

        saveData.deathCount = Mathf.Max(0, deathCount);
        saveData.recentDeathCount = Mathf.Max(0, recentDeathCount);

        SetDiscovered(NPCRole.Combat.ToString(), combatDiscovered);
        SetDiscovered(NPCRole.Shop.ToString(), shopDiscovered);

        SaveNow();
    }

    private void SetDiscovered(string npcId, bool discovered)
    {
        saveData.discoveredNpcIds.Remove(npcId);

        if (discovered)
        {
            saveData.discoveredNpcIds.Add(npcId);
        }
    }
#endif
}
