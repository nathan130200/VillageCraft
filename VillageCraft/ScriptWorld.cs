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
    public ScriptWorld(City city,ScriptRuntime runtime,Engine engine):base(city){this.city=city;this.runtime=runtime;this.engine=engine;}
    bool Match(JsValue? filter,object entity,int id,string tags,string key)
    {
        if(filter==null||filter.IsUndefined()||filter.IsNull())return true;
        if(filter is Jint.Native.Function.Function)return TypeConverter.ToBoolean(engine.Invoke(filter,entity));
        if(filter.IsNumber())return id==filter.AsNumber();
        if(!filter.IsString())return false;
        string text=filter.AsString();if(text==""||text=="*")return true;
        if(text.StartsWith("tag:"))return EntityTags.Has(tags,text[4..]);
        if(text.StartsWith("id:"))return int.TryParse(text[3..],out int value)&&value==id;
        if(text.StartsWith("type:"))return key==text[5..];
        if(text.StartsWith("prefab:"))return key==text[7..];
        if(text.StartsWith("profession:"))return key==text[11..];
        return key==text||EntityTags.Has(tags,text)||id.ToString(System.Globalization.CultureInfo.InvariantCulture)==text;
    }
    public ScriptMonster[] getMonsters(JsValue? filter=null)=>city.Enemies.ToArray().Select(m=>new ScriptMonster(city,m)).Where(m=>Match(filter,m,m.id,m.tags,m.type)).ToArray();
    public ScriptVillager[] getVillagers(JsValue? filter=null)=>city.Citizens.ToArray().Select(v=>new ScriptVillager(city,v)).Where(v=>Match(filter,v,v.id,v.tags,v.profession)).ToArray();
    public ScriptBuilding[] getBuildings(JsValue? filter=null)=>city.Buildings.ToArray().Select(b=>new ScriptBuilding(city,b)).Where(b=>Match(filter,b,b.id,b.tags,b.prefab)).ToArray();
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
        ScriptMonster m=>queueDeleteMonster(m.id),ScriptVillager v=>queueDeleteVillager(v.id),ScriptBuilding b=>queueDeleteBuilding(b.id),
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
    public bool remove()=>city.Scripting.Queue(()=>city.RemoveMonster(value));
}
