"""Real-log pilot comparison; explicit battle selection by starting-HP condition."""
import csv
from pathlib import Path
from statistics import mean
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

SOURCE = Path(r"C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_231635_5ff3174160e344df90963409c2c41fb9")
OUT = Path(__file__).resolve().parent
IDS = ("25bc08255e2946c3a0eef84ca1ad3fa6", "1b6b719113af4576af4c7b27f9358a35")
def read(name):
    with (SOURCE / name).open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))
def write(name, rows):
    with (OUT / name).open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)

actions, battles, participants, configs = [read(n) for n in ("actions.csv", "battles.csv", "participants.csv", "battle_configs.csv")]
assert len({r["actionId"] for r in actions}) == len(actions)
summary = []
for bid in IDS:
    b = next(r for r in battles if r["battleId"] == bid)
    c = next(r for r in configs if r["battleId"] == bid)
    a = [r for r in actions if r["battleId"] == bid]
    m = [r for r in participants if r["battleId"] == bid]
    assert b["mode"] == "Baseline" and b["playerStartHp"] == "100" and b["uniqueMonsterCount"] == "4"
    assert all(r["mode"] == "Baseline" and r["policyVersion"] == "0" for r in a)
    summary.append(dict(battleId=bid, cooperation=b["cooperation"], playerStartHp=b["playerStartHp"],
        playerLevel=c["playerLevel"], actions=len(a), directDamageToPlayer=sum(int(r["damageDealt"]) for r in a),
        battleSeconds=float(b["duration"]), meanEngagedSeconds=mean(float(r["engagedSeconds"]) for r in m),
        killedCount=sum(r["exitReason"] == "Killed" for r in m), surroundSeconds=float(b["surroundSeconds"]),
        surroundCount=int(b["surroundCount"]), overlapSeconds=float(b["overlapSeconds"]), roleSwitchCount=int(b["roleSwitchCount"])))
assert [r["cooperation"] for r in summary] == ["False", "True"]
write("selected_comparison.csv", summary)
write("battle_selection.csv", [dict(battleId=r["battleId"], included=r["battleId"] in IDS,
    playerStartHp=r["playerStartHp"], cooperation=r["cooperation"],
    reason="Baseline, four monsters, starting HP 100" if r["battleId"] in IDS else "Starting HP 78; kept as separate pilot") for r in battles])
write("selected_actions.csv", [r for r in actions if r["battleId"] in IDS])
write("selected_battle_configs.csv", [r for r in configs if r["battleId"] in IDS])
write("selected_participants.csv", [r for r in participants if r["battleId"] in IDS])
write("selected_participant_configs.csv", [r for r in read("participant_configs.csv") if r["battleId"] in IDS])

plt.rcParams.update({"font.family": "Malgun Gothic", "axes.unicode_minus": False})
fig, axes = plt.subplots(1, 3, figsize=(12, 4.8))
for ax, key, title, unit in zip(axes,
        ["directDamageToPlayer", "battleSeconds", "meanEngagedSeconds"],
        ["플레이어에게 준 직접 피해", "전투 지속 시간", "몬스터 평균 교전 참여 시간"], ["피해량", "초", "초"]):
    values = [r[key] for r in summary]
    bars = ax.bar(["협동 OFF", "협동 ON"], values, color=["#7c8999", "#277da1"])
    ax.set(title=title, ylabel=unit, ylim=(0, max(values)*1.3))
    for bar, value in zip(bars, values):
        ax.text(bar.get_x()+bar.get_width()/2, value+max(values)*.035, f"{value:.2f}", ha="center")
fig.suptitle("협동 OFF/ON 예비 비교 — Baseline, 시작 체력 100, 몬스터 4마리", fontsize=14)
fig.text(.5, .025, "조건별 1회 관측. 레벨은 1→2, 기록된 최대 체력·공격력·방어력은 동일.\n피해량은 증가했으나 교전 시간은 감소. 일반적인 성능 향상이나 통계적 유의성을 뜻하지 않음.", ha="center", fontsize=9)
fig.tight_layout(rect=(0,.12,1,.92)); fig.savefig(OUT/"cooperation_comparison.png", dpi=170); plt.close(fig)

# Group logs have no battleId. Select the ON group whose first decision occurs in that battle interval.
on_battle = next(r for r in battles if r["battleId"] == IDS[1])
start = float(on_battle["startTime"]); end = start+float(on_battle["duration"])
decisions = read("squad_decisions.csv")
groups = {r["groupId"] for r in decisions if start <= float(r["time"]) <= end}
assert len(groups) == 1
changes = [r for r in decisions if r["groupId"] in groups and r["formationChanged"] == "True" and start <= float(r["time"]) <= end]
write("on_formation_changes.csv", [dict(elapsedSeconds=float(r["time"])-start, **r) for r in changes])
names = ["Solo", "Pair", "Triangle", "Surround", "Regroup", "Withdrawal"]
fig, ax = plt.subplots(figsize=(10, 4.6))
xs = [float(r["time"])-start for r in changes]; ys = [names.index(r["formation"]) for r in changes]
ax.step(xs+[end-start], ys+[ys[-1]], where="post", color="#277da1", linewidth=2)
ax.scatter(xs, ys, color="#277da1")
ax.set(yticks=range(len(names)), yticklabels=names, xlabel="협동 ON 전투 시작 후 시간 (초)", title="선택한 진형의 전환 기록 — 실제 포위 성공과 구분")
ax.grid(axis="x", alpha=.2)
fig.text(.5,.02,"약 6.49초: 불리한 피해 교환으로 재집결 / 약 9.50초: 재집결 종료 / 약 22.82초: 전방 이탈로 재집결",ha="center",fontsize=9)
fig.tight_layout(rect=(0,.07,1,1)); fig.savefig(OUT/"formation_timeline.png",dpi=170); plt.close(fig)
print("Saved selected real battles, comparison chart and formation timeline to", OUT)
