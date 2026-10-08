using System.Numerics;
using AethertekUI;
using Dalamud.Bindings.ImGui;

namespace ChocoboColourized.Ui;

internal enum UiFontRole { Body, BodyStrong, Title, PluginName, Counter, Action, CompactTitle }

internal static class ChocoboPresentation
{
    // Dalamud owns the shared texture through render submission; callers borrow its wrapper.
    internal static Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? OriginalIcon
        => Plugin.TextureProvider.GetFromManifestResource(typeof(Plugin).Assembly, "ChocoboColourized.images.icon.png").GetWrapOrDefault();

    internal static void DrawPluginIcon(ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        var texture = OriginalIcon;
        if (texture is not null)
            MaterialCanvas.DrawImage(drawList, texture.Handle, new Vector2(texture.Width, texture.Height), min, max);
    }

    internal const uint ReferenceAccent = 0xF7D27A;
    internal static readonly float[] FontSizes = [18, 18, 32.5f, 31, 24, 20, 30.666667f];
    internal static readonly string[] FontFiles = ["segoeui.ttf", "seguisb.ttf", "segoeuib.ttf", "seguisb.ttf", "seguisb.ttf", "seguisb.ttf", "segoeuib.ttf"];
    internal static float AtlasHeight(UiFontRole role) => FontSizes[(int)role] * 4 / 3;
    internal static bool Compact => MaterialTheme.Current.Density == MaterialDensity.Compact;
    internal static float HeaderHeight => Compact ? 64 : 78;
    internal static float Gap => 16;
    internal static float ControlHeight => Compact ? 44 : 52;
    internal static Vector2 PanelPadding => new(Compact ? 25 : 27, Compact ? 4 : 8);
    internal static float HeadingHeight => (Compact ? 29.5f : 31) * 4 / 3;
    internal static float HeadingAdvance => Compact ? -3 : 0;
    internal static float FooterHeight => Compact ? 50 : 52;
    internal static float FooterGap => Compact ? 5 : 18;
    internal static float FooterSavePadding => Compact ? 112 : 115;
    internal static float FooterCopyPadding => 113;
    internal static float FooterActionGap => Compact ? 12 : 14;
    internal static float AccentSize => 33;
    internal static float SelectorHeight => Compact ? 44 : 46;
    internal static float LanguageWidth => Compact ? 188 : 198;
    internal static Vector4 Rgb(uint rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1);

    internal static MaterialTheme Theme(uint accent)
    {
        accent &= 0xFFFFFF;
        var selected = Rgb(accent);
        var reference = Rgb(ReferenceAccent);
        var seed = MaterialColor.LabToLch(MaterialColor.SrgbToOklab(new(selected.X, selected.Y, selected.Z)));
        var original = MaterialColor.LabToLch(MaterialColor.SrgbToOklab(new(reference.X, reference.Y, reference.Z)));
        var hue = seed.Y < .001f ? 0 : seed.Z - original.Z;
        var chroma = seed.Y < .001f ? 0 : seed.Y / original.Y;
        Vector4 Relative(uint rgb)
        {
            var color = Rgb(rgb);
            if (accent == ReferenceAccent) return color;
            var lch = MaterialColor.LabToLch(MaterialColor.SrgbToOklab(new(color.X, color.Y, color.Z)));
            return new(MaterialColor.GamutMap(lch.X, lch.Y * chroma, lch.Z + hue), 1);
        }
        var background = Relative(0x1A232C);
        var foreground = Relative(0xF0F1F4);
        var primary = Relative(ReferenceAccent);
        var palette = new OklchPaletteGenerator().Generate(new(selected.X, selected.Y, selected.Z));
        var colors = new MaterialColorScheme(palette)
        {
            Background = background, OnBackground = foreground,
            Surface = Relative(0x1B2831), OnSurface = foreground,
            SurfaceContainerLowest = Relative(0x152129), SurfaceContainerLow = Relative(0x1B2831),
            SurfaceContainer = Relative(0x1D2730), SurfaceContainerHigh = Relative(0x232E38), SurfaceContainerHighest = Relative(0x1C262F),
            SurfaceVariant = Relative(0x343F46), OnSurfaceVariant = Relative(0xB8BCC5),
            Outline = Relative(0x49535B), OutlineVariant = Relative(0x31404E),
            Primary = primary, OnPrimary = MaterialColor.Contrast(primary, background) >= MaterialColor.Contrast(primary, foreground) ? background : foreground,
            PrimaryContainer = Relative(0x4C4026), OnPrimaryContainer = foreground,
            Secondary = Relative(0xC4BCA8), OnSecondary = background, SecondaryContainer = Relative(0x21313C), OnSecondaryContainer = foreground,
            Tertiary = Relative(0xC2B8A3), OnTertiary = background, TertiaryContainer = Relative(0x363127), OnTertiaryContainer = foreground,
            InverseSurface = foreground, InverseOnSurface = background, InversePrimary = Relative(0x7E6123),
        };
        return new(colors, MaterialDensity.Standard) { SurfaceOpacity = 1 };
    }

    internal static MaterialControlMetrics Controls(float height = 0)
    {
        if (height <= 0) height = ControlHeight;
        var s = MaterialTheme.Metrics.Scale;
        return new() { Height = height * s, Padding = new(12 * s, Math.Max(0, (height * s - ImGui.GetTextLineHeight()) * .5f)),
            Gap = 8 * s, IconSize = 22 * s, Rounding = 4 * s, ItemSpacing = new(10 * s, 6 * s), CellPadding = new(12 * s, 6 * s) };
    }

    internal static void Surface(Vector2 min, Vector2 max)
    {
        var c = MaterialTheme.Current.Colors;
        MaterialCanvas.Surface(min, max, c.SurfaceContainerHigh, c.Surface, 4 * MaterialTheme.Metrics.Scale);
        ImGui.GetWindowDrawList().AddRect(min, max, MaterialCanvas.Color(c.OutlineVariant), 4 * MaterialTheme.Metrics.Scale);
    }

    internal static void Feather(Vector2 origin, float size)
    {
        DrawPluginIcon(ImGui.GetWindowDrawList(), origin, origin + new Vector2(size));
    }
}
