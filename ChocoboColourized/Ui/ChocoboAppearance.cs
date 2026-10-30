using System.Numerics;
using AethertekUI;
using AethertekUI.Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;

namespace ChocoboColourized.Ui;

internal sealed class ChocoboAppearance : IDisposable
{
    private readonly Plugin plugin;
    private readonly MaterialTextHost shapedText;
    private UiText text;
    private ChocoboFonts fonts;
    private MaterialTheme theme;
    private readonly Dictionary<Dalamud.Interface.Windowing.Window, AethertekUI.Dalamud.MaterialAppearanceTitleBar> appearanceTitleBars = new();
    private readonly MaterialWindowFold fontStatusMotion = new();
    private readonly MaterialWindowDecorations fontStatusDecorations = new();
    private readonly Dictionary<string, MaterialWindowOpacity> windowOpacities = new();
    private readonly MaterialOptions<string> languages = new(UiText.Languages.Select(l => new MaterialOption<string>(l.Code, l.Code, l.Name)).ToArray());
    private string appliedLanguage = "";
    private uint appliedAccent;
    private Vector3 accentDraft;
    private MaterialColorSettings colorDraft = new();
    private MaterialColorSettings? appliedColors;
    private MaterialColorSettings? appliedSavedColors;
    private int checkedGeneration = -1;
    private bool fontIssueLogged;
    private MaterialUiSoundPlayer? uiSoundPlayer;
    private bool appliedSoundsMuted = true;
    private readonly Dictionary<Window, TitleBarButton> soundTitleButtons = new();
    private readonly FileDialogManager imagePicker = new();
    private MaterialGlassRenderer? backgroundGlass;
    internal bool BackgroundEnabled => plugin.Configuration.UiStylingEnabled &&
        (colorDraft.HasBackgroundBlur(colorDraft.Mode) || colorDraft.Mode == MaterialColorMode.Glassmorphism && colorDraft.GlassBackground == MaterialGlassBackground.Image);

    private void Apply()
    {
        var language = UiText.Languages.Any(l => l.Code == plugin.Configuration.UiLanguage) ? plugin.Configuration.UiLanguage : "en";
        if (language != appliedLanguage)
        {
            fonts?.Dispose();
            text?.Dispose();
            text = new(language, PushFont);
            fonts = new(Plugin.PluginInterface.UiBuilder.FontAtlas, text.GlyphRanges(), language);
            appliedLanguage = language;
            checkedGeneration = -1;
            fontIssueLogged = false;
        }
        var savedColors = plugin.Configuration.UiColors ?? new MaterialColorSettings();
        if (savedColors != appliedSavedColors)
        {
            colorDraft = savedColors;
            appliedSavedColors = savedColors;
        }
        if (theme is null || appliedAccent != (plugin.Configuration.UiAccentRgb & 0xFFFFFF) || colorDraft != appliedColors)
        {
            appliedAccent = plugin.Configuration.UiAccentRgb & 0xFFFFFF;
            appliedColors = colorDraft;
            theme = ChocoboPresentation.Theme(appliedAccent, colorDraft);
            theme.Feedback = PlayLcarsSound;
            if (!theme.IsLcars) uiSoundPlayer?.Stop();
            var rgb = ChocoboPresentation.Rgb(appliedAccent);
            accentDraft = new(rgb.X, rgb.Y, rgb.Z);
        }
        theme.Density = plugin.Configuration.UiCompact ? MaterialDensity.Compact : MaterialDensity.Standard;
        if (appliedSoundsMuted != plugin.Configuration.UiLcarsSoundsMuted)
        {
            appliedSoundsMuted = plugin.Configuration.UiLcarsSoundsMuted;
            if (appliedSoundsMuted) uiSoundPlayer?.Stop();
        }
    }

    internal void Draw(WindowSystem windows)
    {
        try { DrawCore(windows); }
        catch { backgroundGlass?.EndFrame(false); throw; }
    }

    private void DrawCore(WindowSystem windows)
    {
        Apply();
        if (!windows.Windows.Any(window => window.IsOpen))
        {
            backgroundGlass?.EndFrame(false);
            if (fonts.Ready && checkedGeneration == fonts.Generation)
            {
                using var pickerText = text.Enter();
                using var pickerShaping = shapedText.Push();
                using var pickerTheme = MaterialTheme.Push(theme, ImGuiHelpers.GlobalScale,
                    plugin.Configuration.UiStylingEnabled ? MaterialStyleMode.ColorsOnly : MaterialStyleMode.Native);
                using var pickerBody = fonts.Push(UiFontRole.Body);
                using var pickerBackground = MaterialSkin.PushBackgroundRenderer(null, false, 0);
                imagePicker.Draw();
            }
            return;
        }
        using var resources = text.Enter();
        using var shaping = shapedText.Push();
        if (fonts.Ready && checkedGeneration != fonts.Generation)
        {
            try
            {
                var generation = fonts.Generation;
                foreach (var size in ChocoboPresentation.FontSizes.Distinct())
                    shapedText.Renderer.CheckGlyphs(text.RequiredText, size * ImGuiHelpers.GlobalScale);
                fonts.CheckGlyphs(text.RequiredText);
                var hindiLabel = UiText.Languages.Single(l => l.Code == "hi").Name;
                var hindiAvailable = true;
                foreach (var size in ChocoboPresentation.FontSizes.Distinct())
                    hindiAvailable &= shapedText.Renderer.TryCheckGlyphs([hindiLabel], size * ImGuiHelpers.GlobalScale, out _);
                languages.Replace(UiText.Languages.Select(l => new MaterialOption<string>(l.Code, l.Code,
                    l.Code == "hi" && !hindiAvailable ? "Hindi (unavailable)" : l.Name,
                    l.Code == "hi" && !hindiAvailable)).ToArray());
                checkedGeneration = generation;
            }
            catch (Exception ex)
            {
                if (!fontIssueLogged) { Plugin.Log.Error(ex, "[ChocoboColourized] Required UI glyph coverage failed."); fontIssueLogged = true; }
            }
        }
        using var palette = MaterialTheme.Push(theme, ImGuiHelpers.GlobalScale,
            plugin.Configuration.UiStylingEnabled ? MaterialStyleMode.ColorsOnly : MaterialStyleMode.Native);
        using var chrome = plugin.Configuration.UiStylingEnabled ? MaterialWindowChrome.Push() : default;
        if (!fonts.Ready || checkedGeneration != fonts.Generation)
        {
            backgroundGlass?.EndFrame(false);
            if (!fontIssueLogged && fonts.LoadException is { } error) { Plugin.Log.Error(error, "[ChocoboColourized] Required UI fonts failed to load."); fontIssueLogged = true; }
            ImGui.SetNextWindowSize(new Vector2(460 * ImGuiHelpers.GlobalScale, 0));
            fontStatusMotion.PreDraw("Chocobo Colourized##FontStatus", null, null,
                reducedMotion: !plugin.Configuration.UiStylingEnabled,
                prepareDecorations: plugin.Configuration.UiStylingEnabled ? fontStatusDecorations.Prepare : null);
            bool visible;
            using (var backdrop = MaterialSkin.BackdropStyle())
                visible = ImGui.Begin("Chocobo Colourized##FontStatus", ImGuiWindowFlags.AlwaysAutoResize |
                    (plugin.Configuration.UiWindowsLocked ? ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize : ImGuiWindowFlags.None));
            if (visible)
            {
                MaterialSkin.Backdrop();
                if (plugin.Configuration.UiStylingEnabled) fontStatusDecorations.Paint();
                var failed = fonts.LoadException is not null || fontIssueLogged;
                ImGui.TextWrapped(appliedLanguage == "hi" && failed ? "Hindi UI fonts are unavailable. Use English to continue."
                    : failed ? "UI fonts failed to load. See the plugin log." : "Loading UI fonts...");
                if (appliedLanguage == "hi" && failed && ImGui.Button("Use English"))
                {
                    plugin.Configuration.UiLanguage = "en";
                    plugin.Configuration.Save();
                }
            }
            ImGui.End();
            if (plugin.Configuration.UiStylingEnabled) fontStatusDecorations.Paint();
            fontStatusMotion.PostDraw();
            ApplyWindowOpacity("Chocobo Colourized##FontStatus");
            return;
        }
        var blur = plugin.Configuration.UiStylingEnabled && colorDraft.HasBackgroundBlur(colorDraft.Mode);
        var image = plugin.Configuration.UiStylingEnabled && colorDraft.Mode == MaterialColorMode.Glassmorphism &&
            colorDraft.GlassBackground == MaterialGlassBackground.Image;
        if (blur || image) backgroundGlass ??= new(Plugin.PluginInterface, Plugin.Log);
        backgroundGlass?.ConfigureImage(Plugin.TextureProvider, image, plugin.Configuration.UiColors?.GlassImagePath);
        backgroundGlass?.BeginFrame();
        var restoreBackground = blur || image
            ? windows.Windows.Where(window => (window.Flags & ImGuiWindowFlags.NoBackground) == 0).ToArray()
            : Array.Empty<Window>();
        var completed = false;
        using (MaterialSkin.PushBackgroundRenderer(blur || image ? backgroundGlass : null, blur, colorDraft.BackgroundBlurPercent / 100f))
        {
            try
            {
                using var style = new MaterialStyleScope();
                var s = ImGuiHelpers.GlobalScale;
                if (plugin.Configuration.UiStylingEnabled)
                {
                    style.Style(ImGuiStyleVar.WindowPadding, new Vector2(16) * s);
                    style.Style(ImGuiStyleVar.ItemSpacing, new Vector2(plugin.Configuration.UiCompact ? 8 : 12, plugin.Configuration.UiCompact ? 5 : 10) * s);
                    style.Style(ImGuiStyleVar.FramePadding, new Vector2(plugin.Configuration.UiCompact ? 10 : 14, plugin.Configuration.UiCompact ? 4 : 7) * s);
                    style.Style(ImGuiStyleVar.CellPadding, new Vector2(plugin.Configuration.UiCompact ? 6 : 10, plugin.Configuration.UiCompact ? 4 : 8) * s);
                    style.Style(ImGuiStyleVar.FrameRounding, 4 * s);
                    style.Style(ImGuiStyleVar.ChildRounding, 4 * s);
                    if (ChocoboPresentation.IsMaterial) style.Color(ImGuiCol.ChildBg, Vector4.Zero);
                }
                using var body = fonts.Push(UiFontRole.Body);
                PrepareAppearanceTitleBars(windows);
                foreach (var window in windows.Windows) window.AllowBackgroundBlur = false;
                foreach (var window in restoreBackground) window.Flags |= ImGuiWindowFlags.NoBackground;
                windows.Draw();
                foreach (var (window, titlebar) in appearanceTitleBars)
                {
                    PrepareBackgroundOpacity(window.WindowName);
                    titlebar.DrawPopups();
                }
                foreach (var window in windows.Windows)
                    if (window.IsOpen) ApplyWindowOpacity(window.WindowName);
                completed = true;
            }
            finally
            {
                foreach (var window in restoreBackground) window.Flags &= ~ImGuiWindowFlags.NoBackground;
                backgroundGlass?.EndFrame((blur || image) && completed);
            }
        }
        using (fonts.Push(UiFontRole.Body)) imagePicker.Draw();
    }

    internal float SelectorWidth
    {
        get
        {
            var metrics = ChocoboPresentation.Controls(ChocoboPresentation.SelectorHeight);
            return Math.Max(ChocoboPresentation.LanguageWidth * MaterialTheme.Metrics.Scale, MathF.Ceiling(MaterialText.Measure(languages.LabelFor(appliedLanguage, "Select...")).X
                    + metrics.Height + 3 * metrics.Gap + Math.Min(metrics.IconSize, metrics.Height)));
        }
    }

    internal void DrawSelector(bool includeAccent = true)
    {
        using var controls = MaterialControls.Push(ChocoboPresentation.Controls(ChocoboPresentation.SelectorHeight));
        if (includeAccent) { DrawTitleBarColour(); ImGui.Spacing(); }
        var language = appliedLanguage;
        if (!MaterialAppearanceSelector.DrawLanguage("appearance", ref language, languages, ChocoboPresentation.LanguageWidth)) return;
        plugin.Configuration.UiLanguage = language;
        plugin.Configuration.Save();
    }

    internal ChocoboAppearance(Plugin plugin)
    {
        this.plugin = plugin;
        shapedText = new(Plugin.TextureProvider);
        appliedLanguage = UiText.Languages.Any(l => l.Code == plugin.Configuration.UiLanguage) ? plugin.Configuration.UiLanguage : "en";
        text = new(appliedLanguage, PushFont);
        fonts = new(Plugin.PluginInterface.UiBuilder.FontAtlas, text.GlyphRanges(), appliedLanguage);
        appliedAccent = plugin.Configuration.UiAccentRgb & 0xFFFFFF;
        colorDraft = plugin.Configuration.UiColors ?? new MaterialColorSettings();
        appliedSavedColors = appliedColors = colorDraft;
        theme = ChocoboPresentation.Theme(appliedAccent, colorDraft);
        theme.Feedback = PlayLcarsSound;
        var rgb = ChocoboPresentation.Rgb(appliedAccent);
        accentDraft = new(rgb.X, rgb.Y, rgb.Z);
        Plugin.PluginInterface.UiBuilder.ResizeBuffers += ReleaseBackgroundResources;
    }
    private IDisposable PushFont(UiFontRole role) => fonts.Push(role);
    internal string Label(string key) => text.Label(key);
    internal string Format(string key, params object?[] arguments) => text.Format(key, arguments);

    public void Dispose()
    {
        Plugin.PluginInterface.UiBuilder.ResizeBuffers -= ReleaseBackgroundResources;
        imagePicker.Reset();
        backgroundGlass?.Dispose();
        uiSoundPlayer?.Dispose(); fonts?.Dispose(); text?.Dispose(); shapedText.Dispose();
    }

    private void ReleaseBackgroundResources() => backgroundGlass?.ReleaseResources();

    private bool LcarsActive => plugin.Configuration.UiStylingEnabled && plugin.Configuration.UiColors?.Mode == MaterialColorMode.Lcars;

    internal bool TryApplyCommand(string arguments)
    {
        switch (arguments.Trim().ToLowerInvariant())
        {
            case "lock": plugin.Configuration.UiWindowsLocked = true; break;
            case "unlock": plugin.Configuration.UiWindowsLocked = false; break;
            case "ui": case "style": case "styling": SetStyling(!plugin.Configuration.UiStylingEnabled); return true;
            case "ui on": case "style on": case "styling on": SetStyling(true); return true;
            case "ui off": case "style off": case "styling off": SetStyling(false); return true;
            default: return false;
        }
        plugin.Configuration.Save();
        return true;
    }

    private void SetStyling(bool enabled)
    {
        plugin.Configuration.UiStylingEnabled = enabled;
        if (!enabled) uiSoundPlayer?.Stop();
        plugin.Configuration.Save();
    }

    internal void PrepareBackgroundOpacity(string windowName, bool appearing = false)
    {
        if (backgroundGlass == null) return;
        var owner = ImGuiP.FindWindowByName(windowName);
        appearing |= !owner.IsNull && owner.Appearing;
        backgroundGlass.Opacity = !plugin.Configuration.UiTransparencyEnabled ? 1 : appearing || !windowOpacities.TryGetValue(windowName, out var opacity)
            ? plugin.Configuration.UiWindowOpacityPercent / 100f : opacity.CurrentOpacity;
    }

    private void PlayLcarsSound(MaterialUiSoundCue cue)
    {
        if (!LcarsActive || plugin.Configuration.UiLcarsSoundsMuted) return;
        (uiSoundPlayer ??= new MaterialUiSoundPlayer()).TryPlay(cue);
    }

    internal void SetLcarsSounds(bool enabled)
    {
        plugin.Configuration.UiLcarsSoundsMuted = !enabled;
        if (!enabled) uiSoundPlayer?.Stop();
        plugin.Configuration.Save();
    }

    private void DrawLcarsSounds()
    {
        MaterialSettings.Section(UiText.T("LCARS UI sounds"));
        ImGui.BeginDisabled(!LcarsActive);
        try
        {
            var enabled = !plugin.Configuration.UiLcarsSoundsMuted;
            if (UiGui.Checkbox("UI sounds", ref enabled)) SetLcarsSounds(enabled);
            UiGui.TextWrapped("Custom UI tones. The titlebar speaker toggles the same setting.");
            ImGui.BeginDisabled(plugin.Configuration.UiLcarsSoundsMuted);
            try
            {
                var activeTheme = MaterialTheme.Current;
                var feedback = activeTheme.Feedback;
                bool preview;
                try { activeTheme.Feedback = null; preview = UiGui.Button("Preview sound"); }
                finally { activeTheme.Feedback = feedback; }
                MaterialLcars.Feedback(preview, MaterialUiSoundCue.Preview);
            }
            finally { ImGui.EndDisabled(); }
            if (uiSoundPlayer?.Error is not null) UiGui.TextWrapped("Audio output is unavailable.");
        }
        finally { ImGui.EndDisabled(); }
        if (!LcarsActive) UiGui.TextDisabled("Select LCARS to use UI sounds.");
        else if (plugin.Configuration.UiLcarsSoundsMuted) UiGui.TextDisabled("Turn on UI sounds to preview.");
    }

    private void ApplyWindowOpacity(string windowName)
    {
        if (!windowOpacities.TryGetValue(windowName, out var opacity))
            windowOpacities.Add(windowName, opacity = new MaterialWindowOpacity());
        opacity.Apply(windowName, plugin.Configuration.UiWindowOpacityPercent / 100f,
            plugin.Configuration.UiTransparencyEnabled, plugin.Configuration.UiAutoFade,
            plugin.Configuration.UiFadedOpacityPercent / 100f, plugin.Configuration.UiUnfocusedDelaySeconds);
    }

    internal void DrawTransparencyToggle()
    {
        var enabled = plugin.Configuration.UiTransparencyEnabled;
        if (UiGui.Checkbox("Transparency##MainWindow", ref enabled))
        { plugin.Configuration.UiTransparencyEnabled = enabled; plugin.Configuration.Save(); }
    }

    internal void DrawWindowAppearanceSettings()
    {
        if (!UiGui.CollapsingHeader("Window appearance###UiWindowAppearance")) return;
        var styling = plugin.Configuration.UiStylingEnabled;
        if (MaterialSelection.Checkbox("UI styling", UiText.T("UI styling"), ref styling, Math.Max(1, ImGui.GetContentRegionAvail().X))) SetStyling(styling);
        var locked = plugin.Configuration.UiWindowsLocked;
        if (MaterialSelection.Checkbox("Lock window movement and resizing", UiText.T("Lock window movement and resizing"), ref locked, Math.Max(1, ImGui.GetContentRegionAvail().X)))
        { plugin.Configuration.UiWindowsLocked = locked; plugin.Configuration.Save(); }
        var showTitlebarStatus = plugin.Configuration.UiShowTitlebarStatus;
        if (UiGui.Checkbox(UiText.T("Show titlebar status") + "###UiShowTitlebarStatus", ref showTitlebarStatus))
        { plugin.Configuration.UiShowTitlebarStatus = showTitlebarStatus; plugin.Configuration.Save(); }
        var detailedTitlebarStatus = plugin.Configuration.UiDetailedTitlebarStatus;
        if (UiGui.Checkbox(UiText.T("Detailed titlebar status") + "###UiDetailedTitlebarStatus", ref detailedTitlebarStatus))
        { plugin.Configuration.UiDetailedTitlebarStatus = detailedTitlebarStatus; plugin.Configuration.Save(); }
        var compact = plugin.Configuration.UiCompact;
        if (UiGui.Checkbox("Compact mode", ref compact))
        { plugin.Configuration.UiCompact = compact; plugin.Configuration.Save(); }
        DrawSelector();
        var compactVisible = plugin.Configuration.UiCompactVisibleOnMainWindow;
        if (UiGui.Checkbox("Compact visible on main window", ref compactVisible))
        { plugin.Configuration.UiCompactVisibleOnMainWindow = compactVisible; plugin.Configuration.Save(); }
        var transparencyVisible = plugin.Configuration.UiTransparencyVisibleOnMainWindow;
        if (UiGui.Checkbox("Transparency visible on main window", ref transparencyVisible))
        { plugin.Configuration.UiTransparencyVisibleOnMainWindow = transparencyVisible; plugin.Configuration.Save(); }
        var languageVisible = plugin.Configuration.UiLanguageVisibleOnMainWindow;
        if (UiGui.Checkbox("Language visible on main window", ref languageVisible))
        { plugin.Configuration.UiLanguageVisibleOnMainWindow = languageVisible; plugin.Configuration.Save(); }
        DrawTransparencySettings();
        DrawLcarsSounds();
    }

    private void PrepareAppearanceTitleBars(Dalamud.Interface.Windowing.WindowSystem windows)
    {
        foreach (var window in windows.Windows.OfType<Dalamud.Interface.Windowing.Window>())
        {
            if ((window.Flags & ImGuiWindowFlags.NoTitleBar) != 0) continue;
            if (!appearanceTitleBars.ContainsKey(window))
            {
                var actions = AethertekUI.Dalamud.MaterialAppearanceTitleBar.Register(window,
                    () => new(UiText.T("Language"), UiText.T("Compact mode"), UiText.T("Transparency"), UiText.T("Color")),
                    () => DrawSelector(false),
                    () => { plugin.Configuration.UiCompact = !plugin.Configuration.UiCompact; plugin.Configuration.Save(); },
                    DrawTransparencySettings, DrawTitleBarColour,
                    () => window is global::ChocoboColourized.Windows.MainWindow
                        ? new(plugin.Configuration.UiLanguageVisibleOnMainWindow, plugin.Configuration.UiCompactVisibleOnMainWindow,
                            plugin.Configuration.UiTransparencyVisibleOnMainWindow, true)
                        : new(true, true, true, true));
                actions.RegisterWindowControls(() => SetStyling(!plugin.Configuration.UiStylingEnabled),
                    () => UiText.T(plugin.Configuration.UiStylingEnabled ? "Use native UI" : "Use styled UI"),
                    () => { plugin.Configuration.UiWindowsLocked = !plugin.Configuration.UiWindowsLocked; plugin.Configuration.Save(); },
                    () => UiText.T(plugin.Configuration.UiWindowsLocked ? "Unlock windows" : "Lock windows"));
                appearanceTitleBars.Add(window, actions);
            }
            appearanceTitleBars[window].RefreshVisibility();
            appearanceTitleBars[window].SetLcarsIcons(LcarsActive);
            if (!soundTitleButtons.TryGetValue(window, out var sound))
            {
                sound = new TitleBarButton
                {
                    Priority = (int)Math.Max(int.MinValue + 1L, window.TitleBarButtons.Select(static button => (long)button.Priority).DefaultIfEmpty(0).Min() - 1),
                    IconOffset = new(2, 1),
                    Click = mouse => { if (mouse == ImGuiMouseButton.Left && LcarsActive) SetLcarsSounds(plugin.Configuration.UiLcarsSoundsMuted); },
                    ShowTooltip = () => MaterialText.SetTooltip(UiText.T(plugin.Configuration.UiLcarsSoundsMuted ? "UI sounds muted" : "UI sounds on")),
                };
                soundTitleButtons.Add(window, sound);
            }
            sound.Icon = plugin.Configuration.UiLcarsSoundsMuted ? FontAwesomeIcon.VolumeMute : FontAwesomeIcon.VolumeUp;
            if (LcarsActive && !window.TitleBarButtons.Contains(sound)) window.TitleBarButtons.Add(sound);
            else if (!LcarsActive) window.TitleBarButtons.Remove(sound);
        }
    }

    private void DrawTitleBarColour()
    {
        var changed = MaterialAppearanceSelector.DrawColors("appearance", ref colorDraft, ref accentDraft,
                new(UiText.T("Color"), UiText.T("Language"), UiText.T("Teal"), UiText.T("Blue"), UiText.T("Pink"), UiText.T("Custom RGB")),
                new(UiText.T("Auto"), UiText.T("Picker"), UiText.T("Default"), UiText.T("Background, controls and text follow your theme."),
                    UiText.T("Theme colour"), UiText.T("Choose each colour independently."), UiText.T("Background"), UiText.T("Controls"),
                    UiText.T("Text"), UiText.T("Aa  Preview text"), UiText.T("Control"),
                    UiText.T("Switch to Picker to set each colour yourself."), UiText.T("Switch back to Auto to use the automatic theme."))
                    {
                        Lcars = UiText.T("LCARS"), LcarsHelp = UiText.T("LCARS panels, bars and controls"),
                        Glassmorphism = UiText.T("Glassmorphism"), Spatial = UiText.T("Spatial"), Cyberpunk = UiText.T("Cyberpunk"),
                        LcarsBar1 = UiText.T("LCARS bar 1"), LcarsBar2 = UiText.T("LCARS bar 2"), LcarsBar3 = UiText.T("LCARS bar 3"),
                        Reset = UiText.T("Reset"), ResetColors = UiText.T("Reset colours"),
                        Animation = UiText.T("Animation"), AnimationSpeed = UiText.T("Animation speed (0.1–10x; default 1x)"),
                        WaveColors = UiText.T("Wave colours"), GlassWave1 = UiText.T("Wave colour 1"),
                        GlassWave2 = UiText.T("Wave colour 2"), GlassWave3 = UiText.T("Wave colour 3"),
                        ResetWaveColors = UiText.T("Reset wave colours"),
                        BackgroundMode = UiText.T("Background mode"), Waves = UiText.T("Waves"), Lights = UiText.T("Lights"), Image = UiText.T("Image"),
                        LightEffect = UiText.T("Light effect"), AmbientGlows = UiText.T("Ambient glows"), SweepingBeams = UiText.T("Sweeping beams"),
                        PointerGlow = UiText.T("Mouse-following glow"), ImageFile = UiText.T("Image file"), BrowseImage = UiText.T("Browse..."),
                        LoadImage = UiText.T("Load image"), ImageHelp = UiText.T("The image fills the window. Blur (%) applies to the image."),
                        LightColors = UiText.T("Light colours"), GlassLight1 = UiText.T("Light colour 1"), GlassLight2 = UiText.T("Light colour 2"),
                        GlassLight3 = UiText.T("Light colour 3"), ResetLightColors = UiText.T("Reset light colours"),
                        BackgroundBlur = UiText.T("Background blur"), BlurPercent = UiText.T("Blur (%)"),
                    }, new MaterialColorSettings(), MaterialColorSettings.Rgb(ChocoboPresentation.ReferenceAccent),
                BrowseImage, plugin.Configuration.UiCompact ? 298 : 316);
        if (changed)
        {
            plugin.Configuration.UiColors = colorDraft;
            plugin.Configuration.UiAccentRgb = MaterialColorSettings.ToRgb(accentDraft);
            plugin.Configuration.Save();
        }
        DrawScreenshotPause("colour");
        if (backgroundGlass?.ImageProblem is { } problem) UiGui.TextWrapped(problem);
    }

    private void BrowseImage()
        => imagePicker.OpenFileDialog(UiText.T("Choose background image") + "###ChocoboColourizedBackgroundImagePicker",
            UiText.T("Images") + "{.png,.jpg,.jpeg,.bmp,.gif,.webp,.tif,.tiff,.dds,.tga},.*", (accepted, path) =>
            {
                if (!accepted) return;
                var previous = plugin.Configuration.UiColors?.GlassImagePath;
                plugin.Configuration.UiColors = colorDraft = colorDraft with { GlassImagePath = path };
                if (path != previous) plugin.Configuration.Save();
            });

    private void DrawScreenshotPause(string id)
    {
        var paused = backgroundGlass?.ScreenshotsPaused == true;
        if (!paused && colorDraft.Mode == MaterialColorMode.Glassmorphism && colorDraft.GlassBackground == MaterialGlassBackground.Image)
        { UiGui.TextWrapped("Image backgrounds remain visible to screenshots."); return; }
        ImGui.PushID(id);
        ImGui.BeginDisabled(!paused && backgroundGlass?.CanPauseForScreenshots != true);
        try
        {
            if (MaterialSelection.Checkbox("##pause-backdrop", UiText.T("Pause backdrop for screenshots"), ref paused,
                Math.Max(1, ImGui.GetContentRegionAvail().X))) backgroundGlass?.TrySetScreenshotsPaused(paused);
        }
        finally { ImGui.EndDisabled(); ImGui.PopID(); }
    }

    private void DrawTransparencySettings()
    {
        DrawScreenshotPause("transparency");
        var enabled = plugin.Configuration.UiTransparencyEnabled;
        if (UiGui.Checkbox("Transparency", ref enabled))
        { plugin.Configuration.UiTransparencyEnabled = enabled; plugin.Configuration.Save(); }
        MaterialText.Text(UiText.T("Opacity (%)"));
        ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(160 * MaterialTheme.Metrics.Scale, 80 * MaterialTheme.Metrics.Scale));
        var normalOpacity = plugin.Configuration.UiWindowOpacityPercent;
        using (var field = MaterialSkin.NativeFieldStyle())
        {
            if (ImGui.InputInt("##UiWindowOpacityPercent", ref normalOpacity))
            { plugin.Configuration.UiWindowOpacityPercent = normalOpacity; plugin.Configuration.Save(); }
        }
        var autoFade = plugin.Configuration.UiAutoFade;
        if (UiGui.Checkbox("Auto-fade when unfocused", ref autoFade))
        { plugin.Configuration.UiAutoFade = autoFade; plugin.Configuration.Save(); }
        ImGui.BeginDisabled(!autoFade);
        MaterialText.Text(UiText.T("Unfocused opacity (%)"));
        ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(160 * MaterialTheme.Metrics.Scale, 80 * MaterialTheme.Metrics.Scale));
        var fadedOpacity = plugin.Configuration.UiFadedOpacityPercent;
        using (var field = MaterialSkin.NativeFieldStyle())
        {
            if (ImGui.InputInt("##UiFadedOpacityPercent", ref fadedOpacity))
            { plugin.Configuration.UiFadedOpacityPercent = fadedOpacity; plugin.Configuration.Save(); }
        }
        MaterialText.Text(UiText.T("Unfocused delay (seconds)"));
        ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(160 * MaterialTheme.Metrics.Scale, 80 * MaterialTheme.Metrics.Scale));
        var delay = plugin.Configuration.UiUnfocusedDelaySeconds;
        using (var field = MaterialSkin.NativeFieldStyle())
        {
            if (ImGui.InputInt("##UiUnfocusedDelaySeconds", ref delay))
            { plugin.Configuration.UiUnfocusedDelaySeconds = delay; plugin.Configuration.Save(); }
        }
        ImGui.EndDisabled();
    }
}
