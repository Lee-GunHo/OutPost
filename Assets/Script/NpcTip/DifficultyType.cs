/// <summary>
/// 플레이어가 겪고 있을 가능성이 높은 난관 유형.
/// IDifficultyDiagnoser 구현체(규칙 기반/ML)가 공통으로 사용.
/// </summary>
public enum DifficultyType
{
    EquipmentLack,   // 장비 부족
    CombatStruggle,  // 전투 미숙
    Lost,            // 길 잃음
    ResourceLack,    // 자원 부족
    Smooth           // 순조로움
}
