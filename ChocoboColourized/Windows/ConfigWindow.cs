using System;
using System.Numerics;
using AethertekUI;
using AethertekUI.Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ChocoboColourized.Ui;

namespace ChocoboColourized.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly AethertekUI.Dalamud.MaterialSupportLog supportLog = new();
    private readonly MaterialWindowMotion windowMotion = new();
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base("Chocobo Colourized Settings###ChocoboColourizedConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.HorizontalScrollbar;

        Size = new Vector2(520, 280);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new() { MinimumSize = new(380, 220) };

        configuration = plugin.Configuration;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
        windowMotion.Prepare(this, reducedMotion: false, roundedCorners: true);
    }

    public override void PostDraw() => windowMotion.Restore(this);

    public override void Draw()
    {
        windowMotion.DrawChrome();
        UiGui.Title("Chocobo Colourized Settings", UiText.T("Chocobo Colourized Settings"));
        UiGui.TextWrapped("Chocobo Colourized Settings");
        var settingsRoot = ImGui.GetID("");
        using var tabs = MaterialTabs.Begin("ChocoboColourizedSettingsTabs", new[] { UiText.T("Settings"), UiText.T("Window appearance") }, ImGuiTabBarFlags.FittingPolicyScroll);
        if (!tabs.Visible) return;
        using (var general = MaterialTabs.Item(UiText.T("Settings") + "###Settings", ImGuiTabItemFlags.NoPushId))
        {
            if (general.Visible)
            {
                ImGuiP.PushOverrideID(settingsRoot);
                try
                {
                    supportLog.Draw(Plugin.PluginInterface, key => UiText.T(key),
                        path => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true }), ex => Plugin.Log.Error(ex, "Dalamud log export failed."), Plugin.CommandManager);
                    var movable = configuration.IsConfigWindowMovable;
                    if (UiGui.Checkbox("Movable Config Window", ref movable))
                    {
                        configuration.IsConfigWindowMovable = movable;
                        configuration.Save();
                    }
                }
                finally { ImGui.PopID(); }
            }
        }
        using (var appearance = MaterialTabs.Item(UiText.T("Window appearance") + "###WindowAppearance", ImGuiTabItemFlags.NoPushId))
            if (appearance.Visible)
            {
                ImGuiP.PushOverrideID(settingsRoot);
                try { plugin.Appearance.DrawWindowAppearanceSettings(); }
                finally { ImGui.PopID(); }
            }
    }
}
