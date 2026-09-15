using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelFirst;
    [SerializeField] private GameObject panelOption;
    [SerializeField] private GameObject panelSecond;
    [SerializeField] private GameObject panelCredit;

    [Header("View")]
    [SerializeField] private MainSceneView mainSceneView;

    [Header("Scene")]
    [Tooltip("Build Settings에 등록된 게임 씬 이름")]
    [SerializeField] private string gameplaySceneName = "GameScene";

    [Header("Save")]
    [Tooltip("SaveManager가 사용하는 인벤토리 저장 파일명")]
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

    private GameSaveModel CreateSaveModel()
    {
        return new GameSaveModel(Application.persistentDataPath, saveFileName);
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

        if (!CanLoadGameplayScene())
            return;

        try
        {
            if (!CreateSaveModel().HasPlayerSave())
            {
                if (mainSceneView != null)
                    mainSceneView.ShowMessage("저장된 기록이 없습니다.\n새로하기로 게임을 시작해주세요.", panelSecond.transform);
                Debug.LogWarning("[TITLE] 이어할 저장 기록이 없습니다. 새로하기를 선택해주세요.");
                return;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[TITLE] 저장 기록 확인 실패: " + exception.Message);
            return;
        }

        GameSaveManager.ResetProgressCaches();
        LoadGameplayScene();
    }

    public void NewGameButton()
    {
        Debug.Log("[TITLE] 새로하기 선택");

        // Validate the destination before deleting the previous playthrough.
        if (!CanLoadGameplayScene())
            return;

        try
        {
            CreateSaveModel().DeleteAllProgress();
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[TITLE] 새 게임 초기화 실패: " + exception.Message);
            return;
        }

        GameSaveManager.ResetProgressCaches();
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

    private bool CanLoadGameplayScene()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("[TITLE] gameplaySceneName이 비어있음");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError(
                $"[TITLE] '{gameplaySceneName}' 씬을 찾을 수 없음.\n" +
                $"Build Settings에 추가되었는지 확인하세요."
            );
            return false;
        }

        return true;
    }

    private void LoadGameplayScene()
    {
        UIState.ResetAll();
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
