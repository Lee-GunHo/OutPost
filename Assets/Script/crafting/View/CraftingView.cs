using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제작 UI. 한 줄 3칸 격자를 2줄 높이로 보여주고 나머지는 세로 스크롤.
/// 제목 양옆 ◀ ▶ 화살표로 하위/상위 등급 제작대 레시피를 오간다(코어키퍼 방식, 로직은 CraftingPresenter).
/// 씬/프리팹에서 크기·앵커가 여러 번 덮어써져 있어서, 레이아웃은 Awake에서 코드로 직접 잡는다.
/// 화살표가 인스펙터에 연결되지 않았으면 닫기 버튼을 복제해 만든다.
/// </summary>
public class CraftingView : MonoBehaviour
{
    private const int Columns = 3;
    private const int VisibleRows = 2;

    [Header("Recipe Slots")]
    [SerializeField] private Transform recipeSlotParent;
    [SerializeField] private CraftingRecipeSlotView recipeSlotPrefab;

    [Header("Close Button")]
    [SerializeField] private GameObject closeButton;

    [Header("Title / Tier Arrows (비워두면 자동 생성)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button lowerTierButton;
    [SerializeField] private Button higherTierButton;

    [Header("Layout")]
    [SerializeField] private float cellSize = 90f;
    [SerializeField] private float cellSpacing = 8f;
    [SerializeField] private float padding = 20f;
    [SerializeField] private float titleHeight = 60f;
    [SerializeField] private float scrollbarWidth = 8f;
    [SerializeField] private float scrollSensitivity = 30f;

    private readonly List<CraftingRecipeSlotView> recipeSlots
        = new List<CraftingRecipeSlotView>();

    private ScrollRect scrollRect;
    private bool isLayoutBuilt;

    public event Action<CraftingRecipe> OnRecipeClicked;
    public event Action<CraftingRecipe> OnRecipePointerEntered;
    public event Action OnRecipePointerExited;
    public event Action OnLowerTierClicked;
    public event Action OnHigherTierClicked;

    private void Awake()
    {
        BuildLayout();
    }

    public void SetTitle(string title)
    {
        BuildLayout();

        if (titleText != null)
            titleText.text = title;
    }

    /// <summary>
    /// 이동할 수 있는 방향의 화살표만 보인다. 손 제작/나무 제작대는 둘 다 숨김.
    /// </summary>
    public void SetTierArrows(bool canGoLower, bool canGoHigher)
    {
        BuildLayout();

        if (lowerTierButton != null)
            lowerTierButton.gameObject.SetActive(canGoLower);

        if (higherTierButton != null)
            higherTierButton.gameObject.SetActive(canGoHigher);
    }

    public void ShowRecipes(IReadOnlyList<CraftingRecipe> recipes)
    {
        BuildLayout();

        if (recipeSlotParent == null || recipeSlotPrefab == null)
            return;

        int recipeCount = recipes != null ? recipes.Count : 0;

        // 빈 칸으로 마지막 줄을 채우고, 최소 보이는 2줄은 항상 유지해서 격자 모양을 고정
        int slotCount = Mathf.Max(Columns * VisibleRows,
            Mathf.CeilToInt(recipeCount / (float)Columns) * Columns);

        while (recipeSlots.Count < slotCount)
            recipeSlots.Add(CreateSlot());

        for (int i = 0; i < recipeSlots.Count; i++)
        {
            CraftingRecipeSlotView slot = recipeSlots[i];
            slot.gameObject.SetActive(i < slotCount);

            if (i < recipeCount && recipes[i] != null)
                slot.Initialize(recipes[i]);
            else
                slot.Clear();
        }

        // 제작대를 바꾸거나 새로 열면 맨 위부터
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void SetRecipeCraftable(int index, bool canCraft)
    {
        if (index < 0 || index >= recipeSlots.Count)
            return;

        recipeSlots[index].SetCraftable(canCraft);
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void SetCloseButtonVisible(bool isVisible)
    {
        if (closeButton != null)
            closeButton.SetActive(isVisible);
    }

    public void ClearRecipeSlots()
    {
        ShowRecipes(null);
    }

    // ---------------------------------------------------------------- 레이아웃

    private void BuildLayout()
    {
        if (isLayoutBuilt || recipeSlotParent == null || recipeSlotPrefab == null)
            return;

        isLayoutBuilt = true;

        float gridWidth = Columns * cellSize + (Columns - 1) * cellSpacing;
        float viewportHeight = VisibleRows * cellSize + (VisibleRows - 1) * cellSpacing;
        float panelWidth = gridWidth + padding * 2f + scrollbarWidth * 2f;
        float panelHeight = titleHeight + viewportHeight + padding;

        ((RectTransform)transform).sizeDelta = new Vector2(panelWidth, panelHeight);

        BuildScrollArea(gridWidth, viewportHeight);
        BuildTitle(gridWidth);
        BuildTierArrows();

        // 이전 방식에서 남아있던 슬롯 정리
        for (int i = recipeSlotParent.childCount - 1; i >= 0; i--)
            Destroy(recipeSlotParent.GetChild(i).gameObject);
    }

    /// <summary>
    /// Viewport(마스크 + ScrollRect) 아래로 기존 recipeSlotParent를 옮겨 content로 사용.
    /// content는 GridLayoutGroup + ContentSizeFitter로 레시피 줄 수만큼 세로로 늘어난다.
    /// </summary>
    private void BuildScrollArea(float gridWidth, float viewportHeight)
    {
        GameObject viewportObject = new GameObject("RecipeViewport",
            typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        RectTransform viewport = (RectTransform)viewportObject.transform;
        viewport.SetParent(transform, false);
        SetTopCenter(viewport, new Vector2(gridWidth, viewportHeight), -titleHeight);

        // 빈 공간에서도 휠/드래그가 먹도록 투명 이미지
        viewportObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        RectTransform content = (RectTransform)recipeSlotParent;
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = content.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(cellSpacing, cellSpacing);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.padding = new RectOffset();

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();

        fitter.enabled = true;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = viewportObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = scrollSensitivity;
        scrollRect.verticalScrollbar = CreateScrollbar(viewport, viewportHeight);
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    // 격자 오른쪽의 얇은 세로 스크롤바. 레시피가 6개 이하면 자동으로 숨는다.
    private Scrollbar CreateScrollbar(RectTransform viewport, float height)
    {
        GameObject barObject = new GameObject("RecipeScrollbar",
            typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        RectTransform bar = (RectTransform)barObject.transform;
        bar.SetParent(transform, false);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0f, 1f);
        bar.sizeDelta = new Vector2(scrollbarWidth, height);
        bar.anchoredPosition = new Vector2(viewport.sizeDelta.x * 0.5f + scrollbarWidth * 0.5f, -titleHeight);
        barObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        RectTransform handle = (RectTransform)handleObject.transform;
        handle.SetParent(bar, false);
        handle.anchorMin = Vector2.zero;
        handle.anchorMax = Vector2.one;
        handle.sizeDelta = Vector2.zero;
        handleObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);

        Scrollbar scrollbar = barObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleObject.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    private void BuildTitle(float gridWidth)
    {
        if (titleText == null)
            titleText = FindTitleText();

        if (titleText == null)
            return;

        // 양옆 화살표 자리를 비워둔 폭
        SetTopCenter(titleText.rectTransform, new Vector2(gridWidth - 100f, titleHeight), 0f);
        titleText.alignment = TextAlignmentOptions.Center;
    }

    private void BuildTierArrows()
    {
        if (lowerTierButton == null)
            lowerTierButton = CreateArrowButton("LowerTierButton", "<");

        if (higherTierButton == null)
            higherTierButton = CreateArrowButton("HigherTierButton", ">");

        float arrowSize = 36f;
        float arrowY = -(titleHeight - arrowSize) * 0.5f;
        float arrowX = titleText != null ? titleText.rectTransform.sizeDelta.x * 0.5f + arrowSize * 0.5f + 4f : 90f;

        if (lowerTierButton != null)
        {
            SetTopCenter((RectTransform)lowerTierButton.transform, new Vector2(arrowSize, arrowSize), arrowY, -arrowX);
            lowerTierButton.onClick.AddListener(() => OnLowerTierClicked?.Invoke());
            lowerTierButton.gameObject.SetActive(false);
        }

        if (higherTierButton != null)
        {
            SetTopCenter((RectTransform)higherTierButton.transform, new Vector2(arrowSize, arrowSize), arrowY, arrowX);
            higherTierButton.onClick.AddListener(() => OnHigherTierClicked?.Invoke());
            higherTierButton.gameObject.SetActive(false);
        }
    }

    // 닫기 버튼을 복제해 같은 스타일의 화살표 버튼을 만든다(복제된 닫기 동작은 제거)
    private Button CreateArrowButton(string objectName, string label)
    {
        if (closeButton == null)
            return null;

        GameObject copy = Instantiate(closeButton, transform);
        copy.name = objectName;

        Button button = copy.GetComponent<Button>();
        if (button == null)
            return null;

        button.onClick = new Button.ButtonClickedEvent();

        TMP_Text text = copy.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;

        return button;
    }

    private CraftingRecipeSlotView CreateSlot()
    {
        CraftingRecipeSlotView slot = Instantiate(recipeSlotPrefab, recipeSlotParent);
        slot.Clear();
        slot.OnClicked += HandleRecipeClicked;
        slot.OnPointerEntered += HandleRecipePointerEntered;
        slot.OnPointerExited += HandleRecipePointerExited;
        return slot;
    }

    // 닫기 버튼 밖에 있는 첫 번째 직계 자식 텍스트 = 제목
    private TMP_Text FindTitleText()
    {
        foreach (Transform child in transform)
        {
            if (closeButton != null && child.gameObject == closeButton)
                continue;

            if (child.TryGetComponent(out TMP_Text text))
                return text;
        }

        return null;
    }

    private static void SetTopCenter(RectTransform rect, Vector2 size, float y, float x = 0f)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(x, y);
    }

    // ---------------------------------------------------------------- 이벤트

    private void HandleRecipeClicked(CraftingRecipe recipe)
    {
        OnRecipeClicked?.Invoke(recipe);
    }

    private void HandleRecipePointerEntered(CraftingRecipe recipe)
    {
        OnRecipePointerEntered?.Invoke(recipe);
    }

    private void HandleRecipePointerExited()
    {
        OnRecipePointerExited?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (CraftingRecipeSlotView slot in recipeSlots)
        {
            if (slot == null)
                continue;

            slot.OnClicked -= HandleRecipeClicked;
            slot.OnPointerEntered -= HandleRecipePointerEntered;
            slot.OnPointerExited -= HandleRecipePointerExited;
        }
    }
}
