using System.Collections.Generic;
using UnityEngine;

public class BossPatternController : MonoBehaviour
{
    private BossPatternBase[] patterns;

    private void Awake()
    {
        patterns = GetComponents<BossPatternBase>();
    }

    public bool HasAvailablePattern(BossPresenter boss)
    {
        return GetAvailablePattern(boss) != null;
    }

    public BossPatternBase GetAvailablePattern(BossPresenter boss)
    {
        List<BossPatternBase> availablePatterns = new List<BossPatternBase>();

        foreach (BossPatternBase pattern in patterns)
        {
            if (pattern == null)
            {
                continue;
            }

            if (pattern.CanUse(boss))
            {
                availablePatterns.Add(pattern);
            }
        }

        if (availablePatterns.Count == 0)
        {
            return null;
        }

        return GetRandomPatternByWeight(availablePatterns);
    }

    private BossPatternBase GetRandomPatternByWeight(List<BossPatternBase> availablePatterns)
    {
        int totalWeight = 0;

        foreach (BossPatternBase pattern in availablePatterns)
        {
            totalWeight += pattern.PatternWeight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (BossPatternBase pattern in availablePatterns)
        {
            currentWeight += pattern.PatternWeight;

            if (randomValue < currentWeight)
            {
                return pattern;
            }
        }

        return availablePatterns[0];
    }
}