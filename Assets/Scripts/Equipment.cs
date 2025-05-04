using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class Equipment {
    public int Rank { get; private set; }
    public Stat MainStat { get; private set; }
    public List<Stat> Stats { get; private set; } = new List<Stat>();

    public bool TryParse(string input) {
        if (string.IsNullOrWhiteSpace(input)) {
            return false;
        }

        input = input.Trim().Replace(",", string.Empty);

        // Clear old stats
        Rank = -1;
        Stats.Clear();

        // Define the regex pattern to match stat names and values
        string pattern =
            @"(?<statType>(Health|Attack|Defense|Speed|Critical Hit Chance|Critical Hit Damage|Effect Resistance|Effectiveness))" + // Stat type
            @"\s*(?<rollCount>\(\d+\)\s*)?" + // Roll count
            @"(?<value>\d+%?|\d+)" + // Capture the main value
            @"\s*(?<additionalValue>\(\+\d+%\))?"; // Capture the additional value in parentheses
        // Find matches in the input string
        MatchCollection matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
        
        var statList = new List<(StatType Type, decimal Value, int RollCount)>();
        // Parse checking first (early return)
        foreach (Match match in matches) {
            string statType = match.Groups["statType"].Value;
            string rollCount = match.Groups["rollCount"].Value;
            string value = match.Groups["value"].Value;

            // Parsed values
            StatType parsedType = StatType.Null;
            decimal parsedValue = 0;
            int parsedRollCount = 0;

            // Parsed roll count
            string trimmedValue = rollCount.Trim().TrimStart('(').TrimEnd(')');
            if (!string.IsNullOrEmpty(trimmedValue)) {
                if (!int.TryParse(trimmedValue, out parsedRollCount)) {
                    return false;
                }
            }
            else {
                parsedRollCount = 0;
            }

            // Parse type and value
            int outInt = 0;
            switch (statType) {
                case "Attack":
                    if (value.EndsWith('%')) {
                        if (!int.TryParse(value.TrimEnd('%'), out outInt)) {
                            return false;
                        }

                        parsedType = StatType.AttackPercent;
                        parsedValue = outInt / 100m;
                    }
                    else {
                        if (!int.TryParse(value, out outInt)) {
                            return false;
                        }

                        parsedType = StatType.Attack;
                        parsedValue = outInt;
                    }

                    break;
                case "Health":
                    if (value.EndsWith('%')) {
                        if (!int.TryParse(value.TrimEnd('%'), out outInt)) {
                            return false;
                        }

                        parsedType = StatType.HealthPercent;
                        parsedValue = outInt / 100m;
                    }
                    else {
                        if (!int.TryParse(value, out outInt)) {
                            return false;
                        }

                        parsedType = StatType.Health;
                        parsedValue = outInt;
                    }

                    break;
                case "Defense":
                    if (value.EndsWith('%')) {
                        if (!int.TryParse(value.TrimEnd('%'), out outInt)) {
                            return false;
                        }

                        parsedType = StatType.DefensePercent;
                        parsedValue = outInt / 100m;
                    }
                    else {
                        if (!int.TryParse(value, out outInt)) {
                            return false;
                        }

                        parsedType = StatType.Defense;
                        parsedValue = outInt;
                    }

                    break;
                case "Speed":
                    if (!int.TryParse(value, out outInt)) {
                        return false;
                    }

                    parsedType = StatType.Speed;
                    parsedValue = outInt;

                    break;
                case "Effectiveness":
                    if (!int.TryParse(value.TrimEnd('%'), out outInt)) {
                        return false;
                    }

                    parsedType = StatType.EffectivenessPercent;
                    parsedValue = outInt / 100m;

                    break;
                case "Effect Resistance":
                    if (!int.TryParse(value.TrimEnd('%'), out outInt)) {
                        return false;
                    }

                    parsedType = StatType.EffectResistancePercent;
                    parsedValue = outInt / 100m;

                    break;
                case "Critical Hit Chance":
                    if (!int.TryParse(value.TrimEnd('%'), out outInt)) {
                        return false;
                    }

                    parsedType = StatType.CriticalHitChancePercent;
                    parsedValue = outInt / 100m;

                    break;
                case "Critical Hit Damage":
                    if (!int.TryParse(value.TrimEnd('%'), out outInt)) {
                        return false;
                    }

                    parsedType = StatType.CriticalHitDamagePercent;
                    parsedValue = outInt / 100m;

                    break;
            }

            statList.Add((parsedType, parsedValue, parsedRollCount));
        }
        if (statList.Count == 0) {
            return false;
        }
        
        // Process each match (first match is main stat)
        MainStat = new Stat(statList[0].Type, statList[0].Value, statList[0].RollCount);
        for (var i = 1; i < statList.Count; i++) {
            var values = statList[i];
            Stats.Add(new Stat(
                values.Type,
                values.Value,
                values.RollCount
            ));
        }
        
        // Based on main stat calculate equipment rank (enhanced level)
        if (Constants.StatTypeValue2EquipmentRank.TryGetValue((MainStat.Type, MainStat.Value), out int outRank)) {
            Rank = outRank;
        }
        
        return true;
    }
}