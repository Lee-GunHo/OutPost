using System;

// A managed record outlives its attacker (including projectiles released before death).
[Serializable]
public sealed class MonsterCombatTelemetryModel
{
    public string actionId, battleId, monsterId, monsterType, context, role, mode;
    public string eligibleActions, selectionProbabilities;
    public int action, learnedWeightVersion, damageDealt;
    public float startedAt, distance, selfHpRatio, playerHpRatio, selectedProbability;
    public bool cooperation, naturalSpawn, train, released, playerContact, resolved;
    public bool registeredLearningTest, learningEligible;
    public string outcome;
    [NonSerialized] public int generation;
    [NonSerialized] public MonsterLearningPresenter recorder;

    public void Contact(int actualDamage)
    {
        if (resolved) return;
        playerContact = true;
        damageDealt += Math.Max(0, actualDamage);
    }

    public void Complete(string reason)
    {
        if (resolved) return;
        resolved = true;
        outcome = damageDealt > 0 ? "Hit" : playerContact ? "NoDamageContact" : reason;
        if (recorder != null) recorder.Complete(this);
    }
}
