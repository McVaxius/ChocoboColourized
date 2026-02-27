using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using ChocoboColourized.Core;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace ChocoboColourized.Services;

/// <summary>
/// Handles reading game data: inventory counts, character info, chocobo colour.
/// </summary>
public class GameDataService : IDisposable
{
    private readonly IClientState _clientState;
    private readonly IObjectTable _objectTable;
    private readonly IPluginLog _log;

    public GameDataService(IClientState clientState, IObjectTable objectTable, IPluginLog log)
    {
        _clientState = clientState;
        _objectTable = objectTable;
        _log = log;
    }

    /// <summary>Current character name, or empty if not logged in.</summary>
    public string CharacterName => _objectTable.LocalPlayer?.Name.ToString() ?? "";

    /// <summary>Current character's home world name.</summary>
    public string WorldName => _objectTable.LocalPlayer?.HomeWorld.Value.Name.ToString() ?? "";

    /// <summary>Whether the player is currently logged in.</summary>
    public bool IsLoggedIn => _clientState.IsLoggedIn;

    /// <summary>
    /// Get the inventory count for a specific fruit type.
    /// Uses InventoryManager.GetInventoryItemCount which searches all player inventories.
    /// Pattern from SND (SomethingNeedDoing) InventoryModule.
    /// </summary>
    public unsafe int GetFruitCount(FruitType fruit)
    {
        try
        {
            var itemId = FruitData.GetItemId(fruit);
            var manager = InventoryManager.Instance();
            if (manager == null) return 0;

            // GetInventoryItemCount searches all relevant inventory containers automatically.
            // Second param is isHq (false for fruits), third is checkEquipped, fourth is checkArmory.
            return manager->GetInventoryItemCount(itemId);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to read inventory for {FruitData.GetDisplayName(fruit)}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Get inventory counts for all fruit types.
    /// Returns a dictionary mapping fruit type to quantity owned.
    /// </summary>
    public Dictionary<FruitType, int> GetAllFruitCounts()
    {
        var counts = new Dictionary<FruitType, int>();
        foreach (var fruit in FruitData.AllFruits)
        {
            counts[fruit] = GetFruitCount(fruit);
        }
        return counts;
    }

    /// <summary>
    /// Check if the player has enough of each fruit for the given plan.
    /// Returns true if all required fruits are available.
    /// </summary>
    public bool HasEnoughFruits(Dictionary<FruitType, int> required)
    {
        foreach (var kvp in required)
        {
            if (GetFruitCount(kvp.Key) < kvp.Value)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Get the count of any item by ID.
    /// Used for checking Magicked Stable Broom (8168) etc.
    /// </summary>
    public unsafe int GetItemCount(uint itemId)
    {
        try
        {
            var manager = InventoryManager.Instance();
            if (manager == null) return 0;
            return manager->GetInventoryItemCount(itemId);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to read inventory for item {itemId}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Find the inventory slot containing a specific item.
    /// Returns (InventoryType, slotIndex) or null if not found.
    /// Used by FeedingAutomationService to locate fruits for context menu interaction.
    /// </summary>
    public unsafe (InventoryType container, int slot)? FindItemSlot(uint itemId)
    {
        try
        {
            var manager = InventoryManager.Instance();
            if (manager == null) return null;

            var containers = new[]
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4,
            };

            foreach (var containerType in containers)
            {
                var container = manager->GetInventoryContainer(containerType);
                if (container == null) continue;

                for (int i = 0; i < container->Size; i++)
                {
                    var slot = container->GetInventorySlot(i);
                    if (slot != null && slot->ItemId == itemId)
                        return (containerType, i);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to find item slot for {itemId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Attempt to read the current chocobo colour from game data.
    /// Returns null if unable to determine (user will need to select manually).
    /// 
    /// NOTE: Chocobo colour is stored in the companion module but the exact struct
    /// offset varies by game version. This attempts to read it but falls back gracefully.
    /// </summary>
    public string? TryGetChocoboColor()
    {
        try
        {
            // The chocobo stain/colour is stored in the companion data.
            // This is a best-effort attempt — if the struct layout changes,
            // this will return null and the user selects manually.
            // TODO: Implement when FFXIVClientStructs companion colour offset is confirmed.
            return null;
        }
        catch (Exception ex)
        {
            _log.Debug($"Could not read chocobo colour: {ex.Message}");
            return null;
        }
    }

    public void Dispose() { }
}
