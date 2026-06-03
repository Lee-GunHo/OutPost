using System.Collections.Generic;
using UnityEngine;

public class StatusEffectModel : MonoBehaviour
{
    private List<StatusEffectData> activeEffects = new List<StatusEffectData>();

    public IReadOnlyList<StatusEffectData> ActiveEffects => activeEffects;

    private void Update()
    {
        UpdateEffectDuration();
    }

    public void AddEffect(StatusEffectData newEffect)
    {
        if (newEffect == null)
        {
            return;
        }

        if (newEffect.EffectType == StatusEffectType.None)
        {
            return;
        }

        RemoveEffect(newEffect.EffectType);

        activeEffects.Add(newEffect);

        Debug.Log("상태 효과 추가: " + newEffect.EffectType);
    }

    public void RemoveEffect(StatusEffectType effectType)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].EffectType == effectType)
            {
                Debug.Log("상태 효과 제거: " + activeEffects[i].EffectType);
                activeEffects.RemoveAt(i);
            }
        }
    }

    public bool HasEffect(StatusEffectType effectType)
    {
        foreach (StatusEffectData effect in activeEffects)
        {
            if (effect.EffectType == effectType)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateEffectDuration()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].DecreaseDuration(Time.deltaTime);

            if (activeEffects[i].IsExpired())
            {
                Debug.Log("상태 효과 시간 종료: " + activeEffects[i].EffectType);
                activeEffects.RemoveAt(i);
            }
        }
    }
}