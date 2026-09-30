using System.Collections;
using UnityEngine;

/// <summary>
/// 팁을 준 뒤의 후속 처리(만족도 설문 + 자동 해결 여부 추적)를 총괄.
/// NPCTipDialoguePresenter가 팁을 보여줄 때마다 OnTipGiven을 호출해주면 됨.
/// 새 팁이 오면 이전 팁에 대한 대기 중인 후속 처리는 취소(최신 팁 하나만 추적).
/// </summary>
public class TipFollowUpManager : MonoBehaviour
{
    public static TipFollowUpManager Instance { get; private set; }

    [Header("만족도 설문")]
    [Tooltip("팁을 준 뒤 이 시간(초)이 지나면 만족도 설문을 띄움")]
    [SerializeField] private float feedbackDelaySec = 180f;

    [Header("자동 해결 여부 추적")]
    [Tooltip("해결됐는지 확인하는 주기(초)")]
    [SerializeField] private float resolutionCheckIntervalSec = 30f;
    [Tooltip("이 시간(초)이 지나도 해결 조건을 못 채우면 '해결 안 됨'으로 기록하고 추적 종료")]
    [SerializeField] private float resolutionCheckMaxWaitSec = 600f;

    private Coroutine feedbackCoroutine;
    private Coroutine resolutionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// NPCTipDialoguePresenter가 팁 대화를 시작할 때 호출. baselineSnapshot은 팁을 준 시점의 스냅샷.
    /// </summary>
    public void OnTipGiven(DifficultyType type, string entryId, PlayLogEntry baselineSnapshot)
    {
        if (baselineSnapshot == null)
        {
            return;
        }

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        if (resolutionCoroutine != null)
        {
            StopCoroutine(resolutionCoroutine);
        }

        feedbackCoroutine = StartCoroutine(FeedbackSurveyRoutine(type, entryId));
        resolutionCoroutine = StartCoroutine(ResolutionCheckRoutine(type, entryId, baselineSnapshot));
    }

    private IEnumerator FeedbackSurveyRoutine(DifficultyType type, string entryId)
    {
        yield return new WaitForSeconds(feedbackDelaySec);

        if (TipFeedbackUI.Instance == null)
        {
            feedbackCoroutine = null;
            yield break;
        }

        int? answeredRating = null;
        bool answered = false;

        TipFeedbackUI.Instance.Show(rating =>
        {
            answeredRating = rating;
            answered = true;
        });

        yield return new WaitUntil(() => answered);

        PlayLogManager.Instance?.RecordTipFeedback(type.ToString(), entryId, answeredRating);

        feedbackCoroutine = null;
    }

    private IEnumerator ResolutionCheckRoutine(DifficultyType type, string entryId, PlayLogEntry baseline)
    {
        float elapsed = 0f;
        WaitForSeconds wait = new WaitForSeconds(resolutionCheckIntervalSec);

        while (elapsed < resolutionCheckMaxWaitSec)
        {
            yield return wait;
            elapsed += resolutionCheckIntervalSec;

            if (PlayLogManager.Instance == null)
            {
                continue;
            }

            PlayLogEntry current = PlayLogManager.Instance.BuildCurrentSnapshot("ResolutionCheck");

            if (current == null)
            {
                continue;
            }

            if (DifficultyResolutionChecker.IsResolved(type, baseline, current))
            {
                PlayLogManager.Instance.RecordTipResolution(
                    baseline.Timestamp, type.ToString(), entryId, true, elapsed / 60f);

                resolutionCoroutine = null;
                yield break;
            }
        }

        PlayLogManager.Instance?.RecordTipResolution(
            baseline.Timestamp, type.ToString(), entryId, false, null);

        resolutionCoroutine = null;
    }
}
