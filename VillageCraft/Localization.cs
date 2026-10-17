using System.Globalization;
namespace VillageCraft;

public sealed class LanguageFile
{
    public string Id {get;set;}="";
    public string Name {get;set;}="";
    public Dictionary<string,string> Text {get;set;}=[];
}
public static class L
{
    static readonly Dictionary<string,LanguageFile> languages=[];
    public static string Current {get;private set;}="pt-BR";
    public static IEnumerable<LanguageFile> Languages=>languages.Values;
    public static CultureInfo Culture=>CultureInfo.GetCultureInfo(Current);
    public static void Load(string folder)
    {
        languages.Clear();
        foreach(var file in Directory.GetFiles(folder,"*.toml"))
        {
            var lang=TomlFile.Read<LanguageFile>(file);
            _=CultureInfo.GetCultureInfo(lang.Id);
            if(!languages.TryAdd(lang.Id,lang))throw new InvalidDataException("Duplicate locale "+lang.Id);
        }
        if(!languages.ContainsKey("pt-BR")||!languages.ContainsKey("en-US"))throw new InvalidDataException("Required locales: pt-BR, en-US");
        foreach(var lang in languages.Values)foreach(var entry in lang.Text)
        {
            try
            {
                var format=System.Text.CompositeFormat.Parse(entry.Value);
                if(languages["en-US"].Text.TryGetValue(entry.Key,out var source)&&format.MinimumArgumentCount!=System.Text.CompositeFormat.Parse(source).MinimumArgumentCount)
                    throw new FormatException("Placeholder count differs from en-US");
            }
            catch(FormatException e){throw new InvalidDataException($"Locale {lang.Id}, key {entry.Key}: {e.Message}",e);}
        }
        Set(Current);
    }
    public static void Set(string id)
    {
        Current=languages.ContainsKey(id)?id:"pt-BR";
        CultureInfo.CurrentCulture=Culture;CultureInfo.CurrentUICulture=Culture;
    }
    public static string Get(string key,string fallback)=>languages.TryGetValue(Current,out var lang)&&lang.Text.TryGetValue(key,out var value)?value:languages.TryGetValue("en-US",out var english)&&english.Text.TryGetValue(key,out value)?value:fallback;
    public static string T(string text)=>Get(text,text);
    public static string F(string template,params object?[] args)=>string.Format(Culture,T(template),args);
    public static string Name(object value)=>value switch
    {
        Plan p=>Get("prefab."+p.Id+".name",p.Name),
        ProfessionPlan p=>Get("profession."+p.Id,p.Name),
        UpgradePlan u=>Get("upgrade."+u.Id,u.Name),
        _=>T(value.ToString()??"")
    };
    public static string Description(Plan p)=>Get("prefab."+p.Id+".description",p.Description);
    public static string Entity(string id)=>Get("entity."+id,T(id.Replace('_',' ')));
    public static string Resource(string id)=>Get("resource."+id,id);
    public static string Activity(string id)=>Catalog.Data.Professions.TryGetValue(id,out var p)?Name(p):Catalog.Data.Units.ContainsKey(id)?Entity(id):T(id);
    public static string Task(string task)
    {
        foreach(string prefix in new[]{"Going to ","Assisting: "})if(task.StartsWith(prefix))return T(prefix)+T(task[prefix.Length..]);
        return T(task);
    }
}
