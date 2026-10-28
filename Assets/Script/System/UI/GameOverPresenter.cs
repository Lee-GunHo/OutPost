using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(GameOverView))]
public sealed class GameOverPresenter : MonoBehaviour
{
    private readonly GameOverModel model = new GameOverModel();
    private GameOverView view;

    private void Start()
    {
        UIState.ResetAll();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("Game Over EventSystem", typeof(EventSystem));
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        view = GetComponent<GameOverView>();
        view.MainMenuRequested += ReturnToMain;
        view.QuitRequested += Quit;
        view.Show(model.Title, model.Message);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ReturnToMain();
    }

    private void ReturnToMain()
    {
        if (!model.TryLeave()) return;
        if (!Application.CanStreamedLevelBeLoaded(GameOverModel.MainScene))
        {
            model.CancelLeave();
            view.ShowError("메인 메뉴 씬을 찾을 수 없습니다.");
            return;
        }
        view.SetBusy(true);
        UIState.ResetAll();
        SceneManager.LoadScene(GameOverModel.MainScene);
    }

    private void Quit()
    {
        if (!model.TryLeave()) return;
        view.SetBusy(true);
        UIState.ResetAll();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (view == null) return;
        view.MainMenuRequested -= ReturnToMain;
        view.QuitRequested -= Quit;
    }
}
