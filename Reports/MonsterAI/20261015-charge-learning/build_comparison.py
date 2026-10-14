"""Rebuild the charge comparison from the supplied real session. No simulated trials."""
import csv
import json
from pathlib import Path
from statistics import mean
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

SOURCE = Path(r"C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_221126_27d75bac5fd446cc813ca11be625b26b")
OUT = Path(__file__).resolve().parent
def read(name):
    with (SOURCE / name).open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))
def write(name, rows):
    with (OUT / name).open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)

actions, updates, battles = read("actions.csv"), read("learning_updates.csv"), read("battles.csv")
policy = json.loads((SOURCE / "policy_v219.json").read_text(encoding="utf-8-sig"))
assert len({r["actionId"] for r in actions}) == len(actions)
assert len({r["actionId"] for r in updates}) == len(updates)
by_id = {r["actionId"]: r for r in actions}
assert all(by_id[r["actionId"]]["mode"] == "Training" for r in updates)
assert [int(r["version"]) for r in updates] == list(range(1, policy["version"] + 1))
assert {r["policyVersion"] for r in actions if r["mode"] == "Frozen"} == {str(policy["version"])}
weights = {(r["context"], r["action"]): max(.65, min(1.8, 1 + r["meanReward"] * r["attempts"] / (r["attempts"] + 12))) for r in policy["entries"]}
summary = []
for mode in ("Baseline", "Training", "Frozen"):
    rows = [r for r in actions if r["mode"] == mode]
    selected = [r for r in rows if r["action"] == "Charge"]
    eligible = [r for r in rows if "Charge" in r["eligibleActions"].split(";")]
    resolved = [r for r in selected if r["released"] == "True" and r["outcome"] in {"Hit", "Miss", "NoDamageContact", "Blocked", "OtherTarget"}]
    matched = [r for r in eligible if r["context"] == "1:0:0" and len(r["eligibleActions"].split(";")) > 1]
    summary.append(dict(mode=mode, battles=sum(r["mode"] == mode for r in battles), actions=len(rows),
        chargeSelected=len(selected), chargeShare=len(selected)/len(rows), chargeEligible=len(eligible),
        chargeWhenEligible=len(selected)/len(eligible), chargeHits=sum(r["outcome"] == "Hit" for r in resolved),
        chargeResolved=len(resolved), chargeHitRate=sum(r["outcome"] == "Hit" for r in resolved)/len(resolved),
        contextMultiChoiceCount=len(matched), contextMultiChoiceCharge=sum(r["action"] == "Charge" for r in matched)))
write("charge_comparison.csv", summary)

# Diagnostic reconstruction: remove only the learned multipliers from the same Frozen decisions.
# This is not a replay, a real Baseline run, or an estimate of total combat performance.
reconstructed = []
for r in actions:
    if r["mode"] != "Frozen" or "Charge" not in r["eligibleActions"].split(";") or len(r["eligibleActions"].split(";")) < 2:
        continue
    probabilities = list(map(float, r["probabilities"].split(";")))
    base = [p / weights.get((r["context"], i), 1) for i, p in enumerate(probabilities)]
    reconstructed.append(dict(actionId=r["actionId"], context=r["context"], eligibleActions=r["eligibleActions"],
        reconstructedWithoutLearning=base[3]/sum(base), recordedWithLearning=probabilities[3]))
write("charge_probability_reconstruction.csv", reconstructed)

plt.rcParams.update({"font.family": "Malgun Gothic", "axes.unicode_minus": False, "font.size": 11})
fig, axes = plt.subplots(1, 2, figsize=(12, 4.8))
evals = [summary[0], summary[2]]
bars = axes[0].bar(["Baseline", "Frozen (v219)"], [r["chargeShare"]*100 for r in evals], color=["#7c8999", "#277da1"])
for bar, r in zip(bars, evals):
    axes[0].text(bar.get_x()+bar.get_width()/2, bar.get_height()+1, f'{r["chargeShare"]:.0%} ({r["chargeSelected"]}/{r["actions"]})', ha="center")
axes[0].set(title="전체 공격 중 돌진 선택 비율 — 실제 관측", ylabel="선택 비율 (%)", ylim=(0, 45))
labels = ["투사체", "원형", "돌진"]
values = [weights[("1:0:0", i)] for i in (1, 2, 3)]
bars = axes[1].bar(labels, values, color=["#7c8999", "#7c8999", "#277da1"])
axes[1].axhline(1, color="black", linestyle="--", label="학습 전: 모두 1.00")
for bar, value in zip(bars, values):
    axes[1].text(bar.get_x()+bar.get_width()/2, value+.03, f"{value:.3f}", ha="center")
axes[1].set(title="같은 상황의 학습 배율 — 최종 JSON", ylabel="기존 공격 가중치에 곱하는 배율", ylim=(0, 2))
axes[1].legend()
fig.suptitle("돌진 학습: 가중치 갱신 및 공격 선택 변화 확인", fontsize=15)
fig.text(.5, .02, "상황: 거리 >1.8~3.75, 몬스터 체력 >35%, 플레이어 공격·대시 중 아님.\nBaseline 전투 기록 1개 / Frozen 2개. 조건 차이와 작은 표본으로 성능 향상은 확정할 수 없음.", ha="center", fontsize=9)
fig.tight_layout(rect=(0,.12,1,.92)); fig.savefig(OUT/"charge_comparison.png", dpi=170); plt.close(fig)

fig, ax = plt.subplots(figsize=(9, 4.8))
for name, label, color in [("Charge", "돌진", "#277da1"), ("Projectile", "투사체", "#d97732"), ("Circle", "원형", "#6b8e23")]:
    rows = [r for r in updates if r["context"] == "1:0:0" and r["action"] == name]
    ax.step([0]+[int(r["version"]) for r in rows]+[policy["version"]], [1]+[float(r["newWeight"]) for r in rows]+[float(rows[-1]["newWeight"])], where="post", label=label, color=color)
ax.axhline(1, color="gray", linestyle="--")
ax.set(title="학습 로그: 중거리·체력 높은 상황의 공격별 배율", xlabel="공유 학습 버전 (모든 상황의 갱신 누적)", ylabel="학습 배율", ylim=(.7, 1.9))
ax.legend(); fig.tight_layout(); fig.savefig(OUT/"charge_learning_curve.png", dpi=170); plt.close(fig)
print("Built real-data comparison tables and Korean charts:", OUT)
print("Same-decision diagnostic:", len(reconstructed), mean(r["reconstructedWithoutLearning"] for r in reconstructed), mean(r["recordedWithLearning"] for r in reconstructed))
