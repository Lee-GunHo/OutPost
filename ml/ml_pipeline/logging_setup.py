import logging
import sys


def setup_logging(level=logging.INFO):
    # Windows 콘솔이 cp949 등 non-UTF-8 코드페이지인 경우 이모지/특수문자 출력(예: PyTorch
    # onnx exporter의 체크마크 로그)에서 UnicodeEncodeError가 나므로 항상 UTF-8을 강제.
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")

    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%H:%M:%S",
    )
