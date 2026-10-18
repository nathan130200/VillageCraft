using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace VillageCraft;

public class Building
{
    public Dictionary<ResourceType,float> Storage {get;set;}=[];
    public ResourceType RequestedResource {get;set;}=ResourceType.None;
    public int GrowthDays {get;set;}
    public string Tags {get;set;}=string.Empty;
    public string Behavior {get;set;}=string.Empty;
    public int Id { get; set; }
    public string Prefab { get; set; } = "path";
    public int X { get; set; }
    public int Z { get; set; }
    public int Rotation { get; set; }
    public float Health { get; set; }
    public int Priority { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    [JsonIgnore] public bool WaitingForSupplies {get;set;}
    public bool Worked { get; set; }
    public float WorkEffort {get;set;}
    public float Attendance {get;set;}
    public float ServiceCredit {get;set;}
    [JsonIgnore] public Plan Plan=>Catalog.Get(Prefab);
    [JsonIgnore] public (int,int) Cell=>(X,Z);
    [JsonIgnore] public (int,int) Entrance=>Rotation switch{0=>(X,Z+1),1=>(X-1,Z),2=>(X,Z-1),_=>(X+1,Z)};
}
public class Citizen
{
    public Dictionary<ResourceType,float> Cargo {get;set;}=[];
    public Dictionary<string,float> Effects {get;set;}=[];
    public int PickupSource {get;set;}=-1;
    public int DeliveryTarget {get;set;}=-1;
    public ResourceType DeliveryResource {get;set;}=ResourceType.None;
    [JsonIgnore] public int ResourceTarget {get;set;}=-1;
    public string Tags {get;set;}=string.Empty;
    public string Behavior {get;set;}=string.Empty;
    public string FirstName {get;set;}=string.Empty;
    public string Surname {get;set;}=string.Empty;
    public Dictionary<ResourceType,float> Inventory {get;set;}=[];
    [JsonIgnore] public string FullName {get{CitizenNames.Ensure(this);return FirstName+" "+Surname;}}

    [JsonIgnore] public int RepairTarget {get;set;}=-1;
    [JsonIgnore] public float RepairTime {get;set;}
    public int Id { get; set; }
    public int Home { get; set; }
    public int Work { get; set; } = -1;
    public float X { get; set; }
    public float Z { get; set; }
    float? health;
    [JsonIgnore] public float MaximumHealth=>Catalog.Data.Professions.TryGetValue(Profession,out var plan)?plan.BaseHealth:50;
    public float Health {get=>health??=MaximumHealth;set=>health=value;}
    public void ChangeProfession(string profession)
    {
        float fraction=Math.Clamp(Health/MaximumHealth,0,1);
        Profession=profession;Health=fraction*MaximumHealth;
    }
    public TaskId Task { get; set; } = TaskId.Resting;
    public float Cooldown { get; set; }
    public float WorkTime { get; set; }
    public bool HasWorked { get; set; }
    public List<Vector2> Route { get; set; } = [];
    public int RouteTarget { get; set; } = -1;
    public string Profession {get;set;}="citizen";
    public float Facing {get;set;}
    [JsonIgnore] public Vector2 Position { get=>new(X,Z);set{X=value.X;Z=value.Y;} }
}
public class Creature
{
    public bool LeatherHelmet {get;set;}
    public bool BossWave {get;set;}
    [JsonIgnore] public bool BurningInSun {get;set;}
    [JsonIgnore] public bool SunKilled {get;set;}
    public int Id {get;set;}
    public string Tags {get;set;}=string.Empty;
    public string Behavior {get;set;}=string.Empty;
    public bool NightSpawn {get;set;}
    public float BrainTime {get;set;}
    public string Type { get; set; } = "zombie";
    public int Home { get; set; } = -1;
    public float X { get; set; }
    public float Z { get; set; }
    public float Health { get; set; }
    public float Cooldown { get; set; }
    public float Age { get; set; } = 30;
    public float Facing {get;set;}
    public List<Vector2> Route {get;set;}=[];
    public Vector2 RouteGoal {get;set;}=new(-1,-1);
    [JsonIgnore] public Vector2 Position {get=>new(X,Z);set{X=value.X;Z=value.Y;}}
}
public record Shot(Vector2 From,Vector2 To,string Type){public float Life {get;set;}=.3f;}
public partial class City:IDisposable
{
    public TerrainData Terrain {get;set;}=new();
    public bool TutorialEnabled {get;set;}
    public int TutorialStep {get;set;}
    public bool TutorialInspected {get;set;}
    public int PeaceDays {get;set;}=5;

    internal ScriptRuntime? runtime;
    [JsonIgnore] public ScriptRuntime Scripting=>runtime??=new(this);
    public event Action<WorldEvent>? Changed;
    void Emit(WorldEvent evt)=>Changed?.Invoke(evt);
    internal void Notify(WorldEvent evt)=>Emit(evt);
    public void Dispose()=>runtime?.Dispose();
    public void EnsureEntityIds()
    {
        NextId=Math.Max(NextId,Buildings.Select(b=>b.Id).Concat(Citizens.Select(v=>v.Id)).Concat(Animals.Concat(Defenders).Concat(Enemies).Select(m=>m.Id)).DefaultIfEmpty().Max()+1);
        foreach(var m in Animals.Concat(Defenders).Concat(Enemies))if(m.Id<=0)m.Id=NextId++;
    }

    public int Version { get; set; } = 3;
    public int Width {get;set;}=32;
    public int Height {get;set;}=32;
    public int ArmorLevel {get;set;}
    public int ToolLevel {get;set;}
    public float Growth {get;set;}
    [JsonIgnore] public int AnchorX=>Width/2;
    [JsonIgnore] public int AnchorZ=>Height/2;
    public VillageRules Rules {get;set;}=new();
    public List<Building> Buildings {get;set;}=[];
    public List<Citizen> Citizens {get;set;}=[];
    public List<Creature> Animals {get;set;}=[];
    public List<Creature> Defenders {get;set;}=[];
    public List<Creature> Enemies {get;set;}=[];
    public Dictionary<ResourceType,float> Stock {get;set;}=[];
    public List<string> Events {get;set;}=[];
    public int LastNight {get;set;}
    [JsonIgnore] public float DayPhase=>Math.Clamp(DayTime/Rules.DayLength,0,1);
    [JsonIgnore] public int MinuteOfDay=>Math.Clamp((int)(DayPhase*1440),0,1439);
    [JsonIgnore] public int Hour=>MinuteOfDay/60;
    [JsonIgnore] public int Minute=>MinuteOfDay%60;
    [JsonIgnore] public string ClockTime=>$"{Hour:00}:{Minute:00}";
    [JsonIgnore] public bool IsNight=>DayPhase>=18f/24||DayPhase<6f/24;
    [JsonIgnore] public float SunStrength
    {
        get
        {
            float hour=DayPhase*24;
            float sun=hour<12?Math.Clamp((hour-5)/2,0,1):Math.Clamp((19-hour)/2,0,1);
            return sun*sun*(3-2*sun);
        }
    }
    [JsonIgnore] public float Daylight=>Catalog.Data.Rendering.NightAmbient+(1-Catalog.Data.Rendering.NightAmbient)*SunStrength;
    public int Day {get;set;}=1;
    public float DayTime {get;set;}
    public float Tax {get;set;}=2;
    public float Happiness {get;set;}=80;
    public float Balance {get;set;}
    public int NextId {get;set;}=1;
    public int Waves {get;set;}
    public int Kills {get;set;}
    public string Message {get;set;}="Build a farm and well. Rotate entrances toward connected paths.";
    [JsonIgnore] public List<Shot> Shots {get;}=[];
    [JsonIgnore] public int Capacity=>Buildings.Sum(b=>b.Plan.Homes);
    [JsonIgnore] public int Water {get{var roads=Roads();return Buildings.Where(b=>Connected(b,roads)&&b.Enabled).Sum(b=>b.Plan.Water);}}
    [JsonIgnore] public int RaidIn=>Rules.RaidsEnabled?Rules.RaidInterval-Day%Rules.RaidInterval:-1;
    [JsonIgnore] public float FoodDays=>Stock[ResourceType.Food]/Math.Max(1,Citizens.Count);
    public bool Land(int x,int z)=>x>=0&&z>=0&&x<Width&&z<Height;
    public bool IsWater(int x,int z)=>Land(x,z)&&Terrain.Water.Length==Width*Height&&Terrain.Water[z*Width+x]!=0;
    public int Elevation(int x,int z)=>Land(x,z)&&Terrain.Elevation.Length==Width*Height?Terrain.Elevation[z*Width+x]:0;
    public bool Walkable(int x,int z)=>Land(x,z)&&!IsWater(x,z);
    public bool CanStep((int X,int Z) a,(int X,int Z) b)=>Walkable(a.X,a.Z)&&Walkable(b.X,b.Z)&&Math.Abs(Elevation(a.X,a.Z)-Elevation(b.X,b.Z))<=1;
    public float SurfaceY(float x,float z)
    {
        int x0=(int)MathF.Floor(x),z0=(int)MathF.Floor(z);float fx=x-x0,fz=z-z0;
        float a=Elevation(x0,z0)*(1-fx)+Elevation(x0+1,z0)*fx,b=Elevation(x0,z0+1)*(1-fx)+Elevation(x0+1,z0+1)*fx;
        return (a*(1-fz)+b*fz)*TerrainData.BlockHeight;
    }
    public static IEnumerable<(int,int)> Neighbors((int X,int Z) p)=>[(p.X-1,p.Z),(p.X+1,p.Z),(p.X,p.Z-1),(p.X,p.Z+1)];
    readonly Dictionary<(int,int),Building> tiles=[];
    int indexedCount=-1;
    HashSet<(int,int)>? connectedRoads;
    public void InvalidateTopology(){indexedCount=-1;connectedRoads=null;foreach(var v in Citizens)v.Route.Clear();foreach(var c in Enemies.Concat(Defenders))c.Route.Clear();}
    public Building? At(int x,int z)
    {
        if(indexedCount!=Buildings.Count){tiles.Clear();foreach(var b in Buildings)tiles[b.Cell]=b;indexedCount=Buildings.Count;connectedRoads=null;}
        return tiles.GetValueOrDefault((x,z));
    }
    public void Log(string text){Message=text;Events.Insert(0,L.F("format.day_details",Day,text));if(Events.Count>40)Events.RemoveAt(Events.Count-1);}
    public HashSet<(int,int)> Roads()
    {
        using var profile=Profiler.Measure(ProfileArea.RoadConnectivity);
        if(indexedCount!=Buildings.Count)At(-1,-1);
        if(connectedRoads!=null)return connectedRoads;
        var paths=Buildings.Where(b=>b.Plan.Path).Select(b=>b.Cell).ToHashSet();var found=new HashSet<(int,int)>();var q=new Queue<(int,int)>();
        if(paths.Contains((AnchorX,AnchorZ))){q.Enqueue((AnchorX,AnchorZ));found.Add((AnchorX,AnchorZ));}
        while(q.TryDequeue(out var p))foreach(var n in Neighbors(p))if(paths.Contains(n)&&CanStep(p,n)&&found.Add(n))q.Enqueue(n);connectedRoads=found;return found;
    }
    public bool Connected(Building b,HashSet<(int,int)> roads)=>!b.Plan.NeedsPath||(b.Plan.Path?roads.Contains(b.Cell):roads.Contains(b.Entrance));
    public string PlacementIssue(string id,int x,int z,int rotation)
    {
        ReconcileStock();var p=Catalog.Get(id);if(!Walkable(x,z))return "placement.land_required";var existing=At(x,z);
        if(existing!=null&&!(existing.Plan.Path&&p.Path&&!existing.Plan.BlocksRaiders&&!p.BlocksRaiders&&existing.Prefab!=id))return "placement.tile_occupied";
        if(PlacementCost(id,x,z).Any(e=>Stock[e.Key]<e.Value))return "placement.insufficient_resources";
        if(p.NeedsPath&&!p.Path&&!Roads().Contains(new Building{X=x,Z=z,Rotation=rotation}.Entrance))return "placement.entrance_not_connected";
        return string.Empty;
    }
    public bool Place(string id,int x,int z,int rotation=0)
    {
        string issue=PlacementIssue(id,x,z,rotation);if(!string.IsNullOrEmpty(issue)){Message=L.T(issue);return false;}
        var p=Catalog.Get(id);if(!Spend(PlacementCost(id,x,z)))return false;
        var road=At(x,z);if(road!=null){TutorialRoadBuilt=true;road.Prefab=id;road.Health=p.Health;InvalidateTopology();Emit(new("onBuildingChanged",road));Log(L.F("ui.road_surface_applied",L.Name(p)));return true;}
        var b=Add(id,x,z,rotation);if(!p.Natural)Statistics.BuildingsPlaced++;
        if(p.Path)TutorialRoadBuilt=true;
        if(!string.IsNullOrEmpty(p.Animal))for(int i=0;i<2;i++)Animals.Add(new(){Id=NextId++,Type=p.Animal,Home=b.Id,X=x+(i==0?-.2f:.2f),Z=z,Health=20});
        Assign();Log(L.F("message.building_placed",L.Name(p)));return true;
    }
    Building Add(string id,int x,int z,int rotation=0){var b=new Building{Id=NextId++,Prefab=id,X=x,Z=z,Rotation=rotation,Health=Catalog.Get(id).Health};Buildings.Add(b);InvalidateTopology();Emit(new("onBuildingPlaced",b));return b;}
    public Dictionary<ResourceType,float> PlacementCost(string id,int x,int z)
    {
        var p=Catalog.Get(id);var old=At(x,z);
        return p.Cost.ToDictionary(e=>e.Key,e=>Math.Max(0,e.Value-(old?.Plan.Path==true&&p.Path?old.Plan.Cost.GetValueOrDefault(e.Key):0)));
    }
    public float TravelSpeed(Vector2 position,string profession)
    {
        var road=At((int)MathF.Round(position.X),(int)MathF.Round(position.Y));
        return road?.Plan.Path==true?road.Plan.MoveSpeed*road.Plan.JobSpeed.GetValueOrDefault(profession,1):1;
    }
    public static City New(int? width=null,int? height=null,bool industry=false,int? seed=null,bool generateTerrain=true,bool tutorial=false)
    {
        var defaults=Catalog.Data.Game;int w=width??defaults.DefaultMapWidth,h=height??defaults.DefaultMapHeight;
        if(w is <32 or >512||h is <32 or >512)throw new ArgumentOutOfRangeException(nameof(width),"Map dimensions must be 32..512.");
        var c=new City{initializing=true,Width=w,Height=h,Rules=defaults.Rules.Copy(),TutorialEnabled=tutorial&&!GraphicsOptions.Current.TutorialDismissed,Stock=Catalog.Resources.ToDictionary(id=>id,id=>defaults.InitialStock.GetValueOrDefault(id))};
        c.DayTime=c.Rules.DayLength*.3f;
        c.Terrain=generateTerrain?TerrainData.Generate(w,h,seed??Random.Shared.Next(),Catalog.Data.Terrain):TerrainData.Flat(w,h);
        foreach(var p in defaults.Starter.Where(p=>!p.Industry))
        {
            var b=c.Add(p.Prefab,c.AnchorX+p.X,c.AnchorZ+p.Z,p.Rotation);
            foreach(string profession in p.Residents)c.Citizens.Add(new(){Id=c.NextId++,Home=b.Id,X=b.X,Z=b.Z,Profession=profession});
        }
        if(industry)foreach(var p in defaults.Starter.Where(p=>p.Industry))c.Place(p.Prefab,c.AnchorX+p.X,c.AnchorZ+p.Z,p.Rotation);
        if(generateTerrain)c.PopulateTrees();
        c.EnsureStorage();c.initializing=false;c.Assign();c.Statistics.PeakPopulation=c.Citizens.Count;
        return c;
    }
    public static City StartVillage(int? width=null,int? height=null,int? seed=null,bool tutorial=true)
    {
        var c=New(width,height,true,seed,tutorial:tutorial);
        c.Log(L.F("ui.your_first_farms_and_animal_keepers_are_ready",c.Rules.RaidInterval));return c;
    }
    public void Remove(Building b,bool destroyed=false,string? reason=null)
    {
        if(b.Plan.Indestructible)return;
        if(destroyed&&!b.Plan.Natural)Statistics.BuildingsDestroyed++;
        if(!destroyed&&(b.Cell==(AnchorX,AnchorZ)||Citizens.Any(v=>v.Home==b.Id))){Message=L.T("ui.central_square_and_occupied_homes_are_protected");return;}
        if(!destroyed)Stock[ResourceType.Emeralds]+=b.Plan.Cost.GetValueOrDefault(ResourceType.Emeralds)/2;
        foreach(var item in b.Storage)Stock[item.Key]-=item.Value;
        Buildings.Remove(b);Animals.RemoveAll(a=>a.Home==b.Id);
        InvalidateTopology();
        foreach(var v in Citizens)
        {
            if(v.Home==b.Id){var home=Buildings.FirstOrDefault(h=>h.Plan.Homes>Citizens.Count(c=>c.Home==h.Id));v.Home=home?.Id??-1;}
            v.Route.Clear();
        }
        Assign();Emit(new("onBuildingRemoved",b,Reason:reason??(destroyed?"destroyed":"demolished")));Log(destroyed?L.F("message.building_destroyed",L.Name(b.Plan)):L.F("message.building_removed",L.Name(b.Plan)));
    }
    bool initializing;
    public void Assign()
    {
        if(initializing)return;
        foreach(var v in Citizens)CitizenNames.Ensure(v);
        Scripting.AssignJobs();
    }
    static bool Qualified(Citizen v,Building b)=>Catalog.Data.Professions[v.Profession].Jobs.Contains(b.Plan.Job)||b.Plan.Professions.Contains(v.Profession);
    public float Efficiency(Citizen v)=>Catalog.Data.Professions[v.Profession].Efficiency;
    public bool Train(Citizen v,string profession)
    {
        if(!Catalog.Data.Professions.TryGetValue(profession,out var plan))return false;
        if(v.Profession==profession)return false;
        if(!Spend(plan.TrainingCost)){Message=L.T("ui.not_enough_supplies_for_training");return false;}
        v.ChangeProfession(profession);v.WorkTime=0;Assign();Emit(new("onVillagerProfessionChanged",v));Log(L.F("ui.trained_as",v.FullName,L.Name(plan)));return true;
    }
    public bool Give(Citizen v,ResourceType resource,float amount)
    {
        ReconcileStock();
        if(Currency(resource)||!Citizens.Contains(v)||!Stock.ContainsKey(resource)||!float.IsFinite(amount)||amount<=0||amount>32||v.Inventory.Values.Sum()+amount>32||Stock[resource]<amount)return false;
        var source=FindSupply(v,resource);
        if(source==null||Vector2.Distance(v.Position,new(source.X,source.Z))>.25f||Stored(source,resource)<amount)return false;
        source.Storage[resource]-=amount;Stock[resource]-=amount;v.Inventory[resource]=v.Inventory.GetValueOrDefault(resource)+amount;return true;
    }
    public float ArmorReduction=>Math.Min(.75f,Catalog.Data.Upgrades.Where(u=>u.Kind==EquipmentKind.Armor&&u.Level<=ArmorLevel).Sum(u=>u.Effect)+(EnchantedArmor?Catalog.Data.Economy.EnchantmentProtection:0));
    public float ToolEfficiency=>1+Catalog.Data.Upgrades.Where(u=>u.Kind==EquipmentKind.Tools&&u.Level<=ToolLevel).Sum(u=>u.Effect);
    public bool Upgrade(string id)
    {
        var upgrade=Catalog.Data.Upgrades.FirstOrDefault(u=>u.Id==id);if(upgrade==null)return false;
        if(upgrade.Level!=(upgrade.Kind==EquipmentKind.Armor?ArmorLevel:ToolLevel)+1){Message=L.T("ui.unlock_equipment_upgrades_in_order");return false;}
        if(!Buildings.Any(b=>b.Plan.UpgradeStation&&b.Enabled&&Connected(b,Roads())&&Citizens.Any(v=>v.Work==b.Id))){Message=L.T("ui.a_connected_staffed_forge_is_required");return false;}
        if(!Spend(upgrade.Cost)){Message=L.T("ui.not_enough_materials_for_this_upgrade");return false;}
        if(upgrade.Kind==EquipmentKind.Armor)ArmorLevel++;else ToolLevel++;
        Log(L.F("ui.equipped_throughout_the_village",L.Name(upgrade)));return true;
    }
    public bool Spend(Dictionary<ResourceType,float> cost,float multiplier=1)
    {
        ReconcileStock();
        if(cost.Any(e=>Stock[e.Key]<e.Value*multiplier))return false;
        foreach(var e in cost){if(!Currency(e.Key))Withdraw(e.Key,e.Value*multiplier);Stock[e.Key]-=e.Value*multiplier;}return true;
    }
    public void Update(float dt)
    {
        if(GameOver){runtime?.Flush();return;}
        if(dt<=0||!float.IsFinite(dt))return;
        if(dt>.2f){while(dt>.2f){Update(.2f);dt-=.2f;}if(dt>0)Update(dt);return;}
        using var profile=Profiler.Measure(ProfileArea.SimulationStep);
        var scripts=Scripting;scripts.Flush();ReconcileStock();
        Statistics.SimulatedSeconds+=dt;DayTime+=dt;SettleAtDawn();UpdateSunlight(dt);
        if(DayPhase>=18f/24&&LastNight!=Day){LastNight=Day;Emit(new("onNightStarted"));}
        UpdateNightEncounters();
        Shots.ForEach(s=>s.Life-=dt);Shots.RemoveAll(s=>s.Life<=0);
        using(Profiler.Measure(ProfileArea.Villagers))
        foreach(var v in Citizens.ToArray())
        {
            if(v.Health<=0)continue;
            if(v.Effects.ContainsKey("Burning"))v.Health-=dt*.5f;
            if(v.Effects.ContainsKey("Poison"))v.Health=Math.Max(1,v.Health-dt*.25f);
            foreach(string effect in v.Effects.Keys.ToArray()){v.Effects[effect]-=dt;if(v.Effects[effect]<=0)v.Effects.Remove(effect);}
            v.Cooldown-=dt;scripts.UpdateVillager(v,dt);
        }
        UpdateAnimals(dt);Combat(dt);
        if(GameOver){scripts.Flush();return;}
        while(DayTime>=Rules.DayLength){DayTime-=Rules.DayLength;Tick();}
        scripts.Tick(dt);
    }
    public static Vector2 Toward(Vector2 from,Vector2 to,float amount){var d=to-from;return d.Length()<=amount?to:from+Vector2.Normalize(d)*amount;}
    public static float Heading(Vector2 delta)=>MathF.Atan2(-delta.X,delta.Y);
    static void Move(Citizen v,Vector2 to){var delta=to-v.Position;if(v.Effects.ContainsKey("Slowness")){delta*=.5f;to=v.Position+delta;}if(delta.LengthSquared()>.0000001f)v.Facing=Heading(delta);v.Position=to;}
    static void Move(Creature v,Vector2 to){var delta=to-v.Position;if(delta.LengthSquared()>.0000001f)v.Facing=Heading(delta);v.Position=to;}
    public List<Vector2> Route(Vector2 from,Vector2 to,bool roadsOnly)
    {
        using var profile=Profiler.Measure(ProfileArea.Pathfinding);
        var start=((int)MathF.Round(from.X),(int)MathF.Round(from.Y));var goal=((int)MathF.Round(to.X),(int)MathF.Round(to.Y));
        if(!Walkable(start.Item1,start.Item2)||!Walkable(goal.Item1,goal.Item2))return [];
        var roads=roadsOnly?Roads():null;var blocked=Buildings.Where(b=>b.Plan.BlocksRaiders||!b.Plan.Path&&!b.Plan.Natural).Select(b=>b.Cell).ToHashSet();
        var prev=new Dictionary<(int,int),(int,int)>{{start,start}};var costs=new Dictionary<(int,int),int>{{start,0}};
        var q=new PriorityQueue<(int,int),int>();q.Enqueue(start,0);var visited=new HashSet<(int,int)>();
        bool DoorEdge((int,int) a,(int,int) b)
        {
            var building=At(a.Item1,a.Item2);return building==null||building.Plan.Path||!building.Plan.NeedsPath||building.Plan.Natural||building.Entrance==b;
        }
        while(q.TryDequeue(out var p,out _))
        {
            if(!visited.Add(p))continue;if(p==goal)break;
            foreach(var n in Neighbors(p))
            {
                bool pass=CanStep(p,n)&&(roadsOnly?((roads!.Contains(n)||n==goal)&&DoorEdge(p,n)&&DoorEdge(n,p)):Land(n.Item1,n.Item2)&&(!blocked.Contains(n)||n==goal)&&DoorEdge(p,n)&&DoorEdge(n,p));
                int cost=costs[p]+(!roadsOnly&&At(n.Item1,n.Item2)?.Plan.Path!=true?5:1);
                if(pass&&(!costs.TryGetValue(n,out int old)||cost<old))
                {prev[n]=p;costs[n]=cost;int distance=Math.Abs(n.Item1-goal.Item1)+Math.Abs(n.Item2-goal.Item2);q.Enqueue(n,(cost+distance)*(Width+Height+1)+distance);}
            }
        }
        if(!prev.ContainsKey(goal))return [];
        var result=new List<Vector2>();for(var p=goal;p!=start;p=prev[p])result.Add(new(p.Item1,p.Item2));result.Reverse();return result;
    }
    internal List<Vector2> PreferRoads(Vector2 from,Vector2 to)
    {
        var route=Route(from,to,true);return route.Count>0?route:Route(from,to,false);
    }
    internal bool ProduceRecipe(Building b,Citizen worker)
    {
        using var profile=Profiler.Measure(ProfileArea.Production);
        var profession=Catalog.Data.Professions[worker.Profession];
        float output=b.Plan.Output.Where(e=>!Currency(e.Key)).Sum(e=>e.Value*profession.ResourceBonus.GetValueOrDefault(e.Key,1));
        if(output>StorageFree(b)+b.Plan.Input.Where(e=>!Currency(e.Key)).Sum(e=>e.Value))return false;
        if(!ConsumeLocal(b,b.Plan.Input))
        {
            // Current input shortages are grouped in the Problems panel, not repeated in the hotbar.
            b.WaitingForSupplies=true;return false;
        }
        b.WaitingForSupplies=false;
        Scripting.BuildingWork(b,worker);
        foreach(var e in b.Plan.Output)RecordProduction(e.Key,Receive(b,e.Key,e.Value*profession.ResourceBonus.GetValueOrDefault(e.Key,1)));
        Stock[ResourceType.Goods]+=Catalog.Data.Economy.GoodsPerWorkCycle;return true;
    }
    public void Tick()
    {
        using var profile=Profiler.Measure(ProfileArea.DailyTick);
        if(GameOver)return;
        var scripts=Scripting;ReconcileStock();
        Day++;MatureBabies();
        Emit(new("onDayStarted"));
        foreach(var b in Buildings.Where(b=>b.Enabled).ToArray()){scripts.BuildingDay(b);Emit(new("onBuildingDay",b));}
        Assign();
        bool fed=Citizens.All(v=>v.Inventory.GetValueOrDefault(ResourceType.Food)>=1);
        foreach(var v in Citizens)
        {
            float ration=Math.Min(1,v.Inventory.GetValueOrDefault(ResourceType.Food));
            if(ration>0)v.Inventory[ResourceType.Food]-=ration;

        }
        bool watered=Water>=Citizens.Count;var roads=Roads();
        float target=30+(fed?30:0)+(watered?25:0)+Math.Min(15,Buildings.Where(b=>b.Enabled&&Connected(b,roads)).Sum(b=>b.Plan.Joy))-Tax*4;
        Happiness=Math.Clamp(Happiness+(target-Happiness)*.25f,0,100);
        Stock[ResourceType.Goods]+=Citizens.Count*Tax*Happiness/100;
        if(fed&&watered&&Happiness>=55)Growth+=1+Citizens.Count(v=>v.Profession=="citizen"&&v.Work<0)*Catalog.Data.Game.CitizenGrowthSupport;
        else Growth=Math.Max(0,Growth-.5f);
        Growth=Math.Min(Growth,Catalog.Data.Game.MigrationThreshold*2);
        if(fed&&watered&&Happiness>=55&&Growth>=Catalog.Data.Game.MigrationThreshold&&Citizens.Count<Rules.MaxPopulation)
        {
            var b=Buildings.FirstOrDefault(b=>b.Plan.Homes>Citizens.Count(v=>v.Home==b.Id)&&Connected(b,roads));
            if(b!=null){string profession=Catalog.Data.Game.ImmigrationProfessions[NextId%Catalog.Data.Game.ImmigrationProfessions.Length];AddVillager(profession,b.Id);Growth-=Catalog.Data.Game.MigrationThreshold;Log(L.F("message.villager_arrived",L.Name(Catalog.Data.Professions[profession])));}
        }
        foreach(var a in Animals)a.Age++;
        foreach(var b in Buildings){b.Worked=false;b.WorkEffort=0;b.Attendance=0;}
        foreach(var v in Citizens){v.HasWorked=false;if(fed&&watered)v.Health=Math.Min(v.MaximumHealth,v.Health+3);}
        if(Rules.RaidsEnabled&&Day%Rules.RaidInterval==0)SpawnWave();
        else if(!fed)Message=L.T(Stock[ResourceType.Food]>=Citizens.Count?"message.villagers_collecting_food":"message.food_shortage");
        else if(!watered)Message=L.T("message.water_shortage");
        scripts.Flush();
    }
    void UpdateAnimals(float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.Animals);
        foreach(var animal in Animals.ToArray())Scripting.UpdateAnimal(animal,dt);
    }
    public void SpawnNight()
    {
        string[] types=["zombie","skeleton","creeper"];
        if(Day<=PeaceDays)return;
        var sites=BorderSpawnSites();if(sites.Count==0)return;
        int count=Math.Min(Rules.NightCount,Math.Max(0,Rules.MaxRaiders-Enemies.Count));
        for(int i=0;i<count;i++)
        {
            var p=sites[Random.Shared.Next(sites.Count)];string type=types[(Day+i)%types.Length];
            SpawnMonster(type,p.X,p.Y,night:true);
        }
        if(count>0)Log(L.F("message.night_monsters",count));
    }
    public void SpawnWave()
    {
        Waves++;int count=Math.Max(0,Math.Min(Rules.MaxRaiders-Enemies.Count,Rules.WaveBase+Waves*Rules.WaveGrowth));string[] types=["zombie","skeleton","creeper","piglin","piglin_brute","zombified_piglin","blaze"];
        var sites=BorderSpawnSites();if(sites.Count==0)count=0;
        for(int i=0;i<count;i++){string type=i==0?"nether_warlord":types[(i-1)%types.Length];var p=sites[Random.Shared.Next(sites.Count)];SpawnMonster(type,p.X,p.Y,bossWave:true);}
        Emit(new("onBossWaveStarted",Amount:Waves));
        Log(L.F("ui.raid_hostiles_led_by_a_nether_warlord_are",Waves,count));
    }
    List<Vector2> BorderSpawnSites()
    {
        var sites=new List<Vector2>();
        for(int x=0;x<Width;x++){Add(x,0);Add(x,Height-1);}
        for(int z=1;z<Height-1;z++){Add(0,z);Add(Width-1,z);}
        return sites;
        void Add(int x,int z){if(Walkable(x,z)&&At(x,z)==null)sites.Add(new(x,z));}
    }
    void Combat(float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.Combat);
        foreach(var d in Defenders.Where(d=>d.Health>0).ToArray())
        {d.Cooldown-=dt;Scripting.UpdateGuardian(d,dt);}
        foreach(var e in Enemies.Where(e=>e.Health>0).ToArray())
        {e.Cooldown-=dt;Scripting.UpdateMonster(e,new MobScriptContext(this,e,dt),dt);}
        ResolveDeaths();
    }
    public void ResolveDeaths()
    {
        int dead=Enemies.Count(e=>e.Health<=0);if(dead>0){int burned=Enemies.Count(e=>e.Health<=0&&e.SunKilled);Statistics.SunDeaths+=burned;Kills+=dead;Stock[ResourceType.Goods]+=(dead-burned)*Catalog.Data.Economy.GoodsPerKill;foreach(var m in Enemies.Where(e=>e.Health<=0).ToArray())RemoveMonster(m,"killed");if(Enemies.Count==0)Log(L.T("ui.hostiles_defeated_repair_and_prepare_for_the_next"));}
        foreach(var d in Defenders.Where(d=>d.Health<=0).ToArray()){Defenders.Remove(d);Emit(new("onGuardianRemoved",d,Reason:"killed"));}
        var fallen=Citizens.Where(v=>v.Health<=0).ToArray();int lost=fallen.Length;foreach(var v in fallen)RemoveVillager(v,"killed");if(lost>0){Log(L.F("ui.villagers_fell_during_the_raid",lost));Assign();}
        foreach(var b in Buildings.Where(b=>b.Health<=0).ToArray())Remove(b,true);
    }
    void MoveCreature(Creature c,Vector2 target,float amount)
    {
        amount*=TravelSpeed(c.Position,c.Type);
        if(c.Route.Count==0||Vector2.DistanceSquared(c.RouteGoal,target)>.6f){c.Route=Route(c.Position,target,false);c.RouteGoal=target;}
        if(c.Route.Count>0){Move(c,Toward(c.Position,c.Route[0],amount));if(Vector2.Distance(c.Position,c.Route[0])<.005f)c.Route.RemoveAt(0);}
        else if((int)MathF.Round(c.X)==(int)MathF.Round(target.X)&&(int)MathF.Round(c.Z)==(int)MathF.Round(target.Y)&&Sight(c.Position,target))Move(c,Toward(c.Position,target,amount));
    }
    bool Sight(Vector2 from,Vector2 to)
    {
        float distance=Vector2.Distance(from,to);
        for(float d=.2f;d<distance-.55f;d+=.2f)
        {
            var p=Vector2.Lerp(from,to,d/distance);var b=At((int)MathF.Round(p.X),(int)MathF.Round(p.Y));
            if(b?.Plan.BlocksRaiders==true)return false;
        }
        return true;
    }
    public Creature? SpawnMonster(string type,float x,float z,string tags="",bool night=false,bool bossWave=false)
    {
        if(!Catalog.Data.Units.ContainsKey(type)||!float.IsFinite(x)||!float.IsFinite(z)||x<0||z<0||x>=Width||z>=Height||Enemies.Count>=Rules.MaxRaiders||!Walkable((int)MathF.Round(x),(int)MathF.Round(z)))return null;
        EntityTags.Validate(tags);
        var m=new Creature{Id=NextId++,Type=type,X=x,Z=z,Health=Catalog.Data.Units[type].Health,Tags=tags,NightSpawn=night,BossWave=bossWave,LeatherHelmet=Catalog.Data.Units[type].BurnsInSun&&(bossWave||Random.Shared.NextSingle()<Catalog.Data.Sunlight.RegularHelmetChance)};
        Enemies.Add(m);Emit(new("onMonsterSpawned",m));return m;
    }
    public Citizen? AddVillager(string profession,int home,string tags="")
    {
        var b=Buildings.FirstOrDefault(b=>b.Id==home&&b.Plan.Homes>Citizens.Count(v=>v.Home==b.Id));
        if(GameOver||b==null||Citizens.Count>=Rules.MaxPopulation||!Catalog.Data.Professions.ContainsKey(profession))return null;
        EntityTags.Validate(tags);
        var v=new Citizen{Id=NextId++,Home=home,X=b.X,Z=b.Z,Profession=profession,Tags=tags};
        CitizenNames.Ensure(v);Citizens.Add(v);Statistics.Arrivals++;Statistics.PeakPopulation=Math.Max(Statistics.PeakPopulation,Citizens.Count);Assign();Emit(new("onVillagerSpawned",v));return v;
    }
    public void RemoveMonster(Creature m,string reason="deleted")
    {if(Enemies.Remove(m))Emit(new("onMonsterRemoved",m,Reason:reason));}
    public void RemoveVillager(Citizen v,string reason="deleted")
    {if(Citizens.Remove(v)){Statistics.VillagersLost++;Assign();Emit(new("onVillagerRemoved",v,Reason:reason));if(Citizens.Count==0)LoseVillage();}}
    public void DamageBuilding(Building b,float amount,object? source=null)
    {
        if(b.Plan.Indestructible||!Buildings.Contains(b)||!float.IsFinite(amount)||amount<=0||b.Health<=0)return;
        float damage=Math.Min(b.Health,amount);b.Health-=damage;Emit(new("onBuildingDamaged",b,damage,Source:source));
    }
    public void DamageVillager(Citizen v,float amount,object? source=null)
    {
        if(!Citizens.Contains(v)||!float.IsFinite(amount)||amount<=0||v.Health<=0)return;
        if(source is Creature monster&&monster.Type is "blaze" or "nether_warlord")v.Effects["Burning"]=10;
        float damage=Math.Min(v.Health,amount);v.Health-=damage;Emit(new("onVillagerDamaged",v,damage,Source:source));
    }
    public void DamageMonster(Creature m,float amount,object? source=null)
    {
        if(!Enemies.Contains(m)||!float.IsFinite(amount)||amount<=0||m.Health<=0)return;
        float damage=Math.Min(m.Health,amount);m.Health-=damage;Emit(new("onMonsterDamaged",m,damage,Source:source));
    }
    public void MoveScriptVillager(Citizen v,Vector2 target,float dt)
    {
        var route=PreferRoads(v.Position,target);
        if(route.Count>0)Move(v,Toward(v.Position,route[0],dt*.85f*TravelSpeed(v.Position,v.Profession)));
        else if(Vector2.Distance(v.Position,target)<.7f)Move(v,Toward(v.Position,target,dt*.85f));
        v.Task=TaskId.ScriptedBehavior;
    }
    public bool Trade(ResourceType resource,bool buy)
    {
        ReconcileStock();
        if(!Buildings.Any(b=>b.Enabled&&b.Plan.Job=="Trading"&&Citizens.Any(v=>v.Work==b.Id))){Message=L.T("ui.connect_and_staff_a_trading_hall");return false;}
        var definition=Catalog.Data.ResourceDefinitions.FirstOrDefault(r=>r.Id==resource);if(definition==null||Currency(resource))return false;float price=definition.BuyPrice;
        var market=Buildings.First(b=>b.Enabled&&b.Plan.Job=="Trading"&&Citizens.Any(v=>v.Work==b.Id));
        if(buy&&Stock[ResourceType.Emeralds]>=price&&StorageFree(market)>=20){Stock[ResourceType.Emeralds]-=price;Receive(market,resource,20);return true;}
        if(!buy)market.RequestedResource=resource;
        if(!buy&&Stored(market,resource)>=20){market.Storage[resource]-=20;Stock[resource]-=20;Stock[ResourceType.Emeralds]+=definition.SellPrice;return true;}
        if(!buy){Message=L.T("ui.delivery_required_bring_20_items_to_the_trading");return false;}
        Message=L.T("ui.insufficient_supplies_for_trade");return false;
    }
    public void Repair(Building b){float amount=Math.Min(25,b.Plan.Health-b.Health);if(amount>0&&Spend(new(){[ResourceType.Stone]=2,[ResourceType.Wood]=2}))b.Health+=amount;}
    public void Save(string path)
    {
        ReconcileStock();BinarySave.Write(path,this);
    }
    public static City Load(string path)
    {
        var c=BinarySave.Read(path);
        bool importedStorage=!c.StorageInitialized;Validate(c);
        if(importedStorage)c.Log(L.T("ui.supplies_moved_into_warehouse_storage_train_deliverymen_to"));
        return c;
    }
    public static void Validate(City c)
    {
        bool finite(float n)=>float.IsFinite(n);
        if(c.Version!=3||c.Width is <32 or >512||c.Height is <32 or >512)throw new InvalidDataException("Unsupported save or map size.");
        c.Terrain??=TerrainData.Flat(c.Width,c.Height);
        if(c.Terrain.Elevation.Length==0)c.Terrain=TerrainData.Flat(c.Width,c.Height);
        c.Terrain.Validate(c.Width,c.Height);
        if(c.LastNightSpawnMinute< -1||c.LastNightSpawnMinute>(c.Day-1L)*1440+1440)throw new InvalidDataException("Invalid night encounter clock.");
        if(c.PeaceDays is <0 or >100||c.TutorialStep is <0 or >100||c.TutorialTreesHarvested<0)throw new InvalidDataException("Invalid world progress");
        if(c.Babies==null||c.Babies.Count>4096||c.Babies.Any(b=>b.Home<=0||b.DueDay<c.Day||b.DueDay>c.Day+1))throw new InvalidDataException("Invalid growing babies.");
        c.Rules??=new();
        float previousLength=c.Rules.DayLength;
        if(previousLength==12)c.Rules.DayLength=600;
        c.Rules.Validate();
        if(float.IsFinite(previousLength)&&previousLength>0&&previousLength!=c.Rules.DayLength)c.DayTime*=c.Rules.DayLength/previousLength;
        if(c.Buildings==null||c.Citizens==null||c.Animals==null||c.Defenders==null||c.Enemies==null||c.Stock==null||c.Events==null||
            c.Buildings.Count>c.Width*c.Height||c.Citizens.Count>4096||c.Enemies.Count>4096||c.Animals.Count>32768||c.Defenders.Count>8192||
            c.Buildings.Any(b=>b==null)||c.Citizens.Any(v=>v==null)||c.Animals.Concat(c.Defenders).Concat(c.Enemies).Any(e=>e==null)||
            c.Buildings.Any(b=>b.Id<=0||b.Id>1000000000||b.GrowthDays is <0 or >30||!Catalog.Data.Prefabs.Any(p=>p.Id==b.Prefab)||!c.Land(b.X,b.Z)||b.Rotation<0||b.Rotation>3||!finite(b.Health)||b.Health<=0||!finite(b.WorkEffort)||!finite(b.Attendance)||!finite(b.ServiceCredit)||b.ServiceCredit<0)||
            c.Buildings.Select(b=>b.Cell).Distinct().Count()!=c.Buildings.Count||c.Buildings.Select(b=>b.Id).Distinct().Count()!=c.Buildings.Count||
            c.Citizens.Any(v=>v.Id<=0||v.Id>1000000000||!finite(v.X)||!finite(v.Z)||v.X<0||v.Z<0||v.X>=c.Width||v.Z>=c.Height||!finite(v.Facing)||!finite(v.Health)||!Catalog.Data.Professions.ContainsKey(v.Profession)||v.FirstName==null||v.Surname==null||v.FirstName.Length>60||v.Surname.Length>60||v.Inventory==null||v.Inventory.Count>64||v.Inventory.Any(e=>!Catalog.Resources.Contains(e.Key)||!finite(e.Value)||e.Value<0)||v.Inventory.Values.Sum()>32||!finite(v.WorkTime)||!finite(v.Cooldown)||(v.Home!=-1&&!c.Buildings.Any(b=>b.Id==v.Home&&b.Plan.Homes>0)))||
            c.Animals.Concat(c.Defenders).Concat(c.Enemies).Any(e=>!finite(e.X)||!finite(e.Z)||e.X<0||e.Z<0||e.X>=c.Width||e.Z>=c.Height||!finite(e.Facing)||!finite(e.Health)||!finite(e.Cooldown)||!finite(e.Age))||
            c.Enemies.Concat(c.Defenders).Any(e=>!Catalog.Data.Units.ContainsKey(e.Type))||c.Animals.Any(a=>!Catalog.Data.Models.ContainsKey(a.Type))||
            Catalog.Resources.Any(r=>!c.Stock.ContainsKey(r)||!finite(c.Stock[r]))||c.Citizens.Select(v=>v.Id).Distinct().Count()!=c.Citizens.Count||c.Citizens.Any(v=>c.Buildings.Any(b=>b.Id==v.Id))||c.ArmorLevel<0||c.ToolLevel<0||c.ArmorLevel>Catalog.Data.Upgrades.Where(u=>u.Kind==EquipmentKind.Armor).Select(u=>u.Level).DefaultIfEmpty().Max()||c.ToolLevel>Catalog.Data.Upgrades.Where(u=>u.Kind==EquipmentKind.Tools).Select(u=>u.Level).DefaultIfEmpty().Max()||!finite(c.Balance)||!finite(c.Growth)||c.LastNight<0||c.LastNight>c.Day||c.Enemies.Any(e=>!finite(e.BrainTime)||e.BrainTime<0)||!finite(c.DayTime)||c.DayTime<0||c.DayTime>=c.Rules.DayLength||!finite(c.Happiness)||!finite(c.Tax)||c.Day<1)
            throw new InvalidDataException("Invalid village data");
        c.EnsureEntityIds();
        foreach(var v in c.Citizens)v.Health=Math.Min(v.Health,v.MaximumHealth);
        var ids=c.Buildings.Select(b=>b.Id).Concat(c.Citizens.Select(v=>v.Id)).Concat(c.Animals.Concat(c.Defenders).Concat(c.Enemies).Select(m=>m.Id)).ToArray();
        if(ids.Distinct().Count()!=ids.Length||ids.Any(id=>id<=0||id>1000000000))throw new InvalidDataException("Invalid entity IDs");
        foreach(var data in c.Buildings.Select(b=>(b.Tags,b.Behavior)).Concat(c.Citizens.Select(v=>(v.Tags,v.Behavior))).Concat(c.Animals.Concat(c.Defenders).Concat(c.Enemies).Select(m=>(m.Tags,m.Behavior))))
        {EntityTags.Validate(data.Tags);if(data.Behavior==null||data.Behavior.Length>128)throw new InvalidDataException("Invalid behavior reference");}
        c.NextId=Math.Max(c.NextId,c.Buildings.Select(b=>b.Id).Concat(c.Citizens.Select(v=>v.Id)).DefaultIfEmpty().Max()+1);
        c.ValidateLogistics();c.ValidateOutcome();
        c.Tax=Math.Clamp(c.Tax,0,6);foreach(var v in c.Citizens)v.Route=[];c.InvalidateTopology();c.Assign();
    }
}
