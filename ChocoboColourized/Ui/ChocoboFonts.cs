using AethertekUI;
using Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;

namespace ChocoboColourized.Ui;

internal sealed class ChocoboFonts : IDisposable
{
    private readonly IFontHandle[] handles;
    private int generation;
    internal int Generation => System.Threading.Volatile.Read(ref generation);
    internal ChocoboFonts(IFontAtlas atlas, ushort[] ranges,string language)
    {
        handles=ChocoboPresentation.FontSizes.Select((size,index)=>atlas.NewDelegateFontHandle(toolkit=>toolkit.OnPreBuild(build=>
        {
            size=ChocoboPresentation.AtlasHeight((UiFontRole)index);
            var config=new SafeFontConfig { SizePx=size, GlyphRanges=ranges };
            build.Font=build.AddFontFromFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),ChocoboPresentation.FontFiles[index]),config);
            build.AddFontFromFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "seguisym.ttf"),
                new SafeFontConfig { SizePx=size, MergeFont=build.Font, GlyphRanges=ranges });
            // Host-managed merges retain native coverage; Devanagari is validated and drawn by the text host.
            foreach(var locale in UiText.CjkLanguages(language))
                build.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular,new SafeFontConfig
                {
                    SizePx=size, MergeFont=build.Font, GlyphRanges=ranges,
                    // Verified bundled TTC faces: JP=0, KR=1, SC=2, TC=3.
                    // Keep the selected locale first for region-specific ideographs.
                    FontNo=locale switch { "ja"=>0, "zh-Hans"=>2, "ko"=>1, _=>0 },
                });
            build.AttachExtraGlyphsForDalamudLanguage(new SafeFontConfig { SizePx=size, MergeFont=build.Font });
            build.AddGameSymbol(new SafeFontConfig { SizePx=size,MergeFont=build.Font });
        }))).ToArray();
        foreach(var handle in handles) handle.ImFontChanged+=FontChanged;
    }
    private void FontChanged(IFontHandle handle,ILockedImFont font) => System.Threading.Interlocked.Increment(ref generation);
    internal bool Ready => handles.All(h=>h.Available && h.LoadException is null);
    internal Exception? LoadException => handles.FirstOrDefault(h=>h.LoadException is not null)?.LoadException;
    internal unsafe void CheckGlyphs(IEnumerable<string> strings)
    {
        for(var index=0; index<handles.Length; index++)
        {
            using var font=handles[index].Lock();
            foreach(var text in strings)
                foreach(var character in MaterialText.NativeGlyphText(text).Where(c=>!char.IsControl(c)))
                    if(ImGui.FindGlyphNoFallback(font.ImFont,character).Handle==null)
                        throw new InvalidOperationException("Required UI glyph missing: U+"+((int)character).ToString("X4")+" in "+(UiFontRole)index);
        }
    }
    internal IDisposable Push(UiFontRole role)
    {
        var handle=handles[(int)role];
        if(!handle.Available || handle.LoadException is not null) throw new InvalidOperationException("Chocobo Colourized fonts are not ready.",handle.LoadException);
        return handle.Push();
    }
    public void Dispose() { foreach(var handle in handles) { handle.ImFontChanged-=FontChanged;handle.Dispose(); } }
}
