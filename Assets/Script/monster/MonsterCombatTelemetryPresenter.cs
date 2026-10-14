using System;
using System.Collections.Generic;
using UnityEngine;

// Measurement only: never chooses actions or changes rewards/formation decisions.
public sealed class MonsterCombatTelemetryPresenter : MonoBehaviour
{
    private MonsterLearningPresenter recorder;
    private MonsterEncounterTelemetryModel battle;
    private PlayerPresenter player;
    private readonly List<MonsterBehaviorPresenter> engaged = new List<MonsterBehaviorPresenter>();
    private readonly List<float> angles = new List<float>();
    private float nextSample, lastSample;

    private void Awake() { recorder = GetComponent<MonsterLearningPresenter>(); }

    public string Join(MonsterPresenter owner, PlayerPresenter target)
    {
        if (target == null) return "";
        if (battle != null && (battle.Mode != recorder.Settings.learningMode.ToString() || battle.Cooperation != recorder.Settings.cooperationEnabled))
            EndBattle("ModeChanged");
        if (battle == null)
        {
            player = target;
            battle = new MonsterEncounterTelemetryModel
            {
                StartedAt = Time.time, LastEngagedAt = Time.time, PlayerStartHp = player.CurrentHp,
                Mode = recorder.Settings.learningMode.ToString(), Cooperation = recorder.Settings.cooperationEnabled
            };
            lastSample = Time.time;
            recorder.Write("battle_configs.csv", "battleId,storedWorldSeed,playerLevel,playerMaxHp,playerAttack,playerDefense,playerX,playerY,playerZ,policyVersion,wavesUseLearnedPolicy,squadPolicyVersion,waveSquadWithdrawal",
                battle.Id, GlobalSeedManager.StoredSeed, player.Level, player.MaxHp, player.AttackPower, player.DefensePower,
                player.transform.position.x, player.transform.position.y, player.transform.position.z,
                recorder.Memory.version, recorder.Settings.wavesUseLearnedPolicy, 2, recorder.Settings.waveSquadWithdrawal);
        }
        battle.LastEngagedAt = Time.time;
        if (!battle.Members.TryGetValue(owner.CombatIdentity, out var member) || member.Exited)
        {
            member = new MonsterParticipantTelemetryModel
            {
                Id = owner.CombatIdentity, Type = owner.LearningType, JoinedAt = Time.time
            };
            battle.Members[member.Id] = member;
            var stats = owner.GetComponent<MonsterModel>();
            recorder.Write("participant_configs.csv", "battleId,monsterId,monsterType,naturalSpawn,joinedAt,maxHp,attackPower,moveSpeed,canProjectile,canCircle,canCharge,registeredLearningTest,learningEligible",
                battle.Id, owner.CombatIdentity, owner.LearningType, owner.IsNaturalSpawn, Time.time,
                stats.MaxHp, owner.AttackPower, owner.MoveSpeed, owner.Combat.SupportsAttack(MonsterAttackKind.Projectile),
                owner.Combat.SupportsAttack(MonsterAttackKind.Circle), owner.Combat.SupportsAttack(MonsterAttackKind.Charge),
                owner.IsRegisteredLearningTest, owner.IsLearningEligible);
        }
        member.LastSeenAt = Time.time;
        return battle.Id;
    }

    public void Sample(IEnumerable<MonsterBehaviorPresenter> candidates)
    {
        if (Time.time < nextSample) return;
        nextSample = Time.time + 0.2f;
        engaged.Clear();
        foreach (var member in candidates)
        {
            if (member == null || member.Owner == null || member.Owner.IsDead) continue;
            // Keep fleeing participants alive in telemetry until escape/death, outside formation measurements.
            if (member.FleeTargetForTelemetry != null)
            {
                var threat = member.FleeTargetForTelemetry.GetComponentInParent<PlayerPresenter>();
                if (threat != null && !threat.IsDead) Join(member.Owner, threat);
                continue;
            }
            if (!member.TryGetSquadTarget(out var target)) continue;
            var targetPlayer = target.GetComponentInParent<PlayerPresenter>();
            if (targetPlayer == null || targetPlayer.IsDead) continue;
            Join(member.Owner, targetPlayer);
            engaged.Add(member);
        }
        if (battle == null) return;
        float dt = Mathf.Min(0.5f, Mathf.Max(0f, Time.time - lastSample));
        lastSample = Time.time;
        battle.PlayerEndHp = player != null ? player.CurrentHp : 0;
        foreach (var member in battle.Members.Values)
            if (!member.Exited && Time.time - member.LastSeenAt > 1f) Exit(member.Id, "Disengaged");

        bool overlap = false;
        angles.Clear();
        for (int i = 0; i < engaged.Count; i++)
        {
            var a = engaged[i];
            var snapshot = a.GetSquadSnapshot(player.transform);
            if (snapshot.Distance <= 6f) angles.Add(snapshot.Angle);
            var member = battle.Members[a.Owner.CombatIdentity];
            string role = a.GetComponent<MonsterBehaviorModel>().SquadRole.ToString();
            if (member.Role != null && member.Role != "Solo" && role != "Solo" && member.Role != role) battle.RoleSwitches++;
            member.Role = role;
            for (int j = i + 1; j < engaged.Count; j++)
            {
                Vector3 delta = a.transform.position - engaged[j].transform.position;
                delta.y = 0f;
                float contact = a.Owner.CombatBodyRadius + engaged[j].Owner.CombatBodyRadius;
                if (delta.sqrMagnitude < contact * contact * 0.81f) overlap = true;
            }
        }
        if (overlap) battle.OverlapSeconds += dt;
        bool surrounded = false;
        if (angles.Count >= 3)
        {
            angles.Sort();
            float maxGap = angles[0] + Mathf.PI * 2f - angles[angles.Count - 1];
            for (int i = 1; i < angles.Count; i++) maxGap = Mathf.Max(maxGap, angles[i] - angles[i - 1]);
            surrounded = maxGap <= Mathf.PI; // No empty half-circle; >=3 monsters within 6 units.
        }
        if (surrounded) battle.SurroundedSeconds += dt;
        if (surrounded && !battle.WasSurrounded) battle.SurroundCount++;
        battle.WasSurrounded = surrounded;
        if (player == null || player.IsDead) EndBattle("PlayerDied");
        else if (Time.time - battle.LastEngagedAt >= 5f) EndBattle("Disengaged");
    }

    public void DamageTaken(MonsterPresenter owner, int damage)
    {
        var target = owner.CurrentTarget != null ? owner.CurrentTarget.GetComponentInParent<PlayerPresenter>() : null;
        if (target != null) Join(owner, target);
        if (battle != null && battle.Members.ContainsKey(owner.CombatIdentity))
            battle.DamageToMonsters += Math.Max(0, damage);
    }

    public void Exit(string id, string reason)
    {
        if (battle == null || !battle.Members.TryGetValue(id, out var member) || member.Exited) return;
        member.Exited = true;
        float endedAt = reason == "Disengaged" ? member.LastSeenAt : Time.time;
        recorder.Write("participants.csv", "battleId,monsterId,monsterType,joinedAt,leftAt,engagedSeconds,exitReason",
            battle.Id, member.Id, member.Type, member.JoinedAt, endedAt, endedAt - member.JoinedAt, reason);
    }

    private void EndBattle(string reason)
    {
        if (battle == null) return;
        foreach (var member in battle.Members.Values) Exit(member.Id, reason);
        float endedAt = reason == "Disengaged" ? battle.LastEngagedAt : Time.time;
        recorder.Write("battles.csv", "battleId,mode,cooperation,startTime,duration,playerStartHp,playerEndHp,uniqueMonsterCount,damageToMonsters,result,overlapSeconds,surroundSeconds,surroundCount,roleSwitchCount",
            battle.Id, battle.Mode, battle.Cooperation, battle.StartedAt, endedAt - battle.StartedAt,
            battle.PlayerStartHp, player != null ? player.CurrentHp : battle.PlayerEndHp,
            battle.Members.Count, battle.DamageToMonsters, reason, battle.OverlapSeconds,
            battle.SurroundedSeconds, battle.SurroundCount, battle.RoleSwitches);
        battle = null;
    }

    public void EndSession() { EndBattle("SessionEnded"); }
    public void ResetForLoad() { EndBattle("LoadOrReset"); }
}
