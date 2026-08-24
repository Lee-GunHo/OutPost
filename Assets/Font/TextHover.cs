using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TextHoverFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private TMP_Text targetText;

    [Header("Alpha")]
    [Range(0f, 1f)]
    [SerializeField] private float normalAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float hoverAlpha = 0.35f;

    [Header("Fade Speed")]
    [SerializeField] private float fadeSpeed = 10f;

    private float currentAlpha;
    private float targetAlpha;

    private void Awake()
    {
        if (targetText == null)
        {
            // 버튼 자식에서 자동으로 찾기 (못 찾으면 인스펙터에 넣어줘)
            targetText = GetComponentInChildren<TMP_Text>(true);
        }

        currentAlpha = normalAlpha;
        targetAlpha = normalAlpha;
        ApplyAlphaInstant(normalAlpha);
    }

    private void Update()
    {
        if (Mathf.Approximately(currentAlpha, targetAlpha)) return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        ApplyAlphaInstant(currentAlpha);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetAlpha = hoverAlpha;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = normalAlpha;
    }

    private void ApplyAlphaInstant(float a)
    {
        if (targetText == null) return;

        Color c = targetText.color;
        c.a = a;
        targetText.color = c;
    }
}