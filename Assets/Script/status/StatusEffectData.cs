public class StatusEffectData
{
    public StatusEffectType EffectType { get; private set; }
    public float Duration { get; private set; }
    public float Value { get; private set; }

    public StatusEffectData(StatusEffectType effectType, float duration, float value)
    {
        EffectType = effectType;
        Duration = duration;
        Value = value;
    }

    public void DecreaseDuration(float deltaTime)
    {
        Duration -= deltaTime;
    }

    public bool IsExpired()
    {
        return Duration <= 0f;
    }
}