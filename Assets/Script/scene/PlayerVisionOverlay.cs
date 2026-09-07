using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a circular visibility mask over the world, below screen-space HUD canvases.
/// Radius and feather width are fractions of the shorter screen dimension.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class PlayerVisionOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Material overlayMaterial;

    [Header("Visibility")]
    [Tooltip("Fully visible radius, as a fraction of the shorter screen dimension.")]
    [SerializeField, Range(0.05f, 1f)] private float visionRadius = 0.3f;
    [Tooltip("Width of the gradual transition from clear to dark.")]
    [SerializeField, Range(0.001f, 0.5f)] private float edgeSoftness = 0.2f;
    [Tooltip("Outer darkness: 0 is transparent, 1 is fully opaque.")]
    [SerializeField, Range(0f, 1f)] private float darknessOpacity = 1f;

    private static readonly int CenterId = Shader.PropertyToID("_VisionCenter");
    private static readonly int ScreenScaleId = Shader.PropertyToID("_ScreenScale");
    private static readonly int RadiusId = Shader.PropertyToID("_VisionRadius");
    private static readonly int SoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int OpacityId = Shader.PropertyToID("_DarknessOpacity");

    private Camera viewCamera;
    private Canvas overlayCanvas;
    private RawImage overlayImage;
    private Material runtimeMaterial;

    private void OnEnable()
    {
        viewCamera = GetComponent<Camera>();

        if (overlayMaterial == null)
        {
            Debug.LogError("PlayerVisionOverlay requires a vision overlay material.", this);
            enabled = false;
            return;
        }

        runtimeMaterial = new Material(overlayMaterial);
        runtimeMaterial.name = "Player Vision Overlay (Runtime)";

        // A separate root keeps this overlay independent of camera transforms and HUD layout.
        GameObject canvasObject = new GameObject("Player Vision Overlay", typeof(RectTransform), typeof(Canvas));
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = -100;
        overlayCanvas.targetDisplay = viewCamera.targetDisplay;

        GameObject imageObject = new GameObject("Darkness", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);
        overlayImage = imageObject.GetComponent<RawImage>();
        overlayImage.material = runtimeMaterial;
        overlayImage.raycastTarget = false;
        overlayImage.maskable = false;

        RectTransform rect = overlayImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UpdateOverlay();
    }

    private void LateUpdate()
    {
        // Run after CameraManager so camera smoothing does not offset the visibility circle.
        UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        if (runtimeMaterial == null || overlayCanvas == null)
            return;

        bool visible = target != null && viewCamera != null && viewCamera.isActiveAndEnabled;
        overlayCanvas.enabled = visible;
        if (!visible)
            return;

        Vector3 screenPosition = viewCamera.WorldToScreenPoint(target.position);
        overlayCanvas.enabled = screenPosition.z > 0f;
        if (screenPosition.z <= 0f)
            return;

        float width = Mathf.Max(1, Screen.width);
        float height = Mathf.Max(1, Screen.height);
        float shorterSide = Mathf.Min(width, height);

        runtimeMaterial.SetVector(CenterId, new Vector4(screenPosition.x / width, screenPosition.y / height, 0f, 0f));
        runtimeMaterial.SetVector(ScreenScaleId, new Vector4(width / shorterSide, height / shorterSide, 0f, 0f));
        runtimeMaterial.SetFloat(RadiusId, Mathf.Max(0.001f, visionRadius));
        runtimeMaterial.SetFloat(SoftnessId, Mathf.Max(0.001f, edgeSoftness));
        runtimeMaterial.SetFloat(OpacityId, Mathf.Clamp01(darknessOpacity));
    }

    private void OnDisable()
    {
        if (overlayCanvas != null)
        {
            overlayCanvas.gameObject.SetActive(false);
            Destroy(overlayCanvas.gameObject);
        }

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);

        overlayCanvas = null;
        overlayImage = null;
        runtimeMaterial = null;
    }
}
