"""
ONNX 모델에 Unity(Sentis/Inference Engine)가 지원하지 않을 가능성이 높은 연산자가
있는지 검사. ai.onnx.ml 도메인(ZipMap, LabelEncoder, TreeEnsemble* 등 scikit-learn류
연산자)은 Sentis가 지원하지 않으므로 명확히 차단 대상으로 취급.

주의: KNOWN_SUPPORTED_OPS는 "이 정도면 보통 되더라" 수준의 보수적인 참고 목록이며,
실제 지원 여부는 설치한 Sentis 패키지 버전의 공식 문서에서 반드시 재확인해야 함.
"""

import logging
from pathlib import Path

import onnx

logger = logging.getLogger(__name__)

# 작은 MLP(Linear/ReLU/Softmax류)가 흔히 만들어내는, Sentis가 지원하는 코어 연산자 참고 목록.
KNOWN_SUPPORTED_OPS = {
    "Gemm", "MatMul", "Add", "Sub", "Mul", "Div", "Relu", "Sigmoid", "Tanh",
    "Softmax", "Reshape", "Transpose", "Concat", "Constant", "Identity",
    "Flatten", "Squeeze", "Unsqueeze", "Cast", "Shape", "Gather", "Slice",
    "Conv", "BatchNormalization", "MaxPool", "AveragePool", "GlobalAveragePool",
    "LeakyRelu", "Clip", "Pow", "Sqrt", "ReduceMean", "ReduceSum", "Exp", "Log",
}

# scikit-learn(skl2onnx) 계열 변환에서 흔히 나오는, Sentis가 지원하지 않는 연산자.
UNSUPPORTED_OP_NAMES = {
    "ZipMap", "LabelEncoder", "TreeEnsembleClassifier", "TreeEnsembleRegressor",
    "LinearClassifier", "SVMClassifier", "OneHotEncoder", "Imputer",
    "FeatureVectorizer", "Scaler", "CastMap", "CategoryMapper", "DictVectorizer",
}

UNSUPPORTED_DOMAINS = {"ai.onnx.ml"}


def check_onnx_model(onnx_path: Path) -> dict:
    """반환: {"blocked": [...], "unknown": [...], "ok": [...], "opset": int}"""
    onnx_path = Path(onnx_path)
    model = onnx.load(str(onnx_path))
    onnx.checker.check_model(model)  # ONNX 자체 구조 유효성 검사(실패 시 예외 발생)

    findings = {"blocked": [], "unknown": [], "ok": []}

    for node in model.graph.node:
        domain = node.domain or "ai.onnx"
        op_type = node.op_type
        label = f"{op_type} (domain='{domain}')"

        if domain in UNSUPPORTED_DOMAINS or op_type in UNSUPPORTED_OP_NAMES:
            findings["blocked"].append(label)
        elif op_type not in KNOWN_SUPPORTED_OPS:
            findings["unknown"].append(label)
        else:
            findings["ok"].append(op_type)

    opset_version = model.opset_import[0].version if model.opset_import else None
    findings["opset"] = opset_version

    return findings


def print_report(onnx_path: Path, findings: dict) -> bool:
    """검사 결과를 로그로 출력하고, 배포해도 안전하면 True를 반환."""
    logger.info("ONNX 연산자 검사: %s (opset=%s)", onnx_path, findings["opset"])
    logger.info("  지원 확인된 연산자: %s", sorted(set(findings["ok"])) or "(없음)")

    is_safe = True

    if findings["unknown"]:
        logger.warning(
            "  참고 목록에 없는 연산자 %d개 발견 - Sentis 문서에서 지원 여부를 직접 확인하세요: %s",
            len(findings["unknown"]), findings["unknown"])

    if findings["blocked"]:
        is_safe = False
        logger.error(
            "  Unity가 지원하지 않을 가능성이 매우 높은 연산자 %d개 발견: %s",
            len(findings["blocked"]), findings["blocked"])

    if is_safe and not findings["unknown"]:
        logger.info("  통과: 모든 연산자가 지원 참고 목록 안에 있습니다.")

    return is_safe
