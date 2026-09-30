/// <summary>
/// 플레이어의 현재 스냅샷(1단계 PlayLogEntry)을 보고 난관 유형을 판단하는 진단기.
/// RuleBasedDiagnoser가 기본 구현이며, 추후 ONNX 기반 ML 진단기로 교체 가능하도록
/// NPCTipDialoguePresenter는 이 인터페이스에만 의존함.
/// </summary>
public interface IDifficultyDiagnoser
{
    DifficultyDiagnosis Diagnose(PlayLogEntry snapshot);
}
