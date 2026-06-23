using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class MainSceneManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelFirst;
    [SerializeField] private GameObject panelOption;
    [SerializeField] private GameObject panelSecond;
    [SerializeField] private GameObject panelCredit;

    [Header("Scene")]
    [Tooltip("Build Settings에 등록된 게임 씬 이름")]
    [SerializeField] private string gameplaySceneName = "GameScene";

    [Header("Save")]
    [Tooltip("GlobalDataManager가 사용하는 저장 파일명과 동일해야 함")]
    [SerializeField] private string saveFileName = "SaveFile.json";

    private void Start()
    {
        ShowFirst();
    }

    // =========================
    // Panel
    // =========================

    private void DisableAll()
    {
        if (panelFirst != null) panelFirst.SetActive(false);
        if (panelOption != null) panelOption.SetActive(false);
        if (panelSecond != null) panelSecond.SetActive(false);
        if (panelCredit != null) panelCredit.SetActive(false);
    }

    public void ShowFirst()
    {
        DisableAll();
        if (panelFirst != null)
            panelFirst.SetActive(true);
    }

    public void ShowOption()
    {
        DisableAll();
        if (panelOption != null)
            panelOption.SetActive(true);
    }

    public void ShowSecond()
    {
        DisableAll();
        if (panelSecond != null)
            panelSecond.SetActive(true);
    }

    public void ShowCredit()
    {
        DisableAll();
        if (panelCredit != null)
            panelCredit.SetActive(true);
    }

    // =========================
    // Save File
    // =========================

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    private bool HasSaveFile()
    {
        string path = GetSavePath();

        if (!File.Exists(path))
            return false;

        try
        {
            return new FileInfo(path).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private void DeleteSaveFileIfExists()
    {
        string path = GetSavePath();

        if (!File.Exists(path))
            return;

        try
        {
            File.Delete(path);
            Debug.Log("[TITLE] 세이브 파일 삭제 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"세이브 파일 삭제 실패 : {e.Message}");
        }
    }

    // =========================
    // Buttons
    // =========================

    public void StartGameButton()
    {
        ShowSecond();
    }

    public void ContinueButton()
    {
        Debug.Log("[TITLE] 이어하기 선택");

        if (HasSaveFile())
        {
            Debug.Log("[TITLE] 세이브 파일 확인 완료");
        }
        else
        {
            Debug.LogWarning("[TITLE] 세이브 파일 없음");
        }

        LoadGameplayScene();
    }

    public void NewGameButton()
    {
        Debug.Log("[TITLE] 새로하기 선택");

        if (HasSaveFile())
        {
            Debug.Log("[TITLE] 기존 세이브 발견");
            DeleteSaveFileIfExists();
        }
        else
        {
            Debug.Log("[TITLE] 기존 세이브 없음");
        }

        Debug.Log("[TITLE] 새 게임 시작");

        LoadGameplayScene();
    }

    public void QuitGame()
    {
        Debug.Log("[TITLE] 게임 종료");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // =========================
    // Scene
    // =========================

    private void LoadGameplayScene()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("[TITLE] gameplaySceneName이 비어있음");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError(
                $"[TITLE] '{gameplaySceneName}' 씬을 찾을 수 없음.\n" +
                $"Build Settings에 추가되었는지 확인하세요."
            );
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void SetGameplaySceneName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            gameplaySceneName = sceneName;
        }
    }
}