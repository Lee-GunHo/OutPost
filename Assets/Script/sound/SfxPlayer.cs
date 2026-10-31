using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음 재생. 씬 배치 불필요 — 게임 시작 시 자동 생성(DontDestroyOnLoad).
/// AudioSource를 미리 여러 개 만들어 돌려쓰고, 재생할 때마다 피치를 조금씩 흔들어 코어키퍼처럼 손맛 있게 들리게 함.
/// 전체 소리 크기는 SoundSettingManager(AudioListener.volume)가 그대로 적용됨.
/// </summary>
public class SfxPlayer : MonoBehaviour
{
    private const string LibraryPath = "Sound/SfxLibrary";
    private const int PoolSize = 20;

    private static SfxPlayer instance;

    private SfxLibrary library;
    private readonly List<AudioSource> sources = new List<AudioSource>();
    private readonly Dictionary<SfxId, float> lastPlayTimes = new Dictionary<SfxId, float>();
    private readonly Dictionary<SfxId, int> lastClipIndexes = new Dictionary<SfxId, int>();
    private int nextSourceIndex;
    private Transform listener;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("SfxPlayer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SfxPlayer>();
    }

    private void Awake()
    {
        library = Resources.Load<SfxLibrary>(LibraryPath);

        if (library == null)
            Debug.LogWarning($"[Sfx] Resources/{LibraryPath} 에셋이 없어 효과음이 재생되지 않습니다.");

        for (int i = 0; i < PoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            sources.Add(source);
        }
    }

    /// <summary>위치와 상관없는 소리(UI, 제작, 레벨업 등)</summary>
    public static void Play(SfxId id, float volumeScale = 1f)
    {
        if (instance != null)
            instance.PlayInternal(id, volumeScale, false, Vector3.zero);
    }

    /// <summary>월드 위치에서 난 소리. 멀어지면 작아지고 화면 좌우에 따라 스테레오로 들림</summary>
    public static void PlayAt(SfxId id, Vector3 position, float volumeScale = 1f)
    {
        if (instance != null)
            instance.PlayInternal(id, volumeScale, true, position);
    }

    private void PlayInternal(SfxId id, float volumeScale, bool positional, Vector3 position)
    {
        SfxLibrary.Entry entry = library != null ? library.Find(id) : null;

        if (entry == null || entry.clips == null || entry.clips.Length == 0)
            return;

        float now = Time.unscaledTime;

        if (lastPlayTimes.TryGetValue(id, out float lastTime) && now - lastTime < entry.minInterval)
            return;

        float volume = library.masterVolume * entry.volume * volumeScale;
        float pan = 0f;

        if (positional && !TryGetPositionalMix(position, ref volume, out pan))
            return;

        AudioClip clip = PickClip(id, entry.clips);

        if (clip == null)
            return;

        lastPlayTimes[id] = now;

        AudioSource source = NextSource();
        source.clip = clip;
        source.volume = volume;
        source.pitch = Random.Range(entry.pitchRange.x, entry.pitchRange.y);
        source.panStereo = pan;
        source.Play();
    }

    private bool TryGetPositionalMix(Vector3 position, ref float volume, out float pan)
    {
        pan = 0f;
        Transform center = ResolveListener();

        if (center == null)
            return true;

        // 탑다운 카메라라 높이 차이는 무시하고 바닥 기준 거리로 계산
        Vector3 delta = position - center.position;
        delta.y = 0f;
        float distance = delta.magnitude;

        float near = library.fullVolumeDistance;
        float far = Mathf.Max(near + 0.01f, library.maxHearingDistance);

        if (distance >= far)
            return false;

        if (distance > near)
            volume *= 1f - Mathf.InverseLerp(near, far, distance);

        Camera cam = Camera.main;

        if (cam != null && library.stereoPan > 0f)
        {
            float side = Vector3.Dot(delta, cam.transform.right);
            pan = Mathf.Clamp(side / far, -1f, 1f) * library.stereoPan;
        }

        return volume > 0.001f;
    }

    // 플레이어가 있으면 플레이어 기준, 없으면 카메라 기준
    private Transform ResolveListener()
    {
        if (listener != null && listener.gameObject.activeInHierarchy)
            return listener;

        PlayerPresenter player = FindFirstObjectByType<PlayerPresenter>();

        if (player != null)
        {
            listener = player.transform;
            return listener;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    // 같은 클립이 연속으로 나오지 않게 고름
    private AudioClip PickClip(SfxId id, AudioClip[] clips)
    {
        if (clips.Length == 1)
            return clips[0];

        lastClipIndexes.TryGetValue(id, out int last);
        int index = Random.Range(0, clips.Length - 1);

        if (index >= last)
            index++;

        lastClipIndexes[id] = index;
        return clips[index];
    }

    private AudioSource NextSource()
    {
        for (int i = 0; i < sources.Count; i++)
        {
            AudioSource candidate = sources[(nextSourceIndex + i) % sources.Count];

            if (!candidate.isPlaying)
            {
                nextSourceIndex = (sources.IndexOf(candidate) + 1) % sources.Count;
                return candidate;
            }
        }

        // 전부 재생 중이면 가장 오래된 것부터 끊고 재사용
        AudioSource oldest = sources[nextSourceIndex];
        nextSourceIndex = (nextSourceIndex + 1) % sources.Count;
        return oldest;
    }
}
