"""Plot every completed battle in this supplied session, including earlier retries."""
import csv
from pathlib import Path
from statistics import mean
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

SOURCE = Path(r"C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_232641_79ed80cc96fc47049761bf7b042f4e39")
OUT = Path(__file__).resolve().parent
def read(name):
    with (SOURCE/name).open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))
actions, battles, members, configs = [read(n) for n in ("actions.csv", "battles.csv", "participants.csv", "battle_configs.csv")]
assert len({r["actionId"] for r in actions}) == len(actions)
records = []
counts = {"False": 0, "True": 0}
for b in battles:
    assert b["mode"] == "Baseline" and b["playerStartHp"] == "100" and b["uniqueMonsterCount"] == "4"
    assert b["result"] in {"Disengaged", "PlayerDied"}
    c = next(r for r in configs if r["battleId"] == b["battleId"])
    a = [r for r in actions if r["battleId"] == b["battleId"]]
    m = [r for r in members if r["battleId"] == b["battleId"]]
    counts[b["cooperation"]] += 1
    label = ("ON" if b["cooperation"] == "True" else "OFF") + " " + str(counts[b["cooperation"]])
    records.append(dict(trial=label, battleId=b["battleId"], cooperation=b["cooperation"],
        playerLevel=c["playerLevel"], playerStartHp=b["playerStartHp"],
        directDamage=sum(int(r["damageDealt"]) for r in a), battleSeconds=float(b["duration"]),
        meanEngagedSeconds=mean(float(r["engagedSeconds"]) for r in m),
        surroundCount=int(b["surroundCount"]), overlapSeconds=float(b["overlapSeconds"]), roleSwitches=int(b["roleSwitchCount"])))
with (OUT/'trial_comparison.csv').open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=list(records[0]));w.writeheader();w.writerows(records)
plt.rcParams.update({'font.family':'Malgun Gothic','axes.unicode_minus':False})
fig,axes=plt.subplots(1,3,figsize=(12,5))
for ax,key,title,unit in zip(axes,['directDamage','battleSeconds','meanEngagedSeconds'],
        ['플레이어에게 준 직접 피해','전투 지속 시간','몬스터 평균 교전 참여 시간'],['피해량','초','초']):
    vals=[r[key] for r in records]
    bars=ax.bar([r['trial'] for r in records],vals,color=['#7c8999' if r['cooperation']=='False' else '#277da1' for r in records])
    ax.set(title=title,ylabel=unit,ylim=(0,max(vals)*1.3))
    for bar,v in zip(bars,vals):ax.text(bar.get_x()+bar.get_width()/2,v+max(vals)*.035,f'{v:.2f}',ha='center')
fig.suptitle('협동 예비 비교 — OFF 1회 / ON 2회, 모든 반복 기록 표시',fontsize=14)
fig.text(.5,.025,'모두 Baseline · 시작 체력 100 · 몬스터 4마리. ON 2회차의 레벨은 2, 기록된 전투 능력치는 동일.\n각 조건의 표본이 적고 불균형함. 포위 성공은 전 기록에서 0회이며 일반적인 성능 향상을 확정하지 않음.',ha='center',fontsize=9)
fig.tight_layout(rect=(0,.12,1,.92));fig.savefig(OUT/'cooperation_trials.png',dpi=170);plt.close(fig)
print('Wrote trial_comparison.csv and cooperation_trials.png from',len(records),'real battles.')
