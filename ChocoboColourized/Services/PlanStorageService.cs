using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ChocoboColourized.Models;

namespace ChocoboColourized.Services;

/// <summary>
/// Handles reading/writing per-character plan and timer data to JSON.
/// Follows standard FFXIV plugin storage conventions using the plugin config directory.
/// </summary>
public class PlanStorageService : IDisposable
{
    private readonly string _filePath;
    private readonly IPluginLog _log;
    private PluginPersistentData _data;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PlanStorageService(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        _log = log;
        var configDir = pluginInterface.GetPluginConfigDirectory();
        Directory.CreateDirectory(configDir);
        _filePath = Path.Combine(configDir, "chocobo_plans.json");
        _data = Load();
    }

    /// <summary>Get or create data for a specific character.</summary>
    public CharacterPlanData GetCharacterData(string characterName, string worldName)
    {
        var key = $"{characterName}@{worldName}";
        if (!_data.Characters.TryGetValue(key, out var data))
        {
            data = new CharacterPlanData
            {
                CharacterName = characterName,
                WorldName = worldName,
            };
            _data.Characters[key] = data;
        }
        return data;
    }

    /// <summary>Get all character data entries (for timer list display).</summary>
    public IReadOnlyDictionary<string, CharacterPlanData> GetAllCharacters() => _data.Characters;

    /// <summary>Save a feeding plan for the current character.</summary>
    public void SavePlan(string characterName, string worldName, FeedingPlan plan)
    {
        var data = GetCharacterData(characterName, worldName);
        data.ActivePlan = plan;
        Save();
        _log.Information($"Saved feeding plan for {characterName}@{worldName}: {plan.TotalFruits} fruits");
    }

    /// <summary>Record that one fruit was successfully fed. Increments the plan index.</summary>
    public void RecordFruitFed(string characterName, string worldName)
    {
        var data = GetCharacterData(characterName, worldName);
        if (data.ActivePlan != null && !data.ActivePlan.IsComplete)
        {
            data.ActivePlan.NextFruitIndex++;
            if (data.ActivePlan.IsComplete)
            {
                CompletePlan(characterName, worldName);
            }
            else
            {
                Save();
            }
        }
    }

    /// <summary>
    /// Mark a plan as complete: remove the plan and start the 6-hour timer.
    /// </summary>
    public void CompletePlan(string characterName, string worldName)
    {
        var data = GetCharacterData(characterName, worldName);
        data.ActivePlan = null;
        data.TimerExpiresUtc = DateTime.UtcNow.AddHours(6);
        Save();
        _log.Information($"Plan completed for {characterName}@{worldName}. Timer set for 6 hours.");
    }

    /// <summary>Clear a plan without starting a timer (cancel).</summary>
    public void ClearPlan(string characterName, string worldName)
    {
        var data = GetCharacterData(characterName, worldName);
        data.ActivePlan = null;
        Save();
    }

    /// <summary>Update the chocobo name for a character.</summary>
    public void SetChocoboName(string characterName, string worldName, string chocoboName)
    {
        var data = GetCharacterData(characterName, worldName);
        data.ChocoboName = chocoboName;
        Save();
    }

    /// <summary>Clear expired timers (housekeeping).</summary>
    public void CleanupExpiredTimers()
    {
        var keysToRemove = new List<string>();
        foreach (var kvp in _data.Characters)
        {
            if (kvp.Value.TimerExpiresUtc.HasValue && !kvp.Value.IsTimerActive && kvp.Value.ActivePlan == null)
            {
                // Timer expired and no active plan — can keep for history or remove
                // For now, clear the timer but keep the entry
                kvp.Value.TimerExpiresUtc = null;
            }
        }
        Save();
    }

    /// <summary>Persist current state to disk.</summary>
    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to save plan data: {ex.Message}");
        }
    }

    private PluginPersistentData Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<PluginPersistentData>(json) ?? new PluginPersistentData();
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to load plan data: {ex.Message}");
        }
        return new PluginPersistentData();
    }

    public void Dispose() { }
}
