using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

namespace ChocoboColourized.Services;

/// <summary>
/// Manages IPC communication with TextAdvance and YesAlready plugins.
/// These plugins must be paused during automated feeding to prevent interference.
/// </summary>
public class IpcService : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IPluginLog _log;

    private bool _textAdvanceWasPaused;
    private bool _yesAlreadyWasPaused;
    private bool _isPaused;

    public IpcService(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        _pluginInterface = pluginInterface;
        _log = log;
    }

    /// <summary>Whether we are currently holding pauses on other plugins.</summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Pause TextAdvance and YesAlready before starting automated feeding.
    /// Stores their previous state so we can restore it afterwards.
    /// </summary>
    public void PauseExternalPlugins()
    {
        if (_isPaused) return;

        _textAdvanceWasPaused = false;
        _yesAlreadyWasPaused = false;

        // Pause TextAdvance
        try
        {
            var ta = _pluginInterface.GetIpcSubscriber<bool>("TextAdvance.IsEnabled");
            var wasEnabled = ta.InvokeFunc();
            if (wasEnabled)
            {
                var taSet = _pluginInterface.GetIpcSubscriber<bool, object?>("TextAdvance.SetEnabled");
                taSet.InvokeAction(false);
                _textAdvanceWasPaused = true;
                _log.Information("TextAdvance paused for automated feeding.");
            }
        }
        catch (Exception ex)
        {
            _log.Debug($"TextAdvance IPC not available (plugin may not be loaded): {ex.Message}");
        }

        // Pause YesAlready
        try
        {
            var ya = _pluginInterface.GetIpcSubscriber<bool>("YesAlready.IsEnabled");
            var wasEnabled = ya.InvokeFunc();
            if (wasEnabled)
            {
                var yaSet = _pluginInterface.GetIpcSubscriber<bool, object?>("YesAlready.SetEnabled");
                yaSet.InvokeAction(false);
                _yesAlreadyWasPaused = true;
                _log.Information("YesAlready paused for automated feeding.");
            }
        }
        catch (Exception ex)
        {
            _log.Debug($"YesAlready IPC not available (plugin may not be loaded): {ex.Message}");
        }

        _isPaused = true;
    }

    /// <summary>
    /// Re-enable TextAdvance and YesAlready after automated feeding completes.
    /// Only re-enables plugins that were enabled before we paused them.
    /// </summary>
    public void ResumeExternalPlugins()
    {
        if (!_isPaused) return;

        // Resume TextAdvance
        if (_textAdvanceWasPaused)
        {
            try
            {
                var taSet = _pluginInterface.GetIpcSubscriber<bool, object?>("TextAdvance.SetEnabled");
                taSet.InvokeAction(true);
                _log.Information("TextAdvance resumed.");
            }
            catch (Exception ex)
            {
                _log.Warning($"Failed to resume TextAdvance: {ex.Message}");
            }
            _textAdvanceWasPaused = false;
        }

        // Resume YesAlready
        if (_yesAlreadyWasPaused)
        {
            try
            {
                var yaSet = _pluginInterface.GetIpcSubscriber<bool, object?>("YesAlready.SetEnabled");
                yaSet.InvokeAction(true);
                _log.Information("YesAlready resumed.");
            }
            catch (Exception ex)
            {
                _log.Warning($"Failed to resume YesAlready: {ex.Message}");
            }
            _yesAlreadyWasPaused = false;
        }

        _isPaused = false;
    }

    public void Dispose()
    {
        // Safety: always resume on dispose
        if (_isPaused)
        {
            ResumeExternalPlugins();
        }
    }
}
