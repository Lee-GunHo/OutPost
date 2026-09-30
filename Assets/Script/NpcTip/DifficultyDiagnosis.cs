/// <summary>
/// IDifficultyDiagnoser 하나의 진단 결과.
/// </summary>
public readonly struct DifficultyDiagnosis
{
    public readonly DifficultyType Type;
    public readonly float Confidence; // 0~1

    public DifficultyDiagnosis(DifficultyType type, float confidence)
    {
        Type = type;
        Confidence = confidence;
    }
}
