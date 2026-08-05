using System.Collections;
using System.IO;
using UnityEngine;

public class GameSaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPresenter playerPresenter;

    private string SavePath => Path.Combine(Application.persistentDataPath, "player_save.json");

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

    public void SaveGame()
    {
        if (playerPresenter == null)
        {
            Debug.LogWarning("저장 실패: PlayerPresenter가 없습니다.");
            return;
        }

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
            moveSpeedUpgradeLevel = playerPresenter.MoveSpeedUpgradeLevel
        };

        string json = JsonUtility.ToJson(saveData, true);

        File.WriteAllText(SavePath, json);

        Debug.Log("게임 저장 완료: " + SavePath);
    }

    public void LoadGame()
    {
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

        string json = File.ReadAllText(SavePath);

        PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(json);

        playerPresenter.LoadPlayerData(saveData);

        Debug.Log("게임 불러오기 완료: " + SavePath);
    }

    [ContextMenu("Delete Save File")]
    public void DeleteSave()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("삭제할 저장 파일이 없습니다.");
            return;
        }

        File.Delete(SavePath);

        Debug.Log("저장 파일 삭제 완료: " + SavePath);
    }
}