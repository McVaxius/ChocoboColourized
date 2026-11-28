using AethertekUI;
using AethertekUI.Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;

namespace ChocoboColourized.Ui;

internal sealed class ChocoboFonts : IDisposable
{
    private readonly IFontAtlas atlas;
    private readonly List<IFontHandle> handles = [];
    private int generation;
    internal int Generation => System.Threading.Volatile.Read(ref generation);
    internal ChocoboFonts(IFontAtlas atlas, ushort[] ranges, string language, string assetRoot)
    {
        this.atlas = atlas;
        atlas.BuildStepChange += MaterialFontAtlas.ReleaseRasterizedInputs;
        try
        {
            foreach (var role in Enum.GetValues<UiFontRole>())
            {
                var handle = MaterialFontAtlas.CreateFont(atlas, ChocoboPresentation.FontWeights[(int)role],
                    ChocoboPresentation.AtlasHeight(role), ranges, language, assetRoot);
                handles.Add(handle);
                handle.ImFontChanged += FontChanged;
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }
    private void FontChanged(IFontHandle handle,ILockedImFont font) => System.Threading.Interlocked.Increment(ref generation);
    internal bool Ready => handles.All(h=>h.Available && h.LoadException is null);
    internal Exception? LoadException => handles.FirstOrDefault(h=>h.LoadException is not null)?.LoadException;
    internal unsafe void CheckGlyphs(IEnumerable<string> strings)
    {
        for(var index=0; index<handles.Count; index++)
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
    public void Dispose()
    {
        atlas.BuildStepChange -= MaterialFontAtlas.ReleaseRasterizedInputs;
        foreach (var handle in handles)
        {
            handle.ImFontChanged -= FontChanged;
            handle.Dispose();
        }
        atlas.Dispose();
    }
}
