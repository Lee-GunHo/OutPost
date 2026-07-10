using UnityEngine;

public class StatusEffectData
{
    // 상태효과 종류
    public StatusEffectType EffectType { get; private set; }

    // 상태효과 전체 지속시간
    public float TotalDuration { get; private set; }

    // 현재 남은 지속시간
    public float Duration { get; private set; }

    // 상태효과 기본 수치
    // Poison/Burn: 고정 도트 데미지
    // Bleed: 기본 데미지
    public float Value { get; private set; }

    // 몇 초마다 도트 데미지를 줄지
    public float TickInterval { get; private set; }

    // 공격력 변화량
    public int AttackModifier { get; private set; }

    // 방어력 변화량
    public int DefenseModifier { get; private set; }

    // 상태효과가 시작된 후 지난 시간
    private float elapsedTime;

    // 지금까지 실제로 적용된 틱 횟수
    private int tickCount;

    public int TickCount => tickCount;

    public StatusEffectData(
        StatusEffectType effectType,
        float duration,
        float value,
        float tickInterval = 1f,
        int attackModifier = 0,
        int defenseModifier = 0)
    {
        EffectType = effectType;

        TotalDuration = duration;
        Duration = duration;

        Value = value;
        TickInterval = tickInterval;

        AttackModifier = attackModifier;
        DefenseModifier = defenseModifier;

        elapsedTime = 0f;
        tickCount = 0;
    }

    public void DecreaseDuration(float deltaTime)
    {
        elapsedTime += deltaTime;

        if (elapsedTime > TotalDuration)
        {
            elapsedTime = TotalDuration;
        }

        Duration = TotalDuration - elapsedTime;
    }

    public bool IsExpired()
    {
        return Duration <= 0f;
    }

    // 이번 프레임에 몇 번의 틱 데미지를 적용해야 하는지 계산
    public int GetReadyTickCount(float deltaTime)
    {
        if (TickInterval <= 0f)
        {
            return 0;
        }

        float beforeElapsedTime = elapsedTime;
        float afterElapsedTime = elapsedTime + deltaTime;

        if (afterElapsedTime > TotalDuration)
        {
            afterElapsedTime = TotalDuration;
        }

        int beforeTickIndex = Mathf.FloorToInt(beforeElapsedTime / TickInterval);
        int afterTickIndex = Mathf.FloorToInt(afterElapsedTime / TickInterval);

        int readyTickCount = afterTickIndex - beforeTickIndex;

        return readyTickCount;
    }

    // 틱 데미지가 실제로 1번 적용되었을 때 호출
    public void IncreaseTickCount()
    {
        tickCount++;
    }
}