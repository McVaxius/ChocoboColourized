using System.Numerics;
using AethertekUI;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace ChocoboColourized.Ui;

// Native widgets receive their original English labels/IDs. Only their visible label is painted in the selected locale.
// This also preserves English-derived helper IDs and existing saved window identities.
internal static class UiGui
{
    internal static IDisposable FooterFont() => new FooterFontScope();
    private readonly struct FooterFontScope : IDisposable
    {
        private readonly IDisposable font;
        private readonly float previousScale;
        public FooterFontScope()
        {
            font = UiText.Font(UiFontRole.Body);
            previousScale = ImGuiP.GetCurrentWindow().FontWindowScale;
            ImGui.SetWindowFontScale(previousScale * 16.5f / 18);
        }
        public void Dispose() { ImGui.SetWindowFontScale(previousScale); font.Dispose(); }
    }
    internal static void TextUnformatted(string text) => MaterialText.Text(UiText.T(text));
    internal static void TextWrapped(string text) => MaterialText.TextWrapped(UiText.T(text));
    internal static void Text(string text)
    {
        ImGui.PushTextWrapPos(0);
        try { MaterialText.Text(UiText.T(text)); }
        finally { ImGui.PopTextWrapPos(); }
    }
    internal static void Text(FormattableString text) => Text(UiText.Interpolated(text));
    internal static void TextColored(Vector4 color, string text)
    {
        ImGui.PushTextWrapPos(0);
        try { MaterialText.TextColored(color, UiText.T(text)); }
        finally { ImGui.PopTextWrapPos(); }
    }
    internal static void TextColored(Vector4 color, FormattableString text) => TextColored(color, UiText.Interpolated(text));
    internal static void SetTooltip(string text) => MaterialText.SetTooltip(UiText.T(text));
    internal static bool InputTextWithHint(string label, string hint, ref string value, int length)
    {
        using var controls = MaterialControls.Push(ChocoboPresentation.Controls(ChocoboPresentation.Compact ? 40 : 44));
        var s = MaterialTheme.Metrics.Scale;
        var padding = ImGui.GetStyle().FramePadding;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(44 * s, padding.Y));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1 * s);
        bool changed;
        try
        {
            using var height = MaterialText.PushLineHeight(value, UiText.T(hint));
            changed = MaterialShapedInput.SingleLine(label, UiText.T(hint), ref value, length);
        }
        finally { ImGui.PopStyleVar(2); }
        var min = ImGui.GetItemRectMin();
        MaterialIcons.Draw(MaterialIcon.Search, min + new Vector2(14 * s, (ImGui.GetItemRectMax().Y - min.Y - 22 * s) * .5f), 22 * s, MaterialTheme.Current.Colors.OnSurfaceVariant);
        return changed;
    }
    internal static bool TreeNode(string original)
    {
        var label = UiText.T(original);
        using var height = MaterialText.PushLineHeight(label);
        var origin = ImGui.GetCursorScreenPos();
        var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        ImGui.PushStyleColor(ImGuiCol.Text, Vector4.Zero);
        var open = ImGui.TreeNode(original);
        ImGui.PopStyleColor();
        var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
        var position = new Vector2(origin.X + ImGui.GetFontSize() + 2 * ImGui.GetStyle().FramePadding.X,
            min.Y + (max.Y - min.Y - MaterialText.Measure(label).Y) * .5f);
        var dl = ImGui.GetWindowDrawList();
        MaterialIcons.Draw(open ? MaterialIcon.ChevronDown : MaterialIcon.ArrowRight,
            min, ImGui.GetFontSize(), color);
        MaterialText.AddText(dl, position, MaterialCanvas.Color(color), label);
        return open;
    }

    private static void Label(string original,Vector2 position,Vector4 background,Vector4 foreground,Vector2? clip=null,string? display=null)
    {
        var visible=original.Split("##",2)[0];
        var translated=display ?? UiText.T(visible);
        if(translated==visible) return;
        var dl=ImGui.GetWindowDrawList();
        var width=Math.Max(MaterialText.Measure(visible).X,MaterialText.Measure(translated).X);
        if(clip is { } max) dl.PushClipRect(position,max,true);
        try
        {
            dl.AddRectFilled(position,position+new Vector2(width,Math.Max(ImGui.GetTextLineHeight(),MaterialText.Measure(translated).Y)),ImGui.ColorConvertFloat4ToU32(background));
            foreground.W*=ImGui.GetStyle().Alpha;
            MaterialText.AddText(dl, position,ImGui.ColorConvertFloat4ToU32(foreground),translated);
        }
        finally { if(clip.HasValue) dl.PopClipRect(); }
    }
    internal static bool Button(string label,string? display=null)
    {
        var translated=display ?? UiText.T(label.Split("##",2)[0]);
        using var height = MaterialText.PushLineHeight(translated);
        var width=MaterialText.Measure(translated).X+2*ImGui.GetStyle().FramePadding.X;
        if(width>ImGui.GetContentRegionAvail().X && ImGui.GetCursorPosX()>ImGui.GetStyle().WindowPadding.X+1) ImGui.NewLine();
        var foreground=ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        ImGui.PushStyleColor(ImGuiCol.Text,Vector4.Zero);
        var clicked=ImGui.Button(label,new Vector2(width,0));
        ImGui.PopStyleColor();
        var min=ImGui.GetItemRectMin(); var max=ImGui.GetItemRectMax();
        foreground.W*=ImGui.GetStyle().Alpha;
        ImGui.GetWindowDrawList().PushClipRect(min,max,true);
        try
        {
        MaterialText.AddText(ImGui.GetWindowDrawList(), min+(max-min-MaterialText.Measure(translated))*.5f,ImGui.ColorConvertFloat4ToU32(foreground),translated);
        }
        finally { ImGui.GetWindowDrawList().PopClipRect(); }
        return clicked;
    }
    internal static bool Button(string label, Vector2 pixels)
    {
        var translated = UiText.T(label.Split("##", 2)[0]);
        using var height = MaterialText.PushLineHeight(translated);
        if (MaterialText.RequiresShaping(translated)) pixels.Y = Math.Max(pixels.Y, ImGui.GetFrameHeight());
        if (pixels.X <= 0)
        {
            var minimum = MaterialText.Measure(translated).X + 2 * ImGui.GetStyle().FramePadding.X;
            pixels.X = pixels.X == 0 ? minimum : Math.Max(ImGui.GetContentRegionAvail().X + pixels.X, minimum);
        }
        var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        ImGui.PushStyleColor(ImGuiCol.Text, Vector4.Zero);
        var clicked = ImGui.Button(label, pixels);
        ImGui.PopStyleColor();
        var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(min, max, true);
        try
        {
        MaterialText.AddText(dl, min + (max - min - MaterialText.Measure(translated)) * .5f, MaterialCanvas.Color(color), translated);
        }
        finally { dl.PopClipRect(); }
        return clicked;
    }
    internal static bool FilledAction(string original, MaterialIcon icon, bool disabled, float extraWidth = 0)
    {
        var s = MaterialTheme.Metrics.Scale;
        var c = MaterialTheme.Current.Colors;
        var label = UiText.T(original);
        var textSize = MaterialText.Measure(label);
        var iconSize = (ChocoboPresentation.Compact ? 30 : 32) * s;
        var gap = 16 * s;
        var minimumWidth = textSize.X + iconSize + gap + 32 * s;
        var available = ImGui.GetContentRegionAvail().X + extraWidth;
        var size = new Vector2(Math.Max(available, minimumWidth), Math.Max((ChocoboPresentation.Compact ? 60 : 70) * s, textSize.Y + 16 * s));
        var overflow = minimumWidth > available;
        var parentId = ImGui.GetID("");
        using var scrollStyle = new MaterialStyleScope();
        if (overflow)
        {
            scrollStyle.Style(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.SetNextWindowContentSize(new Vector2(minimumWidth, 0));
            if (!ImGui.BeginChild("##CalculateActionOverflow", new Vector2(0, size.Y + ImGui.GetStyle().ScrollbarSize), false, ImGuiWindowFlags.HorizontalScrollbar))
            {
                ImGui.EndChild();
                return false;
            }
            ImGuiP.PushOverrideID(parentId);
        }
        try
        {
        ImGui.BeginDisabled(disabled);
        try
        {
        ImGui.PushStyleColor(ImGuiCol.Button, c.Primary);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MaterialColor.Layer(c.Primary, c.OnPrimary, .08f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, MaterialColor.Layer(c.Primary, c.OnPrimary, .14f));
        ImGui.PushStyleColor(ImGuiCol.Text, Vector4.Zero);
        var clicked = ImGui.Button(original, size);
        ImGui.PopStyleColor(4);
        var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
        var ink = c.OnPrimary;
        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(min, max, true);
        try
        {
        var x = Math.Max(16 * s, (size.X - textSize.X - iconSize - gap) * .5f);
        MaterialIcons.Draw(icon, min + new Vector2(x, (size.Y - iconSize) * .5f), iconSize, ink);
        MaterialText.AddText(dl, min + new Vector2(x + iconSize + gap, (size.Y - textSize.Y) * .5f), MaterialCanvas.Color(ink), label);
        }
        finally { dl.PopClipRect(); }
        return clicked;
        }
        finally { ImGui.EndDisabled(); }
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
    internal static bool IconButton(string original, MaterialIcon icon, Vector2 pixels, MaterialIcon trailingIcon = MaterialIcon.None, string? display = null, float logicalIconSize = 22, float logicalGap = 12)
    {
        var scale = MaterialTheme.Metrics.Scale;
        var label = display ?? UiText.T(original.Split("##", 2)[0]);
        var iconSize = logicalIconSize * scale; var gap = logicalGap * scale;
        var extra = trailingIcon == MaterialIcon.None ? 0 : gap + iconSize;
        var textSize = MaterialText.Measure(label);
        pixels.X = Math.Max(pixels.X, textSize.X + iconSize + gap + extra + 24 * scale);
        pixels.Y = Math.Max(pixels.Y, Math.Max(iconSize, textSize.Y) + 20 * scale);
        var ink = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        ImGui.PushStyleColor(ImGuiCol.Text, Vector4.Zero);
        var clicked = ImGui.Button(original, pixels);
        ImGui.PopStyleColor();
        var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
        var width = iconSize + gap + textSize.X + extra;
        var position = min + new Vector2(Math.Max(12 * scale, (max.X - min.X - width) * ImGui.GetStyle().ButtonTextAlign.X), (max.Y - min.Y - iconSize) * .5f);
        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(min, max, true);
        try
        {
        MaterialIcons.Draw(icon, position, iconSize, icon == MaterialIcon.Heart ? new Vector4(1, .46f, .48f, ink.W) : ink);
        MaterialText.AddText(drawList, ImGui.GetFont(), ImGui.GetFontSize(), position + new Vector2(iconSize + gap, (iconSize - textSize.Y) * .5f), MaterialCanvas.Color(ink), label);
        if (trailingIcon != MaterialIcon.None)
            MaterialIcons.Draw(trailingIcon, position + new Vector2(width - iconSize, 0), iconSize, ink);
        }
        finally { drawList.PopClipRect(); }
        return clicked;
    }
    internal static float IconButtonHeight(string original, float width)
    {
        var scale = MaterialTheme.Metrics.Scale;
        var textHeight = MaterialText.Measure(UiText.T(original)).Y;
        return Math.Max(ChocoboPresentation.FooterHeight * scale, Math.Max(28 * scale, textHeight) + 20 * scale);
    }
    internal static bool TabItem(string original, MaterialIcon icon, float logicalWidth, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
    {
        var s = MaterialTheme.Metrics.Scale;
        var label = UiText.T(original);
        var tabPadding = new Vector2(16, ChocoboPresentation.Compact ? 12 : 16) * s;
        if (MaterialText.RequiresShaping(label)) tabPadding.Y += Math.Max(0, MaterialText.Measure(label).Y - ImGui.GetTextLineHeight()) * .5f;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, tabPadding);
        ImGui.SetNextItemWidth(Math.Max(logicalWidth * s, MaterialText.Measure(label).X + 80 * s));
        ImGui.PushStyleColor(ImGuiCol.TabActive, MaterialTheme.Current.Colors.SurfaceContainer);
        var window = ImGuiP.GetCurrentWindow();
        var priorContentRight = window.DC.CursorMaxPos.X;
        var open = ImGui.BeginTabItem(original, flags);
        window.DC.CursorMaxPos.X = Math.Max(priorContentRight, Math.Min(window.DC.CursorMaxPos.X, window.WorkRect.Max.X));
        var nativeBackground = ImGui.GetStyle().Colors[(int)(ImGui.IsItemHovered() ? ImGuiCol.TabHovered : open ? ImGuiCol.TabActive : ImGuiCol.Tab)];
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var c = MaterialTheme.Current.Colors;
        var drawList = ImGui.GetWindowDrawList();
        var iconSize = 30 * s;
        var iconGap = 14 * s;
        var left = Math.Max(12 * s, (max.X - min.X - iconSize - iconGap - MaterialText.Measure(label).X) * .5f);
        var bar = ImGui.GetCurrentContext().CurrentTabBar;
        drawList.PushClipRect(new Vector2(Math.Max(min.X, bar.ScrollingRectMinX), min.Y), new Vector2(Math.Min(max.X, bar.ScrollingRectMaxX), max.Y), true);
        try
        {
        var padding = new Vector2(16, ChocoboPresentation.Compact ? 12 : 16) * s;
        drawList.AddRectFilled(min + padding - Vector2.One, min + padding + MaterialText.Measure(original) + Vector2.One, MaterialCanvas.Color(nativeBackground));
        MaterialIcons.Draw(icon, min + new Vector2(left, Math.Max(0, (max.Y - min.Y - iconSize) * .5f)), iconSize, open ? c.Primary : c.OnSurface);
        MaterialText.AddText(drawList, min + new Vector2(left + iconSize + iconGap, Math.Max(0, (max.Y - min.Y - MaterialText.Measure(label).Y) * .5f)),
            MaterialCanvas.Color(open ? c.Primary : c.OnSurface), label);
        drawList.AddRect(min, max, MaterialCanvas.Color(open ? c.Primary : c.OutlineVariant), 4 * s);
        if (open) drawList.AddRectFilled(new Vector2(min.X, max.Y - 4 * s), max, MaterialCanvas.Color(c.Primary));
        }
        finally { drawList.PopClipRect(); }
        return open;
    }
    internal static bool SmallButton(string label,string? display=null)
    {
        // Native small buttons use the same ID and behavior with zero vertical padding.
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,new Vector2(ImGui.GetStyle().FramePadding.X,0));
        var clicked=Button(label,display);
        ImGui.PopStyleVar();
        return clicked;
    }
    internal static bool Checkbox(string label,ref bool value)
    {
        using var frame = new MaterialStyleScope();
        frame.Style(ImGuiStyleVar.FrameBorderSize, MaterialTheme.Metrics.Scale);
        var visible=label.Split("##",2)[0];
        var translated=UiText.T(visible);
        using var height = MaterialText.PushLineHeight(translated);
        var foreground=ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        var gap=ImGui.GetStyle().ItemInnerSpacing;
        // Native Checkbox sizes its hit area from the original label. Adjust that size for the
        // translated ink while keeping the native widget and its original ID.
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing,new Vector2(Math.Max(0,gap.X+MaterialText.Measure(translated).X-MaterialText.Measure(visible).X),gap.Y));
        ImGui.PushStyleColor(ImGuiCol.Text,Vector4.Zero);
        var changed=ImGui.Checkbox(label,ref value);
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
        var p=ImGui.GetItemRectMin()+new Vector2(ImGui.GetFrameHeight()+gap.X,
            (ImGui.GetItemRectMax().Y-ImGui.GetItemRectMin().Y-MaterialText.Measure(translated).Y)*.5f);
        foreground.W*=ImGui.GetStyle().Alpha;
        MaterialText.AddText(ImGui.GetWindowDrawList(), p,ImGui.ColorConvertFloat4ToU32(foreground),translated);
        return changed;
    }
    internal static void Title(string original,string translated)
        => TitleWithButtons(original, translated, null);

    internal static void ReserveTitleSpace(Window owner, string visible, float minimumWidth)
    {
        var style = ImGui.GetStyle();
        var fontSize = ImGui.GetFontSize();
        var collapse = (owner.Flags & (ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.Modal)) == 0
            && style.WindowMenuButtonPosition != ImGuiDir.None;
        var controls = AdditionalTitleButtonWidth(owner, fontSize)
            + ((owner.ShowCloseButton ? 1 : 0) + (collapse ? 1 : 0)) * (fontSize + style.ItemInnerSpacing.X);
        var required = (MaterialText.Measure(visible).X + controls + style.FramePadding.X * 2 + style.ItemInnerSpacing.X)
            / ImGui.GetIO().FontGlobalScale;
        var bounds = owner.SizeConstraints ?? new WindowSizeConstraints();
        bounds.MinimumSize = new(Math.Max(minimumWidth, required), bounds.MinimumSize.Y);
        owner.SizeConstraints = bounds;
    }

    private static float AdditionalTitleButtonWidth(Window? owner, float fontSize)
    {
        if (owner is null) return 0;
        var count = owner.TitleBarButtons.Count(button => !owner.IsClickthrough || button.AvailableClickthrough);
        if (owner.AllowPinning || owner.AllowClickthrough || owner.AllowBackgroundBlur) count++;
        return count * (fontSize + ImGui.GetStyle().ItemInnerSpacing.X);
    }

    internal static void TitleWithButtons(string original,string translated, Window? owner)
    {
        var s=ImGui.GetStyle(); var size=ImGui.GetFontSize();var height=ImGui.GetFrameHeight();
        var flags=ImGuiP.GetCurrentWindow().Flags;
        var collapseOnLeft=(flags & (ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.Modal))==0 && s.WindowMenuButtonPosition==ImGuiDir.Left;
        var position=ImGui.GetWindowPos()+new Vector2(s.FramePadding.X+(collapseOnLeft?size+s.ItemInnerSpacing.X:0),s.FramePadding.Y);
        var originalWidth=MaterialText.Measure(original).X;
        using var font=UiText.Font(UiFontRole.Body);
        var translatedWidth=MaterialText.Measure(translated).X*size/ImGui.GetFontSize();
        if (MaterialText.RequiresShaping(translated))
            position.Y = ImGui.GetWindowPos().Y + Math.Max(0, (height - MaterialText.Measure(translated).Y * size / ImGui.GetFontSize()) * .5f);
        var dl=ImGui.GetWindowDrawList();
        var rightButtons = owner is null ? 0 : size + s.FramePadding.X * 2 + AdditionalTitleButtonWidth(owner, size);
        if (owner is not null && (flags & ImGuiWindowFlags.NoCollapse) == 0 && s.WindowMenuButtonPosition == ImGuiDir.Right)
            rightButtons += size + s.ItemInnerSpacing.X;
        dl.PushClipRect(owner is null ? ImGui.GetWindowPos() : position,ImGui.GetWindowPos()+new Vector2(Math.Max(0,ImGui.GetWindowSize().X-rightButtons),height),false);
        try
        {
        var bg=s.Colors[(int)(ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows)?ImGuiCol.TitleBgActive:ImGuiCol.TitleBg)];
        dl.AddRectFilled(position,position+new Vector2(Math.Max(originalWidth,translatedWidth),height-s.FramePadding.Y),ImGui.ColorConvertFloat4ToU32(bg));
        MaterialText.AddText(dl, ImGui.GetFont(),size,position,ImGui.ColorConvertFloat4ToU32(s.Colors[(int)ImGuiCol.Text]),translated);
        }
        finally { dl.PopClipRect(); }
    }
    internal static void TableHeadersRow(float height=0)
    {
        using var headerFont = UiText.Current.Language == "hi" ? UiText.Font(UiFontRole.BodyStrong) : null;
        height = Math.Max(height, Enumerable.Range(0, ImGui.TableGetColumnCount()).Select(index =>
            MaterialText.Measure(UiText.T(ImGui.TableGetColumnName(index))).Y).DefaultIfEmpty(0).Max());
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers,height);
        for(var index=0;index<ImGui.TableGetColumnCount();index++)
        {
            if(!ImGui.TableSetColumnIndex(index)) continue;
            var original=ImGui.TableGetColumnName(index);
            var position=ImGui.GetCursorScreenPos();
            var available=ImGui.GetContentRegionAvail().X;
            var foreground = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
            ImGui.PushStyleColor(ImGuiCol.Text, Vector4.Zero);
            ImGui.TableHeader(original);
            ImGui.PopStyleColor();
            var translated=UiText.T(original);
            using var font = UiText.Font(UiFontRole.BodyStrong);
            var textSize = MaterialText.Measure(translated);
            var drawList = ImGui.GetWindowDrawList();
            drawList.PushClipRect(position, position + new Vector2(Math.Max(1, available), height), true);
            try
            {
            MaterialText.AddText(drawList, position + new Vector2(index == 0 ? 0 : Math.Max(0, (available - textSize.X) * .5f), 0), MaterialCanvas.Color(foreground), translated);
            }
            finally { drawList.PopClipRect(); }
            if(translated!=original && MaterialText.Measure(translated).X>available-16*AethertekUI.MaterialTheme.Metrics.Scale && ImGui.IsItemHovered())
                MaterialText.SetTooltip(translated);
        }
    }
    internal static bool CollapsingHeader(string original)
    {
        var label = UiText.T(original.Split("##", 2)[0]);
        using var height = MaterialText.PushLineHeight(label);
        var origin = ImGui.GetCursorScreenPos();
        var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        ImGui.PushStyleColor(ImGuiCol.Text, Vector4.Zero);
        var open = ImGui.CollapsingHeader(original);
        ImGui.PopStyleColor();
        var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(min, max, true);
        try
        {
        MaterialIcons.Draw(open ? MaterialIcon.ChevronDown : MaterialIcon.ArrowRight,
            origin + ImGui.GetStyle().FramePadding, ImGui.GetFontSize(), color);
        MaterialText.AddText(drawList, origin + new Vector2(ImGui.GetFontSize() + 2 * ImGui.GetStyle().FramePadding.X,
            (max.Y - origin.Y - MaterialText.Measure(label).Y) * .5f), MaterialCanvas.Color(color), label);
        }
        finally { drawList.PopClipRect(); }
        return open;
    }
}
