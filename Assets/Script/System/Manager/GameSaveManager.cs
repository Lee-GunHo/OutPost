using System.Collections;
using System.IO;
using UnityEngine;

public class GameSaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPresenter playerPresenter;

    [Header("Auto Save / Load")]
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool autoSaveOnQuit = true;

    [Header("Position Safety")]
    [Tooltip("이 높이보다 아래에 있는 플레이어 위치는 비정상 추락으로 보고 저장하지 않습니다.")]
    [SerializeField] private float minimumSafeSaveY = -1f;

    private bool suppressAutoSave;

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "player_save.json");

    private void Awake()
    {
        ResolvePlayerPresenter();
        Debug.Log("저장 경로: " + SavePath);
    }

    private IEnumerator Start()
    {
        if (!autoLoadOnStart)
            yield break;

        // ChunkPresenter / SeedMapPresenter의 Start가 먼저 맵을 생성할 시간을 준다.
        yield return null;

        LoadGame();
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnQuit && !suppressAutoSave)
        {
            SaveGame();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && autoSaveOnQuit && !suppressAutoSave)
        {
            SaveGame();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGame();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadGame();
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            DeleteSave();
        }
    }

    private void ResolvePlayerPresenter()
    {
        if (playerPresenter != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerPresenter = playerObject.GetComponent<PlayerPresenter>();
        }
    }

    public void SaveGame()
    {
        ResolvePlayerPresenter();

        if (playerPresenter == null)
        {
            Debug.LogWarning("저장 실패: PlayerPresenter가 없습니다.");
            return;
        }

        Vector3 playerPosition = playerPresenter.transform.position;

        // 바닥을 뚫고 추락한 좌표가 정상 저장 파일을 덮어쓰는 것을 방지한다.
        if (playerPosition.y < minimumSafeSaveY)
        {
            Debug.LogWarning(
                $"저장 취소: 플레이어 Y({playerPosition.y:F2})가 " +
                $"안전 저장 기준({minimumSafeSaveY:F2})보다 낮습니다."
            );
            return;
        }

        PlayerSaveData saveData = new PlayerSaveData
        {
            playerPosition = playerPosition,

            currentHp = playerPresenter.CurrentHp,
            currentMp = playerPresenter.CurrentMp,

            // 기존 저장 데이터 호환용
            attackPower = playerPresenter.AttackPower,
            defensePower = playerPresenter.DefensePower,

            level = playerPresenter.Level,
            currentExp = playerPresenter.CurrentExp,
            statPoint = playerPresenter.StatPoint,

            hpUpgradeLevel = playerPresenter.HpUpgradeLevel,
            mpUpgradeLevel = playerPresenter.MpUpgradeLevel,
            attackUpgradeLevel = playerPresenter.AttackUpgradeLevel,
            defenseUpgradeLevel = playerPresenter.DefenseUpgradeLevel,
            moveSpeedUpgradeLevel = playerPresenter.MoveSpeedUpgradeLevel
        };

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);

        // F10으로 삭제한 뒤 F5로 다시 저장했다면 자동 저장을 다시 허용한다.
        suppressAutoSave = false;

        Debug.Log("게임 저장 완료: " + SavePath);
    }

    public void LoadGame()
    {
        ResolvePlayerPresenter();

        if (playerPresenter == null)
        {
            Debug.LogWarning("불러오기 실패: PlayerPresenter가 없습니다.");
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.Log("저장 파일이 없습니다. 새 게임으로 시작합니다.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(json);

            if (saveData == null)
            {
                Debug.LogWarning("불러오기 실패: 저장 데이터가 비어 있습니다.");
                return;
            }

            // 위치는 PlayerPresenter에서 실제 바닥을 찾아 안전하게 보정한다.
            playerPresenter.LoadPlayerData(saveData);

            Debug.Log("게임 불러오기 완료: " + SavePath);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("게임 불러오기 실패: " + exception);
        }
    }

    [ContextMenu("Delete Save File")]
    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("저장 파일 삭제 완료: " + SavePath);
        }
        else
        {
            Debug.Log("삭제할 저장 파일이 없습니다.");
        }

        // 삭제 직후 게임을 종료해도 OnApplicationQuit이 파일을 다시 만들지 않게 한다.
        suppressAutoSave = true;
    }
}