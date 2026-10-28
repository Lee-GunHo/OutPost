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


def save_resolution_rate_chart(resolution_result: dict, out_path: Path):
    """난관 유형별 팁 이후 해결률 막대 그래프."""
    if not resolution_result.get("available") or not resolution_result["byType"]:
        return

    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    types = list(resolution_result["byType"].keys())
    rates = [(resolution_result["byType"][t]["rate"] or 0) * 100 for t in types]
    ns = [resolution_result["byType"][t]["n"] for t in types]

    fig, ax = plt.subplots(figsize=(max(5, len(types) * 1.4), 4))
    ax.bar(types, rates)
    ax.set_ylim(0, 105)
    ax.set_ylabel("해결률 (%)")
    ax.set_title("난관 유형별 팁 이후 해결률")

    for i, (rate, n) in enumerate(zip(rates, ns)):
        ax.text(i, rate + 2, f"n={n}", ha="center")

    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    plt.close(fig)
    logger.info("해결률 그래프 저장: %s", out_path)


def save_time_to_resolve_chart(resolution_df: pd.DataFrame, out_path: Path):
    """난관 유형별 해결까지 걸린 시간 상자 그림 (해결된 건만)."""
    if resolution_df.empty:
        return

    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    df = resolution_df.copy()
    df["resolved"] = df["resolved"].astype(str).str.lower() == "true"
    resolved = df[df["resolved"] & df["minutesToResolve"].notna()]

    if resolved.empty:
        return

    types = sorted(resolved["difficultyType"].unique())
    minutes = [resolved.loc[resolved["difficultyType"] == t, "minutesToResolve"].astype(float) for t in types]

    fig, ax = plt.subplots(figsize=(max(5, len(types) * 1.4), 4))
    ax.boxplot(minutes, tick_labels=types)
    ax.set_ylabel("해결까지 걸린 시간 (분)")
    ax.set_title("난관 유형별 해결 소요 시간")
    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    plt.close(fig)
    logger.info("해결 시간 그래프 저장: %s", out_path)


def _format_rate_row(label: str, summary: dict) -> str:
    rate_text = f"{summary['rate'] * 100:.1f}%" if summary["rate"] is not None else "-"
    minutes_text = (
        f"{summary['meanMinutesToResolve']:.1f}" if summary["meanMinutesToResolve"] is not None else "-"
    )
    return f"| {label} | {summary['n']} | {summary['resolvedCount']} | {rate_text} | {minutes_text} |"


def write_summary_markdown(
    resolution_result: dict,
    mismatched_df: pd.DataFrame,
    out_path: Path,
    chart_relative_paths: dict,
):
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    lines = ["# 자체 테스트 결과 요약 (팁 이후 해결률 + Rule/ML 불일치 사례)", ""]

    lines.append(
        "> ⚠️ 팀원 자체 테스트로 모은 소표본 결과입니다. 표(n)를 항상 같이 확인하고 참고 지표로만 활용하세요. "
        "(외부 플레이테스트 없이 진행하므로 만족도 설문과 A/B 그룹 비교는 하지 않음)"
    )
    lines.append("")

    # ---- 해결률 ----
    lines.append("## 1. 팁 이후 난관 해결률 (자동 추적)")
    if resolution_result.get("available"):
        lines.append("")
        lines.append("| 난관 유형 | n | 해결됨 | 해결률 | 평균 해결 시간(분) |")
        lines.append("|---|---|---|---|---|")

        for difficulty_type, summary in resolution_result["byType"].items():
            lines.append(_format_rate_row(difficulty_type, summary))

        lines.append(_format_rate_row("**전체**", resolution_result["overall"]))
        lines.append("")

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
        lines.append("(데이터 없음 - tip_resolution_log.csv를 찾지 못함)")

    lines.append("")

    # ---- Rule vs ML 불일치 사례 ----
    lines.append("## 2. Rule과 ML 판단이 달랐던 사례 (발표용 예시)")
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
