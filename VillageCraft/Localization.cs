using System.Globalization;
using System.Text.RegularExpressions;
namespace VillageCraft;

public sealed class LanguageFile
{
    public string Id {get;set;}=string.Empty;
    public string Name {get;set;}=string.Empty;
    public Dictionary<string,string> Text {get;set;}=[];
}
public static class L
{
    static readonly Dictionary<string,LanguageFile> languages=[];
    static readonly Regex keyPattern=new(@"^[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+$",RegexOptions.CultureInvariant);
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
                if(!keyPattern.IsMatch(entry.Key))throw new FormatException("Expected a stable localization ID such as ui.options");
                var format=System.Text.CompositeFormat.Parse(entry.Value);
                if(languages["en-US"].Text.TryGetValue(entry.Key,out var source)&&!ArgumentIds(entry.Value).SetEquals(ArgumentIds(source)))
                    throw new FormatException("Placeholder IDs differ from en-US");
            }
            catch(FormatException e){throw new InvalidDataException($"Locale {lang.Id}, key {entry.Key}: {e.Message}",e);}
        }
        Set(Current);
    }
    static HashSet<int> ArgumentIds(string format)
    {
        var ids=new HashSet<int>();
        for(int i=0;i<format.Length;i++)
        {
            if(format[i]!='{')continue;
            if(i+1<format.Length&&format[i+1]=='{'){i++;continue;}
            int start=++i;while(i<format.Length&&char.IsAsciiDigit(format[i]))i++;
            ids.Add(int.Parse(format.AsSpan(start,i-start),CultureInfo.InvariantCulture));
        }
        return ids;
    }
    public static void Set(string id)
    {
        Current=languages.ContainsKey(id)?id:"pt-BR";
        CultureInfo.CurrentCulture=Culture;CultureInfo.CurrentUICulture=Culture;
    }
    public static string Get(string key,string fallback)=>languages.TryGetValue(Current,out var lang)&&lang.Text.TryGetValue(key,out var value)?value:languages.TryGetValue("en-US",out var english)&&english.Text.TryGetValue(key,out value)?value:fallback;
    /// <summary>Resolve a stable ID through the chosen language, English, then the ID itself.</summary>
    public static string T(string key)=>Get(key,key);
    public static string Ui(string id)=>T("ui."+id.Replace(' ','_').ToLowerInvariant());
    public static string Category(string id)=>Get("category."+id,id);
    public static string Effect(string id)=>Get("effect."+id,id);
    public static string F(string template,params object?[] args)=>string.Format(Culture,T(template),args);
    public static string Name(object value)=>value switch
    {
        Plan p=>Get("prefab."+p.Id+".name",p.Name),
        ProfessionPlan p=>Get("profession."+p.Id,p.Name),
        UpgradePlan u=>Get("upgrade."+u.Id,u.Name),
        _=>T(value.ToString()??string.Empty)
    };
    public static string Description(Plan p)=>Get("prefab."+p.Id+".description",p.Description);
    public static string Entity(string id)=>Get("entity."+id,id.Replace('_',' '));
    public static string Resource(ResourceType id)=>Get("resource."+id,id.ToString());
    public static string Activity(string id)=>Catalog.Data.Professions.TryGetValue(id,out var p)?Name(p):Catalog.Data.Units.ContainsKey(id)?Entity(id):Get("activity."+id.Replace(' ','_').ToLowerInvariant(),id);
    public static string Task(TaskId task)=>Get("task."+task,Tasks.Label(task));
}
