using System;
using UnityEngine;
using UnityEngine.UI;

// Creates a small responsive uGUI screen; no save or scene-navigation decisions.
public sealed class GameOverView : MonoBehaviour
{
    [SerializeField] private Font font;
    public event Action MainMenuRequested;
    public event Action QuitRequested;
    private Button mainButton, quitButton;
    private Text messageText;
    private GameObject canvasObject;

    public void Show(string title, string message)
    {
        if (canvasObject != null) return;
        canvasObject = new GameObject("Game Over Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform background = Rect("Background", canvasObject.transform, Vector2.zero, Vector2.zero);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.045f, 0.055f, 1f);
        Label("OUTPOST", background, new Vector2(0, 215), new Vector2(800, 45), 24,
            new Color(0.55f, 0.63f, 0.65f));
        Label("GAME OVER", background, new Vector2(0, 145), new Vector2(900, 95), 64,
            new Color(0.85f, 0.38f, 0.28f));
        Label(title, background, new Vector2(0, 70), new Vector2(900, 60), 30, Color.white);
        messageText = Label(message, background, new Vector2(0, -5), new Vector2(1000, 80), 22,
            new Color(0.75f, 0.79f, 0.8f));
        mainButton = MakeButton("메인 메뉴로", background, new Vector2(0, -110),
            new Color(0.22f, 0.36f, 0.38f), () => MainMenuRequested?.Invoke());
        quitButton = MakeButton("게임 종료", background, new Vector2(0, -185),
            new Color(0.16f, 0.19f, 0.22f), () => QuitRequested?.Invoke());
        Label("ESC · 메인 메뉴", background, new Vector2(0, -270), new Vector2(600, 32), 18,
            new Color(0.5f, 0.55f, 0.58f));
        mainButton.Select();
    }

    public void SetBusy(bool busy)
    {
        mainButton.interactable = !busy;
        quitButton.interactable = !busy;
    }

    public void ShowError(string text) { messageText.text = text; }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private Text Label(string text, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        Text label = Rect("Label", parent, position, size).gameObject.AddComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private Button MakeButton(string text, Transform parent, Vector2 position, Color color, Action onClick)
    {
        RectTransform rect = Rect(text, parent, position, new Vector2(360, 58));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(() => onClick());
        Label(text, rect, Vector2.zero, new Vector2(340, 52), 24, Color.white);
        return button;
    }
}
