using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 상호작용 대상 옆에 "[F] 눌러서 ○○" 안내를 띄우는 공용 UI.
/// 예전처럼 상자/제작대/NPC마다 Canvas를 붙이지 않고 씬에 하나만 만들어 위치만 대상 옆으로 옮긴다.
/// 프리팹: Resources/Interaction/InteractionPrompt (Tools > Interaction > Create Interaction Prompt Prefab 으로 생성,
/// 생성 후 색/크기/폰트는 프리팹에서 자유롭게 수정). 프리팹이 없으면 같은 모양을 코드로 만들어 씀.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    public const string ResourcePath = "Interaction/InteractionPrompt";

    [Header("UI 참조")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text actionText;

    [Header("문구")]
    [Tooltip("{0} = 대상의 행동 문구 (열기/사용하기/대화하기)")]
    [SerializeField] private string actionFormat = "눌러서 {0}";

    [Header("위치")]
    [Tooltip("대상 화면 테두리에서 안내창까지 거리 (픽셀)")]
    [SerializeField] private float screenGap = 16f;
    [Tooltip("화면 가장자리 여백 (픽셀)")]
    [SerializeField] private float screenEdgePadding = 8f;

    private static InteractionPromptUI instance;
    public static InteractionPromptUI Instance => instance;

    private static readonly Vector3[] boundsCorners = new Vector3[8];

    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera targetCamera;

    private InteractionHighlight targetHighlight;
    private Transform targetTransform;
    private string shownName;
    private string shownAction;
    private bool layoutDirty;

    public static InteractionPromptUI GetOrCreate()
    {
        if (instance != null)
            return instance;

        InteractionPromptUI prefab = Resources.Load<InteractionPromptUI>(ResourcePath);

        if (prefab != null)
        {
            instance = Instantiate(prefab);
            instance.name = prefab.name;
        }
        else
        {
            Debug.LogWarning("Resources/" + ResourcePath + " 프리팹이 없어 기본 F키 안내 UI를 코드로 생성합니다. " +
                "(Tools > Interaction > Create Interaction Prompt Prefab)");
            instance = BuildDefaultHierarchy(null).GetComponent<InteractionPromptUI>();
        }

        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        canvas = GetComponent<Canvas>();
        canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        SetPanelVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Show(InteractionHighlight highlight, string displayName, string action)
    {
        targetHighlight = highlight;
        targetTransform = highlight != null ? highlight.transform : null;

        if (shownName != displayName)
        {
            shownName = displayName;
            nameText.text = displayName;
            nameText.gameObject.SetActive(!string.IsNullOrEmpty(displayName));
            layoutDirty = true;
        }

        if (shownAction != action)
        {
            shownAction = action;
            actionText.text = string.Format(actionFormat, action);
            layoutDirty = true;
        }

        UpdatePosition();
    }

    public void Hide()
    {
        targetHighlight = null;
        targetTransform = null;
        SetPanelVisible(false);
    }

    // 카메라가 LateUpdate에서 따라가므로 같은 프레임 이후 위치로 맞춤
    private void LateUpdate()
    {
        if (targetTransform != null)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        if (targetTransform == null || canvasRect == null)
        {
            SetPanelVisible(false);
            return;
        }

        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            SetPanelVisible(false);
            return;
        }

        Bounds bounds;

        if (targetHighlight == null || !targetHighlight.TryGetBounds(out bounds))
        {
            bounds = new Bounds(targetTransform.position, Vector3.one);
        }

        if (!TryGetScreenRect(bounds, out Vector2 screenMin, out Vector2 screenMax))
        {
            SetPanelVisible(false);
            return;
        }

        // 막 켜졌거나 문구가 바뀐 직후엔 ContentSizeFitter 크기가 아직 갱신 전이라 즉시 다시 계산
        if (SetPanelVisible(true) || layoutDirty)
        {
            layoutDirty = false;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        float scale = canvas.scaleFactor;
        Vector2 panelSize = panel.rect.size * scale;
        float centerY = (screenMin.y + screenMax.y) * 0.5f;

        // 기본은 대상 오른쪽, 화면 밖으로 나가면 왼쪽
        bool placeRight = screenMax.x + screenGap + panelSize.x <= Screen.width - screenEdgePadding;
        float x = placeRight
            ? screenMax.x + screenGap
            : screenMin.x - screenGap - panelSize.x;

        x = Mathf.Clamp(x, screenEdgePadding, Mathf.Max(screenEdgePadding, Screen.width - screenEdgePadding - panelSize.x));
        float halfHeight = panelSize.y * 0.5f;
        float y = Mathf.Clamp(centerY, screenEdgePadding + halfHeight,
            Mathf.Max(screenEdgePadding + halfHeight, Screen.height - screenEdgePadding - halfHeight));

        // 피벗(0, 0.5) 기준 화면 좌표 → 캔버스 로컬 좌표 (Overlay 캔버스라 카메라는 null)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(x, y), null, out Vector2 localPoint);
        panel.localPosition = localPoint;
    }

    private bool TryGetScreenRect(Bounds bounds, out Vector2 screenMin, out Vector2 screenMax)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        int index = 0;
        for (int xSign = -1; xSign <= 1; xSign += 2)
        for (int ySign = -1; ySign <= 1; ySign += 2)
        for (int zSign = -1; zSign <= 1; zSign += 2)
        {
            boundsCorners[index++] = center + Vector3.Scale(extents, new Vector3(xSign, ySign, zSign));
        }

        screenMin = new Vector2(float.MaxValue, float.MaxValue);
        screenMax = new Vector2(float.MinValue, float.MinValue);

        foreach (Vector3 corner in boundsCorners)
        {
            Vector3 screenPoint = targetCamera.WorldToScreenPoint(corner);

            // 카메라 뒤에 있는 모서리가 있으면 위치가 뒤집히므로 표시하지 않음
            if (screenPoint.z <= 0f)
                return false;

            screenMin = Vector2.Min(screenMin, screenPoint);
            screenMax = Vector2.Max(screenMax, screenPoint);
        }

        return true;
    }

    // 표시 상태가 실제로 바뀌었으면 true
    private bool SetPanelVisible(bool visible)
    {
        if (panel == null || panel.gameObject.activeSelf == visible)
            return false;

        panel.gameObject.SetActive(visible);
        return true;
    }

    // ---------------------------------------------------------------- 기본 모양 (에디터 프리팹 생성 / 프리팹 없을 때 공용)

    /// <summary>
    /// 기본 F키 안내 UI 계층을 만든다. 에디터 메뉴가 이걸로 프리팹을 저장하고, 프리팹이 없을 때 런타임에서도 사용.
    /// </summary>
    public static GameObject BuildDefaultHierarchy(TMP_FontAsset font)
    {
        GameObject root = new GameObject("InteractionPrompt", typeof(RectTransform));
        root.layer = 5;

        Canvas rootCanvas = root.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 40;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 패널: [F 키캡] [이름 / 행동] 가로 배치, 내용 길이에 맞춰 크기 자동
        RectTransform panelRect = CreateRect("Panel_Prompt", root.transform);
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);

        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);
        panelImage.raycastTarget = false;

        HorizontalLayoutGroup panelLayout = panelRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        panelLayout.padding = new RectOffset(12, 16, 8, 8);
        panelLayout.spacing = 12f;
        panelLayout.childAlignment = TextAnchor.MiddleLeft;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = false;
        panelLayout.childForceExpandHeight = false;

        ContentSizeFitter panelFitter = panelRect.gameObject.AddComponent<ContentSizeFitter>();
        panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform keyRect = CreateRect("Image_Key", panelRect);
        Image keyImage = keyRect.gameObject.AddComponent<Image>();
        keyImage.color = Color.white;
        keyImage.raycastTarget = false;

        LayoutElement keyLayout = keyRect.gameObject.AddComponent<LayoutElement>();
        keyLayout.minWidth = keyLayout.preferredWidth = 44f;
        keyLayout.minHeight = keyLayout.preferredHeight = 44f;

        TMP_Text keyText = CreateText("Text_Key", keyRect, font, "F", 30f, Color.black, FontStyles.Bold);
        keyText.alignment = TextAlignmentOptions.Center;
        RectTransform keyTextRect = keyText.rectTransform;
        keyTextRect.anchorMin = Vector2.zero;
        keyTextRect.anchorMax = Vector2.one;
        keyTextRect.offsetMin = keyTextRect.offsetMax = Vector2.zero;

        RectTransform columnRect = CreateRect("TextColumn", panelRect);
        VerticalLayoutGroup columnLayout = columnRect.gameObject.AddComponent<VerticalLayoutGroup>();
        columnLayout.spacing = 2f;
        columnLayout.childAlignment = TextAnchor.MiddleLeft;
        columnLayout.childControlWidth = true;
        columnLayout.childControlHeight = true;
        columnLayout.childForceExpandWidth = false;
        columnLayout.childForceExpandHeight = false;

        TMP_Text nameLabel = CreateText("Text_Name", columnRect, font, "상자", 28f, Color.white, FontStyles.Bold);
        TMP_Text actionLabel = CreateText("Text_Action", columnRect, font, "눌러서 상호작용", 22f,
            new Color(0.85f, 0.85f, 0.85f, 1f), FontStyles.Normal);

        InteractionPromptUI prompt = root.AddComponent<InteractionPromptUI>();
        prompt.panel = panelRect;
        prompt.nameText = nameLabel;
        prompt.actionText = actionLabel;

        // 런타임 생성 시 Awake가 참조 연결 전에 돌았으므로 여기서 다시 맞춤
        prompt.canvas = rootCanvas;
        prompt.canvasRect = root.GetComponent<RectTransform>();
        prompt.SetPanelVisible(false);

        return root;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = 5;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, TMP_FontAsset font,
        string text, float fontSize, Color color, FontStyles style)
    {
        RectTransform rect = CreateRect(objectName, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();

        if (font != null)
        {
            label.font = font;
        }

        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.fontStyle = style;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }
}
