using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPresenter playerPresenter;

    private string SavePath => Path.Combine(Application.persistentDataPath, GameSaveModel.PlayerFileName);
    private bool isReady;

    private void Awake()
    {
        if (playerPresenter == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerPresenter = playerObject.GetComponent<PlayerPresenter>();
            }
        }

        Debug.Log("저장 경로: " + SavePath);
    }

    private IEnumerator Start()
    {
        // 다른 스크립트가 Start에서 플레이어 위치를 초기화할 수 있으니까 한 프레임 늦게 로드
        yield return null;

        LoadGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveGame();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveGame();
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            LoadGame();
        }

        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            DeleteSave();
        }
    }

    public void SaveGame()
    {
        TrySaveGame();
    }

    public bool TrySaveGame()
    {
        // Avoid overwriting a saved run before its initial load has finished.
        if (!isReady || playerPresenter == null)
        {
            Debug.LogWarning("저장 실패: 플레이어의 불러오기가 아직 완료되지 않았습니다.");
            return false;
        }

        if (SaveManager.Instance == null || !SaveManager.Instance.TrySaveGame())
            return false;

        PlayerSaveData saveData = new PlayerSaveData
        {
            playerPosition = playerPresenter.transform.position,

            currentHp = playerPresenter.CurrentHp,
            currentMp = playerPresenter.CurrentMp,

            // 기존 호환용. 로드에서는 사용하지 않는 걸 추천.
            attackPower = playerPresenter.AttackPower,
            defensePower = playerPresenter.DefensePower,

            level = playerPresenter.Level,
            currentExp = playerPresenter.CurrentExp,
            statPoint = playerPresenter.StatPoint,

            hpUpgradeLevel = playerPresenter.HpUpgradeLevel,
            mpUpgradeLevel = playerPresenter.MpUpgradeLevel,
            attackUpgradeLevel = playerPresenter.AttackUpgradeLevel,
            defenseUpgradeLevel = playerPresenter.DefenseUpgradeLevel,
            moveSpeedUpgradeLevel = playerPresenter.MoveSpeedUpgradeLevel,

            // 진행도 저장 정보
            normalMonsterKillCount = GameProgressManager.Instance != null
                ? GameProgressManager.Instance.NormalMonsterKillCount
                : 0,

            bossKillCount = GameProgressManager.Instance != null
                ? GameProgressManager.Instance.BossKillCount
                : 0,

            bossKilled = GameProgressManager.Instance != null &&
                         GameProgressManager.Instance.BossKilled,

            raidClearCount = GameProgressManager.Instance != null
                ? GameProgressManager.Instance.RaidClearCount
                : 0,

            progressLevel = GameProgressManager.Instance != null
                ? GameProgressManager.Instance.ProgressLevel
                : 0,

            isMonsterWaveInProgress = GameProgressManager.Instance != null &&
                          GameProgressManager.Instance.IsMonsterWaveInProgress
        };

        string json = JsonUtility.ToJson(saveData, true);

        try
        {
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("플레이어 저장 실패: " + exception.Message);
            return false;
        }

        if (QuestSaveManager.Instance != null)
            QuestSaveManager.Instance.SaveNow();
        if (ChestSaveManager.Instance != null)
            ChestSaveManager.Instance.SaveNow();
        if (ChunkModificationSaveManager.Instance != null)
            ChunkModificationSaveManager.Instance.SaveNow();
        if (PlacedBlockSaveManager.Instance != null)
            PlacedBlockSaveManager.Instance.SaveNow();

        Debug.Log("게임 저장 완료: " + SavePath);
        return true;
    }

    public void LoadGame()
    {
        isReady = false;
        if (playerPresenter == null)
            playerPresenter = FindFirstObjectByType<PlayerPresenter>();

        if (playerPresenter == null)
        {
            Debug.LogWarning("불러오기 실패: PlayerPresenter가 없습니다.");
            return;
        }

        PlayerSaveData saveData = null;
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                saveData = JsonUtility.FromJson<PlayerSaveData>(json);
                if (saveData == null)
                    throw new System.IO.InvalidDataException("플레이어 저장 데이터가 비어 있습니다.");
            }
            catch (System.Exception exception)
            {
                Debug.LogError("플레이어 불러오기 실패: " + exception.Message);
                return;
            }
        }

        // Equipment changes maximum HP/MP, so restore it before current HP/MP.
        // Prefer the safe position restore in PlayerPresenter when both files exist.
        if (SaveManager.Instance != null && !SaveManager.Instance.TryLoadGame(saveData == null))
            return;

        if (saveData == null)
        {
            isReady = true;
            Debug.Log("플레이어 성장 저장이 없어 기본 성장 데이터로 시작합니다.");
            return;
        }

        playerPresenter.LoadPlayerData(saveData);

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.LoadProgressData(
                saveData.normalMonsterKillCount,
                saveData.bossKillCount,
                saveData.bossKilled,
                saveData.raidClearCount,
                saveData.progressLevel,
                saveData.isMonsterWaveInProgress
            );
        }
        else
        {
            Debug.LogWarning("GameProgressManager.Instance가 없어서 진행도 데이터를 불러오지 못했습니다.");
        }

        Debug.Log("게임 불러오기 완료: " + SavePath);
        isReady = true;
    }

    public static void ResetProgressCaches()
    {
        if (QuestSaveManager.Instance != null)
            QuestSaveManager.Instance.ResetCache();
        if (ChestSaveManager.Instance != null)
            ChestSaveManager.Instance.ResetCache();
        if (ChunkModificationSaveManager.Instance != null)
            ChunkModificationSaveManager.Instance.ResetCache();
        if (PlacedBlockSaveManager.Instance != null)
            PlacedBlockSaveManager.Instance.ResetCache();
    }

    [ContextMenu("Delete All Save Files")]
    public void DeleteSave()
    {
        const string mainSceneName = "MainScene";
        if (Application.isPlaying && !Application.CanStreamedLevelBeLoaded(mainSceneName))
        {
            Debug.LogError("저장 삭제 후 돌아갈 MainScene이 빌드 설정에 없습니다.");
            return;
        }

        string inventoryFileName = SaveManager.Instance != null
            ? SaveManager.Instance.SaveFileName : "SaveFile.json";

        // Closing an open chest restores dragged items and may save its contents.
        // Finish that before deletion so scene teardown cannot recreate old records.
        if (Application.isPlaying && ChestPresenter.Instance != null)
            ChestPresenter.Instance.Close();

        try
        {
            new GameSaveModel(Application.persistentDataPath, inventoryFileName).DeleteAllProgress();
        }
        catch (System.Exception exception)
        {
            Debug.LogError("전체 저장 기록 삭제 실패: " + exception.Message);
            return;
        }

        // Do not let quitting or pausing write the current character back to disk.
        isReady = false;
        ResetProgressCaches();
        Debug.Log("플레이어, 아이템, 퀘스트, 상자, 맵 파괴/설치 저장 기록을 모두 삭제했습니다.");

        if (Application.isPlaying)
        {
            UIState.ResetAll();
            SceneManager.LoadScene(mainSceneName);
        }
    }
}
