using ImGuiNET;
namespace VillageCraft;

static class UiFonts
{
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
    static unsafe bool TryLoad(string path)
    {
        if(!File.Exists(path))return false;
        try
        {
            using var input=File.OpenRead(path);Span<byte> signature=stackalloc byte[4];
            if(input.Read(signature)!=4||!(signature.SequenceEqual(new byte[]{0,1,0,0})||signature.SequenceEqual("OTTO"u8)||signature.SequenceEqual("ttcf"u8)))return false;
            return ImGui.GetIO().Fonts.AddFontFromFileTTF(path,16).NativePtr!=null;
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException)
        {GameLog.Write(GameLogLevel.Warning,"UI font could not be read: "+Path.GetFileName(path));return false;}
    }
    public static void Load(string preferred)
    {
        ImGui.GetIO().Fonts.Clear();
        if(TryLoad(preferred))return;
        foreach(string path in SystemCandidates())
            if(TryLoad(path)){GameLog.Write(GameLogLevel.Warning,"Mojangles unavailable; using system font: "+Path.GetFileName(path));return;}
        ImGui.GetIO().Fonts.AddFontDefault();
        GameLog.Write(GameLogLevel.Warning,"No usable system font found; using the built-in ImGui font.");
    }
}
