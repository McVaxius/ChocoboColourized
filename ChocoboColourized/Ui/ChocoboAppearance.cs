using System.Numerics;
using AethertekUI;
using AethertekUI.Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace ChocoboColourized.Ui;

internal sealed class ChocoboAppearance : IDisposable
{
    private readonly Plugin plugin;
    private readonly MaterialTextHost shapedText;
    private UiText text;
    private ChocoboFonts fonts;
    private MaterialTheme theme;
    private readonly MaterialWindowFold fontStatusMotion = new();
    private readonly MaterialWindowDecorations fontStatusDecorations = new();
    private readonly Dictionary<string, MaterialWindowOpacity> windowOpacities = new();
    private readonly MaterialOptions<string> languages = new(UiText.Languages.Select(l => new MaterialOption<string>(l.Code, l.Code, l.Name)).ToArray());
    private string appliedLanguage = "";
    private uint appliedAccent;
    private Vector3 accentDraft;
    private int checkedGeneration = -1;
    private bool fontIssueLogged;

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
        if (theme is null || appliedAccent != (plugin.Configuration.UiAccentRgb & 0xFFFFFF))
        {
            appliedAccent = plugin.Configuration.UiAccentRgb & 0xFFFFFF;
            theme = ChocoboPresentation.Theme(appliedAccent);
            var rgb = ChocoboPresentation.Rgb(appliedAccent);
            accentDraft = new(rgb.X, rgb.Y, rgb.Z);
        }
        theme.Density = plugin.Configuration.UiCompact ? MaterialDensity.Compact : MaterialDensity.Standard;
    }

    internal void Draw(WindowSystem windows)
    {
        Apply();
        if (!windows.Windows.Any(window => window.IsOpen)) return;
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
        using var palette = MaterialTheme.Push(theme, ImGuiHelpers.GlobalScale, MaterialStyleMode.ColorsOnly);
        using var chrome = MaterialWindowChrome.Push();
        if (!fonts.Ready || checkedGeneration != fonts.Generation)
        {
            if (!fontIssueLogged && fonts.LoadException is { } error) { Plugin.Log.Error(error, "[ChocoboColourized] Required UI fonts failed to load."); fontIssueLogged = true; }
            ImGui.SetNextWindowSize(new Vector2(460 * ImGuiHelpers.GlobalScale, 0));
            fontStatusMotion.PreDraw("Chocobo Colourized##FontStatus", null, null, reducedMotion: false, prepareDecorations: fontStatusDecorations.Prepare);
            if (ImGui.Begin("Chocobo Colourized##FontStatus", ImGuiWindowFlags.AlwaysAutoResize))
            {
                fontStatusDecorations.Paint();
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
            fontStatusDecorations.Paint();
            fontStatusMotion.PostDraw();
            ApplyWindowOpacity("Chocobo Colourized##FontStatus");
            return;
        }
        using var style = new MaterialStyleScope();
        var s = ImGuiHelpers.GlobalScale;
        style.Style(ImGuiStyleVar.WindowPadding, new Vector2(16) * s);
        style.Style(ImGuiStyleVar.ItemSpacing, new Vector2(plugin.Configuration.UiCompact ? 8 : 12, plugin.Configuration.UiCompact ? 5 : 10) * s);
        style.Style(ImGuiStyleVar.FramePadding, new Vector2(plugin.Configuration.UiCompact ? 10 : 14, plugin.Configuration.UiCompact ? 4 : 7) * s);
        style.Style(ImGuiStyleVar.CellPadding, new Vector2(plugin.Configuration.UiCompact ? 6 : 10, plugin.Configuration.UiCompact ? 4 : 8) * s);
        style.Style(ImGuiStyleVar.FrameRounding, 4 * s);
        style.Style(ImGuiStyleVar.ChildRounding, 4 * s);
        using var body = fonts.Push(UiFontRole.Body);
        windows.Draw();
        foreach (var window in windows.Windows)
            if (window.IsOpen) ApplyWindowOpacity(window.WindowName);
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
        var language = appliedLanguage;
        using var controls = MaterialControls.Push(ChocoboPresentation.Controls(ChocoboPresentation.SelectorHeight));
        var labels = new MaterialAppearanceLabels(UiText.T("Color"), UiText.T("Language"), UiText.T("Teal"), UiText.T("Blue"), UiText.T("Pink"), UiText.T("Custom RGB"));
        var rowY = ImGui.GetCursorPosY();
        var accentChanged = false;
        if (includeAccent)
        {
            ImGui.SetCursorPosY(rowY + (ChocoboPresentation.SelectorHeight - ChocoboPresentation.AccentSize) * .5f * MaterialTheme.Metrics.Scale);
            accentChanged = MaterialAppearanceSelector.DrawAccent("appearance", ref accentDraft, labels, ChocoboPresentation.AccentSize);
            ImGui.SameLine();
        }
        ImGui.SetCursorPosY(rowY);
        var languageChanged = MaterialAppearanceSelector.DrawLanguage("appearance", ref language, languages, ChocoboPresentation.LanguageWidth);
        var changed = new MaterialAppearanceChange(accentChanged, languageChanged);
        if (changed.AccentChanged)
            plugin.Configuration.UiAccentRgb = ((uint)Math.Clamp((int)MathF.Round(accentDraft.X * 255), 0, 255) << 16)
                | ((uint)Math.Clamp((int)MathF.Round(accentDraft.Y * 255), 0, 255) << 8) | (uint)Math.Clamp((int)MathF.Round(accentDraft.Z * 255), 0, 255);
        if (changed.LanguageChanged) plugin.Configuration.UiLanguage = language;
        if (changed.AccentChanged || changed.LanguageChanged) plugin.Configuration.Save();
    }

    internal ChocoboAppearance(Plugin plugin)
    {
        this.plugin = plugin;
        shapedText = new(Plugin.TextureProvider);
        appliedLanguage = UiText.Languages.Any(l => l.Code == plugin.Configuration.UiLanguage) ? plugin.Configuration.UiLanguage : "en";
        text = new(appliedLanguage, PushFont);
        fonts = new(Plugin.PluginInterface.UiBuilder.FontAtlas, text.GlyphRanges(), appliedLanguage);
        appliedAccent = plugin.Configuration.UiAccentRgb & 0xFFFFFF;
        theme = ChocoboPresentation.Theme(appliedAccent);
        var rgb = ChocoboPresentation.Rgb(appliedAccent);
        accentDraft = new(rgb.X, rgb.Y, rgb.Z);
    }
    private IDisposable PushFont(UiFontRole role) => fonts.Push(role);
    internal string Label(string key) => text.Label(key);
    internal string Format(string key, params object?[] arguments) => text.Format(key, arguments);

    public void Dispose() { fonts?.Dispose(); text?.Dispose(); shapedText.Dispose(); }

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
        var compact = plugin.Configuration.UiCompact;
        if (UiGui.Checkbox("Compact mode", ref compact))
        { plugin.Configuration.UiCompact = compact; plugin.Configuration.Save(); }
        DrawSelector();
        var compactVisible = plugin.Configuration.UiCompactVisibleOnMainWindow;
        if (UiGui.Checkbox("Compact visible on main window", ref compactVisible))
        { plugin.Configuration.UiCompactVisibleOnMainWindow = compactVisible; plugin.Configuration.Save(); }
        var languageVisible = plugin.Configuration.UiLanguageVisibleOnMainWindow;
        if (UiGui.Checkbox("Language visible on main window", ref languageVisible))
        { plugin.Configuration.UiLanguageVisibleOnMainWindow = languageVisible; plugin.Configuration.Save(); }
        var enabled = plugin.Configuration.UiTransparencyEnabled;
        if (UiGui.Checkbox("Transparency", ref enabled))
        { plugin.Configuration.UiTransparencyEnabled = enabled; plugin.Configuration.Save(); }
        MaterialText.Text(UiText.T("Opacity (%)"));
        ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(160 * MaterialTheme.Metrics.Scale, 80 * MaterialTheme.Metrics.Scale));
        var normalOpacity = plugin.Configuration.UiWindowOpacityPercent;
        if (ImGui.InputInt("##UiWindowOpacityPercent", ref normalOpacity))
        { plugin.Configuration.UiWindowOpacityPercent = normalOpacity; plugin.Configuration.Save(); }
        var autoFade = plugin.Configuration.UiAutoFade;
        if (UiGui.Checkbox("Auto-fade when unfocused", ref autoFade))
        { plugin.Configuration.UiAutoFade = autoFade; plugin.Configuration.Save(); }
        ImGui.BeginDisabled(!autoFade);
        MaterialText.Text(UiText.T("Unfocused opacity (%)"));
        ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(160 * MaterialTheme.Metrics.Scale, 80 * MaterialTheme.Metrics.Scale));
        var fadedOpacity = plugin.Configuration.UiFadedOpacityPercent;
        if (ImGui.InputInt("##UiFadedOpacityPercent", ref fadedOpacity))
        { plugin.Configuration.UiFadedOpacityPercent = fadedOpacity; plugin.Configuration.Save(); }
        MaterialText.Text(UiText.T("Unfocused delay (seconds)"));
        ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(160 * MaterialTheme.Metrics.Scale, 80 * MaterialTheme.Metrics.Scale));
        var delay = plugin.Configuration.UiUnfocusedDelaySeconds;
        if (ImGui.InputInt("##UiUnfocusedDelaySeconds", ref delay))
        { plugin.Configuration.UiUnfocusedDelaySeconds = delay; plugin.Configuration.Save(); }
        ImGui.EndDisabled();
    }
}
