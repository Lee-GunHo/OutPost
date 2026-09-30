using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 구석에 작게 뜨는 비침습 만족도 팝업(1~5점 + 건너뛰기).
/// UIState를 건드리지 않아서 플레이는 그대로 계속됨. 일정 시간 무응답이면 자동으로 건너뜀 처리.
/// </summary>
public class TipFeedbackUI : MonoBehaviour
{
    public static TipFeedbackUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [Tooltip("1~5점 버튼 5개, 순서대로 1점부터")]
    [SerializeField] private Button[] ratingButtons;
    [SerializeField] private Button skipButton;
    [SerializeField] private float autoDismissSeconds = 15f;

    private Coroutine autoDismissCoroutine;
    private Action<int?> onAnswered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (ratingButtons != null)
        {
            for (int i = 0; i < ratingButtons.Length; i++)
            {
                Button button = ratingButtons[i];

                if (button == null)
                {
                    continue;
                }

                int rating = i + 1;
                button.onClick.AddListener(() => Answer(rating));
            }
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(() => Answer(null));
        }
    }

    /// <summary>
    /// 팝업을 띄움. 사용자가 점수를 고르거나 건너뛰거나 시간 초과되면 onAnswered가 호출됨
    /// (rating: 1~5, 건너뜀/타임아웃이면 null).
    /// </summary>
    public void Show(Action<int?> onAnswered)
    {
        if (panel == null)
        {
            onAnswered?.Invoke(null);
            return;
        }

        this.onAnswered = onAnswered;
        panel.SetActive(true);

        if (autoDismissCoroutine != null)
        {
            StopCoroutine(autoDismissCoroutine);
        }

        autoDismissCoroutine = StartCoroutine(AutoDismissRoutine());
    }

    private void Answer(int? rating)
    {
        if (autoDismissCoroutine != null)
        {
            StopCoroutine(autoDismissCoroutine);
            autoDismissCoroutine = null;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }

        Action<int?> callback = onAnswered;
        onAnswered = null;
        callback?.Invoke(rating);
    }

    private IEnumerator AutoDismissRoutine()
    {
        yield return new WaitForSecondsRealtime(autoDismissSeconds);
        Answer(null);
    }
}
