using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ChocoboColourized.Core;
using ChocoboColourized.Models;
using ChocoboColourized.Services;
using ChocoboColourized.Ui;
using AethertekUI;
using AethertekUI.Dalamud;

namespace ChocoboColourized.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly MaterialWindowMotion windowMotion = new();
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
    private uint calculatorRootId;


    // Colours for inventory status
    private static readonly Vector4 ColorRed = new(1f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 ColorYellow = new(1f, 1f, 0.3f, 1f);
    private static readonly Vector4 ColorGreen = new(0.3f, 1f, 0.3f, 1f);
    private static Vector4 ColorGrey => MaterialTheme.Current.Colors.OnSurfaceVariant;
    private static Vector4 ColorWhite => MaterialTheme.Current.Colors.OnSurface;
    private static readonly Vector4 ColorCyan = new(0.3f, 1f, 1f, 1f);

    public MainWindow(Plugin plugin)
        : base("Chocobo Colourized##MainWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Size = plugin.Configuration.UiCompact ? new Vector2(1442, 858) : new Vector2(1515, 1012);
        SizeCondition = ImGuiCond.FirstUseEver;
        this.plugin = plugin;
        colorNames = ColorDatabase.AllColorNames;
        TitleBarButtons.Add(new()
        {
            Icon = FontAwesomeIcon.Cog, Priority = 0, IconOffset = new(2, 1),
            Click = button => { if (button == ImGuiMouseButton.Left) plugin.ToggleConfigUi(); },
            ShowTooltip = () => MaterialText.SetTooltip(UiText.T("Chocobo Colourized Settings")),
        });
        TitleBarButtons.Add(new()
        {
            Icon = FontAwesomeIcon.Play, Priority = -10, IconOffset = new(2, 1),
            Click = button => { if (button == ImGuiMouseButton.Left) StartFeedingFromUi(); },
            ShowTooltip = () => ShowFeedingTitleTooltip(start: true),
        });
        TitleBarButtons.Add(new()
        {
            Icon = FontAwesomeIcon.Stop, Priority = -20, IconOffset = new(2, 1),
            Click = button => { if (button == ImGuiMouseButton.Left) StopFeedingFromUi(); },
            ShowTooltip = () => ShowFeedingTitleTooltip(start: false),
        });
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        UiGui.ReserveTitleSpace(this, UiText.T("Chocobo Colourized") + " v" + typeof(Plugin).Assembly.GetName().Version, 520);
        windowMotion.Prepare(this, reducedMotion: false, roundedCorners: true);
    }

    public override void PostDraw()
    {
        windowMotion.Restore(this);
        UiGui.PaintTitleWithImage(this, UiText.T("Chocobo Colourized") + " v" + (typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "0.0.0.0"));
    }

    public override void Draw()
    {
        windowMotion.DrawChrome();
        DrawHeader();
        using var tabFont = UiText.Font(UiFontRole.Action);
        using var tabs = new MaterialStyleScope();
        tabs.Style(ImGuiStyleVar.FramePadding, new Vector2(16, ChocoboPresentation.Compact ? 12 : 16) * MaterialTheme.Metrics.Scale);
        tabs.Style(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, ImGui.GetStyle().ItemInnerSpacing.Y));
        if (ChocoboPresentation.Compact) ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 2 * MaterialTheme.Metrics.Scale);
        bool tabsOpen;
        using (MaterialText.PushLineHeight(UiText.T("Calculator"), UiText.T("Timers"), UiText.T("Automation")))
            tabsOpen = ImGui.BeginTabBar("MainTabs", ImGuiTabBarFlags.FittingPolicyScroll);
        if (tabsOpen)
        {
            if (UiGui.TabItem("Calculator", MaterialIcon.Calculator, ChocoboPresentation.Compact ? 256 : 260))
            {
                using (UiText.Font(UiFontRole.Body))
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ChocoboPresentation.Compact ? 10 : 4) * MaterialTheme.Metrics.Scale);
                    DrawCalculatorTab();
                }
                ImGui.EndTabItem();
            }
            if (UiGui.TabItem("Timers", MaterialIcon.Clock, ChocoboPresentation.Compact ? 200 : 212))
            {
                using (UiText.Font(UiFontRole.Body)) DrawTimersTab();
                ImGui.EndTabItem();
            }
            var automationFlags = selectAutomationTabNextFrame ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
            if (UiGui.TabItem("Automation", MaterialIcon.Settings, 280, automationFlags))
            {
                selectAutomationTabNextFrame = false;
                using (UiText.Font(UiFontRole.Body)) DrawAutomationTab();
                ImGui.EndTabItem();
            }
            else if (selectAutomationTabNextFrame)
                selectAutomationTabNextFrame = false;
            ImGui.EndTabBar();
        }
    }

    private void DrawHeader()
    {
        var s = MaterialTheme.Metrics.Scale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        ChocoboPresentation.Feather(origin + new Vector2(ChocoboPresentation.Compact ? 9 : 13, ChocoboPresentation.Compact ? -1 : 8) * s, (ChocoboPresentation.Compact ? 60 : 56) * s);
        var titleX = (ChocoboPresentation.Compact ? 82 : 88) * s;
        ImGui.SetCursorScreenPos(origin + new Vector2(titleX, 0));
        using (UiText.Font(ChocoboPresentation.Compact ? UiFontRole.CompactTitle : UiFontRole.Title))
            UiGui.TextUnformatted("Chocobo Colourized");
        var titleRight = ImGui.GetItemRectMax().X;
        ImGui.SetCursorScreenPos(origin + new Vector2(titleX, (ChocoboPresentation.Compact ? 40 : 44) * s));
        UiGui.TextColored(MaterialTheme.Current.Colors.OnSurfaceVariant, "Plan your chocobo's colour with precision.");
        var headerHeight = Math.Max(ChocoboPresentation.HeaderHeight * s, ImGui.GetItemRectMax().Y - origin.Y);
        var selectorWidth = plugin.Configuration.UiLanguageVisibleOnMainWindow ? plugin.Appearance.SelectorWidth : 0;
        var supportWidth = MaterialText.Measure(UiText.T("Support on Ko-fi")).X + 92 * s;
        var compactWidth = plugin.Configuration.UiCompactVisibleOnMainWindow
            ? ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X + MaterialText.Measure("C").X + ImGui.GetStyle().ItemSpacing.X : 0;
        var opacityWidth = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X + MaterialText.Measure(UiText.T("Transparency")).X;
        var controlWidth = compactWidth + opacityWidth + supportWidth + selectorWidth
            + ImGui.GetStyle().ItemSpacing.X * (plugin.Configuration.UiLanguageVisibleOnMainWindow ? 2 : 1);
        var headingRight = Math.Max(titleRight, ImGui.GetItemRectMax().X);
        var wrap = headingRight + 20 * s + controlWidth > origin.X + width;
        ImGui.SetCursorScreenPos(origin + new Vector2(wrap ? 0 : width - controlWidth, wrap ? headerHeight : (ChocoboPresentation.Compact ? 21 : 19) * s));
        if (plugin.Configuration.UiCompactVisibleOnMainWindow)
        {
            var compact = plugin.Configuration.UiCompact;
            if (UiGui.Checkbox("C##CompactMode", ref compact)) { plugin.Configuration.UiCompact = compact; plugin.Configuration.Save(); }
            if (ImGui.IsItemHovered()) UiGui.SetTooltip("Compact mode");
            if (ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + opacityWidth <= origin.X + width) ImGui.SameLine();
        }
        plugin.Appearance.DrawTransparencyToggle();
        if (ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + supportWidth <= origin.X + width) ImGui.SameLine();
        if (UiGui.IconButton("\u2661 Ko-fi \u2661", MaterialIcon.Heart, new Vector2(supportWidth, ImGui.GetFrameHeight()), MaterialIcon.ExternalLink, UiText.T("Support on Ko-fi")))
            Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/mcvaxius", UseShellExecute = true });
        if (ImGui.IsItemHovered()) UiGui.SetTooltip("Support development on Ko-fi");
        if (plugin.Configuration.UiLanguageVisibleOnMainWindow)
        {
            if (ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + selectorWidth <= origin.X + width) ImGui.SameLine();
            plugin.Appearance.DrawSelector(false);
        }
        var bottom = Math.Max(ImGui.GetItemRectMax().Y, origin.Y + headerHeight);
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, Math.Max(ChocoboPresentation.HeaderHeight * s, bottom - origin.Y)));
        if (ChocoboPresentation.Compact) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6 * s);
    }

    private void DrawCalculatorTab()
    {
        var s = MaterialTheme.Metrics.Scale;
        float footerHeight;
        using (UiGui.FooterFont())
        {
            footerHeight = ResultActionsFit() ? Math.Max(
                    UiGui.IconButtonHeight("Save Plan & Switch to Automation", ImGui.GetContentRegionAvail().X),
                    UiGui.IconButtonHeight("Copy Results to Clipboard", ImGui.GetContentRegionAvail().X)) + ChocoboPresentation.FooterGap * s
                : UiGui.IconButtonHeight("Save Plan & Switch to Automation", ImGui.GetContentRegionAvail().X)
                    + UiGui.IconButtonHeight("Copy Results to Clipboard", ImGui.GetContentRegionAvail().X)
                    + ImGui.GetStyle().ItemSpacing.Y + ChocoboPresentation.FooterGap * s;
            if (ResultActionsMinimumWidth() > ImGui.GetContentRegionAvail().X)
                footerHeight += ImGui.GetStyle().ScrollbarSize;
        }
        var parentId = ImGui.GetID("");
        calculatorRootId = parentId;
        if (ImGui.BeginChild("##CalculatorScroll", new Vector2(0, Math.Max(1, ImGui.GetContentRegionAvail().Y - footerHeight)), false))
        {
            ImGuiP.PushOverrideID(parentId);
            var panelWidth = Math.Max(1, ImGui.GetContentRegionAvail().X - ChocoboPresentation.PanelPadding.X * 2 * s);
            var horizontal = ColorColumnsFit(panelWidth);
            var colourRows = ImGui.GetTextLineHeight() * 2 + (ChocoboPresentation.Compact ? 77 : 93) * s + 16 * s;
            var columnPadding = (ChocoboPresentation.Compact ? 25 : 29.5f) * s;
            var actionWeight = ChocoboPresentation.Compact ? 512 : 505;
            var actionWidth = horizontal ? Math.Max(1, MathF.Floor((panelWidth + columnPadding * 2) * actionWeight / 1488 - columnPadding * 2) - 2) : panelWidth;
            var actionBody = ImGui.GetTextLineHeight() + (ChocoboPresentation.Compact ? 104 : 114) * s;
            using (UiText.Font(UiFontRole.Counter))
            {
                if (MaterialText.Measure(UiText.T("Calculate Feeding Path")).X + 80 * s > actionWidth + (horizontal ? 4 * s : 0))
                    actionBody += ImGui.GetStyle().ScrollbarSize;
                if (currentColorIndex == targetColorIndex)
                    actionBody += MaterialText.Measure(UiText.T("Current and target colours are the same!"), false, actionWidth).Y + 4 * s;
                if (!string.IsNullOrEmpty(statusMessage))
                    actionBody += MaterialText.Measure(UiText.T(statusMessage), false, actionWidth).Y + 4 * s;
            }
            // The three stacked groups also retain their native Spacing items.
            var colourBody = (horizontal ? Math.Max(colourRows, actionBody) + 4 * s : colourRows * 2 + actionBody + 12 * s)
                + (ChocoboPresentation.Compact ? 5 : 10) * s;
            var colourHeight = Math.Max(ChocoboPresentation.Compact ? 227 : 264,
                PanelHeight("Colour Calculation", "Select your chocobo's current and target colours, then calculate the feeding path.", colourBody) / s);
            Panel("##ColourCalculation", colourHeight, () =>
            {
                Heading("Colour Calculation", "Select your chocobo's current and target colours, then calculate the feeding path.");
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ChocoboPresentation.Compact ? 5 : 10) * s);
                DrawColorSelection();
            });
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + Math.Max(0, ChocoboPresentation.Gap * s - 4 * s));
            var fruitHeight = Math.Max(ChocoboPresentation.Compact ? 191 : 230,
                PanelHeight("Required Fruits", "The following fruits are required for this colour change.", (ChocoboPresentation.Compact ? 105 : 126) * s) / s);
            Panel("##RequiredFruits", fruitHeight, () =>
            {
                Heading("Required Fruits", "The following fruits are required for this colour change.");
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ChocoboPresentation.Compact ? 3 : 8) * s);
                if (lastResult is null)
                    DrawEmptyFruitTable();
                else
                {
                    DrawInventoryRequirements();
                    DrawPlanButtons();
                    if (lastResult.TotalFruits == 0) UiGui.TextColored(ColorGreen, "Already at the closest possible colour!");
                    if (UiGui.TreeNode("Calculation details"))
                    {
                        ImGuiP.PushOverrideID(calculatorRootId);
                        DrawResults();
                        ImGui.PopID();
                        ImGui.TreePop();
                    }
                }
            });
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + Math.Max(0, ChocoboPresentation.Gap * s - 4 * s));
            var emptyPadding = (ChocoboPresentation.Compact ? 8 : 12) * s;
            var emptyWidth = Math.Max(1, ImGui.GetContentRegionAvail().X - ChocoboPresentation.PanelPadding.X * 2 * s
                - emptyPadding * 2 - 40 * s - 1 - ImGui.GetStyle().ChildBorderSize * 2);
            var emptyHeight = MathF.Ceiling(Math.Max(24 * s, MaterialText.Measure(UiText.T("Your feeding order will appear here."), false, emptyWidth).Y)
                + emptyPadding * 2 + ImGui.GetStyle().ChildBorderSize * 2);
            var feedBodyHeight = Math.Max((ChocoboPresentation.Compact ? 56 : 80) * s, lastResult is null ? emptyHeight : 0);
            var minimumFeedHeight = PanelHeight("Feeding order", "Feed the following fruits to your chocobo in order.",
                feedBodyHeight + (ChocoboPresentation.Compact ? 0 : 2) * s) / s;
            var feedHeight = Math.Max(ChocoboPresentation.Compact ? 139 : 180, minimumFeedHeight);
            if (lastResult is null && ImGui.GetContentRegionAvail().Y / s >= minimumFeedHeight)
                feedHeight = Math.Min(feedHeight, ImGui.GetContentRegionAvail().Y / s);
            Panel("##FeedingOrderPanel", feedHeight, () =>
            {
                Heading("Feeding order", "Feed the following fruits to your chocobo in order.");
                if (!ChocoboPresentation.Compact) ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2 * s);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(ChocoboPresentation.Compact ? 8 : 12) * s);
                if (ImGui.BeginChild("FeedingOrder", new Vector2(-1, lastResult is null ? feedBodyHeight : 0), true))
                {
                    if (lastResult is null) DrawEmptyFeedingOrder();
                    else for (var index = 0; index < lastResult.Fruits.Count; index++)
                        UiGui.Text(UiText.F("{0}. {1}", index + 1, FruitData.GetDisplayName(lastResult.Fruits[index])));
                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
            });
            ImGui.PopID();
        }
        ImGui.EndChild();
        DrawResultActions();
    }

    private static float PanelHeight(string title, string subtitle, float bodyHeight)
    {
        var s = MaterialTheme.Metrics.Scale;
        var padding = ChocoboPresentation.PanelPadding * s;
        var width = Math.Max(1, ImGui.GetContentRegionAvail().X - padding.X * 2);
        float titleHeight;
        using (UiText.Font(UiFontRole.PluginName))
        {
            var fontScale = ImGuiP.GetCurrentWindow().FontWindowScale;
            ImGui.SetWindowFontScale(fontScale * ChocoboPresentation.HeadingHeight / ChocoboPresentation.AtlasHeight(UiFontRole.PluginName));
            try { titleHeight = MaterialText.Measure(UiText.T(title), false, width).Y; }
            finally { ImGui.SetWindowFontScale(fontScale); }
        }
        return MathF.Ceiling(padding.Y * 2 + titleHeight + ChocoboPresentation.HeadingAdvance * s
            + MaterialText.Measure(UiText.T(subtitle), false, width).Y + 8 * s + bodyHeight);
    }

    private static void Panel(string id, float logicalHeight, Action draw)
    {
        var parentId = ImGui.GetID("");
        var s = MaterialTheme.Metrics.Scale;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ChocoboPresentation.PanelPadding * s);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 4) * s);
        if (ImGui.BeginChild(id, new Vector2(0, logicalHeight * s), true, ImGuiWindowFlags.AlwaysUseWindowPadding))
        {
            ImGuiP.PushOverrideID(parentId);
            draw();
            ImGui.PopID();
        }
        ImGui.EndChild();
        ImGui.PopStyleVar(2);
    }

    private static void Heading(string title, string subtitle)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + MaterialTheme.Metrics.Scale);
        using (UiText.Font(UiFontRole.PluginName))
        {
            var fontScale = ImGuiP.GetCurrentWindow().FontWindowScale;
            ImGui.SetWindowFontScale(fontScale * ChocoboPresentation.HeadingHeight / ChocoboPresentation.AtlasHeight(UiFontRole.PluginName));
            try { UiGui.TextWrapped(title); }
            finally { ImGui.SetWindowFontScale(fontScale); }
        }
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ChocoboPresentation.HeadingAdvance - 1) * MaterialTheme.Metrics.Scale);
        UiGui.TextColored(MaterialTheme.Current.Colors.OnSurfaceVariant, subtitle);
    }

    private static void DrawEmptyFruitTable()
    {
        var s = MaterialTheme.Metrics.Scale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var panelClip = ImGuiP.GetCurrentWindow().ClipRect;
        var headers = new[] { "Fruit", "Required", "Owned", "Status" };
        var weights = new[] { 1.4f, 1f, 1f, 1f };
        float[] minimums;
        using (UiText.Font(UiFontRole.BodyStrong))
            minimums = headers.Select(header => MaterialText.Measure(UiText.T(header)).X + 2 * ImGui.GetStyle().CellPadding.X).ToArray();
        if (minimums.Where((minimum, index) => width * weights[index] / weights.Sum() < minimum).Any())
            weights = minimums;
        if (ImGui.BeginTable("FruitReq", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            for (var index = 0; index < headers.Length; index++)
                ImGui.TableSetupColumn(headers[index], ImGuiTableColumnFlags.WidthStretch, weights[index]);
            var rowHeight = (ChocoboPresentation.Compact ? 66 : 74) * s;
            UiGui.TableHeadersRow((ChocoboPresentation.Compact ? 36 : 40) * s);
            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);
            ImGui.TableNextColumn();
            var rowTop = ImGui.GetCursorScreenPos().Y;
            var label = UiText.T("Select colours and click Calculate.");
            var wrapWidth = Math.Max(1, width - 24 * s);
            var textSize = MaterialText.Measure(label, false, wrapWidth);
            var position = new Vector2(origin.X + Math.Max(12 * s, (width - textSize.X) * .5f), rowTop + Math.Max(0, (rowHeight - textSize.Y) * .5f));
            var drawList = ImGui.GetWindowDrawList();
            var clipMin = Vector2.Max(new Vector2(origin.X, rowTop), panelClip.Min);
            var clipMax = Vector2.Min(new Vector2(origin.X + width, rowTop + rowHeight), panelClip.Max);
            if (clipMin.X < clipMax.X && clipMin.Y < clipMax.Y)
            {
                drawList.PushClipRect(clipMin, clipMax, false);
                MaterialText.AddText(drawList, ImGui.GetFont(), ImGui.GetFontSize(), position, MaterialCanvas.Color(ColorGrey), label, wrapWidth);
                drawList.PopClipRect();
            }
            ImGui.EndTable();
        }
    }

    private static bool ResultActionsFit()
    {
        var s = MaterialTheme.Metrics.Scale;
        return MaterialText.Measure(UiText.T("Save Plan & Switch to Automation")).X
            + MaterialText.Measure(UiText.T("Copy Results to Clipboard")).X
            + (ChocoboPresentation.FooterSavePadding + ChocoboPresentation.FooterCopyPadding + ChocoboPresentation.FooterActionGap) * s <= ImGui.GetContentRegionAvail().X;
    }

    private static float ResultActionsMinimumWidth()
        => Math.Max(MaterialText.Measure(UiText.T("Save Plan & Switch to Automation")).X + ChocoboPresentation.FooterSavePadding * MaterialTheme.Metrics.Scale,
            MaterialText.Measure(UiText.T("Copy Results to Clipboard")).X + ChocoboPresentation.FooterCopyPadding * MaterialTheme.Metrics.Scale);

    private void DrawResultActions()
    {
        var s = MaterialTheme.Metrics.Scale;
        using var font = UiGui.FooterFont();
        using var appearance = new MaterialStyleScope();
        appearance.Style(ImGuiStyleVar.FrameBorderSize, s);
        appearance.Style(ImGuiStyleVar.ButtonTextAlign, new Vector2(ChocoboPresentation.Compact ? .545f : .53f, .5f));
        appearance.Style(ImGuiStyleVar.ItemSpacing, new Vector2(ChocoboPresentation.FooterActionGap * s, ImGui.GetStyle().ItemSpacing.Y));
        var colors = MaterialTheme.Current.Colors;
        appearance.Color(ImGuiCol.Button, colors.SurfaceContainerHigh);
        appearance.Color(ImGuiCol.ButtonHovered, MaterialColor.Layer(colors.SurfaceContainerHigh, colors.OnSurface, .08f));
        appearance.Color(ImGuiCol.ButtonActive, MaterialColor.Layer(colors.SurfaceContainerHigh, colors.OnSurface, .14f));
        var usable = lastResult is { TotalFruits: > 0 };
        var canSave = usable && plugin.GameData.IsLoggedIn;
        if (canSave)
        {
            var data = plugin.PlanStorage.GetCharacterData(plugin.GameData.CharacterName, plugin.GameData.WorldName);
            canSave = data.ActivePlan is null && !data.IsTimerActive;
        }
        var horizontal = ResultActionsFit();
        var width = ImGui.GetContentRegionAvail().X;
        var gap = ImGui.GetStyle().ItemSpacing.X;
        var saveMinimum = MaterialText.Measure(UiText.T("Save Plan & Switch to Automation")).X + ChocoboPresentation.FooterSavePadding * s;
        var copyMinimum = MaterialText.Measure(UiText.T("Copy Results to Clipboard")).X + ChocoboPresentation.FooterCopyPadding * s;
        var overflow = Math.Max(saveMinimum, copyMinimum) > width;
        var parentId = ImGui.GetID("");
        using var scrollStyle = new MaterialStyleScope();
        if (overflow)
        {
            width = Math.Max(saveMinimum, copyMinimum);
            scrollStyle.Style(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.SetNextWindowContentSize(new Vector2(width, 0));
            var height = UiGui.IconButtonHeight("Save Plan & Switch to Automation", width)
                + UiGui.IconButtonHeight("Copy Results to Clipboard", width)
                + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().ScrollbarSize;
            if (!ImGui.BeginChild("##ResultActionsOverflow", new Vector2(0, height), false, ImGuiWindowFlags.HorizontalScrollbar))
            {
                ImGui.EndChild();
                return;
            }
            ImGuiP.PushOverrideID(parentId);
        }
        try
        {
            if (horizontal) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, width - saveMinimum - copyMinimum - gap));
            var saveWidth = horizontal ? saveMinimum : width;
            ImGui.BeginDisabled(!canSave);
            if (UiGui.IconButton("Save Plan & Switch to Automation", MaterialIcon.Save, new Vector2(saveWidth, UiGui.IconButtonHeight("Save Plan & Switch to Automation", saveWidth)), logicalIconSize: 28, logicalGap: 14))
            {
                SavePlanFromResult(plugin.GameData.CharacterName, plugin.GameData.WorldName);
                statusMessage = "Plan saved! Switched to Automation tab.";
                selectAutomationTabNextFrame = true;
            }
            ImGui.EndDisabled();
            if (horizontal) ImGui.SameLine();
            ImGui.BeginDisabled(!usable);
            appearance.Style(ImGuiStyleVar.ButtonTextAlign, new Vector2(ChocoboPresentation.Compact ? .545f : .565f, .5f));
            var copyWidth = horizontal ? copyMinimum : width;
            if (UiGui.IconButton("Copy Results to Clipboard", MaterialIcon.Copy, new Vector2(copyWidth, UiGui.IconButtonHeight("Copy Results to Clipboard", copyWidth)), logicalIconSize: 28, logicalGap: 18))
            {
                ImGui.SetClipboardText(FormatResultsForClipboard());
                statusMessage = "Results copied to clipboard!";
            }
            ImGui.EndDisabled();
        }
        finally
        {
            if (overflow)
            {
                ImGui.PopID();
                ImGui.EndChild();
            }
        }
    }

    private static void DrawEmptyFeedingOrder()
    {
        var scale = MaterialTheme.Metrics.Scale;
        var label = UiText.T("Your feeding order will appear here.");
        var available = ImGui.GetContentRegionAvail();
        var iconSize = 24 * scale; var gap = 16 * scale;
        var wrapWidth = Math.Max(1, available.X - iconSize - gap);
        var textSize = MaterialText.Measure(label, false, wrapWidth);
        var width = Math.Min(available.X, iconSize + gap + textSize.X);
        var height = Math.Max(iconSize, textSize.Y);
        var start = ImGui.GetCursorScreenPos() + new Vector2(Math.Max(0, (available.X - width) * .5f), Math.Max(0, (available.Y - height) * .5f));
        MaterialIcons.Draw(MaterialIcon.List, start + new Vector2(0, (height - iconSize) * .5f), iconSize, ColorGrey);
        ImGui.SetCursorScreenPos(start + new Vector2(iconSize + gap, (height - textSize.Y) * .5f));
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
        MaterialText.TextColored(ColorGrey, label);
        ImGui.PopTextWrapPos();
    }

    private void DrawChocoboColorDetection()
    {
        // Feature 5: Attempt auto-detect, or show tooltip for manual lookup
        var detectedColor = plugin.GameData.TryGetChocoboColor();
        if (detectedColor != null)
        {
            UiGui.TextColored(ColorGreen, UiText.Interpolated($"Detected chocobo colour: {detectedColor}"));
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
            UiGui.Text("Current colour:");
            ImGui.SameLine();
            UiGui.TextColored(ColorGrey, "(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                UiGui.Text("How to find your chocobo's current colour:");
                ImGui.Separator();
                UiGui.Text("1. Open the Companion window (default: N key)");
                UiGui.Text("2. Click the Appearance tab");
                UiGui.Text("3. Check the current colour listed");
                UiGui.Text("4. Select it from the dropdown below");
                ImGui.Spacing();
                UiGui.TextColored(ColorGrey, "Auto-detection will be added in a future update.");
                ImGui.EndTooltip();
            }
        }
    }

    private void DrawColorSelection()
    {
        var detected = plugin.GameData.TryGetChocoboColor();
        if (detected is not null)
        {
            var detectedIndex = Array.IndexOf(colorNames, detected);
            if (detectedIndex >= 0) currentColorIndex = detectedIndex;
        }
        var s = MaterialTheme.Metrics.Scale;
        var horizontal = ColorColumnsFit();
        using var tableStyle = new MaterialStyleScope();
        tableStyle.Style(ImGuiStyleVar.CellPadding, new Vector2(ChocoboPresentation.Compact ? 25 : 29.5f, 2) * s);
        if (horizontal && ImGui.BeginTable("##ColourSelectors", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoClip))
        {
            ImGui.TableSetupColumn("##CurrentColourColumn", ImGuiTableColumnFlags.WidthStretch, ChocoboPresentation.Compact ? 501 : 508);
            ImGui.TableSetupColumn("##TargetColourColumn", ImGuiTableColumnFlags.WidthStretch, 475);
            ImGui.TableSetupColumn("##CalculateColumn", ImGuiTableColumnFlags.WidthStretch, ChocoboPresentation.Compact ? 512 : 505);
            for (var index = 0; index < 3; index++)
            {
                ImGui.TableNextColumn();
                DrawColorGroup(index, true);
            }
            ImGui.EndTable();
        }
        else if (!horizontal)
        {
            for (var index = 0; index < 3; index++) { DrawColorGroup(index, false); ImGui.Spacing(); }
        }
    }

    private void DrawColorGroup(int group, bool horizontal)
    {
        var s = MaterialTheme.Metrics.Scale;
        ImGuiP.PushOverrideID(calculatorRootId);
        if (group == 0)
        {
            using (UiText.Font(UiFontRole.BodyStrong)) UiGui.Text("Current colour");
            if (ChocoboPresentation.Compact) ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3 * s);
            ImGui.SetNextItemWidth(horizontal ? ImGui.GetContentRegionAvail().X + (ChocoboPresentation.Compact ? 1 : 4) * s : -1);
            UiGui.InputTextWithHint("##CurrentColorSearch", "Search colours...", ref currentColorSearch, 64);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ChocoboPresentation.Compact ? 4 : 3) * s);
            ImGui.SetNextItemWidth(horizontal ? ImGui.GetContentRegionAvail().X + (ChocoboPresentation.Compact ? 1 : 4) * s : -1);
            DrawColorCombo("##CurrentColor", ref currentColorIndex, currentColorSearch);
            DrawColorRgb(ColorDatabase.GetByIndex(currentColorIndex));
        }
        else if (group == 1)
        {
            using (UiText.Font(UiFontRole.BodyStrong)) UiGui.Text("Target colour");
            if (ChocoboPresentation.Compact) ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 3 * s);
            ImGui.SetNextItemWidth(horizontal ? ImGui.GetContentRegionAvail().X + (ChocoboPresentation.Compact ? 2 : 5) * s : -1);
            UiGui.InputTextWithHint("##TargetColorSearch", "Search colours...", ref targetColorSearch, 64);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - (ChocoboPresentation.Compact ? 4 : 3) * s);
            ImGui.SetNextItemWidth(horizontal ? ImGui.GetContentRegionAvail().X + (ChocoboPresentation.Compact ? 2 : 5) * s : -1);
            DrawColorCombo("##TargetColor", ref targetColorIndex, targetColorSearch);
            DrawColorRgb(ColorDatabase.GetByIndex(targetColorIndex));
        }
        else
        {
            ImGui.Dummy(new Vector2(0, 28 * MaterialTheme.Metrics.Scale));
            DrawCalculateButton(horizontal ? 4 * s : 0);
            DrawChocoboColorDetection();
        }
        ImGui.PopID();
    }

    private static bool ColorColumnsFit(float? availableWidth = null)
    {
        var s = MaterialTheme.Metrics.Scale;
        float buttonWidth;
        using (UiText.Font(UiFontRole.Counter)) buttonWidth = MaterialText.Measure(UiText.T("Calculate Feeding Path")).X + 80 * s;
        return (availableWidth ?? ImGui.GetContentRegionAvail().X) >= Math.Max(1000 * s, buttonWidth * 3 + ImGui.GetStyle().CellPadding.X * 6);
    }

    private void DrawColorCombo(string label, ref int selectedIndex, string search)
    {
        using var controls = MaterialControls.Push(ChocoboPresentation.Controls());
        var colours = MaterialTheme.Current.Colors;
        ImGui.PushStyleColor(ImGuiCol.Button, colours.SurfaceContainerHighest);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MaterialColor.Layer(colours.SurfaceContainerHighest, colours.OnSurface, .08f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, MaterialColor.Layer(colours.SurfaceContainerHighest, colours.OnSurface, .14f));
        var currentName = colorNames[selectedIndex];
        var preview = ColorDatabase.GetByIndex(selectedIndex);
        var drawList = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var padding = ImGui.GetStyle().FramePadding;
        var height = ImGui.GetFrameHeight();
        var max = min + new Vector2(ImGui.CalcItemWidth(), height);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1 * MaterialTheme.Metrics.Scale);
        var open = ImGui.BeginCombo(label, "");
        var swatchSize = Math.Min(height - 8 * MaterialTheme.Metrics.Scale, (ChocoboPresentation.Compact ? 30 : 34) * MaterialTheme.Metrics.Scale);
        drawList.PushClipRect(min, new Vector2(max.X - height, max.Y), true);
        var swatchPosition = min + new Vector2(14 * MaterialTheme.Metrics.Scale, (height - swatchSize) * .5f);
        drawList.AddRectFilled(swatchPosition, swatchPosition + new Vector2(swatchSize),
            MaterialCanvas.Color(new Vector4(preview.R / 255f, preview.G / 255f, preview.B / 255f, 1)), 4 * MaterialTheme.Metrics.Scale);
        MaterialText.AddText(drawList, min + new Vector2(26 * MaterialTheme.Metrics.Scale + swatchSize, (height - ImGui.GetTextLineHeight()) * .5f), MaterialCanvas.Color(colours.OnSurface), currentName);
        drawList.PopClipRect();
        if (open)
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
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar();
    }

    private static void DrawColorRgb(ChocoboColor color)
    {
        UiGui.TextColored(MaterialTheme.Current.Colors.OnSurfaceVariant, UiText.Interpolated($"RGB({color.R}, {color.G}, {color.B})"));
    }

    private void DrawCalculateButton(float extraWidth)
    {
        using var font = UiText.Font(UiFontRole.Counter);
        if (UiGui.FilledAction("Calculate Feeding Path", MaterialIcon.Calculator, currentColorIndex == targetColorIndex, extraWidth))
        {
            RunCalculation();
        }
        if (currentColorIndex == targetColorIndex)
            UiGui.TextColored(ColorYellow, "Current and target colours are the same!");

        if (!string.IsNullOrEmpty(statusMessage))
        {
            UiGui.Text(statusMessage);
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
            UiGui.TextColored(ColorGrey, "Select colours and click Calculate.");
            return;
        }

        // Header
        UiGui.Text(UiText.Interpolated($"From: {lastResult.StartColor.Name}"));
        UiGui.Text(UiText.Interpolated($"To:   {lastResult.TargetColor.Name}"));
        ImGui.Spacing();

        // Final color result
        var finalColorVec = new Vector4(
            lastResult.FinalColor.R / 255f,
            lastResult.FinalColor.G / 255f,
            lastResult.FinalColor.B / 255f, 1f);
        UiGui.Text("Result Colour:");
        ImGui.SameLine();
        ImGui.ColorButton("##finalPreview", finalColorVec,
            ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop, new Vector2(20, 20));
        ImGui.SameLine();
        UiGui.Text(UiText.Interpolated($"RGB({lastResult.FinalColor.R}, {lastResult.FinalColor.G}, {lastResult.FinalColor.B})"));

        UiGui.Text(UiText.Interpolated($"Closest Named Colour: {lastResult.ClosestColorName}"));
        UiGui.Text(UiText.Interpolated($"Distance to Target: {lastResult.FinalDistance:F2}"));
        ImGui.Spacing();

        if (lastResult.TotalFruits == 0)
        {
            UiGui.TextColored(ColorGreen, "Already at the closest possible colour!");
            return;
        }

    }

    // Feature 3: Inventory requirements with red/yellow/green colour coding
    private void DrawInventoryRequirements()
    {
        UiGui.Text(UiText.Interpolated($"Total Fruits Required: {lastResult!.TotalFruits}"));
        ImGui.Spacing();

        var fruitCounts = plugin.GameData.IsLoggedIn
            ? plugin.GameData.GetAllFruitCounts()
            : null;
        var allSatisfied = true;

        if (ImGui.BeginTable("FruitReq", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            // Use scaled widths based on font size to handle UI scaling >100%
            var scale = MaterialTheme.Metrics.Scale;
            ImGui.TableSetupColumn("Fruit", ImGuiTableColumnFlags.WidthStretch, 1.4f);
            ImGui.TableSetupColumn("Required", ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableSetupColumn("Owned", ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthStretch, 1);
            UiGui.TableHeadersRow((ChocoboPresentation.Compact ? 30 : 40) * scale);

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

                ImGui.TableNextRow(ImGuiTableRowFlags.None, (ChocoboPresentation.Compact ? 30 : 40) * scale);
                ImGui.TableNextColumn();
                UiGui.Text(FruitData.GetDisplayName(kvp.Key));
                ImGui.TableNextColumn();
                UiGui.Text(required.ToString("N0", UiText.Current.Culture));
                ImGui.TableNextColumn();
                UiGui.TextColored(statusColor, plugin.GameData.IsLoggedIn ? owned.ToString("N0", UiText.Current.Culture) : "?");
                ImGui.TableNextColumn();
                UiGui.TextColored(statusColor, statusIcon);
            }

            ImGui.EndTable();
        }

        if (plugin.GameData.IsLoggedIn && !allSatisfied)
        {
            UiGui.TextColored(ColorYellow, "You need more fruits. Acquire them however you prefer.");
        }
    }

    // Feature 4: Plan status and quick-start button with gating
    private void DrawPlanButtons()
    {
        if (lastResult == null || lastResult.TotalFruits == 0) return;

        if (!plugin.GameData.IsLoggedIn)
        {
            UiGui.TextColored(ColorGrey, "Log in to use feeding plans.");
            return;
        }

        var charName = plugin.GameData.CharacterName;
        var worldName = plugin.GameData.WorldName;
        var charData = plugin.PlanStorage.GetCharacterData(charName, worldName);

        if (charData.ActivePlan != null)
        {
            UiGui.TextColored(ColorCyan,
                UiText.Interpolated($"Active plan: {charData.ActivePlan.FruitsFed}/{charData.ActivePlan.TotalFruits} fed ({charData.ActivePlan.StartColorName} -> {charData.ActivePlan.TargetColorName})"));

            if (UiGui.Button("Clear Existing Plan"))
            {
                plugin.PlanStorage.ClearPlan(charName, worldName);
            }
            ImGui.SameLine();
            UiGui.TextColored(ColorGrey, "Switch to the Automation tab to start feeding.");
        }
        else if (charData.IsTimerActive)
        {
            var remaining = charData.TimerRemaining;
            UiGui.TextColored(ColorCyan,
                UiText.Interpolated($"Colour change in progress: {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s remaining"));
        }
        else
        {
            var hasEnough = plugin.GameData.HasEnoughFruits(lastResult.FruitCounts);

            if (!hasEnough)
            {
                UiGui.TextColored(ColorYellow,
                    "You do not have all required fruits yet. Automation start will stay disabled until you gather them.");
            }
            else
            {
                UiGui.TextColored(ColorGreen, "All required fruits are in your inventory. Head to the Automation tab to start.");
            }

            UiGui.TextColored(ColorGrey, "Automation controls live in the Automation tab.");
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
        using var tightRows = ChocoboPresentation.Compact ? MaterialTable.PushTightRows() : default;
        UiGui.Text("Colour Change Timers");
        ImGui.Separator();
        ImGui.Spacing();

        var allChars = plugin.PlanStorage.GetAllCharacters();
        var hasAnyTimer = false;
        var widths = new float[4];
        var headings = new[] { "Character", "Chocobo", "Status", "Remaining" };
        using (UiText.Font(UiFontRole.BodyStrong))
            for (var column = 0; column < widths.Length; column++)
                widths[column] = MaterialText.Measure(UiText.T(headings[column])).X;
        foreach (var data in allChars.Values)
        {
            if (!data.IsTimerActive && data.ActivePlan == null) continue;
            widths[0] = Math.Max(widths[0], MaterialText.Measure($"{data.CharacterName} @ {data.WorldName}").X);
            widths[1] = Math.Max(widths[1], MaterialText.Measure(string.IsNullOrEmpty(data.ChocoboName) ? UiText.T("(unnamed)") : data.ChocoboName).X);
            var status = data.IsTimerActive ? UiText.T("Changing...") : UiText.F("Feeding {0}/{1}", data.ActivePlan!.FruitsFed, data.ActivePlan.TotalFruits);
            widths[2] = Math.Max(widths[2], MaterialText.Measure(status).X);
        }
        // Budget every possible single-digit hour/minute/second value, not just the
        // instant rendered below. A tick must not resize the retained timer columns.
        for (var hour = 0; hour <= 6; hour++)
            widths[3] = Math.Max(widths[3], MaterialText.Measure(UiText.F("{0}h {1:D2}m {2:D2}s", hour, 59, 59)).X);
        for (var column = 0; column < widths.Length; column++)
            widths[column] = MathF.Ceiling(widths[column] + 2 * MaterialTheme.Metrics.Scale);
        var innerWidth = Math.Max(ImGui.GetContentRegionAvail().X,
            widths.Sum() + ImGui.GetStyle().CellPadding.X * 2 * widths.Length + 2 * MaterialTheme.Metrics.Scale * (widths.Length + 1));
        var visibleRows = allChars.Values.Count(data => data.IsTimerActive || data.ActivePlan != null);
        var tableHeight = Math.Min(ImGui.GetContentRegionAvail().Y,
            (visibleRows + 1) * (ImGui.GetTextLineHeight() + ImGui.GetStyle().CellPadding.Y * 2)
                + ImGui.GetStyle().ScrollbarSize + 2 * MaterialTheme.Metrics.Scale);
        if (ImGui.BeginTable("Timers", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY,
                new Vector2(0, tableHeight), innerWidth))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("Character", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Chocobo", ImGuiTableColumnFlags.WidthFixed, widths[1]);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, widths[2]);
            ImGui.TableSetupColumn("Remaining", ImGuiTableColumnFlags.WidthFixed, widths[3]);
            UiGui.TableHeadersRow();

            foreach (var kvp in allChars)
            {
                var data = kvp.Value;
                if (!data.IsTimerActive && data.ActivePlan == null) continue;
                hasAnyTimer = true;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                MaterialText.Text($"{data.CharacterName} @ {data.WorldName}");

                ImGui.TableNextColumn();
                MaterialText.Text(string.IsNullOrEmpty(data.ChocoboName) ? UiText.T("(unnamed)") : data.ChocoboName);

                ImGui.TableNextColumn();
                if (data.IsTimerActive)
                {
                    MaterialText.TextColored(ColorCyan, UiText.T("Changing..."));
                }
                else if (data.ActivePlan != null)
                {
                    MaterialText.TextColored(ColorYellow, UiText.F("Feeding {0}/{1}", data.ActivePlan.FruitsFed, data.ActivePlan.TotalFruits));
                }

                ImGui.TableNextColumn();
                if (data.IsTimerActive)
                {
                    var r = data.TimerRemaining;
                    MaterialText.Text(UiText.F("{0}h {1:D2}m {2:D2}s", r.Hours, r.Minutes, r.Seconds));
                }
                else
                {
                    MaterialText.TextColored(ColorGrey, "-");
                }
            }

            ImGui.EndTable();
        }

        if (!hasAnyTimer)
        {
            UiGui.TextColored(ColorGrey, "No active timers or feeding plans.");
            ImGui.Spacing();
            UiGui.Text("Timers will appear here after completing a full feeding.");
            UiGui.Text("Each character on this account will have their own timer.");
        }
    }

    // ========== AUTOMATION TAB ==========
    // Feature 1: Automated feeding
    private void DrawAutomationTab()
    {
        var parentId = ImGui.GetID("");
        if (!ImGui.BeginChild("##AutomationScroll", Vector2.Zero, false, ImGuiWindowFlags.HorizontalScrollbar))
        {
            ImGui.EndChild();
            return;
        }
        ImGuiP.PushOverrideID(parentId);
        try { DrawAutomationContents(); }
        finally
        {
            ImGui.PopID();
            ImGui.EndChild();
        }
    }

    private void DrawAutomationContents()
    {
        UiGui.Text("Automated Feeding");
        ImGui.Separator();
        ImGui.Spacing();

        if (!plugin.GameData.IsLoggedIn)
        {
            UiGui.TextColored(ColorGrey, "You must be logged in to use automated feeding.");
            return;
        }

        var charName = plugin.GameData.CharacterName;
        var worldName = plugin.GameData.WorldName;
        var charData = plugin.PlanStorage.GetCharacterData(charName, worldName);
        var automation = plugin.FeedingAutomation;

        if (charData.IsTimerActive)
        {
            var r = charData.TimerRemaining;
            UiGui.TextColored(ColorCyan,
                UiText.Interpolated($"Colour change already in progress: {r.Hours}h {r.Minutes:D2}m {r.Seconds:D2}s remaining."));
            return;
        }

        if (charData.ActivePlan == null)
        {
            UiGui.TextColored(ColorGrey, "No active feeding plan.");
            UiGui.Text("Calculate a feeding path in the Calculator tab, then click 'Save Plan'.");
            return;
        }

        var plan = charData.ActivePlan;

        // Show plan summary
        UiGui.Text(UiText.Interpolated($"Plan: {plan.StartColorName} -> {plan.TargetColorName}"));
        UiGui.Text(UiText.Interpolated($"Progress: {plan.FruitsFed} / {plan.TotalFruits} fruits fed"));

        // Progress bar
        var progress = plan.TotalFruits > 0 ? (float)plan.FruitsFed / plan.TotalFruits : 0f;
        ImGui.ProgressBar(progress, new Vector2(-1, 20),
            $"{plan.FruitsFed}/{plan.TotalFruits}");

        ImGui.Spacing();

        // Remaining fruit summary
        UiGui.Text("Remaining fruits:");
        var remaining = plan.RemainingFruitCounts;
        foreach (var kvp in remaining.OrderByDescending(x => x.Value))
        {
            UiGui.Text(UiText.Interpolated($"  {kvp.Key} x{kvp.Value}"));
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Automation controls
        if (automation.IsRunning)
        {
            // Show live progress
            UiGui.TextColored(ColorCyan,
                UiText.Interpolated($"Feeding in progress: step {automation.CurrentFruitIndex + 1}/{automation.TotalFruits}"));
            UiGui.Text(UiText.Interpolated($"Current: {automation.CurrentFruitName}"));
            UiGui.Text(UiText.F("State: {0}", UiText.T(automation.State.ToString())));

            ImGui.Spacing();
            if (UiGui.Button("Stop Automation", new Vector2(-1, 0)))
                StopFeedingFromUi();
        }
        else if (automation.State == FeedingState.Completed)
        {
            UiGui.TextColored(ColorGreen, "Feeding complete! Your chocobo's colour will change in 6 hours.");
            if (UiGui.Button("OK", new Vector2(-1, 0)))
            {
                automation.Reset();
            }
        }
        else if (automation.State == FeedingState.Error)
        {
            UiGui.TextColored(ColorRed, UiText.F("Error: {0}", UiText.T(automation.ErrorMessage)));
            if (UiGui.Button("Dismiss", new Vector2(-1, 0)))
            {
                automation.Reset();
            }
        }
        else
        {
            // Feature 4: Gate start button on inventory
            var hasEnough = plugin.GameData.HasEnoughFruits(GetRemainingFruitCounts(plan));

            UiGui.TextColored(ColorYellow,
                "IMPORTANT: You must be at the Chocobo Stable in the FEED screen");
            UiGui.TextColored(ColorYellow,
                "(inventory open with feedable items highlighted) before clicking Start.");
            UiGui.TextColored(ColorYellow,
                "TextAdvance and YesAlready will be paused; buddy-feed cutscene skip is armed when this window opens.");
            ImGui.Spacing();

            // Stable condition check
            var config = plugin.Configuration;
            var checkCondition = config.CheckStableCondition;
            if (UiGui.Checkbox("Check stable condition before feeding", ref checkCondition))
            {
                config.CheckStableCondition = checkCondition;
                config.Save();
            }
            ImGui.SameLine();
            UiGui.TextColored(ColorGrey, "(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                UiGui.Text("If enabled, warns you if the stable condition is Poor or Fair.");
                UiGui.Text("Use a Magicked Stable Broom to clean the stable first.");
                ImGui.EndTooltip();
            }

            bool stableConditionBlocked = false;
            if (config.CheckStableCondition)
            {
                // Check if user has a Magicked Stable Broom (ID: 8168)
                var broomCount = plugin.GameData.GetItemCount(8168);
                UiGui.Text(UiText.Interpolated($"Magicked Stable Broom: {broomCount} in inventory"));
                // Note: We can't directly read stable condition from game state yet,
                // so we inform the user to check manually and provide broom count.
            }

            ImGui.Spacing();

            if (hasEnough && !stableConditionBlocked)
            {
                if (UiGui.Button("Start Automated Feeding", new Vector2(-1, 0)))
                    StartFeedingFromUi();
            }
            else
            {
                ImGui.BeginDisabled();
                UiGui.Button("Start Automated Feeding (Insufficient Fruits)", new Vector2(-1, 0));
                ImGui.EndDisabled();
                UiGui.TextColored(ColorRed, "You do not have enough fruits in your inventory.");
            }

            ImGui.Spacing();
            if (UiGui.Button("Clear Plan"))
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
        if (UiGui.TreeNode("Prerequisites & Troubleshooting"))
        {
            ImGui.Spacing();
            UiGui.TextColored(MaterialTheme.Current.Colors.Primary, "Required Quests:");
            ImGui.Spacing();

            UiGui.Text("1.");
            ImGui.SameLine();
            UiGui.TextColored(ColorWhite, "My Feisty Little Chocobo");
            UiGui.Text("   Obtain your chocobo companion (Camp Tranquil, South Shroud)");

            ImGui.Spacing();
            UiGui.Text("2.");
            ImGui.SameLine();
            UiGui.TextColored(ColorWhite, "Bird in Hand");
            UiGui.Text("   Unlock chocobo stabling and raising (Bentbranch Meadows, Central Shroud)");
            UiGui.Text("   NPC: Luquelot at Bentbranch Meadows (X:21.4, Y:22.1)");

            ImGui.Spacing();
            UiGui.TextColored(MaterialTheme.Current.Colors.Primary, "Other Requirements:");
            ImGui.Spacing();
            UiGui.Text("- Access to a Chocobo Stable (FC house, personal house, or apartment)");
            UiGui.Text("- Your chocobo must be stabled (not summoned as companion)");
            UiGui.Text("- Fruits in your inventory (purchase from vendors or Market Board)");

            ImGui.Spacing();
            UiGui.TextColored(MaterialTheme.Current.Colors.Primary, "Common Errors:");
            ImGui.Spacing();

            UiGui.TextColored(ColorYellow, "\"You have yet to be trained in chocobo raising\"");
            UiGui.Text("  -> Complete the quest \"Bird in Hand\" at Bentbranch Meadows.");

            ImGui.Spacing();
            UiGui.TextColored(ColorYellow, "\"Your chocobo is not stabled\"");
            UiGui.Text("  -> Stable your chocobo at a Chocobo Stable before feeding.");

            ImGui.Spacing();
            UiGui.TextColored(ColorYellow, "Automation errors about context menu or inventory");
            UiGui.Text("  -> Make sure you are on the Feed screen (inventory visible with");
            UiGui.Text("     feedable items highlighted) before clicking Start.");

            ImGui.TreePop();
        }
    }

    private string FeedingActionBlocker(bool start)
    {
        if (!plugin.GameData.IsLoggedIn) return UiText.T("You must be logged in to use automated feeding.");
        var data = plugin.PlanStorage.GetCharacterData(plugin.GameData.CharacterName, plugin.GameData.WorldName);
        if (data.IsTimerActive)
        {
            var remaining = data.TimerRemaining;
            return UiText.Interpolated($"Colour change already in progress: {remaining.Hours}h {remaining.Minutes:D2}m {remaining.Seconds:D2}s remaining.");
        }
        if (data.ActivePlan is not { } plan) return UiText.T("No active feeding plan.");
        var automation = plugin.FeedingAutomation;
        if (!start) return automation.IsRunning ? "" : UiText.T("Automation stopped.");
        if (automation.IsRunning) return UiText.T("Automation is already running.");
        if (automation.State == FeedingState.Completed) return UiText.T("Feeding complete! Your chocobo's colour will change in 6 hours.");
        if (automation.State == FeedingState.Error) return UiText.F("Error: {0}", UiText.T(automation.ErrorMessage));
        if (plan.IsComplete) return UiText.T("Plan is already complete.");
        return plugin.GameData.HasEnoughFruits(GetRemainingFruitCounts(plan)) ? "" : UiText.T("You do not have enough fruits in your inventory.");
    }

    private void StartFeedingFromUi()
    {
        if (FeedingActionBlocker(start: true).Length != 0) return;
        var character = plugin.GameData.CharacterName;
        var world = plugin.GameData.WorldName;
        var data = plugin.PlanStorage.GetCharacterData(character, world);
        if (string.IsNullOrEmpty(data.ChocoboName))
            plugin.PlanStorage.SetChocoboName(character, world, $"{character.Split(' ')[0]}'s Chocobo");
        plugin.FeedingAutomation.Start(data.ActivePlan!, character, world);
    }

    private void StopFeedingFromUi()
    {
        if (FeedingActionBlocker(start: false).Length != 0) return;
        plugin.FeedingAutomation.Stop();
        statusMessage = "Automation stopped.";
    }

    private void ShowFeedingTitleTooltip(bool start)
    {
        var blocker = FeedingActionBlocker(start);
        var label = UiText.T(start ? "Start Automated Feeding" : "Stop Automation");
        if (start && blocker.Length == 0)
            blocker = UiText.T("IMPORTANT: You must be at the Chocobo Stable in the FEED screen") + "\n"
                + UiText.T("(inventory open with feedable items highlighted) before clicking Start.");
        MaterialText.SetTooltip(label + (blocker.Length == 0 ? "" : "\n" + blocker));
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
            UiText.F("From: {0}", lastResult.StartColor),
            UiText.F("To:   {0}", lastResult.TargetColor),
            UiText.F("Result: {0} ({1})", lastResult.FinalColor, lastResult.ClosestColorName),
            UiText.F("Total Fruits: {0}", lastResult.TotalFruits),
            "",
            UiText.T("Fruit Summary:")
        };

        foreach (var kvp in lastResult.FruitCounts.OrderByDescending(x => x.Value))
        {
            lines.Add(UiText.Interpolated($"  {FruitData.GetDisplayName(kvp.Key)} x{kvp.Value}"));
        }

        lines.Add("");
        lines.Add(UiText.T("Feeding Order:"));
        for (var i = 0; i < lastResult.Fruits.Count; i++)
        {
            lines.Add(UiText.Interpolated($"  {i + 1}. {FruitData.GetDisplayName(lastResult.Fruits[i])}"));
        }

        return string.Join("\n", lines);
    }
}
