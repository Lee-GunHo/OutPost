public enum StatusEffectType
{
    None,

    // 상태이상
    Bleed,       // 출혈
    Poison,         // 중독
    Burn,           // 화상
    Frozen,         // 동상
    Stun,           // 기절
    Blind,          // 시야방해
    Hunger,         // 배고픔

    // 버프
    AttackUp,       // 공격력 증가
    MaxHpUp,        // 체력 증가
    MoveSpeedUp,    // 이동속도 증가
    HpRegenUp,      // 체력재생 증가
    DefenseUp       // 방어력 증가
}