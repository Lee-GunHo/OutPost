using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// URP 후처리. 씬 배치 불필요 — 게임 시작 시 전역 Volume을 자동 생성(DontDestroyOnLoad).
/// 기본 화면 보정(Bloom/Vignette/색감/톤매핑/색온도)은 Resources/GameFeel/GamePostProcess 프로필을 그대로 씀
/// → 플레이 중에 그 에셋을 인스펙터에서 바꾸면 바로 반영됨.
/// 플레이어가 맞으면 화면 가장자리가 빨갛게 번쩍이는 효과는 별도 Volume의 weight로 처리.
/// </summary>
public class GamePostProcess : MonoBehaviour
{
    private const string ProfilePath = "GameFeel/GamePostProcess";

    private static GamePostProcess instance;

    private Volume hurtVolume;
    private float hurtFlashDuration;
    private float hurtFlashTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("GamePostProcess");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GamePostProcess>();
    }

    private void Awake()
    {
        VolumeProfile profile = Resources.Load<VolumeProfile>(ProfilePath);

        if (profile != null)
        {
            Volume baseVolume = gameObject.AddComponent<Volume>();
            baseVolume.isGlobal = true;
            baseVolume.priority = 10f;
            baseVolume.sharedProfile = profile;
        }
        else
        {
            Debug.LogWarning($"[PostProcess] Resources/{ProfilePath} 프로필이 없어 화면 보정을 적용하지 않습니다.");
        }

        CreateHurtVolume();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬마다 카메라 설정이 달라서(메인 메뉴 카메라는 후처리 꺼짐) 메인 카메라의 후처리를 켜줌
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Camera cam = Camera.main;

        if (cam != null)
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    private void CreateHurtVolume()
    {
        GameObject child = new GameObject("HurtFlashVolume");
        child.transform.SetParent(transform, false);

        // 코드에서 만든 프로필이라 에셋을 건드리지 않음
        VolumeProfile hurtProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        Vignette vignette = hurtProfile.Add<Vignette>(true);
        vignette.color.Override(HitFeedback.Settings.hurtFlashColor);
        vignette.intensity.Override(HitFeedback.Settings.hurtFlashIntensity);
        vignette.smoothness.Override(0.6f);

        hurtVolume = child.AddComponent<Volume>();
        hurtVolume.isGlobal = true;
        hurtVolume.priority = 20f;
        hurtVolume.sharedProfile = hurtProfile;
        hurtVolume.weight = 0f;
    }

    /// <summary>플레이어 피격 시 화면 가장자리를 빨갛게 번쩍임</summary>
    public static void FlashHurt()
    {
        if (instance == null || instance.hurtVolume == null)
            return;

        instance.hurtFlashDuration = Mathf.Max(0.01f, HitFeedback.Settings.hurtFlashDuration);
        instance.hurtFlashTime = instance.hurtFlashDuration;
    }

    private void Update()
    {
        if (hurtVolume == null || hurtFlashTime <= 0f)
            return;

        // 히트스톱 중에도 줄어들어야 하므로 실제 시간 기준. UI로 멈췄을 때는 그대로 둠
        if (Time.timeScale > 0f)
            hurtFlashTime = Mathf.Max(0f, hurtFlashTime - Time.unscaledDeltaTime);

        float t = hurtFlashTime / hurtFlashDuration;
        hurtVolume.weight = t * t;
    }
}
