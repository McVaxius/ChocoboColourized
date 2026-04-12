using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace ChocoboColourized.Services;

public unsafe class BuddyFeedCutsceneSkipService : IDisposable
{
    private readonly IGameInteropProvider _interopProvider;
    private readonly IPluginLog _log;
    private bool _initialized;

    [Signature("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 33 C0 48 83 C4 ?? C3 CC CC CC CC CC CC CC CC CC CC CC 48 83 EC",
        DetourName = nameof(PlayFeedBuddySceneDetour))]
    private Hook<PlayFeedBuddySceneDelegate>? _playFeedBuddySceneHook;

    public BuddyFeedCutsceneSkipService(IGameInteropProvider interopProvider, IPluginLog log)
    {
        _interopProvider = interopProvider;
        _log = log;
    }

    public bool IsEnabled => _playFeedBuddySceneHook?.IsEnabled == true;

    public void Enable()
    {
        if (!_initialized)
        {
            try
            {
                _interopProvider.InitializeFromAttributes(this);
            }
            catch (Exception ex)
            {
                _initialized = true;
                _log.Warning($"Failed to initialize buddy feed cutscene skip hook: {ex.Message}");
                return;
            }

            _initialized = true;
            if (_playFeedBuddySceneHook == null)
            {
                _log.Warning("Buddy feed cutscene skip hook could not be created.");
                return;
            }
        }

        if (_playFeedBuddySceneHook == null || _playFeedBuddySceneHook.IsEnabled)
        {
            return;
        }

        _playFeedBuddySceneHook.Enable();
        _log.Information("Buddy feed cutscene skip enabled.");
    }

    private void PlayFeedBuddySceneDetour(HousingManager* manager)
    {
        _log.Debug("Skipped buddy feed cutscene.");
    }

    public void Dispose()
    {
        _playFeedBuddySceneHook?.Dispose();
        _playFeedBuddySceneHook = null;
    }

    private delegate void PlayFeedBuddySceneDelegate(HousingManager* manager);
}
