using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainSceneManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelFirst;
    [SerializeField] private GameObject panelOption;
    [SerializeField] private GameObject panelSecond;
    [SerializeField] private GameObject panelCredit;

    [Header("Scene")]
    [Tooltip("게임플레이 씬 이름 (Build Settings에 추가되어 있어야 함)")]
#if UNITY_EDITOR
    [Header("Scene")]
    [SerializeField] private SceneAsset gameplayScene;
#endif

    private string gameplaySceneName;

    [Header("Save")]
    [Tooltip("GlobalDataManager가 쓰는 저장 파일명과 반드시 같아야 함")]
    [SerializeField] private string saveFileName = "SaveFile.json";
    private void Awake()
    {
#if UNITY_EDITOR
        if (gameplayScene != null)
        {
            gameplaySceneName = gameplayScene.name;
        }
#endif
    }
    private void Start()
    {
        ShowFirst();
    }

    private void DisableAll()
    {
        if (panelFirst != null) panelFirst.SetActive(false);
        if (panelOption != null) panelOption.SetActive(false);
        if (panelSecond != null) panelSecond.SetActive(false);
        if (panelCredit != null) panelCredit.SetActive(false);
    }

    // ===== Panel Show =====

    public void ShowFirst()
    {
        DisableAll();
        if (panelFirst != null) panelFirst.SetActive(true);
    }

    public void ShowOption()
    {
        DisableAll();
        if (panelOption != null) panelOption.SetActive(true);
    }

    public void ShowSecond()
    {
        DisableAll();
        if (panelSecond != null) panelSecond.SetActive(true);
    }

    public void ShowCredit()
    {
        DisableAll();
        if (panelCredit != null) panelCredit.SetActive(true);
    }

    // ===== Save Check =====

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    private bool HasSaveFile()
    {
        string path = GetSavePath();
        if (!File.Exists(path)) return false;

        // 파일은 있는데 내용이 비어있을 수도 있어서 최소한의 안전장치
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
        if (!File.Exists(path)) return;

        try
        {
            File.Delete(path);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"세이브 파일 삭제 실패: {e.Message}\nPath: {path}");
        }
    }

    // ===== Button Logic =====

    // [첫 화면] 게임 시작 버튼
    public void StartGameButton()
    {
        ShowSecond();
    }

    // [두 번째 화면] 이어하기
    public void ContinueButton()
    {
        Debug.Log("[TITLE] 이어하기 선택");

        if (HasSaveFile())
        {
            Debug.Log("[TITLE] 세이브 파일 확인 완료 → 이어서 게임 시작");
        }
        else
        {
            Debug.LogWarning("[TITLE]  이어하기 눌렀지만 세이브 파일 없음");
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    // [두 번째 화면] 새로하기
    public void NewGameButton()
    {
        Debug.Log("[TITLE] 새로하기 선택");

        if (HasSaveFile())
        {
            Debug.Log("[TITLE] 기존 세이브 파일 발견 → 삭제 진행");
        }
        else
        {
            Debug.Log("[TITLE] 기존 세이브 파일 없음");
        }

        DeleteSaveFileIfExists();



   
        Debug.Log("[TITLE] 새 게임 시작 (스토리 다시 재생)");


        SceneManager.LoadScene(gameplaySceneName);
        Debug.Log("[TITLE] 새 게임 시작");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // ===== Optional: 씬 이름을 나중에 바꿔 끼울 수 있게 =====
    // 다른 스크립트에서 필요하면 호출해서 씬 이름 교체 가능
    public void SetGameplaySceneName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            gameplaySceneName = sceneName;
    }
}