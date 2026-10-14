using System;
using System.Collections.Generic;
using System.IO;

// Test double for the scene coordinator; production models are compiled unmodified.
public sealed class MonsterLearningPresenter
{
    public int Count;
    public void Complete(MonsterCombatTelemetryModel record) { Count++; }
}

public static class ModelChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    public static string Run()
    {
        checks = 0;
        var memory = new MonsterLearningModel();
        string context = MonsterLearningModel.Context(2f, 1f, false, false);
        Check(memory.Weight("normal", context, 0) == 1f, "Cold start must use baseline");
        for (int i = 0; i < 4; i++) memory.Observe("normal", context, 0, true);
        Check(memory.Weight("normal", context, 0) > 1f && memory.Weight("normal", context, 0) < 1.1f, "Sparse data must adapt gradually");
        Check(memory.Weight("other", context, 0) == 1f, "Types must not contaminate each other");
        Check(memory.Weight("normal", context, 1) == 1f, "Unchosen actions must not be updated");
        Check(memory.Weight("normal", "other context", 0) == 1f, "Contexts must be separate");
        for (int i = 0; i < 1000; i++) memory.Observe("normal", context, 0, true);
        Check(memory.Weight("normal", context, 0) <= 1.8f, "Weight ceiling");
        for (int i = 0; i < 1000; i++) memory.Observe("normal", context, 0, false);
        Check(memory.Weight("normal", context, 0) < 1f && memory.Weight("normal", context, 0) >= 0.65f, "Adapt to new habits without eliminating actions");
        Check(memory.version == 2004 && memory.entries[0].hits == 1004, "Experience/version accounting");
        Check(memory.IsValid(), "Valid memory rejected");
        memory.entries[0].meanReward = float.NaN;
        Check(!memory.IsValid(), "Corrupt NaN save accepted");
        memory.entries[0].meanReward = 0;
        memory.entries.Add(memory.entries[0]);
        Check(!memory.IsValid(), "Duplicate save entry accepted");
        Check(MonsterLearningModel.Context(2, 1, true, false) != context, "Player attack observation ignored");
        Check(MonsterLearningModel.Context(2, 1, false, true) != context, "Player dash observation ignored");
        Check(MonsterLearningModel.Context(2, .2f, false, false) != context, "Low health observation ignored");

        var recorder = new MonsterLearningPresenter();
        var hit = new MonsterCombatTelemetryModel { recorder = recorder, released = true };
        hit.Contact(3); hit.Complete("Miss"); hit.Complete("Interrupted"); hit.Contact(10);
        Check(hit.damageDealt == 3 && hit.outcome == "Hit" && recorder.Count == 1, "Resolve once, preserve damage");
        var immune = new MonsterCombatTelemetryModel { recorder = recorder, released = true };
        immune.Contact(0); immune.Complete("Miss");
        Check(immune.outcome == "NoDamageContact", "Contact is not damage");
        var cancelled = new MonsterCombatTelemetryModel { recorder = recorder };
        cancelled.Complete("Cancelled");
        Check(!cancelled.released && cancelled.outcome == "Cancelled", "Cancellation is not a miss");

        var squad = new MonsterSquadModel();
        var members = new List<MonsterSquadMember>
        {
            new MonsterSquadMember { Id=1, HpRatio=1, MaxHp=100, MoveSpeed=3, Distance=2 },
            new MonsterSquadMember { Id=2, HpRatio=1, MaxHp=30, MoveSpeed=2, Distance=2, CanProjectile=true },
            new MonsterSquadMember { Id=3, HpRatio=1, MaxHp=30, MoveSpeed=4, Distance=2, CanCharge=true }
        };
        Check(squad.Synchronize(members, 0), "Initial formation missing");
        Check(squad.GetAssignment(1).Role == MonsterSquadRole.Pressure, "Durable member should lead");
        Check(squad.GetAssignment(2).Role == MonsterSquadRole.Support, "Ranged member should support");
        Check(squad.GetAssignment(3).Role == MonsterSquadRole.Flanker, "Mobile charge member should flank");
        var wounded = members[0]; wounded.NeedsCover = true; wounded.HpRatio=.1f; members[0]=wounded;
        Check(squad.Synchronize(members, .1f), "Wounded front should immediately trigger reassignment");
        Check(squad.GetAssignment(1).Role == MonsterSquadRole.Support, "Wounded front must get cover");
        float before = squad.GetAssignment(1).Angle;
        squad.AdvanceFlank(0); squad.AdvanceFlank(2.3f);
        Check(squad.GetAssignment(1).Angle != before, "Orbit must advance");
        Check(squad.GetAssignment(1).Angle != squad.GetAssignment(2).Angle, "Slots overlap");
        Check(squad.TryStartAttack(3, 0) && !squad.TryStartAttack(3.1f, 0), "Shared attack interval");
        Check(!squad.CanStartAttack(4, 2), "Concurrent attack cap");
        members.RemoveRange(1,2);
        var survivor = members[0]; survivor.HpRatio=1f; survivor.NeedsCover=false; members[0]=survivor;
        squad.Synchronize(members,4);
        Check(squad.Formation == MonsterSquadFormation.Regroup, "Front loss should trigger regroup");
        squad.Synchronize(members,7.1f);
        Check(squad.GetAssignment(1).Role == MonsterSquadRole.Solo && squad.Count == 1, "Roster must shrink to solo");
        members.Clear(); squad.Synchronize(members,8);
        Check(squad.Count == 0, "Empty formation cleanup");

        CheckSquadTactics();

        string directory = Path.Combine(Path.GetTempPath(), "outpost-ai-save-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,"monster_learning.json"),"{}");
        File.WriteAllText(Path.Combine(directory,"monster_learning.json.tmp"),"{}");
        File.WriteAllText(Path.Combine(directory,"player_save.json"),"{}");
        File.WriteAllText(Path.Combine(directory,"settings.json"),"{}");
        Directory.CreateDirectory(Path.Combine(directory,"MonsterAIResearch"));
        File.WriteAllText(Path.Combine(directory,"MonsterAIResearch","snapshot.json"),"{}");
        new GameSaveModel(directory,"SaveFile.json").DeleteAllProgress();
        Check(!File.Exists(Path.Combine(directory,"monster_learning.json")), "New game must delete learned memory");
        Check(!File.Exists(Path.Combine(directory,"monster_learning.json.tmp")), "New game must delete temporary learning save");
        Check(File.Exists(Path.Combine(directory,"settings.json")), "Settings must be preserved");
        Check(File.Exists(Path.Combine(directory,"MonsterAIResearch","snapshot.json")), "Research exports must be preserved");
        return checks + " checks passed (models/save scope only; no Unity play mode).";
    }

    private static List<MonsterSquadMember> Pair(bool wave, float hp) => new List<MonsterSquadMember>
    {
        new MonsterSquadMember { Id=10, HpRatio=hp, MaxHp=50, MoveSpeed=3, Distance=2, CanMelee=true, IsWave=wave, NeedsCover=hp<=.35f },
        new MonsterSquadMember { Id=20, HpRatio=hp, MaxHp=50, MoveSpeed=3, Distance=2, CanMelee=true, IsWave=wave, NeedsCover=hp<=.35f }
    };

    private static void CheckSquadTactics()
    {
        var natural = new MonsterSquadModel();
        var weak = Pair(false,.2f);
        natural.Synchronize(weak,0);
        Check(natural.Formation == MonsterSquadFormation.Regroup, "Weak group must regroup first");
        Check(!natural.TryStartAttack(1,0), "Regroup must not start new attacks");
        float angle = natural.GetAssignment(10).Angle;
        natural.AdvanceFlank(0); natural.AdvanceFlank(2.5f);
        Check(natural.GetAssignment(10).Angle == angle, "Regroup must not orbit");
        natural.Synchronize(weak,3.1f);
        Check(natural.Formation == MonsterSquadFormation.Withdrawal && natural.GetAssignment(10).Role == MonsterSquadRole.Withdraw,
            "Natural weak group should withdraw after regroup");
        weak.RemoveAt(0); natural.Synchronize(weak,3.2f);
        Check(natural.GetAssignment(20).Role == MonsterSquadRole.Withdraw, "Withdrawal must survive staggered member exits");
        natural.Synchronize(weak,7.2f);
        Check(natural.Formation == MonsterSquadFormation.Solo, "Expired command must not permanently disable failed escapers");

        var wave = new MonsterSquadModel();
        var wavePair = Pair(true,.2f);
        wave.Synchronize(wavePair,0); wave.Synchronize(wavePair,3.1f);
        Check(wave.Formation == MonsterSquadFormation.Pair, "Default wave policy must resume combat");
        wave.Synchronize(wavePair,4f);
        Check(wave.Formation == MonsterSquadFormation.Pair, "Regroup cooldown must prevent immediate retreat loop");
        var permittedWave = new MonsterSquadModel();
        var allow = new MonsterSquadSituation { PlayerHpRatio=1f, AllowWaveWithdrawal=true };
        permittedWave.Synchronize(wavePair,0,allow); permittedWave.Synchronize(wavePair,3.1f,allow);
        Check(permittedWave.Formation == MonsterSquadFormation.Withdrawal, "Optional wave withdrawal policy ignored");

        var mixed = Pair(false,.2f); var m=mixed[1]; m.IsWave=true; mixed[1]=m;
        var mixedSquad = new MonsterSquadModel(); mixedSquad.Synchronize(mixed,0); mixedSquad.Synchronize(mixed,3.1f);
        Check(mixedSquad.GetAssignment(10).Role == MonsterSquadRole.Withdraw && mixedSquad.GetAssignment(20).Role == MonsterSquadRole.Regroup,
            "Mixed group must obey per-member wave restriction");

        var weakPlayer = new MonsterSquadModel();
        weakPlayer.Synchronize(Pair(false,.2f),0,new MonsterSquadSituation { PlayerHpRatio=.15f });
        Check(weakPlayer.Formation == MonsterSquadFormation.Pair, "Weak player should allow continued pressure");

        var ready = Pair(false,1f); var readySquad = new MonsterSquadModel(); readySquad.Synchronize(ready,0);
        m=ready[1]; m.MeleeReady=true; ready[1]=m;
        readySquad.Synchronize(ready,.8f);
        Check(readySquad.GetAssignment(20).Role == MonsterSquadRole.Pressure, "Ready peer should replace front on cooldown");
        readySquad.Synchronize(ready,.9f);
        Check(readySquad.GetAssignment(20).Role == MonsterSquadRole.Pressure, "Role hysteresis/reassessment must be stable");

        var tradeSquad = new MonsterSquadModel(); tradeSquad.Synchronize(Pair(false,1f),0);
        tradeSquad.RecordDamage(0,40); tradeSquad.Synchronize(Pair(false,1f),6.1f);
        Check(tradeSquad.Formation == MonsterSquadFormation.Regroup && tradeSquad.ChangeReason == "UnfavorableDamageTrade", "Poor trade should trigger regroup");

        var trio=Pair(false,1f);
        m=trio[1]; m.CanProjectile=true; m.ProjectileReady=true; trio[1]=m;
        trio.Add(new MonsterSquadMember { Id=30, HpRatio=1, MaxHp=20, MoveSpeed=4, Distance=3, CanCharge=true, ChargeReady=true });
        var focusSquad=new MonsterSquadModel();
        focusSquad.Synchronize(trio,0,new MonsterSquadSituation { PlayerHpRatio=1, PlayerFocusedMember=10 });
        Check(focusSquad.GetAssignment(30).Role == MonsterSquadRole.Flanker && focusSquad.GetAssignment(30).ExploitOpening,
            "Ready flanker should exploit attention on front");
        m=trio[2]; m.ChargeReady=false; trio[2]=m;
        focusSquad.Synchronize(trio,.8f,new MonsterSquadSituation { PlayerHpRatio=1, PlayerFocusedMember=10 });
        Check(!focusSquad.GetAssignment(30).ExploitOpening, "Flank opportunity must respect cooldown");
    }
}
