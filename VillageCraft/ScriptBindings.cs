using System.Numerics;
namespace VillageCraft;

// These objects bind to a live city, without exposing its mutable collections or persistence APIs.
public class ScriptGame
{
    readonly City city;readonly string component;
    public ScriptGame(City city,string component=""){this.city=city;this.component=component;}
    public int day=>city.Day;
    public int width=>city.Width;
    public int height=>city.Height;
    public float timeOfDay=>city.DayPhase;
    public bool night=>city.IsNight;
    public int population=>city.Citizens.Count;
    public bool gameOver=>city.GameOver;
    public float stock(string resource)=>ResourceIds.TryParse(resource,out var id)?city.Stock.GetValueOrDefault(id):0;
    public void addResource(string resource,float amount)
    {
        if(!ResourceIds.TryParse(resource,out var id)||!city.Stock.ContainsKey(id)||!float.IsFinite(amount)||amount<0||amount>100)throw new ArgumentException("Invalid resource grant");
        city.Stock[id]+=amount;city.ReconcileStock();
    }
    public bool takeResource(string resource,float amount)
    {
        if(!ResourceIds.TryParse(resource,out var id)||!city.Stock.ContainsKey(id)||!float.IsFinite(amount)||amount<0||amount>100)return false;
        return city.Spend(new(){[id]=amount});
    }
    public ScriptVillager? villager(int id)=>city.Citizens.FirstOrDefault(v=>v.Id==id) is {} v?new(city,v):null;
    public ScriptVillager[] villagers()=>city.Citizens.Select(v=>new ScriptVillager(city,v)).ToArray();
    public ScriptBuilding? building(int id)=>city.Buildings.FirstOrDefault(b=>b.Id==id) is {} b?new(city,b):null;
    public ScriptBuilding[] buildings()=>city.Buildings.Select(b=>new ScriptBuilding(city,b)).ToArray();
    public bool spawnMob(string type,float x,float z)
    {
        return city.Scripting.Queue(()=>city.SpawnMonster(type,x,z));
    }
    public string translate(string key)=>L.T(key);
    public string format(string key,object?[] arguments)=>L.F(key,arguments);
    public void log(string message){GameLog.Script(component,GameLogLevel.Info,message);if(message.Length<=256)city.Log(message);}
    public void warn(string message)=>GameLog.Script(component,GameLogLevel.Warning,message);
    public void error(string message)=>GameLog.Script(component,GameLogLevel.Error,message);
}
public sealed partial class ScriptVillager
{
    readonly City city;readonly Citizen citizen;
    public ScriptVillager(City city,Citizen citizen){this.city=city;this.citizen=citizen;}
    public int id=>citizen.Id;
    public string tags {get=>citizen.Tags;set{EntityTags.Validate(value);citizen.Tags=value;}}
    public string tag {get=>tags;set=>tags=value;}
    public bool hasTag(string key)=>EntityTags.Has(tags,key);
    public string behavior=>citizen.Behavior;
    public bool attachBehavior(string script)=>city.Scripting.Attach(citizen,script);
    public bool remove()=>city.Scripting.Queue(()=>city.RemoveVillager(citizen));
    public bool moveTo(float x,float z,float dt)
    {
        if(!city.Citizens.Contains(citizen)||!float.IsFinite(dt)||dt<=0||dt>.2f||!float.IsFinite(x)||!float.IsFinite(z)||x<0||z<0||x>=city.Width||z>=city.Height)return false;
        city.MoveScriptVillager(citizen,new(x,z),dt);return true;
    }
    public string name=>citizen.FullName;
    public string profession=>citizen.Profession;
    public float x=>citizen.X;
    public float z=>citizen.Z;
    public float health=>citizen.Health;
    public float maximumHealth=>citizen.MaximumHealth;
    public float inventory(string resource)=>ResourceIds.TryParse(resource,out var id)?citizen.Inventory.GetValueOrDefault(id):0;
    public float cargo(string resource)=>ResourceIds.TryParse(resource,out var id)?citizen.Cargo.GetValueOrDefault(id):0;
    public float effect(string name)=>citizen.Effects.GetValueOrDefault(name);
    public bool applyEffect(string name,float seconds)
    {
        if(name is not ("Burning" or "Poison" or "Slowness" or "Weakness")||!float.IsFinite(seconds)||seconds<=0||seconds>3600)return false;
        citizen.Effects[name]=seconds;return true;
    }
    public bool give(string resource,float amount)=>ResourceIds.TryParse(resource,out var id)&&city.Give(citizen,id,amount);
    public void heal(float amount){if(float.IsFinite(amount))citizen.Health=Math.Min(citizen.MaximumHealth,citizen.Health+Math.Clamp(amount,0,25));}
}
public sealed partial class ScriptBuilding
{
    readonly City city;readonly Building value;
    public ScriptBuilding(City city,Building value){this.city=city;this.value=value;}
    public int id=>value.Id;
    public string tags {get=>value.Tags;set{EntityTags.Validate(value);this.value.Tags=value;}}
    public string tag {get=>tags;set=>tags=value;}
    public bool hasTag(string key)=>EntityTags.Has(tags,key);
    public string behavior=>value.Behavior;
    public bool attachBehavior(string script)=>city.Scripting.Attach(value,script);
    public bool remove()=>city.Scripting.Queue(()=>{if(city.Buildings.Contains(value))city.Remove(value,true,"deleted");});
    public string prefab=>value.Prefab;
    public int x=>value.X;
    public int z=>value.Z;
    public float health=>value.Health;
    public float maximumHealth=>value.Plan.Health;
    public float storage(string resource)=>ResourceIds.TryParse(resource,out var id)?value.Storage.GetValueOrDefault(id):0;
    public int storageCapacity=>value.Plan.StorageCapacity;
    public bool enabled {get=>value.Enabled;set{this.value.Enabled=value;city.Assign();}}
    public void repair(float amount){if(float.IsFinite(amount))value.Health=Math.Min(value.Plan.Health,value.Health+Math.Clamp(amount,0,25));}
}
