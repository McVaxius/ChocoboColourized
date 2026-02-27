using Dalamud.Configuration;
using System;

namespace ChocoboColourized;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;

    // If true, check stable condition before feeding and warn if Poor/Fair
    public bool CheckStableCondition { get; set; } = true;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
