using Jint;
namespace VillageCraft;

public static class Scripts
{
    public static IReadOnlyDictionary<string,string> Sources=>sources;
    static Dictionary<string,string> sources=[];
    public static void Load(string folder)
    {
        var loaded=new Dictionary<string,string>();
        foreach(var file in Directory.GetFiles(folder,"*.js").Order(StringComparer.Ordinal))
        {
            string code=File.ReadAllText(file);
            try{Engine.PrepareScript(code);}catch(Exception e){throw new InvalidDataException("JavaScript "+file+": "+e.Message,e);}
            loaded.Add(Path.GetFileNameWithoutExtension(file),code);
        }
        sources=loaded;
    }
    public static void Require(string id){if(!sources.ContainsKey(id))throw new InvalidDataException("Missing script: "+id);}
}
