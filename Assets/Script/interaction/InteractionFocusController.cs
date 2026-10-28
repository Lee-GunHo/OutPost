using UnityEngine;

/// <summary>
/// F키를 눌렀을 때 실제로 상호작용될 대상(PlayerPresenter.FindClosestInteractable) 하나에만
/// 흰색 외곽선(InteractionHighlight)과 F키 안내(InteractionPromptUI)를 표시한다.
/// UI가 열려 있거나 플레이어가 죽어 있으면 모두 숨김. PlayerPresenter가 Awake에서 자동으로 붙임.
/// </summary>
[RequireComponent(typeof(PlayerPresenter))]
public class InteractionFocusController : MonoBehaviour
{
    private const string DefaultActionText = "상호작용";

    private PlayerPresenter player;
    private IInteractable focused;
    private MonoBehaviour focusedObject;
    private InteractionHighlight focusedHighlight;

    private void Awake()
    {
        player = GetComponent<PlayerPresenter>();
    }

    private void Update()
    {
        IInteractable target = CanFocus() ? player.FindClosestInteractable() : null;

        // 대상이 바뀌었거나 포커스 중이던 오브젝트가 파괴된 경우
        if (!ReferenceEquals(target, focused) || (focused != null && focusedObject == null))
        {
            ChangeFocus(target);
        }

        if (focused != null)
        {
            ShowPrompt();
        }
    }

    private void OnDisable()
    {
        ChangeFocus(null);
    }

    private bool CanFocus()
    {
        return player != null && !player.IsDead && !UIState.IsAnyUIOpen;
    }

    private void ChangeFocus(IInteractable target)
    {
        if (focusedHighlight != null)
        {
            focusedHighlight.SetHighlighted(false);
        }

        focused = null;
        focusedObject = null;
        focusedHighlight = null;

        MonoBehaviour targetObject = target as MonoBehaviour;

        if (targetObject == null)
        {
            HidePrompt();
            return;
        }

        focused = target;
        focusedObject = targetObject;

        focusedHighlight = targetObject.GetComponent<InteractionHighlight>();

        if (focusedHighlight == null)
        {
            focusedHighlight = targetObject.gameObject.AddComponent<InteractionHighlight>();
        }

        focusedHighlight.SetHighlighted(true);
    }

    private void ShowPrompt()
    {
        string displayName = focusedObject.gameObject.name;
        string actionText = DefaultActionText;

        if (focused is IInteractionPromptSource source)
        {
            displayName = source.InteractionDisplayName;
            actionText = string.IsNullOrEmpty(source.InteractionActionText)
                ? DefaultActionText
                : source.InteractionActionText;
        }

        InteractionPromptUI.GetOrCreate().Show(focusedHighlight, displayName, actionText);
    }

    private static void HidePrompt()
    {
        // 씬 종료 중엔 새로 만들지 않도록 기존 것만 숨김
        if (InteractionPromptUI.Instance != null)
        {
            InteractionPromptUI.Instance.Hide();
        }
    }
}
