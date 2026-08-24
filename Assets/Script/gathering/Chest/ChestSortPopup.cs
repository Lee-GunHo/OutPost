using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 창고 정렬 방식 선택 팝업
/// 현재 커서 위치에 표시되며,
/// 정렬 버튼 외부 영역 클릭시 닫힘
/// </summary>
public class ChestSortPopup : MonoBehaviour
{
    [Header("전체 팝업")]
    [SerializeField] private GameObject popupRoot;

    [Header("위치가 이동할 팝업 패널")]
    [SerializeField] private RectTransform popupPanel;

    [Header("팝업 외부 클릭 감지")]
    [SerializeField] private Button backdropButton;

    [Header("정렬 버튼")]
    [SerializeField] private Button gatherButton;
    [SerializeField] private Button sortByTypeButton;

    private Action onGather;
    private Action onSortByType;
    private bool isInitialized;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        Close();
    }

    public void Initialize(
        Action gatherAction,
        Action sortByTypeAction)
    {
        if (isInitialized)
            return;

        onGather = gatherAction;
        onSortByType = sortByTypeAction;

        if(gatherButton != null)
        {
            gatherButton.onClick.AddListener(
                OnGatherButtonClicked
            );
        }

        if(sortByTypeButton != null)
        {
            sortByTypeButton.onClick.AddListener(
                OnSortByTypeButtonClicked
            );
        }

        if(backdropButton != null)
        {
            backdropButton.onClick.AddListener(Close);
        }

        isInitialized = true;
    }

    // 지정된 화면 좌표에 팝업을 표시
    public void Open(
        Vector2 screenPosition,
        Canvas parentCanvas)
    {
        if (popupRoot == null || popupPanel == null)
            return;

        popupRoot.SetActive(true);

        PositionPopup(screenPosition, parentCanvas);

        // Backdrop보다 앞에 표시
        popupPanel.SetAsLastSibling();
    }

    public void Close()
    {
        if(popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnGatherButtonClicked()
    {
        onGather?.Invoke();
    }

    private void OnSortByTypeButtonClicked()
    {
        onSortByType?.Invoke();
    }

    private void PositionPopup(
        Vector2 screenPosition,
        Canvas parentCanvas)
    {
        RectTransform parentRect =
            popupPanel.parent as RectTransform;

        if (parentRect == null)
            return;

        Camera uiCamera = null;

        if(parentCanvas != null &&
            parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = parentCanvas.worldCamera;
        }

        bool converted =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                uiCamera,
                out Vector2 localPosition
            );

        if (!converted)
            return;

        popupPanel.anchoredPosition = localPosition;

        ClampPopupInsideParent(parentRect);
    }

    private void ClampPopupInsideParent(
        RectTransform parentRect)
    {
        Canvas.ForceUpdateCanvases();

        Vector2 position = popupPanel.anchoredPosition;
        Rect parentBounds = parentRect.rect;
        Rect popupBounds = popupPanel.rect;

        float pivotX = popupPanel.pivot.x;
        float pivotY = popupPanel.pivot.y;

        float minimumX =
            parentBounds.xMin + popupBounds.width * pivotX;

        float maximumX = 
            parentBounds.xMax - 
            popupBounds.width * (1f - pivotX);

        float minimumY = 
            parentBounds.yMin + popupBounds.height * pivotY;

        float maximumY = 
            parentBounds.yMax -
            popupBounds.height * (1f - pivotY);

        position.x = Mathf.Clamp(
            position.x,
            minimumX,
            maximumX
        );

        position.y = Mathf.Clamp(
            position.y,
            minimumY,
            maximumY
        );

        popupPanel.anchoredPosition = position;
    }
}
