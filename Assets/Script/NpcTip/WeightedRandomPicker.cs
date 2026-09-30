using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 누적 가중치 방식의 랜덤 선택. BossPatternController.GetRandomPatternByWeight와 같은 알고리즘을
/// 재사용 가능한 형태로 일반화한 것.
/// </summary>
public static class WeightedRandomPicker
{
    public static T Pick<T>(IReadOnlyList<T> items, Func<T, float> weightSelector)
    {
        if (items == null || items.Count == 0)
        {
            return default;
        }

        float totalWeight = 0f;

        foreach (T item in items)
        {
            totalWeight += Mathf.Max(0f, weightSelector(item));
        }

        if (totalWeight <= 0f)
        {
            return items[0];
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (T item in items)
        {
            cumulativeWeight += Mathf.Max(0f, weightSelector(item));

            if (randomValue < cumulativeWeight)
            {
                return item;
            }
        }

        return items[items.Count - 1];
    }
}
