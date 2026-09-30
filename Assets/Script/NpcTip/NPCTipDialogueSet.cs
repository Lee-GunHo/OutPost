using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 난관 유형별 대사(NPCTipEntry) 목록. 기획자가 코드 수정 없이 인스펙터에서 대사를 추가/수정.
/// </summary>
[CreateAssetMenu(fileName = "NPCTipDialogueSet", menuName = "NPC/Tip Dialogue Set")]
public class NPCTipDialogueSet : ScriptableObject
{
    [Serializable]
    public class DifficultyEntries
    {
        public DifficultyType difficultyType;
        public NPCTipEntry[] entries;
    }

    [SerializeField] private DifficultyEntries[] difficultyEntries;

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 툴(NpcTipSampleDataCreator 등)이 샘플/초기 데이터를 채워 넣을 때만 사용.
    /// </summary>
    public void EditorSetEntries(DifficultyEntries[] entries)
    {
        difficultyEntries = entries;
    }
#endif

    public NPCTipEntry[] GetEntries(DifficultyType type)
    {
        if (difficultyEntries == null)
        {
            return null;
        }

        foreach (DifficultyEntries group in difficultyEntries)
        {
            if (group != null && group.difficultyType == type)
            {
                return group.entries;
            }
        }

        return null;
    }

    /// <summary>
    /// 해당 난관 유형의 대사 중 가중치 랜덤으로 하나 선택. lastEntryId와 같은 건 가능하면 피함.
    /// </summary>
    public NPCTipEntry PickEntry(DifficultyType type, string lastEntryId)
    {
        NPCTipEntry[] pool = GetEntries(type);

        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        IReadOnlyList<NPCTipEntry> candidates = pool;

        if (pool.Length > 1 && !string.IsNullOrEmpty(lastEntryId))
        {
            NPCTipEntry[] filtered = pool.Where(e => e != null && e.entryId != lastEntryId).ToArray();

            if (filtered.Length > 0)
            {
                candidates = filtered;
            }
        }

        return WeightedRandomPicker.Pick(candidates, e => e.weight);
    }
}
