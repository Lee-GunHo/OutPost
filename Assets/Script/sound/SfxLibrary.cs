using System;
using UnityEngine;

/// <summary>
/// 효과음 목록. Resources/Sound/SfxLibrary 에셋을 SfxPlayer가 읽음.
/// 소리를 바꾸려면 코드 수정 없이 인스펙터에서 clips만 교체하면 됨(여러 개면 랜덤 재생).
/// </summary>
[CreateAssetMenu(fileName = "SfxLibrary", menuName = "Sound/Sfx Library")]
public class SfxLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SfxId id;
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 0.8f;

        [Tooltip("재생할 때마다 피치를 이 범위에서 랜덤으로 골라 같은 소리 반복이 덜 거슬리게 함")]
        public Vector2 pitchRange = new Vector2(0.92f, 1.08f);

        [Tooltip("같은 효과음을 이 시간(초) 안에 다시 재생하지 않음. 몬스터 여러 마리가 동시에 맞을 때 소리가 겹쳐 터지는 것 방지")]
        [Min(0f)] public float minInterval = 0.03f;
    }

    [Header("전체")]
    [Tooltip("모든 효과음에 곱해지는 볼륨. 효과음 전체가 크거나 작으면 이 값만 조절")]
    [Range(0f, 1f)] public float masterVolume = 0.45f;

    [Header("거리 감쇠 (위치가 있는 소리만)")]
    [Tooltip("이 거리 안에서는 최대 볼륨")]
    [Min(0f)] public float fullVolumeDistance = 8f;

    [Tooltip("이 거리 밖에서는 들리지 않음")]
    [Min(0f)] public float maxHearingDistance = 26f;

    [Tooltip("화면 좌우 위치에 따른 스테레오 정도 (0 = 끔)")]
    [Range(0f, 1f)] public float stereoPan = 0.5f;

    public Entry[] entries = Array.Empty<Entry>();

    public Entry Find(SfxId id)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.id == id)
                return entry;
        }

        return null;
    }
}
