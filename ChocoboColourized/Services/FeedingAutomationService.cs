using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ChocoboColourized.Core;
using ChocoboColourized.Models;

namespace ChocoboColourized.Services;

/// <summary>
/// State machine for automated chocobo fruit feeding.
/// The actual FFXIV stable feeding UI shows the player's inventory with feedable items
/// highlighted. You right-click a fruit and select "Feed" from the context menu.
/// This service automates that flow using AgentInventoryContext and addon callbacks.
/// </summary>
public enum FeedingState
{
    Idle,
    FindingFruitSlot,
    OpeningContextMenu,
    WaitingForContextMenu,
    SelectingFeed,
    WaitingForStableMenu,
    SelectingFeedFromMenu,
    WaitingForFeedInventory,
    Completed,
    Error,
}

public class FeedingAutomationService : IDisposable
{
    private readonly IGameGui _gameGui;
    private readonly IFramework _framework;
    private readonly IPluginLog _log;
    private readonly IpcService _ipcService;
    private readonly PlanStorageService _planStorage;
    private readonly GameDataService _gameData;

    private FeedingState _state = FeedingState.Idle;
    private FeedingPlan? _activePlan;
    private string _characterName = "";
    private string _worldName = "";
    private DateTime _stateEnteredAt;
    private string _errorMessage = "";
    private int _retryCount;
    private int _debugTickCounter;

    // Current item being fed
    private InventoryType _currentContainer;
    private int _currentSlot;

    // Timeouts
    private const float StateTimeoutSec = 10f;
    private const float MenuTimeoutSec = 15f;  // Stable menu can take 5+ seconds to appear
    private const float PreContextMenuDelaySec = 0.5f;
    private const int MaxRetries = 3;

    // Stable menu: In-game title "Personal Chocobo", addon from /xldata: "HousingMyChocobo"
    // The menu list (Train/Feed/Change Name/Fetch/View Details/Quit) may appear as a
    // separate "SelectString" addon or be embedded in HousingMyChocobo.
    // We check BOTH and prefer SelectString for clicking (proven by YesAlready/ECommons).
    private const int StableMenuFeedIndex = 1;  // Feed is index 1 in the list
    private const string SelectStringAddon = "SelectString";
    private const string HousingChocoboAddon = "HousingMyChocobo";

    // Addon names
    private const string ContextMenuAddon = "ContextMenu";

    // Inventory addon names to search for (different inventory layouts)
    private static readonly string[] InventoryAddonNames =
    {
        "InventoryExpansion",
        "InventoryLarge",
        "InventoryGrid0E", "InventoryGrid1E", "InventoryGrid2E", "InventoryGrid3E",
        "InventoryGrid0", "InventoryGrid1", "InventoryGrid2", "InventoryGrid3",
        "InventoryBuddy", "InventoryBuddy2",
    };

    public FeedingAutomationService(
        IGameGui gameGui,
        IFramework framework,
        IPluginLog log,
        IpcService ipcService,
        PlanStorageService planStorage,
        GameDataService gameData)
    {
        _gameGui = gameGui;
        _framework = framework;
        _log = log;
        _ipcService = ipcService;
        _planStorage = planStorage;
        _gameData = gameData;
    }

    public FeedingState State => _state;

    public bool IsRunning => _state != FeedingState.Idle &&
                             _state != FeedingState.Completed &&
                             _state != FeedingState.Error;

    public string ErrorMessage => _errorMessage;

    public int CurrentFruitIndex => _activePlan?.NextFruitIndex ?? 0;

    public int TotalFruits => _activePlan?.TotalFruits ?? 0;

    public string CurrentFruitName
    {
        get
        {
            if (_activePlan == null || _activePlan.IsComplete) return "";
            return _activePlan.FruitOrder[_activePlan.NextFruitIndex];
        }
    }

    /// <summary>
    /// Start automated feeding. User must already be in the stable's Feed inventory screen.
    /// </summary>
    public bool Start(FeedingPlan plan, string characterName, string worldName)
    {
        if (IsRunning)
        {
            _errorMessage = "Automation is already running.";
            return false;
        }

        if (plan.IsComplete)
        {
            _errorMessage = "Plan is already complete.";
            return false;
        }

        _activePlan = plan;
        _characterName = characterName;
        _worldName = worldName;
        _errorMessage = "";
        _retryCount = 0;

        _ipcService.PauseExternalPlugins();
        _framework.Update += OnFrameworkTick;

        // User is already in the feed inventory screen for the first feed
        TransitionTo(FeedingState.FindingFruitSlot);
        _log.Information($"Feeding automation started. {plan.FruitsRemaining} fruits remaining.");
        return true;
    }

    public void Stop()
    {
        if (!IsRunning && _state != FeedingState.Completed && _state != FeedingState.Error) return;

        _framework.Update -= OnFrameworkTick;
        _ipcService.ResumeExternalPlugins();

        if (_state != FeedingState.Completed)
        {
            _log.Information($"Feeding stopped. Fed {_activePlan?.FruitsFed ?? 0}/{_activePlan?.TotalFruits ?? 0}.");
        }

        TransitionTo(FeedingState.Idle);
    }

    public void Reset()
    {
        _framework.Update -= OnFrameworkTick;
        _ipcService.ResumeExternalPlugins();
        _state = FeedingState.Idle;
        _activePlan = null;
        _errorMessage = "";
    }

    private void TransitionTo(FeedingState newState)
    {
        _log.Debug($"Feeding state: {_state} -> {newState}");
        _state = newState;
        _stateEnteredAt = DateTime.UtcNow;
    }

    private float ElapsedInState => (float)(DateTime.UtcNow - _stateEnteredAt).TotalSeconds;

    private void OnFrameworkTick(IFramework framework)
    {
        try
        {
            switch (_state)
            {
                case FeedingState.FindingFruitSlot:
                    HandleFindingFruitSlot();
                    break;
                case FeedingState.OpeningContextMenu:
                    HandleOpeningContextMenu();
                    break;
                case FeedingState.WaitingForContextMenu:
                    HandleWaitingForContextMenu();
                    break;
                case FeedingState.SelectingFeed:
                    HandleSelectingFeed();
                    break;
                case FeedingState.WaitingForStableMenu:
                    HandleWaitingForStableMenu();
                    break;
                case FeedingState.SelectingFeedFromMenu:
                    HandleSelectingFeedFromMenu();
                    break;
                case FeedingState.WaitingForFeedInventory:
                    HandleWaitingForFeedInventory();
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Feeding automation tick error: {ex}");
            SetError($"Unexpected error: {ex.Message}");
        }
    }

    // --- State handlers ---

    private unsafe void HandleFindingFruitSlot()
    {
        if (_activePlan == null || _activePlan.IsComplete)
        {
            HandlePlanComplete();
            return;
        }

        var fruitName = _activePlan.FruitOrder[_activePlan.NextFruitIndex];
        var fruitType = FruitNameToType(fruitName);
        if (fruitType == null)
        {
            SetError($"Unknown fruit type: {fruitName}");
            return;
        }

        var itemId = FruitData.GetItemId(fruitType.Value);
        var slot = _gameData.FindItemSlot(itemId);
        if (slot == null)
        {
            SetError($"Could not find {fruitName} (ID:{itemId}) in inventory. Do you have it?");
            return;
        }

        _currentContainer = slot.Value.container;
        _currentSlot = slot.Value.slot;

        _log.Information($"[{_activePlan.NextFruitIndex + 1}/{_activePlan.TotalFruits}] Found {fruitName} at {_currentContainer} slot {_currentSlot}");
        TransitionTo(FeedingState.OpeningContextMenu);
    }

    private unsafe void HandleOpeningContextMenu()
    {
        // Small delay before opening context menu to avoid race conditions
        if (ElapsedInState < PreContextMenuDelaySec) return;

        try
        {
            // Find a visible inventory addon to get its ID
            uint addonId = GetVisibleInventoryAddonId();
            _log.Debug($"Opening context menu on {_currentContainer} slot {_currentSlot}, addonId={addonId}");

            var agent = AgentInventoryContext.Instance();
            agent->OpenForItemSlot(_currentContainer, _currentSlot, 0, addonId);

            TransitionTo(FeedingState.WaitingForContextMenu);
        }
        catch (Exception ex)
        {
            _log.Warning($"Error opening context menu: {ex}");
            if (++_retryCount > MaxRetries)
                SetError($"Failed to open context menu after {MaxRetries} retries: {ex.Message}");
            else
                TransitionTo(FeedingState.FindingFruitSlot);
        }
    }

    private void HandleWaitingForContextMenu()
    {
        if (IsAddonVisible(ContextMenuAddon))
        {
            TransitionTo(FeedingState.SelectingFeed);
        }
        else if (ElapsedInState > StateTimeoutSec)
        {
            _log.Warning("Context menu did not appear.");
            if (++_retryCount > MaxRetries)
                SetError("Context menu did not appear. Ensure you are in the stable's Feed screen with your inventory open.");
            else
                TransitionTo(FeedingState.FindingFruitSlot);
        }
    }

    private unsafe void HandleSelectingFeed()
    {
        var addon = GetAddon(ContextMenuAddon);
        if (addon == null)
        {
            if (ElapsedInState > StateTimeoutSec)
                SetError("Context menu disappeared before we could select Feed.");
            return;
        }

        try
        {
            // "Feed" is the first option (index 0) in the context menu when in stable feed mode
            _log.Debug("Clicking 'Feed' (index 0) in context menu");
            ClickContextMenu(addon, 0);
            // No confirmation dialog — feed happens immediately, animation plays,
            // then the stable SelectString menu reappears.
            TransitionTo(FeedingState.WaitingForStableMenu);
        }
        catch (Exception ex)
        {
            _log.Warning($"Error clicking Feed: {ex}");
            SetError($"Failed to select Feed from context menu: {ex.Message}");
        }
    }

    private void RecordFeedSuccess()
    {
        if (_activePlan == null) return;

        var fruitName = CurrentFruitName;
        _planStorage.RecordFruitFed(_characterName, _worldName);
        _retryCount = 0;

        _log.Information($"Fed fruit {_activePlan.FruitsFed}/{_activePlan.TotalFruits}: {fruitName}");
    }

    // --- Post-feed stable menu loop ---

    private unsafe void HandleWaitingForStableMenu()
    {
        // After clicking Feed in context menu, the fruit feeds immediately (animation plays),
        // then the stable menu reappears. This can take 5+ seconds.
        // The menu might appear as SelectString addon or be part of HousingMyChocobo.

        // Comprehensive debug logging every ~30 ticks (~0.5 second)
        if (++_debugTickCounter % 30 == 0)
        {
            nint ssPtr = _gameGui.GetAddonByName(SelectStringAddon);
            nint hmcPtr = _gameGui.GetAddonByName(HousingChocoboAddon);
            nint talkPtr = _gameGui.GetAddonByName("Talk");
            _log.Debug($"WaitForMenu: elapsed={ElapsedInState:F1}s, SelectString=0x{ssPtr:X}, HousingMyChocobo=0x{hmcPtr:X}, Talk=0x{talkPtr:X}");
        }

        // After feeding, the game may still show a Talk dialog (e.g. "You feed your chocobo...").
        // The buddy-feed scene hook should suppress the normal feed scene, but keep the
        // manual Talk dismiss fallback in case the client still surfaces a dialog here.
        if (IsTalkAddonVisible())
        {
            DismissTalkAddon();
            return;
        }

        // Check for BOTH possible menu addons.
        // SelectString = the standard list-selection addon (used by YesAlready)
        // HousingMyChocobo = the stable window container (found via /xldata)
        bool selectStringFound = AddonExists(SelectStringAddon);
        bool housingChocoboFound = AddonExists(HousingChocoboAddon);

        if (selectStringFound || housingChocoboFound)
        {
            _log.Debug($"Stable menu detected! SelectString={selectStringFound}, HousingMyChocobo={housingChocoboFound}");

            // Feed confirmed! Record it.
            RecordFeedSuccess();

            if (_activePlan == null || _activePlan.IsComplete)
            {
                HandlePlanComplete();
            }
            else
            {
                // More fruits to feed — click Feed in the menu
                TransitionTo(FeedingState.SelectingFeedFromMenu);
            }
        }
        else if (ElapsedInState > MenuTimeoutSec)
        {
            SetError("Stable menu did not reappear after feeding. Neither SelectString nor HousingMyChocobo found. The UI may have changed or closed.");
        }
    }

    private unsafe void HandleSelectingFeedFromMenu()
    {
        // Longer delay to let the menu fully render and become ready
        if (ElapsedInState < 1.0f) return;

        try
        {
            // Find the addon to click on.
            // Prefer SelectString (proven by YesAlready/ECommons), fall back to HousingMyChocobo.
            AtkUnitBase* addon = null;
            string addonUsed = "";

            // Try SelectString first
            nint ssPtr = _gameGui.GetAddonByName(SelectStringAddon);
            if (ssPtr != nint.Zero)
            {
                addon = (AtkUnitBase*)ssPtr;
                addonUsed = SelectStringAddon;
            }

            // Fall back to HousingMyChocobo
            if (addon == null)
            {
                nint hmcPtr = _gameGui.GetAddonByName(HousingChocoboAddon);
                if (hmcPtr != nint.Zero)
                {
                    addon = (AtkUnitBase*)hmcPtr;
                    addonUsed = HousingChocoboAddon;
                }
            }

            if (addon == null)
            {
                if (ElapsedInState > StateTimeoutSec)
                    SetError("Stable menu disappeared before we could click Feed. Neither SelectString nor HousingMyChocobo found.");
                return;
            }

            // Menu: Train=0, Feed=1, Change Name=2, Fetch=3, View Details=4, Quit=5
            _log.Debug($"Clicking 'Feed' (index {StableMenuFeedIndex}) on {addonUsed} addon (ptr=0x{(nint)addon:X})");
            ClickSelectString(addon, StableMenuFeedIndex);
            TransitionTo(FeedingState.WaitingForFeedInventory);
        }
        catch (Exception ex)
        {
            _log.Warning($"Error selecting Feed from stable menu: {ex}");
            if (++_retryCount > MaxRetries)
                SetError($"Failed to select Feed from stable menu: {ex.Message}");
            else
                TransitionTo(FeedingState.WaitingForStableMenu);
        }
    }

    private void HandleWaitingForFeedInventory()
    {
        // After selecting Feed from the stable menu, inventory opens with feedable items.
        // Wait for any inventory addon to become visible.
        if (IsAnyInventoryAddonVisible())
        {
            _log.Debug("Feed inventory appeared. Proceeding to find fruit slot.");
            TransitionTo(FeedingState.FindingFruitSlot);
        }
        else if (ElapsedInState > MenuTimeoutSec)
        {
            SetError("Feed inventory did not open after selecting Feed from stable menu. " +
                    "This happens if you are not in expanded inventory mode. " +
                    "Please expand your inventory to 'Open all + Expanded', apply, then start again. " +
                    "It will resume from where it left off, so no steps should be lost under normal circumstances.");
        }
    }

    private void HandlePlanComplete()
    {
        _log.Information("All fruits fed! Plan complete.");
        _planStorage.CompletePlan(_characterName, _worldName);
        TransitionTo(FeedingState.Completed);
        _framework.Update -= OnFrameworkTick;
        _ipcService.ResumeExternalPlugins();
    }

    private void SetError(string message)
    {
        _errorMessage = message;
        _log.Error($"Feeding automation error: {message}");
        TransitionTo(FeedingState.Error);
        _framework.Update -= OnFrameworkTick;
        _ipcService.ResumeExternalPlugins();
    }

    // --- Low-level addon interaction helpers ---

    /// <summary>Check if addon exists (non-null pointer). Does NOT check IsVisible.</summary>
    private bool AddonExists(string name)
    {
        nint ptr = _gameGui.GetAddonByName(name);
        return ptr != nint.Zero;
    }

    /// <summary>Check if addon exists AND has IsVisible = true.</summary>
    private unsafe bool IsAddonVisible(string name)
    {
        nint ptr = _gameGui.GetAddonByName(name);
        if (ptr == nint.Zero) return false;
        var addon = (AtkUnitBase*)ptr;
        return addon->IsVisible;
    }

    /// <summary>Get addon pointer only if it exists AND IsVisible. Returns null otherwise.</summary>
    private unsafe AtkUnitBase* GetAddon(string name)
    {
        nint ptr = _gameGui.GetAddonByName(name);
        if (ptr == nint.Zero) return null;
        var addon = (AtkUnitBase*)ptr;
        return addon->IsVisible ? addon : null;
    }

    /// <summary>Check if Talk addon exists AND is visible.</summary>
    private unsafe bool IsTalkAddonVisible()
    {
        nint ptr = _gameGui.GetAddonByName("Talk");
        if (ptr == nint.Zero) return false;
        var addon = (AtkUnitBase*)ptr;
        return addon->IsVisible;
    }

    /// <summary>
    /// Dismiss a Talk dialog by firing a callback.
    /// After feeding, the game may show "You feed your chocobo..." etc.
    /// This is a fallback if the buddy-feed cutscene hook still leaves a Talk dialog visible.
    /// </summary>
    private unsafe void DismissTalkAddon()
    {
        nint ptr = _gameGui.GetAddonByName("Talk");
        if (ptr == nint.Zero) return;
        var addon = (AtkUnitBase*)ptr;
        if (!addon->IsVisible) return;

        _log.Debug("Dismissing Talk dialog with manual fallback callback");
        // Click/advance the Talk dialog
        var values = stackalloc AtkValue[1];
        values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.Int;
        values[0].Int = 0;
        addon->FireCallback(1, values, true);  // updateState: true
    }

    /// <summary>
    /// Find any visible inventory addon and return its ID.
    /// Different inventory layouts use different addon names.
    /// </summary>
    private unsafe uint GetVisibleInventoryAddonId()
    {
        foreach (var name in InventoryAddonNames)
        {
            var addon = GetAddon(name);
            if (addon != null)
            {
                _log.Debug($"Found visible inventory addon: {name} (ID={addon->Id})");
                return addon->Id;
            }
        }

        _log.Warning("No visible inventory addon found, using ID 0 as fallback");
        return 0;
    }

    /// <summary>Check if any inventory addon is currently visible.</summary>
    private unsafe bool IsAnyInventoryAddonVisible()
    {
        foreach (var name in InventoryAddonNames)
        {
            if (IsAddonVisible(name)) return true;
        }
        return false;
    }

    /// <summary>
    /// Click a menu item in a list-based addon (SelectString or HousingMyChocobo).
    /// For HousingMyChocobo, the menu items are in an AtkComponentList (component ID 3).
    /// Pattern from YesAlready: get the list component, then fire callback with the index.
    /// </summary>
    private unsafe void ClickSelectString(AtkUnitBase* addon, int index)
    {
        // Try to get the list component (ID 3 for HousingMyChocobo, also common for SelectString)
        var listComponent = addon->GetComponentListById(3);
        if (listComponent != null)
        {
            var itemCount = listComponent->GetItemCount();
            _log.Debug($"List component found: {itemCount} items, clicking index {index}");
            
            // Verify index is valid
            if (index < 0 || index >= itemCount)
            {
                _log.Warning($"Invalid list index {index} (count={itemCount})");
                return;
            }
        }
        else
        {
            _log.Debug($"No list component found on addon, using direct callback");
        }

        // Fire callback with the index
        var values = stackalloc AtkValue[1];
        values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.Int;
        values[0].Int = index;
        addon->FireCallback(1, values, true);  // updateState: true (matches ECommons Callback.Fire)
    }

    /// <summary>
    /// Fire callback on ContextMenu to select an option by index.
    /// Pattern from Dropbox plugin: Callback.Fire(addon, true, 0, index, 0, 0, 0)
    /// </summary>
    private unsafe void ClickContextMenu(AtkUnitBase* addon, int index)
    {
        var values = stackalloc AtkValue[5];
        values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.Int;
        values[0].Int = 0;
        values[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.Int;
        values[1].Int = index;
        values[2].Type = FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.UInt;
        values[2].UInt = 0;
        values[3].Type = FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.Int;
        values[3].Int = 0;
        values[4].Type = FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.Int;
        values[4].Int = 0;
        addon->FireCallback(5, values, true);  // updateState: true (matches ECommons Callback.Fire)
    }


    /// <summary>Map display name back to FruitType enum.</summary>
    private static FruitType? FruitNameToType(string name)
    {
        foreach (var ft in FruitData.AllFruits)
        {
            if (FruitData.GetDisplayName(ft) == name)
                return ft;
        }
        return null;
    }

    public void Dispose()
    {
        if (IsRunning)
        {
            _framework.Update -= OnFrameworkTick;
            _ipcService.ResumeExternalPlugins();
        }
    }
}
