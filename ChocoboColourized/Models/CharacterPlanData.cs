using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ChocoboColourized.Core;

namespace ChocoboColourized.Models;

/// <summary>
/// Represents a single character's feeding plan and timer data.
/// Stored per-character in JSON.
/// </summary>
public class CharacterPlanData
{
    /// <summary>Character name (e.g., "Firstname Lastname").</summary>
    public string CharacterName { get; set; } = "";

    /// <summary>Character's home world name.</summary>
    public string WorldName { get; set; } = "";

    /// <summary>Chocobo's name as displayed in-game.</summary>
    public string ChocoboName { get; set; } = "";

    /// <summary>Active feeding plan, if any. Null when no plan is active.</summary>
    public FeedingPlan? ActivePlan { get; set; }

    /// <summary>
    /// UTC timestamp when the 6-hour colour change timer expires.
    /// Null if no timer is active.
    /// </summary>
    public DateTime? TimerExpiresUtc { get; set; }

    /// <summary>Unique key: "CharacterName@WorldName".</summary>
    [JsonIgnore]
    public string Key => $"{CharacterName}@{WorldName}";

    /// <summary>Check if the 6-hour timer is still counting down.</summary>
    [JsonIgnore]
    public bool IsTimerActive => TimerExpiresUtc.HasValue && DateTime.UtcNow < TimerExpiresUtc.Value;

    /// <summary>Remaining time on the timer, or TimeSpan.Zero if expired.</summary>
    [JsonIgnore]
    public TimeSpan TimerRemaining => IsTimerActive
        ? TimerExpiresUtc!.Value - DateTime.UtcNow
        : TimeSpan.Zero;
}

/// <summary>
/// A feeding plan: the calculated list of fruits to feed in order,
/// with tracking for partial completion.
/// </summary>
public class FeedingPlan
{
    /// <summary>Name of the starting colour.</summary>
    public string StartColorName { get; set; } = "";

    /// <summary>Name of the target colour.</summary>
    public string TargetColorName { get; set; } = "";

    /// <summary>Ordered list of fruit type names to feed.</summary>
    public List<string> FruitOrder { get; set; } = new();

    /// <summary>Index of the next fruit to feed (0-based). Incremented after each successful feed.</summary>
    public int NextFruitIndex { get; set; } = 0;

    /// <summary>When this plan was created (UTC).</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Total fruits in the plan.</summary>
    [JsonIgnore]
    public int TotalFruits => FruitOrder.Count;

    /// <summary>Number of fruits already fed.</summary>
    [JsonIgnore]
    public int FruitsFed => NextFruitIndex;

    /// <summary>Number of fruits remaining.</summary>
    [JsonIgnore]
    public int FruitsRemaining => TotalFruits - NextFruitIndex;

    /// <summary>Whether the plan is fully completed.</summary>
    [JsonIgnore]
    public bool IsComplete => NextFruitIndex >= TotalFruits;

    /// <summary>Get remaining fruit counts as a summary dictionary.</summary>
    [JsonIgnore]
    public Dictionary<string, int> RemainingFruitCounts
    {
        get
        {
            var counts = new Dictionary<string, int>();
            for (var i = NextFruitIndex; i < FruitOrder.Count; i++)
            {
                var name = FruitOrder[i];
                if (counts.ContainsKey(name))
                    counts[name]++;
                else
                    counts[name] = 1;
            }
            return counts;
        }
    }

    /// <summary>Create a FeedingPlan from a CalculationResult.</summary>
    public static FeedingPlan FromCalculationResult(CalculationResult result)
    {
        var plan = new FeedingPlan
        {
            StartColorName = result.StartColor.Name,
            TargetColorName = result.TargetColor.Name,
            CreatedUtc = DateTime.UtcNow,
        };
        foreach (var fruit in result.Fruits)
        {
            plan.FruitOrder.Add(FruitData.GetDisplayName(fruit));
        }
        return plan;
    }
}

/// <summary>
/// Root object for the JSON file: maps character keys to their data.
/// </summary>
public class PluginPersistentData
{
    public Dictionary<string, CharacterPlanData> Characters { get; set; } = new();
}
