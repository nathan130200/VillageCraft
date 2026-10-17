using System.Numerics;
namespace VillageCraft;

// These objects bind to a live city, without exposing its mutable collections or persistence APIs.
public class ScriptGame
{
    readonly City city;
    public ScriptGame(City city)=>this.city=city;
    public int day=>city.Day;
    public int width=>city.Width;
    public int height=>city.Height;
    public float timeOfDay=>city.DayPhase;
    public bool night=>city.IsNight;
    public int population=>city.Citizens.Count;
    public bool gameOver=>city.GameOver;
    public float stock(string resource)=>city.Stock.GetValueOrDefault(resource);
    public void addResource(string resource,float amount)
    {
        if(!city.Stock.ContainsKey(resource)||!float.IsFinite(amount)||amount<0||amount>100)throw new ArgumentException("Invalid resource grant");
        city.Stock[resource]+=amount;city.ReconcileStock();
    }
    public bool takeResource(string resource,float amount)
    {
        if(!city.Stock.ContainsKey(resource)||!float.IsFinite(amount)||amount<0||amount>100)return false;
        return city.Spend(new(){[resource]=amount});
    }
    public ScriptVillager? villager(int id)=>city.Citizens.FirstOrDefault(v=>v.Id==id) is {} v?new(city,v):null;
    public ScriptVillager[] villagers()=>city.Citizens.Select(v=>new ScriptVillager(city,v)).ToArray();
    public ScriptBuilding? building(int id)=>city.Buildings.FirstOrDefault(b=>b.Id==id) is {} b?new(city,b):null;
    public ScriptBuilding[] buildings()=>city.Buildings.Select(b=>new ScriptBuilding(city,b)).ToArray();
    public bool spawnMob(string type,float x,float z)
    {
        return city.Scripting.Queue(()=>city.SpawnMonster(type,x,z));
    }
    public void log(string message){if(message.Length<=256)city.Log(message);}
}
public sealed class ScriptVillager
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
    public float inventory(string resource)=>citizen.Inventory.GetValueOrDefault(resource);
    public float cargo(string resource)=>citizen.Cargo.GetValueOrDefault(resource);
    public float effect(string name)=>citizen.Effects.GetValueOrDefault(name);
    public bool applyEffect(string name,float seconds)
    {
        if(name is not ("Burning" or "Poison" or "Slowness" or "Weakness")||!float.IsFinite(seconds)||seconds<=0||seconds>3600)return false;
        citizen.Effects[name]=seconds;return true;
    }
    public bool give(string resource,float amount)=>city.Give(citizen,resource,amount);
    public void heal(float amount){if(float.IsFinite(amount))citizen.Health=Math.Min(50,citizen.Health+Math.Clamp(amount,0,25));}
}
public sealed class ScriptBuilding
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
    public float storage(string resource)=>value.Storage.GetValueOrDefault(resource);
    public int storageCapacity=>value.Plan.StorageCapacity;
    public bool torchLit=>value.TorchLit;
    public bool setTorch(bool lit)=>city.SetTorch(value,lit);
    public bool enabled {get=>value.Enabled;set{this.value.Enabled=value;city.Assign();}}
    public void repair(float amount){if(float.IsFinite(amount))value.Health=Math.Min(value.Plan.Health,value.Health+Math.Clamp(amount,0,25));}
}
public sealed class MobScriptContext
{
    readonly City city;readonly Action<Vector2>? steer;readonly Action<int,bool>? hit;
    readonly Creature mob;readonly Action<string> command;bool acted;
    public ScriptMonster self {get;}
    public string hook=>"tick";
    public ScriptGame game {get;}
    public string type=>mob.Type;
    public float x=>mob.X;
    public float z=>mob.Z;
    public float health=>mob.Health;
    public float state {get=>mob.BrainTime;set{if(float.IsFinite(value))mob.BrainTime=Math.Clamp(value,0,3600);}}
    public float dt {get;}
    public float distance {get;}
    public float range {get;}
    public bool visible {get;}
    public float cooldown=>mob.Cooldown;
    public bool night=>game.night;
    public ScriptVillager? targetVillager {get;}
    public ScriptBuilding? targetBuilding {get;}
    public MobScriptContext(City city,Creature mob,float dt,float distance,bool visible,Citizen? citizen,Building? building,Action<string> command,Action<Vector2>? steer=null,Action<int,bool>? hit=null)
    {self=new(city,mob);this.city=city;this.steer=steer;this.hit=hit;game=new(city);this.mob=mob;this.dt=dt;this.distance=distance;this.visible=visible;range=Catalog.Data.Units[mob.Type].Range;targetVillager=citizen==null?null:new(city,citizen);targetBuilding=building==null?null:new(city,building);this.command=command;}
    void Act(string action){if(!acted){acted=true;command(action);}}
    public bool moveTo(float x,float z)
    {
        if(acted||steer==null||!float.IsFinite(x)||!float.IsFinite(z)||x<0||z<0||x>=city.Width||z>=city.Height)return false;
        acted=true;steer(new(x,z));return true;
    }
    public void attackVillager(int id){if(!acted&&hit!=null){acted=true;hit(id,true);}}
    public void attackBuilding(int id){if(!acted&&hit!=null){acted=true;hit(id,false);}}
    public void move()=>Act("move");
    public void attack()=>Act("attack");
    public void explode()=>Act("explode");
}
