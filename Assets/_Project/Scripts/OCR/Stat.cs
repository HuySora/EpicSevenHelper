public enum StatType {
    Null = 0,
    Attack = 1,
    AttackPercent,
    Defense,
    DefensePercent,
    Health,
    HealthPercent,
    Speed,
    EffectivenessPercent,
    EffectResistancePercent,
    CriticalHitChancePercent,
    CriticalHitDamagePercent
}

public struct Stat {
    public Stat(StatType type, decimal value, int rollCount) {
        Type = type;
        Value = value;
        RollCount = rollCount;
    }

    public StatType Type { get; }
    public decimal Value { get; }
    public int RollCount { get; private set; }

    public decimal GetGearScore() {
        if (!Constants.StatType2GearScoreMultiplier.TryGetValue(Type, out var mul))
            return -1;

        return Value * mul;
    }
    public decimal GetReforgedGearScore() {
        if (!Constants.StatType2GearScoreMultiplier.TryGetValue(Type, out var mul))
            return -1;
        if (!Constants.StatTypeRollCount2ReforgedBonusValue.TryGetValue((Type, RollCount), out var bonus))
            return -1;

        return (Value + bonus) * mul;
    }
}