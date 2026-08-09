using System.Collections;
using UnityEngine;

public abstract class BossPatternBase : MonoBehaviour
{
    [Header("Pattern Base Data")]
    [SerializeField] protected float cooldown = 3f;
    [SerializeField] protected float patternRange = 20f;
    [SerializeField] protected int patternWeight = 1;

    private float lastUsedTime = -999f;

    public int PatternWeight => Mathf.Max(1, patternWeight);
    public bool IsCooldownReady => Time.time >= lastUsedTime + cooldown;

    public virtual bool CanUse(BossPresenter boss)
    {
        if (!IsCooldownReady)
        {
            return false;
        }

        if (boss == null || boss.Target == null)
        {
            return false;
        }

        float distance = Vector3.Distance(boss.transform.position, boss.Target.position);

        return distance <= patternRange;
    }

    public IEnumerator RunPattern(BossPresenter boss)
    {
        lastUsedTime = Time.time;
        yield return ExecutePattern(boss);
    }

    protected abstract IEnumerator ExecutePattern(BossPresenter boss);
}