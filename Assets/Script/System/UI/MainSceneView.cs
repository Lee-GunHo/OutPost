using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays title-screen messages. Save checks and scene changes belong to MainSceneManager.
/// </summary>
[DisallowMultipleComponent]
public sealed class MainSceneView : MonoBehaviour
{
    private GameObject messagePanel;
    private TMP_Text messageText;
    private Coroutine hideRoutine;

    public void ShowMessage(string message, Transform parent)
    {
        if (parent == null)
            return;

        if (messagePanel == null)
            CreateMessagePanel(parent);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        messageText.text = message;
        messagePanel.SetActive(true);
        messagePanel.transform.SetAsLastSibling();
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private void CreateMessagePanel(Transform parent)
    {
        TMP_Text fontTemplate = parent.GetComponentInChildren<TMP_Text>(true);
        messagePanel = new GameObject("SaveMessage", typeof(RectTransform), typeof(Image));
        messagePanel.transform.SetParent(parent, false);

        RectTransform panelRect = (RectTransform)messagePanel.transform;
        panelRect.anchorMin = new Vector2(0.1f, 0.06f);
        panelRect.anchorMax = new Vector2(0.9f, 0.06f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, 90f);

        Image background = messagePanel.GetComponent<Image>();
        background.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(messagePanel.transform, false);
        messageText = textObject.GetComponent<TextMeshProUGUI>();
        if (fontTemplate != null)
        {
            messageText.font = fontTemplate.font;
            messageText.fontSharedMaterial = fontTemplate.fontSharedMaterial;
        }
        messageText.fontSize = 26f;
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 18f;
        messageText.fontSizeMax = 26f;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.raycastTarget = false;

        RectTransform textRect = messageText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 10f);
        textRect.offsetMax = new Vector2(-16f, -10f);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(4f);
        if (messagePanel != null)
            messagePanel.SetActive(false);
        hideRoutine = null;
    }

    private void OnDisable()
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = null;
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (messagePanel != null)
            Destroy(messagePanel);
    }
}
