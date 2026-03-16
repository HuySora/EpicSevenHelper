using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public partial class Equipment {
    public int Rank { get; private set; }
    public Stat MainStat { get; private set; }
    public List<Stat> SubStatList { get; private set; } = new();

    public bool TryParse(string? input) {
        if (string.IsNullOrWhiteSpace(input)) {
            return false;
        }

        // Clear old stats
        Rank = -1;
        MainStat = new Stat(StatType.Null, -1m, -1);
        SubStatList.Clear();
        // Define the regex pattern to match stat names and values
        /* Example input:
        > Speed 22 «18
        Critical Hit Chance (1) 9% (+4%)
        Attack (1) 72
        Attack (1) 13%
        Critical Hit Damage 5%
        or
        > Speed22 «18
        CriticalHitChance(1)9%(+4%)
        Attack(1)72
        Attack(1)13%
        CriticalHitDamage5%
        */
        // Clean up invalid characters first (non-alphanumeric characters except for...)
        input = Regex.Replace(input, "[^a-zA-Z0-9()%\t\r\n ]", "");
        // Matching into groups
        string pattern =
            @"(?<statType>(Health|Attack|Defense|Speed|Critical\s*Hit\s*Chance|Critical\s*Hit\s*Damage|Effect\s*Resistance|Effectiveness))" + // Stat type
            @"\s*" + // Spaces
            @"(?<rollCount>\(\d+\))?" + // Roll count (optional)
            @"\s*" + // Spaces
            @"(?<value>\d+%?)" + // Capture the main value
            @"\s*" + // Spaces
            @"(?<additionalValue>\(\+\d+%\))?"; // Additional value (midway enhancing)
        MatchCollection matches = Regex.Matches(input, pattern);

        var statList = new List<(StatType Type, decimal Value, int RollCount)>();
        // Parse checking first (early return)
        foreach (Match match in matches) {
            string statType = match.Groups["statType"].Value;
            string rollCount = match.Groups["rollCount"].Value;
            string value = match.Groups["value"].Value;

            // Parsed values
            int outInt;
            string trimmedRollCount;
            string trimmedValue;
            StatType parsedType = StatType.Null;
            decimal parsedValue = 0;
            int parsedRollCount = 0;

            // Parsed roll count
            trimmedRollCount = rollCount.Trim().TrimStart('(').TrimEnd(')');
            if (!string.IsNullOrEmpty(trimmedRollCount)) {
                parsedRollCount = int.TryParse(trimmedRollCount, out outInt)
                    ? outInt
                    : -1;
            }
            else {
                parsedRollCount = 0;
            }

            // Parse type and value
            bool isPercent;
            switch (statType) {
                case "Attack":
                    isPercent = value.EndsWith('%');
                    trimmedValue = isPercent ? value.TrimEnd('%') : value;

                    parsedType = isPercent ? StatType.AttackPercent : StatType.Attack;
                    parsedValue = int.TryParse(trimmedValue, out outInt)
                        ? isPercent ? outInt / 100m : outInt
                        : -1;
                    break;
                case "Health":
                    isPercent = value.EndsWith('%');
                    trimmedValue = isPercent ? value.TrimEnd('%') : value;

                    parsedType = isPercent ? StatType.HealthPercent : StatType.Health;
                    parsedValue = int.TryParse(trimmedValue, out outInt)
                        ? isPercent ? outInt / 100m : outInt
                        : -1;
                    break;
                case "Defense":
                    isPercent = value.EndsWith('%');
                    trimmedValue = isPercent ? value.TrimEnd('%') : value;

                    parsedType = isPercent ? StatType.DefensePercent : StatType.Defense;
                    parsedValue = int.TryParse(trimmedValue, out outInt)
                        ? isPercent ? outInt / 100m : outInt
                        : -1;
                    break;
                case "Speed":
                    parsedType = StatType.Speed;
                    parsedValue = int.TryParse(value, out outInt)
                        ? outInt
                        : -1;
                    break;
                case "Effectiveness":
                    parsedType = StatType.EffectivenessPercent;
                    parsedValue = int.TryParse(value.TrimEnd('%'), out outInt)
                        ? outInt / 100m
                        : -1;
                    break;
                case "Effect Resistance":
                case "EffectResistance":
                    parsedType = StatType.EffectResistancePercent;
                    parsedValue = int.TryParse(value.TrimEnd('%'), out outInt)
                        ? outInt / 100m
                        : -1;
                    break;
                case "Critical Hit Chance":
                case "CriticalHitChance":
                    parsedType = StatType.CriticalHitChancePercent;
                    parsedValue = int.TryParse(value.TrimEnd('%'), out outInt)
                        ? outInt / 100m
                        : -1;
                    break;
                case "Critical Hit Damage":
                case "CriticalHitDamage":
                    parsedType = StatType.CriticalHitDamagePercent;
                    parsedValue = int.TryParse(value.TrimEnd('%'), out outInt)
                        ? outInt / 100m
                        : -1;
                    break;
            }

            statList.Add((parsedType, parsedValue, parsedRollCount));
        }

        if (statList.Count == 0) {
            return false;
        }

        // Process each match (the first match is the main stat)
        MainStat = new Stat(statList[0].Type, statList[0].Value, statList[0].RollCount);
        for (var i = 1; i < statList.Count; i++) {
            var values = statList[i];
            SubStatList.Add(
                new Stat(
                    values.Type,
                    values.Value,
                    values.RollCount
                )
            );
        }

        // Based on main stat calculate equipment rank (enhanced level)
        if (Constants.StatTypeValue2EquipmentRank.TryGetValue((MainStat.Type, MainStat.Value), out int outRank)) {
            Rank = outRank;
        }

        return true;
    }

    public override string ToString() {
        var obj = new JObject {
            // Rank
            ["rank"] = Rank,
            // Main stat
            ["mainStat"] = new JObject {
                ["type"] = MainStat.Type.ToString(), ["value"] = MainStat.Value, ["rollCount"] = MainStat.RollCount
            },
            // Sub stats
            ["subStats"] = new JArray(
                SubStatList.Select(subStat => new JObject {
                        ["type"] = subStat.Type.ToString(), ["value"] = subStat.Value, ["rollCount"] = subStat.RollCount
                    }
                )
            )
        };
        return JsonConvert.SerializeObject(obj);
    }
}

public partial class Equipment : IComparable<Equipment> {
    public int CompareTo(Equipment other) {
        if (ReferenceEquals(other, null)) return 1;

        int rankComparison = Rank.CompareTo(other.Rank);
        if (rankComparison != 0) return rankComparison;

        int mainStatComparison = MainStat.Value.CompareTo(other.MainStat.Value);
        if (mainStatComparison != 0) return mainStatComparison;

        // TODO: Fast way to check for sub stats difference
        decimal gs = SubStatList.Sum(stat => stat.GetGearScore());
        decimal otherGs = other.SubStatList.Sum(stat => stat.GetGearScore());
        int gsComparison = gs.CompareTo(otherGs);
        if (gsComparison != 0) return gsComparison;

        return 0;
    }
}