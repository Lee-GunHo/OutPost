/// <summary>
/// NPCTipDialoguePresenter가 어떤 진단기를 쓸지 인스펙터에서 선택.
/// </summary>
public enum DiagnosisMode
{
    Rule,     // RuleBasedDiagnoser만 사용
    ML,       // MLDiagnoser 사용(내부적으로 실패 시 규칙 기반으로 자동 대체)
    Compare,  // 둘 다 실행해서 결과를 로그로 비교, 대사는 ML 결과로 표시
}
