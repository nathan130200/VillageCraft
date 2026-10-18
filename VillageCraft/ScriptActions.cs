using System.Numerics;
namespace VillageCraft;

// Validated simulation actions. Scheduling, priorities and target choice live in Content/Scripts.
public partial class City
{
    internal bool ScriptWalk(Citizen v,Building b,float dt,bool roads){using var profile=Profiler.Measure(ProfileArea.ManagedActions);return WalkTo(v,b,dt,roads,.025f);}
    internal bool ScriptWork(Citizen v,float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        var b=Buildings.FirstOrDefault(b=>b.Id==v.Work&&b.Enabled&&Connected(b,Roads()));
        if(b==null||Vector2.Distance(v.Position,new(b.X,b.Z))>.05f)return false;
        v.WorkTime+=dt*Efficiency(v)*ToolEfficiency;b.Attendance+=dt;b.WorkEffort+=dt*Efficiency(v)*ToolEfficiency;
        if(v.WorkTime>=Rules.ProductionCycle){v.WorkTime-=Rules.ProductionCycle;b.Worked|=Scripting.Produce(b,v);v.HasWorked=true;}
        return true;
    }
    internal bool ScriptChop(Citizen v,Building tree,float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        var camp=Buildings.FirstOrDefault(b=>b.Id==v.Work&&b.Enabled&&Connected(b,Roads())&&b.Plan.HarvestSource==tree.Prefab);
        if(camp==null||tree.GrowthDays>0||Vector2.Distance(v.Position,new(tree.X,tree.Z))>.45f||Vector2.Distance(new(camp.X,camp.Z),new(tree.X,tree.Z))>camp.Plan.HarvestRadius)return false;
        v.WorkTime+=dt*Efficiency(v)*ToolEfficiency;camp.Attendance+=dt;camp.WorkEffort+=dt*Efficiency(v)*ToolEfficiency;
        if(v.WorkTime<Rules.ProductionCycle)return true;
        v.WorkTime-=Rules.ProductionCycle;
        if(HarvestTree(tree,v)){camp.Worked=true;v.HasWorked=true;Scripting.BuildingWork(camp,v);}
        return true;
    }
    internal bool ScriptRepair(Citizen v,Building b,float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        var settings=Catalog.Data.Economy;
        if(!Buildings.Contains(b)||b.Plan.Natural||b.Plan.Indestructible||b.Health<=0||b.Health>=b.Plan.Health||Vector2.Distance(v.Position,new(b.X,b.Z))>.25f||settings.RepairCost.Any(e=>v.Inventory.GetValueOrDefault(e.Key)<e.Value))return false;
        v.RepairTime+=dt*Efficiency(v)*ToolEfficiency;
        if(v.RepairTime<settings.RepairWorkSeconds)return true;
        v.RepairTime-=settings.RepairWorkSeconds;
        foreach(var item in settings.RepairCost)v.Inventory[item.Key]-=item.Value;
        float repaired=Math.Min(settings.RepairAmount,b.Plan.Health-b.Health);b.Health+=repaired;v.HasWorked=true;
        Emit(new("onBuildingRepaired",b,Amount:repaired,Source:v));return true;
    }
    internal bool ScriptDrinkMilk(Citizen v)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        if(v.Inventory.GetValueOrDefault(ResourceType.Milk)<1)return false;
        v.Inventory[ResourceType.Milk]--;v.Effects.Clear();v.Health=Math.Min(v.MaximumHealth,v.Health+Catalog.Data.Economy.MilkHealing);return true;
    }
    internal bool ScriptPickup(Citizen v,Building from,Building to,ResourceType resource)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        if(!from.Enabled||!to.Enabled||Currency(resource)||!Catalog.Resources.Contains(resource)||Vector2.Distance(v.Position,new(from.X,from.Z))>.18f||v.Cargo.Values.Sum()>0||!Connected(to,Roads()))return false;
        float amount=Math.Min(Math.Min(SupplyAvailable(from,resource),StorageFree(to)),Catalog.Data.Economy.DeliveryCapacity*Efficiency(v));
        if(to.Plan.Input.ContainsKey(resource))amount=Math.Min(amount,Math.Max(0,InputTarget(to,resource)-Stored(to,resource)));
        if(to.RequestedResource==resource)amount=Math.Min(amount,Math.Max(0,20-Stored(to,resource)));
        amount=Math.Min(amount,Math.Max(0,32-v.Inventory.Values.Sum()));
        if(amount<=.001f)return false;
        from.Storage[resource]-=amount;Stock[resource]-=amount;v.Cargo[resource]=amount;v.DeliveryTarget=to.Id;v.DeliveryResource=resource;return true;
    }
    internal bool ScriptDeliver(Citizen v,Building to)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        if(!to.Enabled||Vector2.Distance(v.Position,new(to.X,to.Z))>.18f||StorageFree(to)<=0||v.Cargo.Values.Sum()<=0)return false;
        float moved=0;
        foreach(var item in v.Cargo.ToArray()){float n=Receive(to,item.Key,item.Value);v.Cargo[item.Key]-=n;moved+=n;if(v.Cargo[item.Key]<.001f)v.Cargo.Remove(item.Key);}
        if(moved<=0)return false;
        Stock[ResourceType.Goods]+=Catalog.Data.Economy.GoodsPerWorkCycle;v.HasWorked=true;Statistics.Deliveries++;Emit(new("onDeliveryCompleted",v,Source:to));return true;
    }
    internal bool ScriptCollect(Citizen v,Building b,ResourceType resource,float amount,float dt){using var profile=Profiler.Measure(ProfileArea.ManagedActions);return !Currency(resource)&&Catalog.Resources.Contains(resource)&&float.IsFinite(amount)&&amount>0&&amount<=32&&Collect(v,b,resource,amount,dt);}
    internal bool ScriptReach(Vector2 from,Vector2 to){using var profile=Profiler.Measure(ProfileArea.ManagedActions);return MathF.Round(from.X)==MathF.Round(to.X)&&MathF.Round(from.Y)==MathF.Round(to.Y)||Route(from,to,false).Count>0;}
    internal void ScriptMove(Creature mob,Vector2 to,float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        float speed=Catalog.Data.Units.TryGetValue(mob.Type,out var unit)?unit.Speed:.2f;
        MoveCreature(mob,to,speed*dt);
    }
    internal void ScriptPatrol(Citizen v,float x,float z,float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        var b=Buildings.FirstOrDefault(b=>b.Id==v.Work);float speed=b!=null&&Catalog.Data.Units.TryGetValue(b.Plan.Defender,out var unit)?unit.Speed:.9f;
        var route=PreferRoads(v.Position,new(x,z));if(route.Count>0)Move(v,Toward(v.Position,route[0],dt*speed*TravelSpeed(v.Position,v.Profession)));v.Route.Clear();
    }
    internal bool ScriptSight(Vector2 from,Vector2 to){using var profile=Profiler.Measure(ProfileArea.ManagedActions);return Sight(from,to);}
    internal bool ScriptAttack(Citizen v,Creature enemy)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        var b=Buildings.FirstOrDefault(b=>b.Id==v.Work&&b.Enabled&&Connected(b,Roads()));
        if(b==null||b.Plan.CreatesGuardian||!Catalog.Data.Units.TryGetValue(b.Plan.Defender,out var unit)||enemy.Health<=0||v.Cooldown>0||Vector2.Distance(v.Position,enemy.Position)>unit.Range||!Sight(v.Position,enemy.Position))return false;
        DamageMonster(enemy,unit.Damage*Efficiency(v)*(v.Effects.ContainsKey("Weakness")?.7f:1)*(EnchantedArmor?1+Catalog.Data.Economy.EnchantmentDamage:1),v);
        Emit(new("onVillagerAttack",v,Source:enemy));
        v.Cooldown=unit.Cooldown;Shots.Add(new(v.Position,enemy.Position,b.Plan.Defender));return true;
    }
    internal bool ScriptAttack(Creature mob,object target)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        if(!Catalog.Data.Units.TryGetValue(mob.Type,out var unit)||mob.Health<=0||mob.Cooldown>0)return false;
        Vector2 point=target switch{Citizen v=>v.Position,Creature c=>c.Position,Building b=>new(b.X,b.Z),_=>new(-1,-1)};
        if(Vector2.Distance(mob.Position,point)>unit.Range||!Sight(mob.Position,point))return false;
        bool hostile=Enemies.Contains(mob);
        bool valid=hostile?target switch {Citizen person=>Citizens.Contains(person),Creature guardian=>Defenders.Contains(guardian),Building structure=>Buildings.Contains(structure)&&!structure.Plan.Natural&&!structure.Plan.Indestructible,_=>false}:target is Creature enemy&&Enemies.Contains(enemy);
        if(!valid)return false;
        Emit(new(hostile?"onMonsterAttack":"onGuardianAttack",mob,Source:target));
        mob.Cooldown=unit.Cooldown;Shots.Add(new(mob.Position,point,mob.Type));
        switch(target){case Citizen v:DamageVillager(v,unit.Damage*(1-ArmorReduction),mob);break;case Building b:DamageBuilding(b,unit.Damage,mob);break;case Creature c:if(hostile){float damage=unit.Damage*(1-ArmorReduction);c.Health-=damage;Emit(new("onGuardianDamaged",c,Amount:damage,Source:mob));}else DamageMonster(c,unit.Damage,mob);break;}
        return true;
    }
    internal bool ScriptExplode(Creature mob,Vector2 target)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        if(!Enemies.Contains(mob)||mob.Health<=0||!Catalog.Data.Units.TryGetValue(mob.Type,out var unit)||Vector2.Distance(mob.Position,target)>2.5f)return false;
        foreach(var b in Buildings.Where(b=>Vector2.Distance(mob.Position,new(b.X,b.Z))<=2).ToArray())DamageBuilding(b,unit.Damage,mob);
        foreach(var v in Citizens.Where(v=>Vector2.Distance(mob.Position,v.Position)<=2).ToArray())DamageVillager(v,unit.Damage*(1-ArmorReduction),mob);
        foreach(var d in Defenders.Where(d=>Vector2.Distance(mob.Position,d.Position)<=2))d.Health-=unit.Damage*(1-ArmorReduction);
        Emit(new("onExplosion",mob));
        Shots.Add(new(mob.Position,target,"blaze"));DamageMonster(mob,mob.Health,mob);return true;
    }
}

public sealed partial class ScriptVillager
{
    internal Citizen Value=>citizen;
    internal bool Active=>city.Citizens.Contains(citizen)&&citizen.Health>0;
    internal bool Own(ScriptBuilding b)=>city.Buildings.Contains(b.Value);
    static bool Step(float dt)=>float.IsFinite(dt)&&dt>0&&dt<=.2f;
    public ScriptBuilding? home=>city.Buildings.FirstOrDefault(b=>b.Id==citizen.Home) is {} b?new(city,b):null;
    public ScriptBuilding? workplace=>city.Buildings.FirstOrDefault(b=>b.Id==citizen.Work&&b.Enabled&&city.Connected(b,city.Roads())) is {} b?new(city,b):null;
    public int repairTarget {get=>citizen.RepairTarget;set{if(Active&&citizen.RepairTarget!=value){citizen.RepairTarget=value;citizen.RepairTime=0;}}}
    public bool repair(ScriptBuilding b,float dt)=>Active&&Own(b)&&Step(dt)&&city.ScriptRepair(citizen,b.Value,dt);
    public bool setProfession(string profession)
    {
        if(!Active||!Catalog.Data.Professions.ContainsKey(profession)||citizen.Profession==profession)return false;
        citizen.ChangeProfession(profession);citizen.WorkTime=0;city.Notify(new("onVillagerProfessionChanged",citizen));return true;
    }
    public bool hasWorked {get=>citizen.HasWorked;set{if(Active)citizen.HasWorked=value;}}
    public string task {get=>citizen.Task.ToString();set{if(Active&&Enum.TryParse<TaskId>(value,out var id)&&Enum.IsDefined(id))citizen.Task=id;}}
    public int resourceTarget {get=>citizen.ResourceTarget;set{if(Active)citizen.ResourceTarget=value;}}
    public int pickupSource {get=>citizen.PickupSource;set{if(Active)citizen.PickupSource=value;}}
    public int deliveryTarget {get=>citizen.DeliveryTarget;set{if(Active)citizen.DeliveryTarget=value;}}
    public string deliveryResource {get=>ResourceIds.Name(citizen.DeliveryResource);set{if(Active&&ResourceIds.TryParse(value,out var resource)&&!City.Currency(resource))citizen.DeliveryResource=resource;else if(Active&&string.IsNullOrEmpty(value))citizen.DeliveryResource=ResourceType.None;}}
    public float cargoCount=>citizen.Cargo.Values.Sum();
    public bool hasEffects=>citizen.Effects.Count>0;
    public float combatRange=>workplace is {} b&&Catalog.Data.Units.TryGetValue(b.defender,out var u)?u.Range:0;
    public bool drinkMilk()=>Active&&city.ScriptDrinkMilk(citizen);
    public bool canReach(ScriptBuilding b)=>Active&&Own(b)&&city.ScriptReach(citizen.Position,new(b.x,b.z));
    public bool walkTo(ScriptBuilding b,float dt,bool roadsOnly=false)=>Active&&Own(b)&&Step(dt)&&city.ScriptWalk(citizen,b.Value,dt,roadsOnly);
    public bool collect(ScriptBuilding b,string resource,float amount,float dt)=>Active&&Own(b)&&Step(dt)&&ResourceIds.TryParse(resource,out var id)&&city.ScriptCollect(citizen,b.Value,id,amount,dt);
    public bool work(float dt)=>Active&&Step(dt)&&city.ScriptWork(citizen,dt);
    public bool chop(ScriptBuilding b,float dt)=>Active&&Own(b)&&Step(dt)&&city.ScriptChop(citizen,b.Value,dt);
    public bool pickup(ScriptBuilding from,ScriptBuilding to,string resource)=>Active&&Own(from)&&Own(to)&&ResourceIds.TryParse(resource,out var id)&&city.ScriptPickup(citizen,from.Value,to.Value,id);
    public bool deliver(ScriptBuilding b)=>Active&&Own(b)&&city.ScriptDeliver(citizen,b.Value);
    public bool patrol(float x,float z,float dt){if(!Active||!Step(dt)||!float.IsFinite(x)||!float.IsFinite(z)||!city.Land((int)x,(int)z))return false;city.ScriptPatrol(citizen,x,z,dt);return true;}
    public bool attackMonster(int id)=>Active&&city.Enemies.FirstOrDefault(e=>e.Id==id&&e.Health>0) is {} enemy&&city.ScriptAttack(citizen,enemy);
    public bool canSee(float x,float z)=>float.IsFinite(x)&&float.IsFinite(z)&&city.ScriptSight(citizen.Position,new(x,z));
}
public sealed partial class ScriptBuilding
{
    internal Building Value=>value;
    public bool connected=>city.Connected(value,city.Roads());
    public bool worked=>value.Worked;
    public float efficiency=>value.Attendance>0?value.WorkEffort/value.Attendance:1;
    public float serviceCredit {get=>value.ServiceCredit;set{if(float.IsFinite(value))this.value.ServiceCredit=Math.Clamp(value,0,2);}}
    public int homes=>value.Plan.Homes;
    public string[] allowedProfessions=>value.Plan.Professions;
    public int jobs=>value.Plan.Jobs;
    public string animal=>value.Plan.Animal;
    public bool qualified(ScriptVillager v)=>city.Citizens.Contains(v.Value)&&(Catalog.Data.Professions[v.profession].Jobs.Contains(value.Plan.Job)||value.Plan.Professions.Contains(v.profession));
    public bool produce(ScriptVillager worker)=>city.Citizens.Contains(worker.Value)&&worker.Value.Work==id&&value.Enabled&&city.Connected(value,city.Roads())&&aiNear(worker)&&city.ProduceRecipe(value,worker.Value);
    bool aiNear(ScriptVillager worker)=>Vector2.Distance(new(x,z),new(worker.x,worker.z))<=.05f;
    public bool rewardWork(ScriptVillager worker)
    {
        if(!city.Citizens.Contains(worker.Value)||worker.Value.Work!=id||!value.Enabled||!city.Connected(value,city.Roads())||!aiNear(worker))return false;
        city.Stock[ResourceType.Goods]+=Catalog.Data.Economy.GoodsPerWorkCycle;return true;
    }
    public bool breed()=>city.ScriptBreed(value);
    public int adultAnimals=>city.Animals.Count(a=>a.Home==id&&a.Age>=3);
    public bool collectAnimalProduct(string resource,float amount)=>ResourceIds.TryParse(resource,out var type)&&city.ScriptAnimalProduct(value,type,amount);
    public void grow(){if(city.Buildings.Contains(value)&&value.GrowthDays>0)value.GrowthDays--;}
    public bool createGuardian()=>city.ScriptCreateGuardian(value);
    public bool path=>value.Plan.Path;
    public bool natural=>value.Plan.Natural;
    public bool indestructible=>value.Plan.Indestructible;
    public bool blocksRaiders=>value.Plan.BlocksRaiders;
    public bool warehouse=>value.Plan.Warehouse;
    public int priority=>value.Priority;
    public string job=>value.Plan.Job;
    public string defender=>value.Plan.Defender;
    public bool createsGuardian=>value.Plan.CreatesGuardian;
    public string harvestSource=>value.Plan.HarvestSource;
    public float harvestRadius=>value.Plan.HarvestRadius;
    public int growthDays=>value.GrowthDays;
    public float storageFree=>city.StorageFree(value);
    public string requestedResource=>ResourceIds.Name(value.RequestedResource);
    public string[] inputs=>value.Plan.Input.Keys.Where(r=>!City.Currency(r)).Select(ResourceIds.Name).ToArray();
    public string[] outputs=>City.ScriptOutputs(value);
    public float inputAmount(string resource)=>ResourceIds.TryParse(resource,out var id)?value.Plan.Input.GetValueOrDefault(id):0;
    public float availableSupply(string resource)=>ResourceIds.TryParse(resource,out var id)?city.SupplyAvailable(value,id):0;
    public string[] repairInputs=>Catalog.Data.Economy.RepairCost.Keys.Select(ResourceIds.Name).ToArray();
    public float repairCost(string resource)=>ResourceIds.TryParse(resource,out var id)?Catalog.Data.Economy.RepairCost.GetValueOrDefault(id):0;
    public float inputTarget(string resource)=>ResourceIds.TryParse(resource,out var id)?City.ScriptInputTarget(value,id):0;
    public bool canReach(ScriptBuilding other)=>city.Buildings.Contains(value)&&city.Buildings.Contains(other.Value)&&city.ScriptReach(new(x,z),new(other.x,other.z));
}
public partial class City
{
    internal bool ScriptBreed(Building b)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        var herd=Animals.Where(a=>a.Home==b.Id).ToArray();
        if(!Buildings.Contains(b)||!b.Enabled||string.IsNullOrEmpty(b.Plan.Animal)||herd.Count(a=>a.Age>=3)<2||herd.Length>=Rules.AnimalsPerPen||b.ServiceCredit<.999f||!ConsumeLocal(b,new(){[ResourceType.Food]=2}))return false;
        b.ServiceCredit=Math.Max(0,b.ServiceCredit-1);var baby=new Creature{Id=NextId++,Type=b.Plan.Animal,Home=b.Id,X=b.X,Z=b.Z,Health=20,Age=0};Animals.Add(baby);Emit(new("onAnimalSpawned",baby));Log(L.F("message.animal_born",L.Entity(b.Plan.Animal)));return true;
    }
    internal bool ScriptAnimalProduct(Building b,ResourceType resource,float amount)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        if(!Buildings.Contains(b)||!b.Enabled||string.IsNullOrEmpty(b.Plan.Animal)||Currency(resource)||!Catalog.Resources.Contains(resource)||!float.IsFinite(amount)||amount<=0||amount>100)return false;
        RecordProduction(resource,Receive(b,resource,amount));return true;
    }
    internal bool ScriptCreateGuardian(Building b)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        if(!Buildings.Contains(b)||!b.Enabled||!b.Plan.CreatesGuardian||!Catalog.Data.Units.TryGetValue(b.Plan.Defender,out var unit)||Defenders.Count(d=>d.Home==b.Id)>=Rules.GolemsPerWorkshop||b.ServiceCredit<.999f||!ConsumeLocal(b,b.Plan.Input))return false;
        b.ServiceCredit=Math.Max(0,b.ServiceCredit-1);var guardian=new Creature{Id=NextId++,Type=b.Plan.Defender,Home=b.Id,X=b.X,Z=b.Z,Health=unit.Health};Defenders.Add(guardian);Emit(new("onGuardianSpawned",guardian));Log(L.F("ui.awakened",L.Entity(guardian.Type)));return true;
    }
    internal bool ScriptAssignJobs(int[] people,int[] workplaces)
    {
        using var profile=Profiler.Measure(ProfileArea.ManagedActions);
        if(people.Length!=workplaces.Length||people.Length>Citizens.Count||people.Distinct().Count()!=people.Length)return false;
        var assignments=new Dictionary<int,int>();var slots=new Dictionary<int,int>();
        for(int i=0;i<people.Length;i++)
        {
            var v=Citizens.FirstOrDefault(v=>v.Id==people[i]);var b=Buildings.FirstOrDefault(b=>b.Id==workplaces[i]&&b.Enabled&&Connected(b,Roads()));
            if(v==null||b==null||!Qualified(v,b)&&v.Profession!="citizen"||slots.GetValueOrDefault(b.Id)>=b.Plan.Jobs)return false;
            assignments[v.Id]=b.Id;slots[b.Id]=slots.GetValueOrDefault(b.Id)+1;
        }
        foreach(var v in Citizens){int work=assignments.GetValueOrDefault(v.Id,-1);if(v.Work!=work){v.Work=work;v.WorkTime=0;v.Route.Clear();}}
        return true;
    }
    internal static string[] ScriptOutputs(Building b)=>ProducedResources(b).Select(ResourceIds.Name).ToArray();
    internal static float ScriptInputTarget(Building b,ResourceType resource)=>InputTarget(b,resource);
}

public sealed class MobScriptContext
{
    readonly City city;readonly Creature mob;object? selected;bool acted;
    public MobScriptContext(City city,Creature mob,float dt){this.city=city;this.mob=mob;this.dt=dt;self=new(city,mob);game=new(city);}
    public ScriptMonster self {get;}
    public ScriptGame game {get;}
    public string hook=>"tick";
    public string type=>mob.Type;
    public float x=>mob.X;
    public float z=>mob.Z;
    public float health=>mob.Health;
    public float state {get=>mob.BrainTime;set{if(float.IsFinite(value)){if(mob.Type=="creeper"&&mob.BrainTime<=0&&value>0)city.Notify(new("onMonsterPrimed",mob));mob.BrainTime=Math.Clamp(value,0,3600);}}}
    public float dt {get;}
    Vector2 point=>selected switch{Citizen v=>v.Position,Creature c=>c.Position,Building b=>new(b.X,b.Z),_=>new(city.AnchorX,city.AnchorZ)};
    public float distance=>Vector2.Distance(mob.Position,point);
    public float range=>Catalog.Data.Units.TryGetValue(mob.Type,out var unit)?unit.Range:0;
    public bool visible=>city.ScriptSight(mob.Position,point);
    public float cooldown=>mob.Cooldown;
    public bool night=>city.IsNight;
    public ScriptVillager? targetVillager=>selected is Citizen v?new(city,v):null;
    public ScriptBuilding? targetBuilding=>selected is Building b?new(city,b):null;
    public ScriptMonster? targetGuardian=>selected is Creature c?new(city,c):null;
    public bool selectTarget(string kind,int id)
    {
        selected=kind switch{"villager"=>city.Citizens.FirstOrDefault(v=>v.Id==id&&v.Health>0),"building"=>city.Buildings.FirstOrDefault(b=>b.Id==id&&!b.Plan.Natural&&!b.Plan.Indestructible),"guardian"=>city.Defenders.FirstOrDefault(d=>d.Id==id&&d.Health>0),"monster"=>city.Enemies.FirstOrDefault(e=>e.Id==id&&e.Health>0),_=>null};
        return selected!=null;
    }
    public bool canReach(float x,float z)=>float.IsFinite(x)&&float.IsFinite(z)&&x>=0&&z>=0&&x<city.Width&&z<city.Height&&city.ScriptReach(mob.Position,new(x,z));
    public bool canSee(float x,float z)=>float.IsFinite(x)&&float.IsFinite(z)&&city.ScriptSight(mob.Position,new(x,z));
    public bool moveTo(float x,float z)
    {
        if(acted||mob.Health<=0||!float.IsFinite(x)||!float.IsFinite(z)||x<0||z<0||x>=city.Width||z>=city.Height)return false;
        acted=true;city.ScriptMove(mob,new(x,z),dt);return true;
    }
    public void move()=>moveTo(point.X,point.Y);
    public void attack(){if(!acted&&selected!=null)acted=city.ScriptAttack(mob,selected);}
    public void attackVillager(int id){if(selectTarget("villager",id))attack();}
    public void attackBuilding(int id){if(selectTarget("building",id))attack();}
    public void attackGuardian(int id){if(selectTarget("guardian",id))attack();}
    public void attackMonster(int id){if(selectTarget("monster",id))attack();}
    public void explode(){if(!acted)acted=city.ScriptExplode(mob,point);}
}

