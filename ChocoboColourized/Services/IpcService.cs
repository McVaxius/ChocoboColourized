using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ChocoboColourized.Services;

/// <summary>Owns stop requests during automated feeding.</summary>
public class IpcService : IDisposable
{
    private const string RequestOwner = "ChocoboColourized";
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IPluginLog _log;
    private HashSet<string>? _textAdvanceRequests;
    private HashSet<string>? _yesAlreadyRequests;
    private bool _isPaused;
    private volatile bool _providerChanged;

    public IpcService(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        _pluginInterface = pluginInterface;
        _log = log;
        _pluginInterface.ActivePluginsChanged += OnActivePluginsChanged;
    }

    public bool IsPaused => _isPaused;

    public bool PauseExternalPlugins()
    {
        if (_isPaused) return ValidatePauses();

        _providerChanged = false;
        try
        {
            AcquirePause("TextAdvance", ref _textAdvanceRequests);
            AcquirePause("YesAlready", ref _yesAlreadyRequests);
            _isPaused = true;
            if (ValidatePauses()) return true;

            throw new InvalidOperationException("A feeding helper changed while acquiring its pause.");
        }
        catch (Exception ex)
        {
            ResumeExternalPlugins();
            _log.Error($"Could not pause feeding helpers: {ex.Message}");
            return false;
        }
    }

    private bool IsLoaded(string provider)
        => _pluginInterface.InstalledPlugins.Any(plugin => plugin.InternalName == provider && plugin.IsLoaded);

    private HashSet<string>? GetStopRequests(string provider)
    {
        var key = $"{provider}.StopRequests";
        if (!_pluginInterface.TryGetData<HashSet<string>>(key, out var requests))
            return null;

        // Do not keep the shared data alive across a provider unload/reload.
        _pluginInterface.RelinquishData(key);
        return requests;
    }

    private void AcquirePause(string provider, ref HashSet<string>? ownedRequests)
    {
        if (!IsLoaded(provider)) return;

        var requests = GetStopRequests(provider)
            ?? throw new InvalidOperationException($"{provider} has no StopRequests set available.");
        if (!requests.Add(RequestOwner))
            throw new InvalidOperationException($"{provider} already has a stop request for {RequestOwner}.");

        ownedRequests = requests;
        _log.Information($"{provider} paused for automated feeding.");
    }

    public bool ValidatePauses()
    {
        if (!_isPaused || _providerChanged) return false;

        try
        {
            return HasPause("TextAdvance", _textAdvanceRequests)
                && HasPause("YesAlready", _yesAlreadyRequests);
        }
        catch (Exception ex)
        {
            _log.Warning($"Could not verify feeding helper pauses: {ex.Message}");
            return false;
        }
    }

    private bool HasPause(string provider, HashSet<string>? ownedRequests)
    {
        // A provider appearing during feeding also requires a fresh start.
        if (ownedRequests == null) return !IsLoaded(provider);

        return IsLoaded(provider)
            && ReferenceEquals(ownedRequests, GetStopRequests(provider))
            && ownedRequests.Contains(RequestOwner);
    }

    public void ResumeExternalPlugins()
    {
        // Remove only entries we added, from the exact sets that received them.
        _textAdvanceRequests?.Remove(RequestOwner);
        _textAdvanceRequests = null;
        _yesAlreadyRequests?.Remove(RequestOwner);
        _yesAlreadyRequests = null;
        _isPaused = false;
    }

    private void OnActivePluginsChanged(IActivePluginsChangedEventArgs args)
    {
        if (args.AffectedInternalNames.Any(name => name is "TextAdvance" or "YesAlready"))
            _providerChanged = true;
    }

    public void Dispose()
    {
        _pluginInterface.ActivePluginsChanged -= OnActivePluginsChanged;
        ResumeExternalPlugins();
    }
}
