using System.Numerics;
using ImGuiNET;
namespace VillageCraft;

// Original pixel icons described as editable 16x16 grids; drawn at whole-pixel sizes.
public sealed class UiIconFile
{
    public Dictionary<string,string> Palette {get;set;}=[];
    public List<UiIcon> Icons {get;set;}=[];
}
public sealed class UiIcon
{
    public string Id {get;set;}="";
    public string[] Pixels {get;set;}=[];
}
static class UiIcons
{
    static Dictionary<string,UiIcon> icons=[];
    static Dictionary<char,uint> palette=[];
    public static void Load()
    {
        var file=TomlFile.Read<UiIconFile>(Path.Combine(AppContext.BaseDirectory,"Content","ui_icons.toml"));
        icons=file.Icons.ToDictionary(i=>i.Id);palette=file.Palette.ToDictionary(e=>e.Key[0],e=>ImGui.ColorConvertFloat4ToU32(new Vector4(Scene.ColorOf(e.Value).R/255f,Scene.ColorOf(e.Value).G/255f,Scene.ColorOf(e.Value).B/255f,1)));
        foreach(var icon in icons.Values)if(icon.Pixels.Length!=16||icon.Pixels.Any(r=>r.Length!=16||r.Any(c=>c!='.'&&!palette.ContainsKey(c))))throw new InvalidDataException("Invalid UI icon "+icon.Id);
    }
    public static void Draw(string id,Vector2 position,float size=16)
    {
        if(!icons.TryGetValue(id,out var icon))icon=icons["inspect"];
        int scale=Math.Max(1,(int)(size/16));position=new(MathF.Floor(position.X),MathF.Floor(position.Y));
        var draw=ImGui.GetWindowDrawList();
        for(int y=0;y<16;y++)for(int x=0;x<16;x++)if(palette.TryGetValue(icon.Pixels[y][x],out var color))
        {
            var point=position+new Vector2(x,y)*scale;
            draw.AddRectFilled(point,point+new Vector2(scale),color);
        }
    }
}
