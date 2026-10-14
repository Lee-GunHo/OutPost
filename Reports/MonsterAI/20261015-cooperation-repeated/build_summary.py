"""Summarize all completed trials in this session, without selecting favorable outcomes."""
import csv
from pathlib import Path
from statistics import mean, stdev
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

SOURCE = Path(r"C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_232641_79ed80cc96fc47049761bf7b042f4e39")
OUT = Path(__file__).resolve().parent
def read(name):
    with (SOURCE / name).open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))
def write(name, rows):
    with (OUT / name).open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)

actions, battles, members, configs = [read(n) for n in ("actions.csv", "battles.csv", "participants.csv", "battle_configs.csv")]
assert len({r["actionId"] for r in actions}) == len(actions)
assert all(r["mode"] == "Baseline" and r["policyVersion"] == "0" for r in actions)
trials = []
for b in battles:
    assert b["mode"] == "Baseline" and b["playerStartHp"] == "100" and b["uniqueMonsterCount"] == "4"
    assert b["result"] in {"Disengaged", "PlayerDied"}
    bid = b["battleId"]
    a = [r for r in actions if r["battleId"] == bid]
    m = [r for r in members if r["battleId"] == bid]
    c = next(r for r in configs if r["battleId"] == bid)
    trials.append(dict(battleId=bid, cooperation=b["cooperation"], playerLevel=c["playerLevel"],
        playerStartHp=b["playerStartHp"], directDamage=sum(int(r["damageDealt"]) for r in a),
        battleSeconds=float(b["duration"]), meanEngagedSeconds=mean(float(r["engagedSeconds"]) for r in m),
        roleChanges=int(b["roleSwitchCount"]), surroundCount=int(b["surroundCount"]),
        surroundSeconds=float(b["surroundSeconds"]), overlapSeconds=float(b["overlapSeconds"])))
write("trials.csv", trials)
groups = [[r for r in trials if r["cooperation"] == v] for v in ("False", "True")]
metrics = [("directDamage", "플레이어에게 준 직접 피해량"), ("battleSeconds", "전투 지속 시간(초)"),
           ("meanEngagedSeconds", "몬스터 평균 교전 참여 시간(초)"), ("roleChanges", "개체별 역할 변경 누적"),
           ("surroundCount", "포위 성공 횟수"), ("surroundSeconds", "포위 유지 시간(초)"), ("overlapSeconds", "겹침 시간(초)")]
stats = []
for coop, group in zip(("OFF", "ON"), groups):
    for key, title in metrics:
        values = [r[key] for r in group]
        stats.append(dict(cooperation=coop, metric=key, n=len(values), mean=mean(values),
            sampleSD=stdev(values) if len(values)>1 else "", minimum=min(values), maximum=max(values)))
write("descriptive_statistics.csv", stats)

lines = ["# 협동 AI 반복 측정 결과", "", "2026-10-15. 이 세션의 완료된 전투 3개를 모두 포함했다. OFF 1회, ON 2회이며 결과가 좋은 ON만 고르지 않았다.", "",
    "**Baseline·시작 체력 100·몬스터 4마리 조건은 맞았다. 협동 ON의 관측 평균 피해량과 교전 시간은 높았지만, 표본이 작고 ON 피해량 변동이 커 일반적인 성능 향상을 확정할 수 없다.**", "",
    "원본: `"+str(SOURCE)+"`", "", "## 개별 기록", "",
    "| 전투 | 플레이어 레벨 | 직접 피해 | 전투 시간(초) | 몬스터 평균 교전 시간(초) | 역할 변경 |",
    "|---|---:|---:|---:|---:|---:|"]
counts = {"False":0,"True":0}
for r in trials:
    counts[r["cooperation"]]+=1
    label = ("ON" if r["cooperation"]=="True" else "OFF")+" "+str(counts[r["cooperation"]])
    lines.append(f'| {label} | {r["playerLevel"]} | {r["directDamage"]} | {r["battleSeconds"]:.2f} | {r["meanEngagedSeconds"]:.2f} | {r["roleChanges"]} |')
lines += ["", "## 평균과 편차", "", "| 항목 | OFF 평균 (n=1) | ON 평균 ± 표본 표준편차 (n=2) |", "|---|---:|---:|"]
for key, title in metrics:
    off = [r[key] for r in groups[0]]; on = [r[key] for r in groups[1]]
    lines.append(f"| {title} | {mean(off):.2f} | {mean(on):.2f} ± {stdev(on):.2f} |")
lines += ["", "OFF는 1회뿐이므로 표준편차를 추정할 수 없다. ON의 표준편차도 2개 전투에서 계산한 기술통계이며 신뢰구간이나 통계적 유의성을 뜻하지 않는다. 각 전투에서 계산한 몬스터 평균 교전 시간을 하나의 표본으로 취급했고, 같은 전투의 몬스터 4마리를 독립 전투 4개로 세지 않았다.", "",
    "## 조건과 해석", "",
    "- 모든 공격은 Baseline / policyVersion 0이다. 학습 갱신 파일은 없으며 개별 학습 효과는 적용되지 않았다.",
    "- 동일 세션·월드 시드에서 같은 TestMonster 4마리를 상대했다. 기록된 플레이어 최대 체력 100, 공격력 10, 방어력 0 및 몬스터 최대 체력 50, 공격력 10, 기본 이동속도 3은 동일하다.",
    "- 마지막 ON에서는 플레이어 레벨이 1에서 2로 변했다. 위 기록된 전투 능력치는 같지만 완전히 같은 조건이라고 단정하지 않는다. 시작 위치와 실제 플레이 행동도 동일 재생이 아니다.",
    "- ON 직접 피해량은 22와 53으로 변동이 컸다. 평균 37.5가 OFF 24보다 높다는 것은 이번 표본의 관측이며 협동의 확정적 효과 크기가 아니다.",
    "- 세 전투 모두 몬스터 4마리가 처치됐다. 평균 교전 참여 시간은 생성부터의 전체 생존 시간과 구분한다.",
    "- 모든 전투의 포위 성공과 겹침 시간은 0이다. 진형 이름 Surround의 선택은 실제 위치 기준 포위 성공과 다르다. 포위 기능의 성능 개선 근거는 아직 확보되지 않았다.",
    "- 역할 변경은 각 개체가 Solo 외 역할 사이에서 바뀐 누적 횟수다. 그룹 교대 성공 횟수가 아니며 Regroup 전환도 포함한다. 큰 수치가 곧 좋은 성능은 아니다.",
    "- 협동 ON/OFF는 역할 배치 외에도 역할별 효과, 공동 공격 제한, 재집결을 함께 바꾼다. 이 비교는 협동 시스템 전체에 대한 것이다.", "",
    "## 발표용 설명", "",
    "> 학습 효과를 끈 Baseline에서 동일한 시작 체력과 몬스터 수로 협동 유무를 비교했다. OFF 1회와 ON 2회에서 ON의 평균 직접 피해량과 교전 시간이 더 높게 관측됐다. 다만 ON 피해량 변동이 크고 표본이 적어 예비 결과로 제시한다. 역할 분담은 로그로 확인했지만 실제 포위 성공은 아직 관측되지 않았다.", "",
    "## 산출물", "",
    "- comparison_repeated.png: 전투별 관측 점과 평균 막대. 각 전투를 모두 표시한다.",
    "- trials.csv: battleId와 전투별 측정값.",
    "- descriptive_statistics.csv: 평균, 표본 표준편차, 최솟값, 최댓값.",
    "- report.md 및 기존 분석 CSV: 원본 세션 전체 집계.",
    "- build_summary.py: 이 보고서와 비교 그림 재생성. 기존 다른 세션은 월드/조건 차이로 이 평균에 혼합하지 않았다. 원본은 보관된다.", "",
    "게임 코드와 원본 로그는 수정하지 않았다."]
(OUT/"결과설명.md").write_text("\n".join(lines)+"\n",encoding="utf-8")

plt.rcParams.update({"font.family":"Malgun Gothic","axes.unicode_minus":False})
fig, axes = plt.subplots(1,3,figsize=(12,4.8))
for ax,(key,title) in zip(axes,metrics[:3]):
    values = [[r[key] for r in g] for g in groups]
    ax.bar([0,1],[mean(v) for v in values],color=["#aab3bf","#8ec5db"],width=.6)
    for i,vs in enumerate(values):
        xs = [i] if len(vs)==1 else [i-.07+j*.14/(len(vs)-1) for j in range(len(vs))]
        ax.scatter(xs,vs,color="#17324d",zorder=3)
        for x,y in zip(xs,vs):ax.annotate(f"{y:.2f}",(x,y),xytext=(0,7),textcoords="offset points",ha="center",fontsize=9)
    ax.set(title=title,xticks=[0,1],xticklabels=["OFF (1회)","ON (2회)"],ylim=(0,max(max(v) for v in values)*1.3))
fig.suptitle("협동 AI 예비 반복 측정 — 막대: 평균 / 점: 각 전투",fontsize=14)
fig.text(.5,.025,"Baseline · 시작 체력 100 · 몬스터 4마리. 표본 불균형(1회 vs 2회), 마지막 ON 레벨 차이 있음.\n좋은 결과만 선택하지 않음. 통계적 유의성이나 일반적인 성능 향상을 확정하지 않음.",ha="center",fontsize=9)
fig.tight_layout(rect=(0,.12,1,.92));fig.savefig(OUT/"comparison_repeated.png",dpi=170);plt.close(fig)
print("Wrote real-trial report, sample statistics and chart to",OUT)
for r in stats:print(r)
