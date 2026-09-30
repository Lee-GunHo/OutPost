using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 종료 직전에 "이번 플레이에서 가장 힘들었던 점"을 5지선다로 물어보는 팝업.
/// Application.wantsToQuit를 가로채서 첫 종료 시도는 취소하고 설문을 띄운 뒤,
/// 답변(혹은 건너뛰기)을 PlayLogManager.SetSessionEndLabel에 넣고 나서 실제 종료를 진행.
/// 자유서술형이 아니라 DifficultyType 5종 그대로 고정한 이유: 3단계 학습 파이프라인이
/// 이 5개 값만 라벨로 인정하기 때문(ml_pipeline/data_prep.py의 merge_and_label 참고).
/// </summary>
public class SessionEndSurveyUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [Tooltip("DifficultyType 5개 순서대로: EquipmentLack, CombatStruggle, Lost, ResourceLack, Smooth")]
    [SerializeField] private Button[] difficultyButtons;
    [SerializeField] private Button skipButton;

    private bool surveyAnswered;
    private bool readyToQuit;

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (difficultyButtons != null)
        {
            for (int i = 0; i < difficultyButtons.Length && i < DifficultyTypesInOrder.Length; i++)
            {
                Button button = difficultyButtons[i];

                if (button == null)
                {
                    continue;
                }

                DifficultyType difficultyType = DifficultyTypesInOrder[i];
                button.onClick.AddListener(() => Answer(difficultyType.ToString()));
            }
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(() => Answer(string.Empty));
        }
    }

    private static readonly DifficultyType[] DifficultyTypesInOrder =
    {
        DifficultyType.EquipmentLack,
        DifficultyType.CombatStruggle,
        DifficultyType.Lost,
        DifficultyType.ResourceLack,
        DifficultyType.Smooth,
    };

    private void OnEnable()
    {
        Application.wantsToQuit += OnWantsToQuit;
    }

    private void OnDisable()
    {
        Application.wantsToQuit -= OnWantsToQuit;
    }

    private bool OnWantsToQuit()
    {
        if (readyToQuit || panel == null || surveyAnswered)
        {
            return true;
        }

        panel.SetActive(true);
        return false;
    }

    private void Answer(string difficultyLabel)
    {
        surveyAnswered = true;

        if (!string.IsNullOrEmpty(difficultyLabel))
        {
            PlayLogManager.Instance?.SetSessionEndLabel(difficultyLabel);
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }

        readyToQuit = true;
        Application.Quit();
    }
}
