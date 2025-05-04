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
    CriticalHitDamagePercent,
}

public class Stat {
    public StatType Type { get; private set; }
    public decimal Value { get; private set; }
    public int RollCount { get; private set; }

    public Stat(StatType type, decimal value, int rollCount) {
        Type = type;
        Value = value;
        RollCount = rollCount;
    }

	public decimal GetGearScore() {
        if (!Constants.StatType2GearScoreMultiplier.TryGetValue(Type, out decimal mul)) {
            return 0;
        }
        return Value * mul;
    }
}