using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전체 소리 크기 설정. AudioListener.volume을 쓰므로 AudioMixer 출력 그룹 연결 여부와 상관없이
// 모든 AudioSource에 적용되고, 저장값은 게임 시작 시 자동으로 적용됨(씬마다 매니저가 없어도 됨).
public class SoundSettingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider masterSlider; // 0~100 (비우면 같은 GameObject의 Slider)

    private const string PrefKey = "MasterVolume_0to100";
    private const string VolumeSliderName = "Slider_Volume";

    private Slider boundSlider;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitOnLoad()
    {
        ApplyVolume(GetSavedVolume());
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 설정창 슬라이더(Slider_Volume)에 매니저가 없는 씬(GameScene 등)에서도 동작하도록 자동으로 붙임
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var managers = FindObjectsByType<SoundSettingManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var slider in sliders)
        {
            if (slider.name != VolumeSliderName || IsHandled(slider, managers))
                continue;
            slider.gameObject.AddComponent<SoundSettingManager>().Bind(slider);
        }
    }

    private static bool IsHandled(Slider slider, SoundSettingManager[] managers)
    {
        foreach (var manager in managers)
        {
            if (manager.masterSlider == slider || manager.gameObject == slider.gameObject)
                return true;
        }
        return false;
    }

    public static float GetSavedVolume() => Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey, 100f), 0f, 100f);

    private static void ApplyVolume(float value0to100)
    {
        AudioListener.volume = Mathf.Clamp01(value0to100 / 100f);
    }

    private void Awake()
    {
        Bind(masterSlider != null ? masterSlider : GetComponent<Slider>());
    }

    private void OnDestroy()
    {
        if (boundSlider != null)
            boundSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
    }

    private void Bind(Slider slider)
    {
        if (slider == null || slider == boundSlider)
            return;

        boundSlider = slider;
        masterSlider = slider;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.SetValueWithoutNotify(GetSavedVolume());
        // 인스펙터 OnValueChanged 연결이 비어 있어도 동작하도록 코드에서 연결
        slider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        slider.onValueChanged.AddListener(OnMasterSliderChanged);
    }

    // Slider OnValueChanged(float) 에 연결
    public void OnMasterSliderChanged(float value0to100)
    {
        ApplyVolume(value0to100);
        PlayerPrefs.SetFloat(PrefKey, value0to100);
        PlayerPrefs.Save();
    }
}
