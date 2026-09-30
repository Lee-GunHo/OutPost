"""matplotlib 차트에 한글 라벨/제목을 쓸 때 글리프가 깨지는 문제를 막기 위한 공통 폰트 설정.
차트를 그리는 스크립트는 pyplot으로 뭔가 그리기 전에 configure_korean_font()를 한 번 호출할 것."""

import logging

import matplotlib

logger = logging.getLogger(__name__)

# 우선순위대로 시도. Windows는 보통 Malgun Gothic이 깔려 있음.
_KOREAN_FONT_CANDIDATES = [
    "Malgun Gothic", "AppleGothic", "NanumGothic", "Noto Sans KR", "Noto Sans CJK KR",
]

_configured = False


def configure_korean_font():
    global _configured

    if _configured:
        return

    import matplotlib.font_manager as fm

    available = {f.name for f in fm.fontManager.ttflist}
    chosen = next((name for name in _KOREAN_FONT_CANDIDATES if name in available), None)

    if chosen:
        matplotlib.rcParams["font.family"] = chosen
    else:
        logger.warning(
            "한글 지원 폰트를 찾지 못했습니다(%s 중 없음). 차트의 한글 라벨이 깨질 수 있습니다.",
            _KOREAN_FONT_CANDIDATES)

    matplotlib.rcParams["axes.unicode_minus"] = False  # 한글 폰트에서 마이너스 기호 깨짐 방지

    _configured = True
