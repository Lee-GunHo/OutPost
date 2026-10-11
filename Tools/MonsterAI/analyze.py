"""Summarize real monster AI logs. Standard-library CSV output; optional matplotlib plots.

python Tools/MonsterAI/analyze.py SESSION_A SESSION_B --output REPORT_DIRECTORY
No demo observations or invented performance values are generated.
"""
import argparse
import csv
from collections import defaultdict
from pathlib import Path
from statistics import mean

ACTIONS = ("Melee", "Projectile", "Circle", "Charge")
RESOLVED = {"Hit", "Miss", "NoDamageContact", "Blocked", "OtherTarget"}


def read(path):
    if not path.exists():
        return []
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def write(path, rows):
    if not rows:
        return
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def ratio(numerator, denominator):
    return numerator / denominator if denominator else ""


def analyze(directories, output):
    actions, battles, participants, updates = [], [], [], []
    for directory in directories:
        actions.extend(read(directory / "actions.csv"))
        battles.extend(read(directory / "battles.csv"))
        participants.extend(read(directory / "participants.csv"))
        for row in read(directory / "learning_updates.csv"):
            row["session"] = directory.name
            updates.append(row)
    if not actions and not battles:
        raise ValueError("No actions.csv or battles.csv observations found. Play and flush logs first.")
    output.mkdir(parents=True, exist_ok=True)
    grouped = defaultdict(list)
    contextual = defaultdict(list)
    damage_by_battle = defaultdict(int)
    for row in actions:
        key = (row["mode"], row["cooperation"], row["naturalSpawn"])
        grouped[key].append(row)
        contextual[key + (row["monsterType"], row["context"])].append(row)
        damage_by_battle[row["battleId"]] += int(row["damageDealt"])

    def summarize(groups, context=False):
        result = []
        for key, rows in sorted(groups.items()):
            for action in ACTIONS:
                chosen = [r for r in rows if r["action"] == action]
                resolved = [r for r in chosen if r["released"] == "True" and r["outcome"] in RESOLVED]
                eligible = [r for r in rows if action in r["eligibleActions"].split(";")]
                result.append(dict(mode=key[0], cooperation=key[1], naturalSpawn=key[2],
                    **({"monsterType": key[3], "context": key[4]} if context else {}),
                    action=action, selected=len(chosen), eligibleDecisions=len(eligible),
                    selectionRate=ratio(len(chosen), len(rows)),
                    selectionWhenEligible=ratio(len(chosen), len(eligible)),
                    releasedResolved=len(resolved), damageHits=sum(r["outcome"] == "Hit" for r in resolved),
                    damageHitRate=ratio(sum(r["outcome"] == "Hit" for r in resolved), len(resolved)),
                    damage=sum(int(r["damageDealt"]) for r in chosen),
                    cancelled=sum(r["outcome"] == "Cancelled" for r in chosen),
                    blocked=sum(r["outcome"] == "Blocked" for r in chosen)))
        return result

    summaries = summarize(grouped)
    write(output / "attacks_summary.csv", summaries)
    write(output / "attacks_by_context.csv", summarize(contextual, True))
    write(output / "learning_weights.csv", updates)
    for battle in battles:
        battle["directDamageToPlayer"] = damage_by_battle[battle["battleId"]]
    write(output / "battles_enriched.csv", battles)
    battle_groups = defaultdict(list)
    for battle in battles:
        # Mode switches, loading and scene teardown are censored encounters, not completed trials.
        if battle["result"] in {"PlayerDied", "Disengaged"}:
            battle_groups[(battle["mode"], battle["cooperation"])].append(battle)
    comparison = []
    for (mode, cooperation), rows in sorted(battle_groups.items()):
        ids = {r["battleId"] for r in rows}
        members = [r for r in participants if r["battleId"] in ids]
        killed = [r for r in members if r["exitReason"] == "Killed"]
        comparison.append(dict(mode=mode, cooperation=cooperation, completedBattles=len(rows),
            meanBattleSeconds=mean(float(r["duration"]) for r in rows),
            meanDirectDamageToPlayer=mean(r["directDamageToPlayer"] for r in rows),
            meanEngagedSeconds=mean(float(r["engagedSeconds"]) for r in members) if members else "",
            meanTimeToDeathAmongKilled=mean(float(r["engagedSeconds"]) for r in killed) if killed else "",
            killedCount=len(killed), escapedCount=sum(r["exitReason"] == "Escaped" for r in members),
            meanOverlapSeconds=mean(float(r["overlapSeconds"]) for r in rows),
            meanSurroundSeconds=mean(float(r["surroundSeconds"]) for r in rows),
            meanSurroundCount=mean(int(r["surroundCount"]) for r in rows),
            meanRoleSwitchCount=mean(int(r["roleSwitchCount"]) for r in rows)))
    write(output / "comparison.csv", comparison)
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
    except ImportError:
        print("CSV reports complete. Install matplotlib separately if PNG graphs are needed.")
        return
    if summaries:
        fig, axes = plt.subplots(1, 2, figsize=(12, 4))
        for key in sorted(grouped):
            subset = [r for r in summaries if (r["mode"], r["cooperation"], r["naturalSpawn"]) == key]
            label = f"{key[0]} coop={key[1]} natural={key[2]}"
            axes[0].plot(ACTIONS, [float(r["selectionRate"]) for r in subset], marker="o", label=label)
            axes[1].plot(ACTIONS, [float(r["damageHitRate"]) if r["damageHitRate"] != "" else float("nan") for r in subset], marker="o", label=label)
        axes[0].set_title("Attack selection share")
        axes[1].set_title("Damage hit rate (resolved releases)")
        for ax in axes:
            ax.set_ylim(0, 1)
            ax.legend(fontsize=7)
        fig.tight_layout()
        fig.savefig(output / "attacks.png", dpi=160)
        plt.close(fig)
    if updates:
        curves = defaultdict(list)
        for row in updates:
            curves[(row["session"], row["monsterType"], row["context"], row["action"])].append(row)
        fig, ax = plt.subplots(figsize=(10, 5))
        for key, rows in sorted(curves.items(), key=lambda item: len(item[1]), reverse=True)[:8]:
            rows.sort(key=lambda r: int(r["version"]))
            ax.plot([int(r["version"]) for r in rows], [float(r["newWeight"]) for r in rows],
                    label=f"{key[0][-6:]} / {key[1]} / {key[2]} / {key[3]}")
        ax.axhline(1, color="gray", linestyle="--")
        ax.set(xlabel="Learned policy version", ylabel="Learned multiplier", title="Most-observed contexts (up to 8)")
        ax.legend(fontsize=7)
        fig.tight_layout()
        fig.savefig(output / "learning_weights.png", dpi=160)
        plt.close(fig)
    print(f"Reports saved to {output}. Compare matched trials; these summaries do not establish causality.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("sessions", nargs="+", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        analyze(list(dict.fromkeys(args.sessions)), args.output)
    except (ValueError, OSError, KeyError) as error:
        parser.exit(1, f"Cannot analyze logs: {error}\n")
