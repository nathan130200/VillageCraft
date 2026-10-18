using Tomlyn;
namespace VillageCraft;

public class VillageRules
{
    public VillageRules Copy()=>(VillageRules)MemberwiseClone();
    public bool NightMonsters {get;set;}=true;
    public int NightSpawnIntervalMinutes {get;set;}=60;
    public int NightCount {get;set;}=3;
    public bool RaidsEnabled {get;set;}=true;
    public int RaidInterval {get;set;}=30;
    public int WaveBase {get;set;}=6;
    public int WaveGrowth {get;set;}=3;
    public int MaxRaiders {get;set;}=100;
    public int MaxPopulation {get;set;}=256;
    public int AnimalsPerPen {get;set;}=8;
    public int GolemsPerWorkshop {get;set;}=2;
    public float DayLength {get;set;}=600;
    public float BirthChance {get;set;}=.6f;
    public float ProductionCycle {get;set;}=2;
    public void Validate()
    {
        BirthChance=float.IsFinite(BirthChance)?Math.Clamp(BirthChance,0,1):.6f;
        NightSpawnIntervalMinutes=Math.Clamp(NightSpawnIntervalMinutes,5,660);
        NightCount=Math.Clamp(NightCount,0,12);
        RaidInterval=Math.Clamp(RaidInterval,1,1000);WaveBase=Math.Clamp(WaveBase,1,100);WaveGrowth=Math.Clamp(WaveGrowth,0,50);
        MaxRaiders=Math.Clamp(MaxRaiders,1,256);MaxPopulation=Math.Clamp(MaxPopulation,12,1024);
        AnimalsPerPen=Math.Clamp(AnimalsPerPen,2,32);GolemsPerWorkshop=Math.Clamp(GolemsPerWorkshop,0,8);
        DayLength=float.IsFinite(DayLength)?Math.Clamp(DayLength,60,3600):600;
        ProductionCycle=float.IsFinite(ProductionCycle)?Math.Clamp(ProductionCycle,.5f,10):2;
    }
}
public class GraphicsOptions
{
    public static GraphicsOptions Current {get;private set;}=new();
    public bool TutorialDismissed {get;set;}
    public string Language {get;set;}="pt-BR";
    public int DisplayMode {get;set;}
    public int DisplayWidth {get;set;}=1280;
    public int DisplayHeight {get;set;}=720;
    public float UiScale {get;set;}=1;
    public int Quality {get;set;}=2;
    public bool Textures {get;set;}=true;
    public bool Shadows {get;set;}=true;
    public bool AnimateWater {get;set;}=true;
    public bool EnhancedVisuals {get;set;}
    public bool SmoothShadows {get;set;}=true;
    public float MasterVolume {get;set;}=.75f;
    public float EffectsVolume {get;set;}=.8f;
    public float CreaturesVolume {get;set;}=.7f;
    public float AmbienceVolume {get;set;}=.45f;
    public float AlertsVolume {get;set;}=.8f;
    public int Fps {get;set;}=60;
    public void Validate(){MasterVolume=Volume(MasterVolume);EffectsVolume=Volume(EffectsVolume);CreaturesVolume=Volume(CreaturesVolume);AmbienceVolume=Volume(AmbienceVolume);AlertsVolume=Volume(AlertsVolume);UiScale=float.IsFinite(UiScale)?Math.Clamp(UiScale,1,2):1;DisplayMode=Math.Clamp(DisplayMode,0,2);DisplayWidth=Math.Clamp(DisplayWidth,640,7680);DisplayHeight=Math.Clamp(DisplayHeight,480,4320);Quality=Math.Clamp(Quality,0,2);if(Fps is not (0 or 30 or 60 or 120))Fps=60;}
    static float Volume(float value)=>float.IsFinite(value)?Math.Clamp(value,0,1):.75f;
    public static void Load()
    {
        try{if(File.Exists(SaveStore.OptionsPath))Current=TomlFile.Read<GraphicsOptions>(SaveStore.OptionsPath);Current.Validate();L.Set(Current.Language);}
        catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException){Console.Error.WriteLine("Options reset: "+e.Message);Current=new();}
    }
    public void Save(){Language=L.Current;Validate();SaveStore.WriteAtomic(SaveStore.OptionsPath,TomlSerializer.Serialize(this));}
}
public static class SaveStore
{
    public static string Folder=>Path.Combine(AppContext.BaseDirectory,"UserData");
    public static string OptionsPath=>Path.Combine(Folder,"options.toml");
    public static string PathFor(string name)
    {
        name=name.Trim();if(name.EndsWith(".vcs",StringComparison.OrdinalIgnoreCase))name=name[..^4];else if(name.EndsWith(".json",StringComparison.OrdinalIgnoreCase))name=name[..^5];
        if(name.Length is <1 or >64||name.EndsWith('.')||name.IndexOfAny("<>:\"/\\|?*".ToCharArray())>=0||name.Any(char.IsControl)||name is "." or ".."||name.Equals("options",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Use 1â€“64 characters without path separators or reserved filename characters.");
        string stem=name.Split('.')[0].ToUpperInvariant();
        if(new[]{"CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9","LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"}.Contains(stem))throw new ArgumentException("That filename is reserved by the operating system.");
        return Path.Combine(Folder,name+".vcs");
    }
    public static string LoadPathFor(string name){string binary=PathFor(name);return File.Exists(binary)?binary:Path.ChangeExtension(binary,".json");}
    public static void Delete(string name)
    {
        string binary=PathFor(name);File.Delete(binary);File.Delete(Path.ChangeExtension(binary,".json"));
    }
    public static string[] List()=>Directory.Exists(Folder)?Directory.GetFiles(Folder).Where(p=>Path.GetExtension(p) is ".vcs" or ".json"&&!Path.GetFileName(p).Equals("options.json",StringComparison.OrdinalIgnoreCase)).OrderByDescending(File.GetLastWriteTimeUtc).Select(p=>Path.GetFileNameWithoutExtension(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray():[];
    public static void WriteAtomic(string path,string text)
    {Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);File.WriteAllText(path+".tmp",text);File.Move(path+".tmp",path,true);}
}
