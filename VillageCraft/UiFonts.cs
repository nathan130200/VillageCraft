using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
namespace VillageCraft;

static class UiFonts
{
    static string preferredPath=string.Empty;
    static float loadedScale;
    internal static IEnumerable<string> SystemCandidates()
    {
        string windows=Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if(!string.IsNullOrEmpty(windows))
            foreach(string name in new[]{"segoeui.ttf","arial.ttf"})yield return Path.Combine(windows,name);
        yield return "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        yield return "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf";
        yield return "/System/Library/Fonts/Helvetica.ttc";
        yield return "/System/Library/Fonts/Supplemental/Arial.ttf";
    }
    static unsafe bool TryLoad(string path,float size)
    {
        if(!File.Exists(path))return false;
        try
        {
            using var input=File.OpenRead(path);Span<byte> signature=stackalloc byte[4];
            if(input.Read(signature)!=4||!(signature.SequenceEqual(new byte[]{0,1,0,0})||signature.SequenceEqual("OTTO"u8)||signature.SequenceEqual("ttcf"u8)))return false;
            return ImGui.GetIO().Fonts.AddFontFromFileTTF(path,size).NativePtr!=null;
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException)
        {GameLog.Write(GameLogLevel.Warning,"UI font could not be read: "+Path.GetFileName(path));return false;}
    }
    public static void Load(string preferred)
    {
        preferredPath=preferred;
        loadedScale=0;
        EnsureScale(GraphicsOptions.Current.UiScale);
    }
    public static unsafe void EnsureScale(float scale)
    {
        if(loadedScale==scale)return;
        // Rasterize at physical resolution, but retain the UI's logical font size.
        ImGui.GetIO().Fonts.Clear();
        LoadFont(16*scale);
        ImGui.GetIO().FontGlobalScale=1/scale;
        rlImGui.ReloadFonts();
        Rlgl.TextureParameters((uint)ImGui.GetIO().Fonts.TexID,0x2800,0x2600);
        Rlgl.TextureParameters((uint)ImGui.GetIO().Fonts.TexID,0x2801,0x2600);
        loadedScale=scale;
    }
    static unsafe void LoadFont(float size)
    {
        if(TryLoad(preferredPath,size))return;
        foreach(string path in SystemCandidates())
            if(TryLoad(path,size)){GameLog.Write(GameLogLevel.Warning,"Mojangles unavailable; using system font: "+Path.GetFileName(path));return;}
        var config=new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
        try{config.SizePixels=size;ImGui.GetIO().Fonts.AddFontDefault(config);}
        finally{ImGuiNative.ImFontConfig_destroy(config.NativePtr);}
        GameLog.Write(GameLogLevel.Warning,"No usable system font found; using the built-in ImGui font.");
    }
}
