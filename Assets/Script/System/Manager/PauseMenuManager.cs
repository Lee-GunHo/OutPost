using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public static bool IsMenuOpen { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionPanel;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainScene";

    [Header("Input")]
    [SerializeField] private UIInputManager inputManager;

    private bool isPauseOpen = false;
    private bool isOptionOpen = false;

    private void Start()
    {
        IsMenuOpen = false;
        UIState.SetPauseOpen(false);

        pausePanel.SetActive(false);
        optionPanel.SetActive(false);
    }


    private void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnPausePressed += TogglePause;
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnPausePressed -= TogglePause;
        }
    }


    private void TogglePause()
    {
        if (isOptionOpen)
        {
            CloseOptionPanel();
        }
        else if (isPauseOpen)
        {
            ClosePausePanel();
        }
        else
        {
            OpenPausePanel();
        }
    }

    private void OpenPausePanel()
    {
        isPauseOpen = true;
        isOptionOpen = false;
        IsMenuOpen = true;
        UIState.SetPauseOpen(true);

        pausePanel.SetActive(true);
        optionPanel.SetActive(false);
    }

    private void ClosePausePanel()
    {
        isPauseOpen = false;
        isOptionOpen = false;
        IsMenuOpen = false;
        UIState.SetPauseOpen(false);

        pausePanel.SetActive(false);
        optionPanel.SetActive(false);
    }

    public void OpenOptionPanel()
    {
        isPauseOpen = false;
        isOptionOpen = true;
        IsMenuOpen = true;
        UIState.SetPauseOpen(true);

        pausePanel.SetActive(false);
        optionPanel.SetActive(true);
    }

    public void CloseOptionPanel()
    {
        isOptionOpen = false;
        isPauseOpen = true;
        IsMenuOpen = true;
        UIState.SetPauseOpen(true);

        optionPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void GoToMainScene()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        IsMenuOpen = false;
        UIState.SetPauseOpen(false);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        IsMenuOpen = false;
        UIState.SetPauseOpen(false);

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}