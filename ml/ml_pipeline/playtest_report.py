"""playtest_analysis.py의 계산 결과를 그래프 이미지 + Markdown 요약 표로 저장."""

import logging
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import pandas as pd

from .plot_style import configure_korean_font

logger = logging.getLogger(__name__)

configure_korean_font()


def save_feedback_chart(feedback_result: dict, out_path: Path):
    if not feedback_result.get("available"):
        return

    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    groups = ["Rule", "ML"]
    means = [feedback_result["rule"]["mean"] or 0, feedback_result["ml"]["mean"] or 0]
    stds = [feedback_result["rule"]["std"] or 0, feedback_result["ml"]["std"] or 0]
    ns = [feedback_result["rule"]["n"], feedback_result["ml"]["n"]]

    fig, ax = plt.subplots(figsize=(5, 4))
    ax.bar(groups, means, yerr=stds, capsize=6)
    ax.set_ylim(0, 5.5)
    ax.set_ylabel("평균 만족도 (1~5)")
    ax.set_title("그룹별 팁 만족도")

    for i, (mean, n) in enumerate(zip(means, ns)):
        ax.text(i, mean + 0.15, f"n={n}", ha="center")

    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    plt.close(fig)
    logger.info("만족도 그래프 저장: %s", out_path)


def save_resolution_rate_chart(resolution_result: dict, out_path: Path):
    if not resolution_result.get("available"):
        return

    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    groups = ["Rule", "ML"]
    rates = [
        (resolution_result["rule"]["rate"] or 0) * 100,
        (resolution_result["ml"]["rate"] or 0) * 100,
    ]
    ns = [resolution_result["rule"]["n"], resolution_result["ml"]["n"]]

    fig, ax = plt.subplots(figsize=(5, 4))
    ax.bar(groups, rates)
    ax.set_ylim(0, 100)
    ax.set_ylabel("해결률 (%)")
    ax.set_title("그룹별 난관 해결률")

    for i, (rate, n) in enumerate(zip(rates, ns)):
        ax.text(i, rate + 2, f"n={n}", ha="center")

    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    plt.close(fig)
    logger.info("해결률 그래프 저장: %s", out_path)


def save_time_to_resolve_chart(resolution_df: pd.DataFrame, group_lookup: pd.Series, out_path: Path):
    if resolution_df.empty or group_lookup.empty:
        return

    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    df = resolution_df.copy()
    df["group"] = df["sessionId"].map(group_lookup)
    resolved = df[(df["resolved"] == True) & df["minutesToResolve"].notna() & df["group"].notna()]

    rule_minutes = resolved.loc[resolved["group"] == "Rule", "minutesToResolve"].astype(float)
    ml_minutes = resolved.loc[resolved["group"] == "ML", "minutesToResolve"].astype(float)

    if rule_minutes.empty and ml_minutes.empty:
        return

    fig, ax = plt.subplots(figsize=(5, 4))
    ax.boxplot([rule_minutes, ml_minutes], tick_labels=["Rule", "ML"])
    ax.set_ylabel("해결까지 걸린 시간 (분)")
    ax.set_title("그룹별 해결 소요 시간")
    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    plt.close(fig)
    logger.info("해결 시간 그래프 저장: %s", out_path)


def _format_p_value(test_result: dict, key: str) -> str:
    if not test_result:
        return "표본 부족으로 검정 불가"

    p_value = test_result["pValue"]
    significance = "**유의미(p<0.05)**" if p_value < 0.05 else "유의미하지 않음"
    return f"p={p_value:.4f} ({significance})"


def write_summary_markdown(
    feedback_result: dict,
    resolution_result: dict,
    mismatched_df: pd.DataFrame,
    out_path: Path,
    chart_relative_paths: dict,
):
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    lines = ["# 플레이테스트 결과 요약 (Rule vs ML 그룹)", ""]

    lines.append(
        "> ⚠️ 이 보고서의 모든 수치는 실제 플레이테스트 참가자 수에 좌우되는 소표본 결과입니다. "
        "표(n)를 항상 같이 확인하고, 통계 검정 결과는 참고 지표로만 활용하세요."
    )
    lines.append("")

    # ---- 만족도 ----
    lines.append("## 1. 팁 만족도")
    if feedback_result.get("available"):
        rule = feedback_result["rule"]
        ml = feedback_result["ml"]
        lines.append("")
        lines.append("| 그룹 | n | 평균 | 표준편차 |")
        lines.append("|---|---|---|---|")
        lines.append(f"| Rule | {rule['n']} | {rule['mean']:.2f} | {rule['std']:.2f} |"
                      if rule["n"] else "| Rule | 0 | - | - |")
        lines.append(f"| ML | {ml['n']} | {ml['mean']:.2f} | {ml['std']:.2f} |"
                      if ml["n"] else "| ML | 0 | - | - |")
        lines.append("")
        lines.append(f"- 건너뛴 응답: {feedback_result['skippedCount']}건")
        lines.append(f"- Mann-Whitney U 검정: {_format_p_value(feedback_result['mannWhitneyU'], 'pValue')}")

        if feedback_result.get("warning"):
            lines.append(f"- {feedback_result['warning']}")

        if "feedback" in chart_relative_paths:
            lines.append("")
            lines.append(f"![팁 만족도]({chart_relative_paths['feedback']})")
    else:
        lines.append("")
        lines.append("(데이터 없음 - tip_feedback_log.csv 또는 ab_test_assignment.csv를 찾지 못함)")

    lines.append("")

    # ---- 해결률 ----
    lines.append("## 2. 팁 이후 난관 해결률")
    if resolution_result.get("available"):
        rule = resolution_result["rule"]
        ml = resolution_result["ml"]
        lines.append("")
        lines.append("| 그룹 | n | 해결됨 | 해결률 | 평균 해결 시간(분) |")
        lines.append("|---|---|---|---|---|")

        rule_minutes_text = (
            f"{rule['meanMinutesToResolve']:.1f}" if rule["meanMinutesToResolve"] is not None else "-"
        )
        ml_minutes_text = (
            f"{ml['meanMinutesToResolve']:.1f}" if ml["meanMinutesToResolve"] is not None else "-"
        )

        lines.append(
            f"| Rule | {rule['n']} | {rule['resolvedCount']} | "
            f"{(rule['rate'] or 0) * 100:.1f}% | {rule_minutes_text} |"
        )
        lines.append(
            f"| ML | {ml['n']} | {ml['resolvedCount']} | "
            f"{(ml['rate'] or 0) * 100:.1f}% | {ml_minutes_text} |"
        )
        lines.append("")
        lines.append(f"- 해결률 비교(Fisher's exact test): {_format_p_value(resolution_result['fisherExact'], 'pValue')}")
        lines.append(
            f"- 해결 시간 비교(Mann-Whitney U): "
            f"{_format_p_value(resolution_result['timeToResolveMannWhitneyU'], 'pValue')}"
        )

        if resolution_result.get("warning"):
            lines.append(f"- {resolution_result['warning']}")

        if "resolution_rate" in chart_relative_paths:
            lines.append("")
            lines.append(f"![해결률]({chart_relative_paths['resolution_rate']})")

        if "time_to_resolve" in chart_relative_paths:
            lines.append("")
            lines.append(f"![해결 시간]({chart_relative_paths['time_to_resolve']})")
    else:
        lines.append("")
        lines.append("(데이터 없음 - tip_resolution_log.csv 또는 ab_test_assignment.csv를 찾지 못함)")

    lines.append("")

    # ---- Rule vs ML 불일치 사례 ----
    lines.append("## 3. Rule과 ML 판단이 달랐던 사례 (발표용 예시)")
    lines.append("")

    if mismatched_df.empty:
        lines.append("(Compare 모드 로그에서 불일치 사례를 찾지 못함)")
    else:
        lines.append(f"총 {len(mismatched_df)}건 발견. 상위 10건:")
        lines.append("")
        lines.append("| sessionId | timestamp | Rule 판단 | Rule 확신도 | ML 판단 | ML 확신도 |")
        lines.append("|---|---|---|---|---|---|")

        for _, row in mismatched_df.head(10).iterrows():
            lines.append(
                f"| {row['sessionId'][:12]}... | {row['timestamp']} | {row['ruleType']} | "
                f"{row['ruleConfidence']:.2f} | {row['mlType']} | {row['mlConfidence']:.2f} |"
            )

    lines.append("")

    with open(out_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    logger.info("요약 보고서 저장: %s", out_path)
