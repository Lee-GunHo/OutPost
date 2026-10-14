"""Summarize real monster AI logs. Standard-library CSV output; optional matplotlib plots.

python Tools/MonsterAI/analyze.py SESSION_A SESSION_B --output REPORT_DIRECTORY
No demo observations or invented performance values are generated.
"""
import argparse
import csv
import os
import re
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
        if path.is_file():
            path.unlink()  # Don't leave a previous run's table looking like current evidence.
        return
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def ratio(numerator, denominator):
    return numerator / denominator if denominator else ""


def analyze(directories, output):
    actions, battles, participants, updates, decisions = [], [], [], [], []
    configs = {}
    for directory in directories:
        actions.extend(read(directory / "actions.csv"))
        battles.extend(read(directory / "battles.csv"))
        participants.extend(read(directory / "participants.csv"))
        configs.update({r["battleId"]: r for r in read(directory / "battle_configs.csv")})
        decisions.extend(read(directory / "squad_decisions.csv"))
        for row in read(directory / "learning_updates.csv"):
            row["session"] = directory.name
            updates.append(row)
    if not actions and not battles and not decisions:
        raise ValueError("No actions.csv or battles.csv observations found. Play and flush logs first.")
    output.mkdir(parents=True, exist_ok=True)
    grouped = defaultdict(list)
    contextual = defaultdict(list)
    damage_by_battle = defaultdict(int)
    for row in actions:
        key = (row["mode"], row["cooperation"], row["naturalSpawn"], configs.get(row["battleId"], {}).get("squadPolicyVersion", "1"))
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
                result.append(dict(mode=key[0], cooperation=key[1], naturalSpawn=key[2], squadPolicyVersion=key[3],
                    **({"monsterType": key[4], "context": key[5]} if context else {}),
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
        battle["squadPolicyVersion"] = configs.get(battle["battleId"], {}).get("squadPolicyVersion", "1")
    write(output / "battles_enriched.csv", battles)
    battle_groups = defaultdict(list)
    for battle in battles:
        # Mode switches, loading and scene teardown are censored encounters, not completed trials.
        if battle["result"] in {"PlayerDied", "Disengaged"}:
            battle_groups[(battle["mode"], battle["cooperation"], battle["squadPolicyVersion"])].append(battle)
    comparison = []
    for (mode, cooperation, policy_version), rows in sorted(battle_groups.items()):
        ids = {r["battleId"] for r in rows}
        members = [r for r in participants if r["battleId"] in ids]
        killed = [r for r in members if r["exitReason"] == "Killed"]
        comparison.append(dict(mode=mode, cooperation=cooperation, squadPolicyVersion=policy_version, completedBattles=len(rows),
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
    formations = defaultdict(list)
    for row in decisions:
        formations[(row["mode"], row["cooperation"], row["squadPolicyVersion"], row["formation"])].append(row)
    formation_summary = [dict(mode=k[0], cooperation=k[1], squadPolicyVersion=k[2], formation=k[3],
        decisionCount=len(rows), formationEntries=sum(r["formationChanged"] == "True" for r in rows))
        for k, rows in sorted(formations.items())]
    write(output / "formations_summary.csv", formation_summary)
    lines = ["# Monster AI recorded observations", "",
        f"Input sessions: {len(directories)}; resolved action records: {len(actions)}; battle records: {len(battles)}.", "",
        "These are observations, not a controlled causal comparison. Training rows are not frozen evaluation results.",
        "Missing modes require additional play. Policy versions are kept separate. No values are invented.", ""]
    lines.extend(["Source sessions:", ""] + [f"- {p.resolve()}" for p in directories] + [""])
    def table(title, rows, columns):
        lines.extend(["## " + title, ""])
        if not rows:
            lines.extend(["No recorded samples for this table yet.", ""])
            return
        lines.extend(["| " + " | ".join(columns) + " |", "| " + " | ".join("---" for _ in columns) + " |"])
        for row in rows:
            def display(value):
                return f"{value:.3f}" if isinstance(value, float) else str(value) if value != "" else "N/A"
            lines.append("| " + " | ".join(display(row[c]) for c in columns) + " |")
        lines.append("")
    table("Completed battles", comparison, ["mode", "cooperation", "squadPolicyVersion", "completedBattles", "meanDirectDamageToPlayer", "meanBattleSeconds", "meanOverlapSeconds", "meanSurroundSeconds"])
    table("Attack observations (rates are 0..1)", summaries, ["mode", "cooperation", "naturalSpawn", "squadPolicyVersion", "action", "selected", "releasedResolved", "damageHitRate"])
    table("Formation decisions", formation_summary, ["mode", "cooperation", "squadPolicyVersion", "formation", "decisionCount", "formationEntries"])
    (output / "report.md").write_text("\n".join(lines), encoding="utf-8")
    for filename in ("attacks.png", "learning_weights.png"):
        old_graph = output / filename
        if old_graph.is_file():
            old_graph.unlink()
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
            subset = [r for r in summaries if (r["mode"], r["cooperation"], r["naturalSpawn"], r["squadPolicyVersion"]) == key]
            label = f"{key[0]} coop={key[1]} natural={key[2]} squadV={key[3]}"
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


def default_research_root():
    settings = (Path(__file__).resolve().parents[2] / "ProjectSettings/ProjectSettings.asset").read_text(encoding="utf-8-sig")
    def setting(name):
        match = re.search(r"^\s*" + name + r":\s*(.+)$", settings, re.MULTILINE)
        if not match:
            raise ValueError(f"Missing Unity project setting: {name}")
        return match.group(1).strip().strip("\"'")
    return Path(os.environ.get("USERPROFILE", str(Path.home()))) / "AppData/LocalLow" / setting("companyName") / setting("productName") / "MonsterAIResearch"


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("sessions", nargs="*", type=Path)
    parser.add_argument("--latest", action="store_true", help="Read the newest session under Unity's Windows research folder")
    parser.add_argument("--research-root", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        if args.latest:
            if args.sessions:
                raise ValueError("Use --latest or explicit session paths, not both.")
            root = args.research_root or default_research_root()
            candidates = [p for p in root.iterdir() if p.is_dir()] if root.exists() else []
            if not candidates:
                raise ValueError(f"No research sessions found at {root}. Play first, then use Flush Logs and Export Current Policy.")
            args.sessions = [max(candidates, key=lambda p: p.stat().st_mtime)]
        if not args.sessions:
            raise ValueError("Provide session paths or --latest.")
        analyze(list(dict.fromkeys(args.sessions)), args.output)
    except (ValueError, OSError, KeyError) as error:
        parser.exit(1, f"Cannot analyze logs: {error}\n")
