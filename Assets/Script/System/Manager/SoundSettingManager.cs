using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider masterSlider; // 0~100

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string exposedParam = "MasterVol"; // AudioMixer Exposed Parameter 이름

    private const string PrefKey = "MasterVolume_0to100";
    private const float MinDb = -80f;

    private void Awake()
    {
        // 안전: 슬라이더 범위 강제
        if (masterSlider != null)
        {
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 100f;
        }
    }

    private void Start()
    {
        // 저장값 로드(없으면 100)
        float saved = PlayerPrefs.GetFloat(PrefKey, 100f);
        Apply(saved, save: false);
    }

    // Slider OnValueChanged(float) 에 연결
    public void OnMasterSliderChanged(float value0to100)
    {
        Apply(value0to100, save: true);
    }

    private void Apply(float value0to100, bool save)
    {
        // UI 업데이트(이벤트 재발동 방지)
        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(value0to100);

        // 0~100 -> 0.0001~1 (0에서 log 터짐 방지)
        float linear = Mathf.Clamp(value0to100 / 100f, 0.0001f, 1f);

        // linear -> dB
        float db = (value0to100 <= 0f) ? MinDb : Mathf.Log10(linear) * 20f;

        audioMixer.SetFloat(exposedParam, db);

        if (save)
        {
            PlayerPrefs.SetFloat(PrefKey, value0to100);
            PlayerPrefs.Save();
        }
    }
}