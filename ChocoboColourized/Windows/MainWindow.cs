using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ChocoboColourized.Core;
using ChocoboColourized.Models;
using ChocoboColourized.Services;

namespace ChocoboColourized.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    // Colour selection state
    private int currentColorIndex = 0;
    private int targetColorIndex = 0;
    private readonly string[] colorNames;

    private string currentColorSearch = string.Empty;
    private string targetColorSearch = string.Empty;

    // Calculation state
    private CalculationResult? lastResult = null;
    private string statusMessage = "";

    private bool selectAutomationTabNextFrame;


    // Colours for inventory status
    private static readonly Vector4 ColorRed = new(1f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 ColorYellow = new(1f, 1f, 0.3f, 1f);
    private static readonly Vector4 ColorGreen = new(0.3f, 1f, 0.3f, 1f);
    private static readonly Vector4 ColorGrey = new(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Vector4 ColorWhite = new(1f, 1f, 1f, 1f);
    private static readonly Vector4 ColorCyan = new(0.3f, 1f, 1f, 1f);

    public MainWindow(Plugin plugin)
        : base("Chocobo Colourized##MainWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 640),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        colorNames = ColorDatabase.AllColorNames;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // Ko-fi donation button in upper right
        ImGui.SameLine(ImGui.GetWindowWidth() - 120);
        if (ImGui.SmallButton("\u2661 Ko-fi \u2661"))
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "https://ko-fi.com/mcvaxius",
                UseShellExecute = true
            });
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Support development on Ko-fi");
        }
        
        if (ImGui.BeginTabBar("MainTabs"))
        {
            if (ImGui.BeginTabItem("Calculator"))
            {
                DrawCalculatorTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Timers"))
            {
                DrawTimersTab();
                ImGui.EndTabItem();
            }
            var automationFlags = selectAutomationTabNextFrame ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            if (ImGui.BeginTabItem("Automation", automationFlags))
            {
                selectAutomationTabNextFrame = false;
                DrawAutomationTab();
                ImGui.EndTabItem();
            }
            else if (selectAutomationTabNextFrame)
            {
                // If tab failed to open for any reason, avoid repeatedly forcing selection
                selectAutomationTabNextFrame = false;
            }
            ImGui.EndTabBar();
        }
    }

    // ========== CALCULATOR TAB ==========
    private void DrawCalculatorTab()
    {
        DrawChocoboColorDetection();
        ImGui.Spacing();
        DrawColorSelection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawCalculateButton();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawResults();
    }

    private void DrawChocoboColorDetection()
    {
        // Feature 5: Attempt auto-detect, or show tooltip for manual lookup
        var detectedColor = plugin.GameData.TryGetChocoboColor();
        if (detectedColor != null)
        {
            ImGui.TextColored(ColorGreen, $"Detected chocobo colour: {detectedColor}");
            // Try to set the dropdown to the detected colour
            for (int i = 0; i < colorNames.Length; i++)
            {
                if (colorNames[i] == detectedColor)
                {
                    currentColorIndex = i;
                    break;
                }
            }
        }
        else
        {
            ImGui.Text("Current colour:");
            ImGui.SameLine();
            ImGui.TextColored(ColorGrey, "(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("How to find your chocobo's current colour:");
                ImGui.Separator();
                ImGui.Text("1. Open the Companion window (default: N key)");
                ImGui.Text("2. Click the Appearance tab");
                ImGui.Text("3. Check the current colour listed");
                ImGui.Text("4. Select it from the dropdown below");
                ImGui.Spacing();
                ImGui.TextColored(ColorGrey, "Auto-detection will be added in a future update.");
                ImGui.EndTooltip();
            }
        }
    }

    private void DrawColorSelection()
    {
        // Current Color
        ImGui.Text("Current Chocobo Colour:");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##CurrentColorSearch", "Search colours...", ref currentColorSearch, 64);
        ImGui.SetNextItemWidth(-1);
        DrawColorCombo("##CurrentColor", ref currentColorIndex, currentColorSearch);
        var currentColor = ColorDatabase.GetByIndex(currentColorIndex);
        DrawColorPreview("Current:", currentColor);

        ImGui.Spacing();

        // Target Color
        ImGui.Text("Target Colour:");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##TargetColorSearch", "Search colours...", ref targetColorSearch, 64);
        ImGui.SetNextItemWidth(-1);
        DrawColorCombo("##TargetColor", ref targetColorIndex, targetColorSearch);
        var targetColor = ColorDatabase.GetByIndex(targetColorIndex);
        DrawColorPreview("Target:", targetColor);
    }

    private void DrawColorCombo(string label, ref int selectedIndex, string search)
    {
        var currentName = colorNames[selectedIndex];
        if (ImGui.BeginCombo(label, currentName))
        {
            for (var i = 0; i < colorNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(search) &&
                    !colorNames[i].Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var isSelected = selectedIndex == i;
                if (ImGui.Selectable(colorNames[i], isSelected))
                {
                    selectedIndex = i;
                }
                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
    }

    private void DrawColorPreview(string label, ChocoboColor color)
    {
        var colorVec = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f);
        ImGui.Text($"  {label}");
        ImGui.SameLine();
        ImGui.ColorButton($"##preview_{label}", colorVec,
            ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop, new Vector2(20, 20));
        ImGui.SameLine();
        ImGui.Text($"RGB({color.R}, {color.G}, {color.B})");
    }

    private void DrawCalculateButton()
    {
        if (currentColorIndex == targetColorIndex)
        {
            ImGui.TextColored(ColorYellow, "Current and target colours are the same!");
            return;
        }

        if (ImGui.Button("Calculate Feeding Path", new Vector2(-1, 30)))
        {
            RunCalculation();
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            ImGui.Text(statusMessage);
        }
    }

    private void RunCalculation()
    {
        var startColor = ColorDatabase.GetByIndex(currentColorIndex);
        var targetColor = ColorDatabase.GetByIndex(targetColorIndex);

        var calculator = new ColorCalculator(3);
        lastResult = calculator.Calculate(startColor, targetColor);
        statusMessage = $"Calculation complete! ({lastResult.TotalFruits} fruits needed)";
    }

    private void DrawResults()
    {
        if (lastResult == null)
        {
            ImGui.TextColored(ColorGrey, "Select colours and click Calculate.");
            return;
        }

        // Header
        ImGui.Text($"From: {lastResult.StartColor.Name}");
        ImGui.Text($"To:   {lastResult.TargetColor.Name}");
        ImGui.Spacing();

        // Final color result
        var finalColorVec = new Vector4(
            lastResult.FinalColor.R / 255f,
            lastResult.FinalColor.G / 255f,
            lastResult.FinalColor.B / 255f, 1f);
        ImGui.Text("Result Colour:");
        ImGui.SameLine();
        ImGui.ColorButton("##finalPreview", finalColorVec,
            ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop, new Vector2(20, 20));
        ImGui.SameLine();
        ImGui.Text($"RGB({lastResult.FinalColor.R}, {lastResult.FinalColor.G}, {lastResult.FinalColor.B})");

        ImGui.Text($"Closest Named Colour: {lastResult.ClosestColorName}");
        ImGui.Text($"Distance to Target: {lastResult.FinalDistance:F2}");
        ImGui.Spacing();

        if (lastResult.TotalFruits == 0)
        {
            ImGui.TextColored(ColorGreen, "Already at the closest possible colour!");
            return;
        }

        // Feature 3: Inventory requirement display with colour coding
        DrawInventoryRequirements();
        ImGui.Spacing();

        // Feeding order
        ImGui.Text("Recommended Feeding Order:");
        if (ImGui.BeginChild("FeedingOrder", new Vector2(-1, 120), true))
        {
            for (var i = 0; i < lastResult.Fruits.Count; i++)
            {
                ImGui.Text($"  {i + 1}. {FruitData.GetDisplayName(lastResult.Fruits[i])}");
            }
        }
        ImGui.EndChild();

        ImGui.Spacing();

        // Feature 4: Save Plan + Start button
        DrawPlanButtons();

        ImGui.Spacing();

        // Copy button
        if (ImGui.Button("Copy Results to Clipboard", new Vector2(-1, 25)))
        {
            var text = FormatResultsForClipboard();
            ImGui.SetClipboardText(text);
            statusMessage = "Results copied to clipboard!";
        }
    }

    // Feature 3: Inventory requirements with red/yellow/green colour coding
    private void DrawInventoryRequirements()
    {
        ImGui.Text($"Total Fruits Required: {lastResult!.TotalFruits}");
        ImGui.Spacing();

        var fruitCounts = plugin.GameData.IsLoggedIn
            ? plugin.GameData.GetAllFruitCounts()
            : null;
        var allSatisfied = true;

        if (ImGui.BeginTable("FruitReq", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            // Use scaled widths based on font size to handle UI scaling >100%
            var scale = ImGui.GetFontSize() / 17f;  // 17 = default Dalamud font size
            ImGui.TableSetupColumn("Fruit", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Required", ImGuiTableColumnFlags.WidthFixed, 70 * scale);
            ImGui.TableSetupColumn("Owned", ImGuiTableColumnFlags.WidthFixed, 70 * scale);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 50 * scale);
            ImGui.TableHeadersRow();

            foreach (var kvp in lastResult.FruitCounts.OrderByDescending(x => x.Value))
            {
                var required = kvp.Value;
                var owned = fruitCounts != null && fruitCounts.ContainsKey(kvp.Key)
                    ? fruitCounts[kvp.Key] : 0;

                Vector4 statusColor;
                string statusIcon;
                if (!plugin.GameData.IsLoggedIn)
                {
                    statusColor = ColorGrey;
                    statusIcon = "?";
                }
                else if (owned >= required)
                {
                    statusColor = ColorGreen;
                    statusIcon = "OK";
                }
                else if (owned > 0)
                {
                    statusColor = ColorYellow;
                    statusIcon = "Low";
                    allSatisfied = false;
                }
                else
                {
                    statusColor = ColorRed;
                    statusIcon = "None";
                    allSatisfied = false;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(FruitData.GetDisplayName(kvp.Key));
                ImGui.TableNextColumn();
                ImGui.Text(required.ToString());
                ImGui.TableNextColumn();
                ImGui.TextColored(statusColor, plugin.GameData.IsLoggedIn ? owned.ToString() : "?");
                ImGui.TableNextColumn();
                ImGui.TextColored(statusColor, statusIcon);
            }

            ImGui.EndTable();
        }

        if (plugin.GameData.IsLoggedIn && !allSatisfied)
        {
            ImGui.TextColored(ColorYellow, "You need more fruits. Acquire them however you prefer.");
        }
    }

    // Feature 4: Plan status and quick-start button with gating
    private void DrawPlanButtons()
    {
        if (lastResult == null || lastResult.TotalFruits == 0) return;

        if (!plugin.GameData.IsLoggedIn)
        {
            ImGui.TextColored(ColorGrey, "Log in to use feeding plans.");
            return;
        }

        var charName = plugin.GameData.CharacterName;
        var worldName = plugin.GameData.WorldName;
        var charData = plugin.PlanStorage.GetCharacterData(charName, worldName);

        if (charData.ActivePlan != null)
        {
            ImGui.TextColored(ColorCyan,
                $"Active plan: {charData.ActivePlan.FruitsFed}/{charData.ActivePlan.TotalFruits} fed " +
                $"({charData.ActivePlan.StartColorName} -> {charData.ActivePlan.TargetColorName})");

            if (ImGui.Button("Clear Existing Plan", new Vector2(200, 25)))
            {
                plugin.PlanStorage.ClearPlan(charName, worldName);
            }
            ImGui.SameLine();
            ImGui.TextColored(ColorGrey, "Switch to the Automation tab to start feeding.");
        }
        else if (charData.IsTimerActive)
        {
            var remaining = charData.TimerRemaining;
            ImGui.TextColored(ColorCyan,
                $"Colour change in progress: {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s remaining");
        }
        else
        {
            var hasEnough = plugin.GameData.HasEnoughFruits(lastResult.FruitCounts);

            if (ImGui.Button("Save Plan & Switch to Automation", new Vector2(-1, 30)))
            {
                SavePlanFromResult(charName, worldName);
                statusMessage = "Plan saved! Switched to Automation tab.";
                selectAutomationTabNextFrame = true;
            }

            if (!hasEnough)
            {
                ImGui.TextColored(ColorYellow,
                    "You do not have all required fruits yet. Automation start will stay disabled until you gather them.");
            }
            else
            {
                ImGui.TextColored(ColorGreen, "All required fruits are in your inventory. Head to the Automation tab to start.");
            }

            ImGui.TextColored(ColorGrey, "Automation controls live in the Automation tab.");
        }
    }

    /// <summary>Save plan from current calculation result and auto-set chocobo name.</summary>
    private void SavePlanFromResult(string charName, string worldName)
    {
        if (lastResult == null) return;
        var plan = FeedingPlan.FromCalculationResult(lastResult);
        plugin.PlanStorage.SavePlan(charName, worldName, plan);

        // Auto-set chocobo name from player's first name
        var firstName = charName.Split(' ')[0];
        var autoName = $"{firstName}'s Chocobo";
        plugin.PlanStorage.SetChocoboName(charName, worldName, autoName);
    }

    // ========== TIMERS TAB ==========
    // Feature 2: Six-hour timer list per character
    private void DrawTimersTab()
    {
        ImGui.Text("Colour Change Timers");
        ImGui.Separator();
        ImGui.Spacing();

        var allChars = plugin.PlanStorage.GetAllCharacters();
        var hasAnyTimer = false;

        if (ImGui.BeginTable("Timers", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            var scale = ImGui.GetFontSize() / 17f;
            ImGui.TableSetupColumn("Character", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Chocobo", ImGuiTableColumnFlags.WidthFixed, 130 * scale);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 110 * scale);
            ImGui.TableSetupColumn("Remaining", ImGuiTableColumnFlags.WidthFixed, 110 * scale);
            ImGui.TableHeadersRow();

            foreach (var kvp in allChars)
            {
                var data = kvp.Value;
                if (!data.IsTimerActive && data.ActivePlan == null) continue;
                hasAnyTimer = true;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"{data.CharacterName} @ {data.WorldName}");

                ImGui.TableNextColumn();
                ImGui.Text(string.IsNullOrEmpty(data.ChocoboName) ? "(unnamed)" : data.ChocoboName);

                ImGui.TableNextColumn();
                if (data.IsTimerActive)
                {
                    ImGui.TextColored(ColorCyan, "Changing...");
                }
                else if (data.ActivePlan != null)
                {
                    ImGui.TextColored(ColorYellow, $"Feeding {data.ActivePlan.FruitsFed}/{data.ActivePlan.TotalFruits}");
                }

                ImGui.TableNextColumn();
                if (data.IsTimerActive)
                {
                    var r = data.TimerRemaining;
                    ImGui.Text($"{r.Hours}h {r.Minutes:D2}m {r.Seconds:D2}s");
                }
                else
                {
                    ImGui.TextColored(ColorGrey, "-");
                }
            }

            ImGui.EndTable();
        }

        if (!hasAnyTimer)
        {
            ImGui.TextColored(ColorGrey, "No active timers or feeding plans.");
            ImGui.Spacing();
            ImGui.Text("Timers will appear here after completing a full feeding.");
            ImGui.Text("Each character on this account will have their own timer.");
        }
    }

    // ========== AUTOMATION TAB ==========
    // Feature 1: Automated feeding
    private void DrawAutomationTab()
    {
        ImGui.Text("Automated Feeding");
        ImGui.Separator();
        ImGui.Spacing();

        if (!plugin.GameData.IsLoggedIn)
        {
            ImGui.TextColored(ColorGrey, "You must be logged in to use automated feeding.");
            return;
        }

        var charName = plugin.GameData.CharacterName;
        var worldName = plugin.GameData.WorldName;
        var charData = plugin.PlanStorage.GetCharacterData(charName, worldName);
        var automation = plugin.FeedingAutomation;

        if (charData.IsTimerActive)
        {
            var r = charData.TimerRemaining;
            ImGui.TextColored(ColorCyan,
                $"Colour change already in progress: {r.Hours}h {r.Minutes:D2}m {r.Seconds:D2}s remaining.");
            return;
        }

        if (charData.ActivePlan == null)
        {
            ImGui.TextColored(ColorGrey, "No active feeding plan.");
            ImGui.Text("Calculate a feeding path in the Calculator tab, then click 'Save Plan'.");
            return;
        }

        var plan = charData.ActivePlan;

        // Show plan summary
        ImGui.Text($"Plan: {plan.StartColorName} -> {plan.TargetColorName}");
        ImGui.Text($"Progress: {plan.FruitsFed} / {plan.TotalFruits} fruits fed");

        // Progress bar
        var progress = plan.TotalFruits > 0 ? (float)plan.FruitsFed / plan.TotalFruits : 0f;
        ImGui.ProgressBar(progress, new Vector2(-1, 20),
            $"{plan.FruitsFed}/{plan.TotalFruits}");

        ImGui.Spacing();

        // Remaining fruit summary
        ImGui.Text("Remaining fruits:");
        var remaining = plan.RemainingFruitCounts;
        foreach (var kvp in remaining.OrderByDescending(x => x.Value))
        {
            ImGui.Text($"  {kvp.Key} x{kvp.Value}");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Automation controls
        if (automation.IsRunning)
        {
            // Show live progress
            ImGui.TextColored(ColorCyan,
                $"Feeding in progress: step {automation.CurrentFruitIndex + 1}/{automation.TotalFruits}");
            ImGui.Text($"Current: {automation.CurrentFruitName}");
            ImGui.Text($"State: {automation.State}");

            ImGui.Spacing();
            if (ImGui.Button("Stop Automation", new Vector2(-1, 30)))
            {
                automation.Stop();
                statusMessage = "Automation stopped.";
            }
        }
        else if (automation.State == FeedingState.Completed)
        {
            ImGui.TextColored(ColorGreen, "Feeding complete! Your chocobo's colour will change in 6 hours.");
            if (ImGui.Button("OK", new Vector2(-1, 25)))
            {
                automation.Reset();
            }
        }
        else if (automation.State == FeedingState.Error)
        {
            ImGui.TextColored(ColorRed, $"Error: {automation.ErrorMessage}");
            if (ImGui.Button("Dismiss", new Vector2(-1, 25)))
            {
                automation.Reset();
            }
        }
        else
        {
            // Feature 4: Gate start button on inventory
            var hasEnough = plugin.GameData.HasEnoughFruits(GetRemainingFruitCounts(plan));

            ImGui.TextColored(ColorYellow,
                "IMPORTANT: You must be at the Chocobo Stable in the FEED screen");
            ImGui.TextColored(ColorYellow,
                "(inventory open with feedable items highlighted) before clicking Start.");
            ImGui.TextColored(ColorYellow,
                "TextAdvance and YesAlready will be paused during automation.");
            ImGui.Spacing();

            // Stable condition check
            var config = plugin.Configuration;
            var checkCondition = config.CheckStableCondition;
            if (ImGui.Checkbox("Check stable condition before feeding", ref checkCondition))
            {
                config.CheckStableCondition = checkCondition;
                config.Save();
            }
            ImGui.SameLine();
            ImGui.TextColored(ColorGrey, "(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("If enabled, warns you if the stable condition is Poor or Fair.");
                ImGui.Text("Use a Magicked Stable Broom to clean the stable first.");
                ImGui.EndTooltip();
            }

            bool stableConditionBlocked = false;
            if (config.CheckStableCondition)
            {
                // Check if user has a Magicked Stable Broom (ID: 8168)
                var broomCount = plugin.GameData.GetItemCount(8168);
                ImGui.Text($"Magicked Stable Broom: {broomCount} in inventory");
                // Note: We can't directly read stable condition from game state yet,
                // so we inform the user to check manually and provide broom count.
            }

            ImGui.Spacing();

            if (hasEnough && !stableConditionBlocked)
            {
                if (ImGui.Button("Start Automated Feeding", new Vector2(-1, 35)))
                {
                    // Auto-set chocobo name from player's first name if not already set
                    if (string.IsNullOrEmpty(charData.ChocoboName))
                    {
                        var firstName = charName.Split(' ')[0];
                        plugin.PlanStorage.SetChocoboName(charName, worldName, $"{firstName}'s Chocobo");
                    }
                    automation.Start(plan, charName, worldName);
                }
            }
            else
            {
                ImGui.BeginDisabled();
                ImGui.Button("Start Automated Feeding (Insufficient Fruits)", new Vector2(-1, 35));
                ImGui.EndDisabled();
                ImGui.TextColored(ColorRed, "You do not have enough fruits in your inventory.");
            }

            ImGui.Spacing();
            if (ImGui.Button("Clear Plan", new Vector2(120, 25)))
            {
                plugin.PlanStorage.ClearPlan(charName, worldName);
            }
        }

        // Quest prerequisites guide (always shown at bottom of Automation tab)
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawQuestPrerequisites();
    }

    private void DrawQuestPrerequisites()
    {
        if (ImGui.TreeNode("Prerequisites & Troubleshooting"))
        {
            ImGui.Spacing();
            ImGui.TextColored(ColorCyan, "Required Quests:");
            ImGui.Spacing();

            ImGui.Text("1.");
            ImGui.SameLine();
            ImGui.TextColored(ColorWhite, "My Feisty Little Chocobo");
            ImGui.Text("   Obtain your chocobo companion (Camp Tranquil, South Shroud)");

            ImGui.Spacing();
            ImGui.Text("2.");
            ImGui.SameLine();
            ImGui.TextColored(ColorWhite, "Bird in Hand");
            ImGui.Text("   Unlock chocobo stabling and raising (Bentbranch Meadows, Central Shroud)");
            ImGui.Text("   NPC: Luquelot at Bentbranch Meadows (X:21.4, Y:22.1)");

            ImGui.Spacing();
            ImGui.TextColored(ColorCyan, "Other Requirements:");
            ImGui.Spacing();
            ImGui.Text("- Access to a Chocobo Stable (FC house, personal house, or apartment)");
            ImGui.Text("- Your chocobo must be stabled (not summoned as companion)");
            ImGui.Text("- Fruits in your inventory (purchase from vendors or Market Board)");

            ImGui.Spacing();
            ImGui.TextColored(ColorCyan, "Common Errors:");
            ImGui.Spacing();

            ImGui.TextColored(ColorYellow, "\"You have yet to be trained in chocobo raising\"");
            ImGui.Text("  -> Complete the quest \"Bird in Hand\" at Bentbranch Meadows.");

            ImGui.Spacing();
            ImGui.TextColored(ColorYellow, "\"Your chocobo is not stabled\"");
            ImGui.Text("  -> Stable your chocobo at a Chocobo Stable before feeding.");

            ImGui.Spacing();
            ImGui.TextColored(ColorYellow, "Automation errors about context menu or inventory");
            ImGui.Text("  -> Make sure you are on the Feed screen (inventory visible with");
            ImGui.Text("     feedable items highlighted) before clicking Start.");

            ImGui.TreePop();
        }
    }

    // Helper: convert remaining plan fruits to FruitType counts for inventory check
    private static Dictionary<FruitType, int> GetRemainingFruitCounts(FeedingPlan plan)
    {
        var counts = new Dictionary<FruitType, int>();
        var nameToType = new Dictionary<string, FruitType>();
        foreach (var ft in FruitData.AllFruits)
            nameToType[FruitData.GetDisplayName(ft)] = ft;

        for (var i = plan.NextFruitIndex; i < plan.FruitOrder.Count; i++)
        {
            if (nameToType.TryGetValue(plan.FruitOrder[i], out var fruitType))
            {
                if (counts.ContainsKey(fruitType))
                    counts[fruitType]++;
                else
                    counts[fruitType] = 1;
            }
        }
        return counts;
    }

    private string FormatResultsForClipboard()
    {
        if (lastResult == null) return "";

        var lines = new List<string>
        {
            "=== Chocobo Colourized ===",
            $"From: {lastResult.StartColor}",
            $"To:   {lastResult.TargetColor}",
            $"Result: {lastResult.FinalColor} ({lastResult.ClosestColorName})",
            $"Total Fruits: {lastResult.TotalFruits}",
            "",
            "Fruit Summary:"
        };

        foreach (var kvp in lastResult.FruitCounts.OrderByDescending(x => x.Value))
        {
            lines.Add($"  {FruitData.GetDisplayName(kvp.Key)} x{kvp.Value}");
        }

        lines.Add("");
        lines.Add("Feeding Order:");
        for (var i = 0; i < lastResult.Fruits.Count; i++)
        {
            lines.Add($"  {i + 1}. {FruitData.GetDisplayName(lastResult.Fruits[i])}");
        }

        return string.Join("\n", lines);
    }
}
