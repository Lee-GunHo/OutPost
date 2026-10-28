using System;

/// <summary>
/// 채굴(벽 내구도) 규칙(순수 함수). 코어키퍼처럼 벽마다 내구도(HP)가 있고 곡괭이 채굴력만큼 깎임.
/// MonoBehaviour/씬과 분리해서 EditMode 테스트로 검증.
/// </summary>
public static class MiningRules
{
    /// <summary>
    /// 한 번 휘둘렀을 때 벽에 들어가는 데미지.
    /// pickPower: 들고 있는 곡괭이의 채굴력(맨손/다른 도구면 호출부에서 handPower를 넘김)
    /// hardness: 벽의 단단함(BreakableWallModel 인스펙터 값). 0이면 아무 도구로나 정상 채굴
    /// </summary>
    public static int CalculateDamage(int pickPower, int hardness)
    {
        // TODO(사용자 구현): 채굴력이 단단함보다 낮을 때 어떻게 할지 결정
        return Math.Max(1, pickPower);
    }

    /// <summary>
    /// 지금까지 받은 데미지 → 제거된 단계 수(0..stageCount). 내구도를 단계 수로 균등하게 나눔.
    /// 예) maxDurability 30, stageCount 3 → 데미지 10마다 Up0/Down0, Up1/Down1 ... 순서로 제거
    /// </summary>
    public static int GetClearedStageCount(int damageTaken, int maxDurability, int stageCount)
    {
        if (stageCount <= 1 || maxDurability <= 0)
            return damageTaken >= maxDurability ? Math.Max(1, stageCount) : 0;

        if (damageTaken >= maxDurability)
            return stageCount;

        if (damageTaken <= 0)
            return 0;

        return (int)((long)damageTaken * stageCount / maxDurability);
    }
}
