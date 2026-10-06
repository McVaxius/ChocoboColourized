using Dalamud.Configuration;
using System;

namespace ChocoboColourized;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public string UiLanguage { get; set; } = "en";
    public uint UiAccentRgb { get; set; } = 0xF7D27A;
    public bool UiCompact { get; set; } = false;
    public bool UiCompactVisibleOnMainWindow { get; set; } = true;
    public bool UiLanguageVisibleOnMainWindow { get; set; } = true;
    public bool UiTransparencyEnabled { get; set; } = true;
    private int uiWindowOpacityPercent = 100;
    public int UiWindowOpacityPercent { get => uiWindowOpacityPercent; set => uiWindowOpacityPercent = System.Math.Clamp(value, 10, 100); }
    public bool UiAutoFade { get; set; } = true;
    private int uiFadedOpacityPercent = 50;
    public int UiFadedOpacityPercent { get => uiFadedOpacityPercent; set => uiFadedOpacityPercent = System.Math.Clamp(value, 10, 100); }
    private int uiUnfocusedDelaySeconds = 10;
    public int UiUnfocusedDelaySeconds { get => uiUnfocusedDelaySeconds; set => uiUnfocusedDelaySeconds = System.Math.Clamp(value, 0, 3600); }

    public bool IsConfigWindowMovable { get; set; } = true;

    // If true, check stable condition before feeding and warn if Poor/Fair
    public bool CheckStableCondition { get; set; } = true;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
