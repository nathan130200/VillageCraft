using Jint;
using Jint.Native;
using Jint.Runtime;
namespace VillageCraft;

public static class EntityTags
{
    public static void Validate(string tags)
    {if(tags==null||tags.Length>256||tags.Any(char.IsControl))throw new InvalidDataException("Tags must be a string of at most 256 characters, without control characters.");}
    public static bool Has(string tags,string key)=>tags==key||tags.Split([' ', ',', ';'],StringSplitOptions.RemoveEmptyEntries).Contains(key,StringComparer.Ordinal);
}
public sealed class ScriptWorld:ScriptGame
{
    readonly City city;readonly ScriptRuntime runtime;readonly Engine engine;
    public ScriptWorld(City city,ScriptRuntime runtime,Engine engine,string component=""):base(city,component){this.city=city;this.runtime=runtime;this.engine=engine;}
    bool Match(JsValue? filter,object entity,int id,string tags,string key)
    {
        if(filter==null||filter.IsUndefined()||filter.IsNull())return true;
        if(filter is Jint.Native.Function.Function){using var profile=Profiler.Measure(ProfileArea.ScriptPredicate);return TypeConverter.ToBoolean(engine.Invoke(filter,entity));}
        if(filter.IsNumber())return id==filter.AsNumber();
        if(!filter.IsString())return false;
        string text=filter.AsString();if(string.IsNullOrEmpty(text)||text=="*")return true;
        if(text.StartsWith("tag:"))return EntityTags.Has(tags,text[4..]);
        if(text.StartsWith("id:"))return int.TryParse(text[3..],out int value)&&value==id;
        if(text.StartsWith("type:"))return key==text[5..];
        if(text.StartsWith("prefab:"))return key==text[7..];
        if(text.StartsWith("profession:"))return key==text[11..];
        return key==text||EntityTags.Has(tags,text)||id.ToString(System.Globalization.CultureInfo.InvariantCulture)==text;
    }
    public ScriptMonster[] getMonsters(JsValue? filter=null){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Enemies.ToArray().Select(m=>new ScriptMonster(city,m)).Where(m=>Match(filter,m,m.id,m.tags,m.type)).ToArray();}
    public ScriptVillager[] getVillagers(JsValue? filter=null){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Citizens.ToArray().Select(v=>new ScriptVillager(city,v)).Where(v=>Match(filter,v,v.id,v.tags,v.profession)).ToArray();}
    public ScriptBuilding[] getBuildings(JsValue? filter=null){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Buildings.ToArray().Select(b=>new ScriptBuilding(city,b)).Where(b=>Match(filter,b,b.id,b.tags,b.prefab)).ToArray();}
    public float birthChance=>city.Rules.BirthChance;
    public int pendingBirths=>city.Babies.Count;
    public bool queueBirth(int homeId)=>runtime.Queue(()=>city.ScheduleBirth(homeId));
    public bool professionPrioritiesActive=>city.ProfessionPriorities.Count>0;
    public ScriptProfession[] getProfessions(){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return Catalog.Data.Professions.Values.Select(p=>new ScriptProfession(p.Id,p.Jobs,city.ProfessionPriority(p.Id))).ToArray();}
    public ScriptBuilding[] getWorkplaces(){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Buildings.Where(b=>b.Plan.Jobs>0).Select(b=>new ScriptBuilding(city,b)).ToArray();}
    public bool assignJobs(int[] villagers,int[] workplaces)=>city.ScriptAssignJobs(villagers,workplaces);
    public ScriptBuilding[] getStores(){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Buildings.Where(b=>b.Plan.Warehouse||b.Storage.Count>0||b.Plan.Input.Count>0||b.Plan.Output.Count>0||!string.IsNullOrEmpty(b.Plan.Animal)||b.RequestedResource!=ResourceType.None).Select(b=>new ScriptBuilding(city,b)).ToArray();}
    public ScriptBuilding[] getDamagedBuildings(){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Buildings.Where(b=>!b.Plan.Natural&&!b.Plan.Indestructible&&b.Health>0&&b.Health<b.Plan.Health).Select(b=>new ScriptBuilding(city,b)).ToArray();}
    public ScriptBuilding[] getAttackableBuildings(){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Buildings.Where(b=>!b.Plan.Natural&&!b.Plan.Indestructible&&(!b.Plan.Path||b.Plan.BlocksRaiders)).Select(b=>new ScriptBuilding(city,b)).ToArray();}
    public ScriptBuilding[] getHarvestSources(string prefab,float x,float z,float radius)
    {
        using var profile=Profiler.Measure(ProfileArea.WorldQueries);
        if(!float.IsFinite(x)||!float.IsFinite(z)||!float.IsFinite(radius)||radius<0||radius>32)return [];
        return city.Buildings.Where(b=>b.Prefab==prefab&&System.Numerics.Vector2.DistanceSquared(new(x,z),new(b.X,b.Z))<=radius*radius).Select(b=>new ScriptBuilding(city,b)).ToArray();
    }
    public ScriptMonster[] getGuardians(JsValue? filter=null){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Defenders.ToArray().Select(m=>new ScriptMonster(city,m)).Where(m=>Match(filter,m,m.id,m.tags,m.type)).ToArray();}
    public ScriptMonster[] getAnimals(JsValue? filter=null){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Animals.ToArray().Select(m=>new ScriptMonster(city,m)).Where(m=>Match(filter,m,m.id,m.tags,m.type)).ToArray();}
    public object[] roads(){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return city.Roads().OrderBy(p=>p.Item1).ThenBy(p=>p.Item2).Select(p=>(object)new ScriptPoint(p.Item1,p.Item2)).ToArray();}
    public string[] resourceNames(){using var profile=Profiler.Measure(ProfileArea.WorldQueries);return Catalog.Resources.Select(ResourceIds.Name).ToArray();}
    public int anchorX=>city.AnchorX;
    public int anchorZ=>city.AnchorZ;
    public float elapsedDay=>city.DayTime;
    // Alias for the spelling in the original API request.
    public ScriptBuilding[] gerBuildings(JsValue? filter=null)=>getBuildings(filter);
    public bool queueSpawnMonster(string type,float x,float z,string tags="")
    {
        EntityTags.Validate(tags);
        if(!Catalog.Data.Units.ContainsKey(type)||!float.IsFinite(x)||!float.IsFinite(z)||x<0||z<0||x>=city.Width||z>=city.Height)return false;
        return runtime.Queue(()=>{if(city.SpawnMonster(type,x,z,tags)==null)city.Notify(new("onCommandFailed",Reason:"Monster limit reached"));});
    }
    public bool queueSpawnBuilding(string prefab,int x,int z,int rotation=0,string tags="")
    {
        EntityTags.Validate(tags);
        if(!Catalog.Data.Prefabs.Any(p=>p.Id==prefab)||!city.Land(x,z)||rotation is <0 or >3)return false;
        return runtime.Queue(()=>
        {
            if(city.Place(prefab,x,z,rotation)){var b=city.At(x,z)!;b.Tags=tags;}
            else city.Notify(new("onCommandFailed",Reason:city.Message));
        });
    }
    public bool queueSpawnVillager(string profession,int home,string tags="")
    {
        EntityTags.Validate(tags);
        if(!Catalog.Data.Professions.ContainsKey(profession))return false;
        return runtime.Queue(()=>{if(city.AddVillager(profession,home,tags)==null)city.Notify(new("onCommandFailed",Reason:"No available home or population cap reached"));});
    }
    public bool queueDeleteMonster(int id)=>runtime.Queue(()=>{var m=city.Enemies.FirstOrDefault(m=>m.Id==id);if(m!=null)city.RemoveMonster(m);});
    public bool queueDeleteVillager(int id)=>runtime.Queue(()=>{var v=city.Citizens.FirstOrDefault(v=>v.Id==id);if(v!=null)city.RemoveVillager(v);});
    public bool queueDeleteBuilding(int id)=>runtime.Queue(()=>{var b=city.Buildings.FirstOrDefault(b=>b.Id==id);if(b!=null)city.Remove(b,true,"deleted");});
    public bool queueDelete(object entity)=>entity switch
    {
        ScriptMonster m=>m.remove(),ScriptVillager v=>queueDeleteVillager(v.id),ScriptBuilding b=>queueDeleteBuilding(b.id),
        _=>false
    };
    public bool queueDamageBuilding(int id,float amount)
    {
        if(!float.IsFinite(amount)||amount<=0||amount>10000)return false;
        return runtime.Queue(()=>{var b=city.Buildings.FirstOrDefault(b=>b.Id==id);if(b!=null)city.DamageBuilding(b,amount);});
    }
    public bool queueDamageMonster(int id,float amount)
    {
        if(!float.IsFinite(amount)||amount<=0||amount>10000)return false;
        return runtime.Queue(()=>{var m=city.Enemies.FirstOrDefault(m=>m.Id==id);if(m!=null)city.DamageMonster(m,amount);});
    }
    public bool queueDamageVillager(int id,float amount)
    {
        if(!float.IsFinite(amount)||amount<=0||amount>10000)return false;
        return runtime.Queue(()=>{var v=city.Citizens.FirstOrDefault(v=>v.Id==id);if(v!=null)city.DamageVillager(v,amount);});
    }
}
public sealed class ScriptMonster
{
    readonly City city;readonly Creature value;
    public ScriptMonster(City city,Creature value){this.city=city;this.value=value;}
    public ScriptBuilding? home=>city.Buildings.FirstOrDefault(b=>b.Id==value.Home) is {} b?new(city,b):null;
    public float age=>value.Age;
    public int id=>value.Id;
    public string type=>value.Type;
    public bool leatherHelmet=>value.LeatherHelmet;
    public bool burningInSun=>value.BurningInSun;
    public bool bossWave=>value.BossWave;
    public float x=>value.X;
    public float z=>value.Z;
    public float health=>value.Health;
    public float state {get=>value.BrainTime;set{if(float.IsFinite(value))this.value.BrainTime=Math.Clamp(value,0,3600);}}
    public string tags {get=>value.Tags;set{EntityTags.Validate(value);this.value.Tags=value;}}
    public string tag {get=>tags;set=>tags=value;}
    public bool hasTag(string key)=>EntityTags.Has(tags,key);
    public string behavior=>value.Behavior;
    public bool attachBehavior(string script)=>city.Scripting.Attach(value,script);
    public bool remove()=>city.Scripting.Queue(()=>
    {
        if(city.Enemies.Contains(value))city.RemoveMonster(value);
        else if(city.Defenders.Remove(value))city.Notify(new("onGuardianRemoved",value,Reason:"deleted"));
        else if(city.Animals.Remove(value))city.Notify(new("onAnimalRemoved",value,Reason:"deleted"));
    });
}

public sealed record ScriptPoint(int x,int z);

public sealed record ScriptProfession(string id,string[] jobs,int priority);
