using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ChocoboColourized.Services;
using ChocoboColourized.Windows;
using ChocoboColourized.Ui;

namespace ChocoboColourized;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

    private const string CommandName = "/chococolor";

    internal AethertekUI.MaterialTitlebarStatus? TitlebarStatus
    {
        get
        {
            if (!Configuration.UiShowTitlebarStatus) return null;
            var data = PlanStorage.GetAllCharacters().Values.FirstOrDefault(value =>
                value.CharacterName == GameData.CharacterName && value.WorldName == GameData.WorldName);
            var blocked = FeedingAutomation.State == FeedingState.Error;
            var state = UiText.T(blocked ? "Blocked" : FeedingAutomation.IsRunning ? "Feeding" : data?.IsTimerActive == true ? "Waiting" : "Idle");
            var concise = state;
            var details = state;
            if (data?.ActivePlan is { } plan)
            {
                details += " \u00b7 " + UiText.T(plan.TargetColorName);
                var progress = plan.FruitsFed.ToString(UiText.Current.Culture) + "/" + plan.TotalFruits.ToString(UiText.Current.Culture);
                concise += " \u00b7 " + progress;
                details += " \u00b7 " + progress;
            }
            if (data?.IsTimerActive == true)
            {
                var remaining = UiText.F("{0}m left", (int)Math.Ceiling(data.TimerRemaining.TotalMinutes));
                concise += " \u00b7 " + remaining;
                details += " \u00b7 " + remaining;
            }
            var tooltip = details + (blocked ? "\n" + UiText.T(FeedingAutomation.ErrorMessage) : "");
            return new(Configuration.UiDetailedTitlebarStatus ? details : concise, concise, tooltip,
                blocked ? AethertekUI.MaterialTheme.Current.Colors.Error : AethertekUI.MaterialTheme.Current.Colors.OnSurfaceVariant);
        }
    }

    public Configuration Configuration { get; init; }
    internal ChocoboAppearance Appearance { get; }

    // Services
    public GameDataService GameData { get; init; }
    public PlanStorageService PlanStorage { get; init; }
    public IpcService Ipc { get; init; }
    public BuddyFeedCutsceneSkipService BuddyFeedCutsceneSkip { get; init; }
    public FeedingAutomationService FeedingAutomation { get; init; }

    public readonly WindowSystem WindowSystem = new("ChocoboColourized");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (Configuration.ApplyCompactDefaults()) Configuration.Save();
        Appearance = new(this);

        // Initialize services
        GameData = new GameDataService(ClientState, ObjectTable, Log);
        PlanStorage = new PlanStorageService(PluginInterface, Log);
        Ipc = new IpcService(PluginInterface, Log);
        BuddyFeedCutsceneSkip = new BuddyFeedCutsceneSkipService(GameInteropProvider, Log);
        FeedingAutomation = new FeedingAutomationService(GameGui, Framework, Log, Ipc, PlanStorage, GameData);

        // Clean up expired timers on load
        PlanStorage.CleanupExpiredTimers();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Chocobo Colourized calculator window."
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;

        Log.Information($"===Chocobo Colourized loaded!===");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;

        WindowSystem.RemoveAllWindows();

        FeedingAutomation.Dispose();
        BuddyFeedCutsceneSkip.Dispose();
        Ipc.Dispose();
        PlanStorage.Dispose();
        GameData.Dispose();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        Appearance.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        if (Appearance.TryApplyCommand(args)) return;
        MainWindow.IsOpen = !MainWindow.IsOpen;
        if (MainWindow.IsOpen)
            BuddyFeedCutsceneSkip.Enable();
    }

    private void DrawUi() => Appearance.Draw(WindowSystem);

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void OpenMainUi()
    {
        BuddyFeedCutsceneSkip.Enable();
        MainWindow.IsOpen = true;
    }
}
