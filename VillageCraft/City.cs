using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace VillageCraft;

public class Building
{
    public Dictionary<string,float> Storage {get;set;}=[];
    public bool TorchLit {get;set;}
    public string RequestedResource {get;set;}="";
    public int GrowthDays {get;set;}
    public string Tags {get;set;}="";
    public string Behavior {get;set;}="";
    public int Id { get; set; }
    public string Prefab { get; set; } = "path";
    public int X { get; set; }
    public int Z { get; set; }
    public int Rotation { get; set; }
    public float Health { get; set; }
    public int Priority { get; set; } = 1;
    public bool Enabled { get; set; } = true;
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
    public Dictionary<string,float> Cargo {get;set;}=[];
    public Dictionary<string,float> Effects {get;set;}=[];
    public int PickupSource {get;set;}=-1;
    public int DeliveryTarget {get;set;}=-1;
    public string DeliveryResource {get;set;}="";
    [JsonIgnore] public int TorchTarget {get;set;}=-1;
    [JsonIgnore] public int ResourceTarget {get;set;}=-1;
    public string Tags {get;set;}="";
    public string Behavior {get;set;}="";
    public string FirstName {get;set;}="";
    public string Surname {get;set;}="";
    public Dictionary<string,float> Inventory {get;set;}=[];
    [JsonIgnore] public string FullName {get{CitizenNames.Ensure(this);return FirstName+" "+Surname;}}

    public int Id { get; set; }
    public int Home { get; set; }
    public int Work { get; set; } = -1;
    public float X { get; set; }
    public float Z { get; set; }
    public float Health { get; set; } = 50;
    public string Task { get; set; } = "Resting";
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
    public string Tags {get;set;}="";
    public string Behavior {get;set;}="";
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
    public Dictionary<string,float> Stock {get;set;}=[];
    public List<string> Events {get;set;}=[];
    public int LastNight {get;set;}
    [JsonIgnore] public float DayPhase=>Math.Clamp(DayTime/Rules.DayLength,0,1);
    [JsonIgnore] public int MinuteOfDay=>Math.Clamp((int)(DayPhase*1440),0,1439);
    [JsonIgnore] public int Hour=>MinuteOfDay/60;
    [JsonIgnore] public int Minute=>MinuteOfDay%60;
    [JsonIgnore] public string ClockTime=>$"{Hour:00}:{Minute:00}";
    [JsonIgnore] public bool IsNight=>DayPhase>=18f/24||DayPhase<6f/24;
    [JsonIgnore] public float Daylight
    {
        get
        {
            float hour=DayPhase*24;
            float sun=hour<12?Math.Clamp((hour-5)/2,0,1):Math.Clamp((19-hour)/2,0,1);
            sun=sun*sun*(3-2*sun);
            return .16f+.84f*sun;
        }
    }
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
    [JsonIgnore] public float FoodDays=>Stock["Food"]/Math.Max(1,Citizens.Count);
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
    public void Log(string text){Message=text;Events.Insert(0,L.F("Day {0}: {1}",Day,text));if(Events.Count>40)Events.RemoveAt(Events.Count-1);}
    public HashSet<(int,int)> Roads()
    {
        if(indexedCount!=Buildings.Count)At(-1,-1);
        if(connectedRoads!=null)return connectedRoads;
        var paths=Buildings.Where(b=>b.Plan.Path).Select(b=>b.Cell).ToHashSet();var found=new HashSet<(int,int)>();var q=new Queue<(int,int)>();
        if(paths.Contains((AnchorX,AnchorZ))){q.Enqueue((AnchorX,AnchorZ));found.Add((AnchorX,AnchorZ));}
        while(q.TryDequeue(out var p))foreach(var n in Neighbors(p))if(paths.Contains(n)&&CanStep(p,n)&&found.Add(n))q.Enqueue(n);connectedRoads=found;return found;
    }
    public bool Connected(Building b,HashSet<(int,int)> roads)=>!b.Plan.NeedsPath||(b.Plan.Path?roads.Contains(b.Cell):roads.Contains(b.Entrance));
    public string PlacementIssue(string id,int x,int z,int rotation)
    {
        ReconcileStock();var p=Catalog.Get(id);if(!Walkable(x,z))return "Land required";var existing=At(x,z);
        if(existing!=null&&!(existing.Plan.Path&&p.Path&&!existing.Plan.BlocksRaiders&&!p.BlocksRaiders&&existing.Prefab!=id))return "Tile occupied";
        if(PlacementCost(id,x,z).Any(e=>Stock[e.Key]<e.Value))return "Insufficient resources";
        if(p.NeedsPath&&!p.Path&&!Roads().Contains(new Building{X=x,Z=z,Rotation=rotation}.Entrance))return "Rotate entrance to face a connected path";
        return "";
    }
    public bool Place(string id,int x,int z,int rotation=0)
    {
        string issue=PlacementIssue(id,x,z,rotation);if(issue!=""){Message=issue;return false;}
        var p=Catalog.Get(id);if(!Spend(PlacementCost(id,x,z)))return false;
        var road=At(x,z);if(road!=null){TutorialRoadBuilt=true;road.Prefab=id;road.Health=p.Health;InvalidateTopology();Emit(new("onBuildingChanged",road));Log(L.F("{0} road surface applied.",L.Name(p)));return true;}
        var b=Add(id,x,z,rotation);if(!p.Natural)Statistics.BuildingsPlaced++;
        if(p.Path)TutorialRoadBuilt=true;
        if(p.Animal!="")for(int i=0;i<2;i++)Animals.Add(new(){Id=NextId++,Type=p.Animal,Home=b.Id,X=x+(i==0?-.2f:.2f),Z=z,Health=20});
        Assign();Log(L.F("{0} built.",L.Name(p)));return true;
    }
    Building Add(string id,int x,int z,int rotation=0){var b=new Building{Id=NextId++,Prefab=id,X=x,Z=z,Rotation=rotation,Health=Catalog.Get(id).Health};Buildings.Add(b);InvalidateTopology();lightingDirty=true;Emit(new("onBuildingPlaced",b));return b;}
    public Dictionary<string,float> PlacementCost(string id,int x,int z)
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
        var c=new City{Width=w,Height=h,Rules=defaults.Rules.Copy(),TutorialEnabled=tutorial&&!GraphicsOptions.Current.TutorialDismissed,Stock=Catalog.Resources.ToDictionary(id=>id,id=>defaults.InitialStock.GetValueOrDefault(id))};
        c.DayTime=c.Rules.DayLength*.3f;
        c.Terrain=generateTerrain?TerrainData.Generate(w,h,seed??Random.Shared.Next(),Catalog.Data.Terrain):TerrainData.Flat(w,h);
        foreach(var p in defaults.Starter.Where(p=>!p.Industry))
        {
            var b=c.Add(p.Prefab,c.AnchorX+p.X,c.AnchorZ+p.Z,p.Rotation);
            foreach(string profession in p.Residents)c.Citizens.Add(new(){Id=c.NextId++,Home=b.Id,X=b.X,Z=b.Z,Profession=profession});
        }
        if(industry)foreach(var p in defaults.Starter.Where(p=>p.Industry))c.Place(p.Prefab,c.AnchorX+p.X,c.AnchorZ+p.Z,p.Rotation);
        if(generateTerrain)c.PopulateTrees();
        c.EnsureStorage();c.Assign();c.Statistics.PeakPopulation=c.Citizens.Count;
        return c;
    }
    public static City StartVillage(int? width=null,int? height=null,int? seed=null,bool tutorial=true)
    {
        var c=New(width,height,true,seed,tutorial:tutorial);
        c.Log(L.F("Your first farms and animal keepers are ready. Build a forge and prepare defenses before day {0}.",c.Rules.RaidInterval));return c;
    }
    public void Remove(Building b,bool destroyed=false,string? reason=null)
    {
        if(b.Plan.Indestructible)return;
        if(destroyed&&!b.Plan.Natural)Statistics.BuildingsDestroyed++;
        if(!destroyed&&(b.Cell==(AnchorX,AnchorZ)||Citizens.Any(v=>v.Home==b.Id))){Message=L.T("Central square and occupied homes are protected.");return;}
        if(!destroyed)Stock["Emeralds"]+=b.Plan.Cost.GetValueOrDefault("Emeralds")/2;
        foreach(var item in b.Storage)Stock[item.Key]-=item.Value;
        foreach(var v in Citizens.Where(v=>v.TorchTarget==b.Id))ReleaseTorchDuty(v);
        Buildings.Remove(b);lightingDirty=true;Animals.RemoveAll(a=>a.Home==b.Id);
        InvalidateTopology();
        foreach(var v in Citizens)
        {
            if(v.Home==b.Id){var home=Buildings.FirstOrDefault(h=>h.Plan.Homes>Citizens.Count(c=>c.Home==h.Id));v.Home=home?.Id??-1;}
            v.Route.Clear();
        }
        Assign();Emit(new("onBuildingRemoved",b,Reason:reason??(destroyed?"destroyed":"demolished")));Log(destroyed?L.F("{0} was destroyed!",L.Name(b.Plan)):L.F("{0} removed.",L.Name(b.Plan)));
    }
    public void Assign()
    {
        var roads=Roads();var available=Citizens.OrderBy(v=>v.Profession=="citizen"?1:0).ThenBy(v=>v.Id).ToList();var jobs=new Dictionary<int,int>();
        var workplaces=Buildings.Where(b=>b.Enabled&&Connected(b,roads)&&b.Plan.Jobs>0).OrderByDescending(b=>b.Priority).ThenBy(b=>b.Id).ToList();
        // One worker per workplace before filling extra slots prevents new professions from starving.
        for(int slot=0;slot<workplaces.Select(b=>b.Plan.Jobs).DefaultIfEmpty().Max();slot++)foreach(var b in workplaces)
            if(slot<b.Plan.Jobs)
            {
                var v=available.FirstOrDefault(v=>v.Profession!="citizen"&&Qualified(v,b))??available.FirstOrDefault(v=>v.Profession=="citizen");
                if(v!=null){jobs[v.Id]=b.Id;available.Remove(v);}
            }
        foreach(var v in Citizens){CitizenNames.Ensure(v);int work=jobs.GetValueOrDefault(v.Id,-1);if(v.Work!=work){v.Route.Clear();v.WorkTime=0;}v.Work=work;}
    }
    static bool Qualified(Citizen v,Building b)=>Catalog.Data.Professions[v.Profession].Jobs.Contains(b.Plan.Job)||b.Plan.Professions.Contains(v.Profession);
    public float Efficiency(Citizen v)=>Catalog.Data.Professions[v.Profession].Efficiency;
    public bool Train(Citizen v,string profession)
    {
        if(!Catalog.Data.Professions.TryGetValue(profession,out var plan))return false;
        if(v.Profession==profession)return false;
        if(!Spend(plan.TrainingCost)){Message=L.T("Not enough supplies for training.");return false;}
        v.Profession=profession;v.WorkTime=0;Assign();Emit(new("onVillagerProfessionChanged",v));Log(L.F("{0} trained as {1}.",v.FullName,L.Name(plan)));return true;
    }
    public bool Give(Citizen v,string resource,float amount)
    {
        ReconcileStock();
        if(Currency(resource)||!Citizens.Contains(v)||!Stock.ContainsKey(resource)||!float.IsFinite(amount)||amount<=0||amount>32||v.Inventory.Values.Sum()+amount>32||Stock[resource]<amount)return false;
        var source=FindSupply(v,resource);
        if(source==null||Vector2.Distance(v.Position,new(source.X,source.Z))>.25f||Stored(source,resource)<amount)return false;
        source.Storage[resource]-=amount;Stock[resource]-=amount;v.Inventory[resource]=v.Inventory.GetValueOrDefault(resource)+amount;return true;
    }
    public float ArmorReduction=>Math.Min(.75f,Catalog.Data.Upgrades.Where(u=>u.Kind=="Armor"&&u.Level<=ArmorLevel).Sum(u=>u.Effect)+(EnchantedArmor?Catalog.Data.Economy.EnchantmentProtection:0));
    public float ToolEfficiency=>1+Catalog.Data.Upgrades.Where(u=>u.Kind=="Tools"&&u.Level<=ToolLevel).Sum(u=>u.Effect);
    public bool Upgrade(string id)
    {
        var upgrade=Catalog.Data.Upgrades.FirstOrDefault(u=>u.Id==id);if(upgrade==null)return false;
        if(upgrade.Level!=(upgrade.Kind=="Armor"?ArmorLevel:ToolLevel)+1){Message=L.T("Unlock equipment upgrades in order.");return false;}
        if(!Buildings.Any(b=>b.Plan.UpgradeStation&&b.Enabled&&Connected(b,Roads())&&Citizens.Any(v=>v.Work==b.Id))){Message=L.T("A connected, staffed forge is required.");return false;}
        if(!Spend(upgrade.Cost)){Message=L.T("Not enough materials for this upgrade.");return false;}
        if(upgrade.Kind=="Armor")ArmorLevel++;else ToolLevel++;
        Log(L.F("{0} equipped throughout the village.",L.Name(upgrade)));return true;
    }
    public bool Spend(Dictionary<string,float> cost,float multiplier=1)
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
        var scripts=Scripting;scripts.Flush();ReconcileStock();
        Statistics.SimulatedSeconds+=dt;DayTime+=dt;SettleAtDawn();UpdateSunlight(dt);
        if(DayPhase>=18f/24&&LastNight!=Day){LastNight=Day;Emit(new("onNightStarted"));if(Rules.NightMonsters)SpawnNight();}
        Shots.ForEach(s=>s.Life-=dt);Shots.RemoveAll(s=>s.Life<=0);
        var patrolRoads=IsNight?Roads().OrderBy(p=>p.Item1).ThenBy(p=>p.Item2).ToArray():[];
        foreach(var v in Citizens.ToArray())
        {
            if(v.Effects.ContainsKey("Burning"))v.Health-=dt*.5f;
            if(v.Effects.ContainsKey("Poison"))v.Health=Math.Max(1,v.Health-dt*.25f);
            foreach(string effect in v.Effects.Keys.ToArray()){v.Effects[effect]-=dt;if(v.Effects[effect]<=0)v.Effects.Remove(effect);}
            if(scripts.UpdateVillager(v,dt)){ReleaseTorchDuty(v);continue;}
            var work=Buildings.FirstOrDefault(b=>b.Id==v.Work);
            if(Enemies.Count>0&&work!=null&&work.Plan.Defender!=""&&!work.Plan.CreatesGuardian)
            {ReleaseTorchDuty(v);Fight(v,work.Plan.Defender,dt);v.Task="Defending village";continue;}
            if(UpdateTorchDuty(v,dt))continue;
            if(UpdatePersonalSupplies(v,dt))continue;
            if(work?.Plan.Job=="Delivery"&&(!IsNight||v.Cargo.Count>0)){UpdateDelivery(v,work,dt);continue;}
            if(IsNight&&work!=null&&work.Plan.Defender!=""&&!work.Plan.CreatesGuardian)
            {
                // Patrol connected roads outside the workplace instead of retiring indoors.
                var destination=patrolRoads.Length>0?patrolRoads[((int)(DayTime/12)+v.Id)%patrolRoads.Length]:work.Entrance;
                var route=Route(v.Position,new(destination.Item1,destination.Item2),false);
                if(route.Count>0)Move(v,Toward(v.Position,route[0],dt*Catalog.Data.Units[work.Plan.Defender].Speed));
                v.Route.Clear();v.Task="Night patrol";v.HasWorked=true;continue;
            }
            if(work!=null&&work.Plan.HarvestSource!=""&&!IsNight&&(DayPhase<17f/24||!v.HasWorked))
            {UpdateForester(v,work,dt);continue;}
            var home=Buildings.FirstOrDefault(b=>b.Id==v.Home);
            var target=!IsNight&&(DayPhase<17f/24||!v.HasWorked)?work??home:home;
            v.Task=target==work&&work!=null?"Going to "+work.Plan.Job:"Going home";
            if(target!=null)
            {
                if(v.RouteTarget!=target.Id){v.RouteTarget=target.Id;v.Route.Clear();}
                var dest=new Vector2(target.X,target.Z);
                if(Vector2.Distance(v.Position,dest)>.025f)
                {
                    WalkTo(v,target,dt,true,.025f);
                }
                else if(target==work)
                {
                    v.Task=(v.Profession=="citizen"?"Assisting: ":"")+work!.Plan.Job;v.WorkTime+=dt*Efficiency(v)*ToolEfficiency;
                    work.Attendance+=dt;work.WorkEffort+=dt*Efficiency(v)*ToolEfficiency;
                    if(v.WorkTime>=Rules.ProductionCycle){v.WorkTime-=Rules.ProductionCycle;work.Worked|=Produce(work,v);v.HasWorked=true;}
                }
                else v.Task=v.Profession=="citizen"&&v.Work<0?"Helping families grow":"Resting";
            }
            else v.Task="Homeless";
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
        var start=((int)MathF.Round(from.X),(int)MathF.Round(from.Y));var goal=((int)MathF.Round(to.X),(int)MathF.Round(to.Y));
        if(!Walkable(start.Item1,start.Item2)||!Walkable(goal.Item1,goal.Item2))return [];
        var roads=roadsOnly?Roads():null;var blocked=Buildings.Where(b=>b.Plan.BlocksRaiders).Select(b=>b.Cell).ToHashSet();
        var prev=new Dictionary<(int,int),(int,int)>{{start,start}};var costs=new Dictionary<(int,int),int>{{start,0}};
        var q=new PriorityQueue<(int,int),int>();q.Enqueue(start,0);int visited=0;
        bool DoorEdge((int,int) a,(int,int) b)
        {
            var building=At(a.Item1,a.Item2);return building==null||building.Plan.Path||building.Entrance==b;
        }
        while(q.TryDequeue(out var p,out _)&&p!=goal&&visited++<Width*Height)foreach(var n in Neighbors(p))
        {
            bool pass=CanStep(p,n)&&(roadsOnly?((roads!.Contains(n)||n==goal)&&DoorEdge(p,n)&&DoorEdge(n,p)):Land(n.Item1,n.Item2)&&(!blocked.Contains(n)||n==goal));
            int cost=costs[p]+1;if(pass&&(!costs.TryGetValue(n,out int old)||cost<old))
            {prev[n]=p;costs[n]=cost;int distance=Math.Abs(n.Item1-goal.Item1)+Math.Abs(n.Item2-goal.Item2);q.Enqueue(n,(cost+distance)*(Width+Height+1)+distance);}
        }
        if(!prev.ContainsKey(goal))return [];
        var result=new List<Vector2>();for(var p=goal;p!=start;p=prev[p])result.Add(new(p.Item1,p.Item2));result.Reverse();return result;
    }
    bool Produce(Building b,Citizen worker)
    {
        if(b.Plan.HarvestSource!=""||b.Plan.Job=="Delivery")return false;
        if(b.Plan.CreatesGuardian){Stock["Goods"]+=Catalog.Data.Economy.GoodsPerWorkCycle;return true;}
        var profession=Catalog.Data.Professions[worker.Profession];
        float output=b.Plan.Output.Where(e=>!Currency(e.Key)).Sum(e=>e.Value*profession.ResourceBonus.GetValueOrDefault(e.Key,1));
        if(output>StorageFree(b)+b.Plan.Input.Where(e=>!Currency(e.Key)).Sum(e=>e.Value))return false;
        if(!ConsumeLocal(b,b.Plan.Input)){Message=L.F("{0} needs {1}",L.Name(b.Plan),string.Join(", ",b.Plan.Input.Keys.Select(L.Resource)));return false;}
        Scripting.BuildingWork(b,worker);
        foreach(var e in b.Plan.Output)RecordProduction(e.Key,Receive(b,e.Key,e.Value*profession.ResourceBonus.GetValueOrDefault(e.Key,1)));
        Stock["Goods"]+=Catalog.Data.Economy.GoodsPerWorkCycle;return true;
    }
    public void Tick()
    {
        if(GameOver)return;
        var scripts=Scripting;ReconcileStock();
        Day++;foreach(var tree in Buildings.Where(b=>b.Plan.Harvest.Count>0&&b.GrowthDays>0))tree.GrowthDays--;
        Emit(new("onDayStarted"));
        foreach(var b in Buildings.Where(b=>b.Enabled).ToArray())Emit(new("onBuildingDay",b));
        Assign();
        bool fed=Citizens.All(v=>v.Inventory.GetValueOrDefault("Food")>=1);
        foreach(var v in Citizens)
        {
            float ration=Math.Min(1,v.Inventory.GetValueOrDefault("Food"));
            if(ration>0)v.Inventory["Food"]-=ration;

        }
        bool watered=Water>=Citizens.Count;var roads=Roads();
        float target=30+(fed?30:0)+(watered?25:0)+Math.Min(15,Buildings.Where(b=>b.Enabled&&Connected(b,roads)).Sum(b=>b.Plan.Joy))-Tax*4;
        Happiness=Math.Clamp(Happiness+(target-Happiness)*.25f,0,100);
        Stock["Goods"]+=Citizens.Count*Tax*Happiness/100;
        if(fed&&watered&&Happiness>=55)Growth+=1+Citizens.Count(v=>v.Profession=="citizen"&&v.Work<0)*Catalog.Data.Game.CitizenGrowthSupport;
        else Growth=Math.Max(0,Growth-.5f);
        Growth=Math.Min(Growth,Catalog.Data.Game.MigrationThreshold*2);
        if(fed&&watered&&Happiness>=55&&Growth>=Catalog.Data.Game.MigrationThreshold&&Citizens.Count<Rules.MaxPopulation)
        {
            var b=Buildings.FirstOrDefault(b=>b.Plan.Homes>Citizens.Count(v=>v.Home==b.Id)&&Connected(b,roads));
            if(b!=null){string profession=Catalog.Data.Game.ImmigrationProfessions[NextId%Catalog.Data.Game.ImmigrationProfessions.Length];AddVillager(profession,b.Id);Growth-=Catalog.Data.Game.MigrationThreshold;Log(L.F("A new {0} arrived.",L.Name(Catalog.Data.Professions[profession])));}
        }
        foreach(var b in Buildings.Where(b=>b.Worked&&b.Plan.Animal!="").ToArray())
        {
            var herd=Animals.Where(a=>a.Home==b.Id).ToList();int adults=herd.Count(a=>a.Age>=3);
            float efficiency=b.Attendance>0?b.WorkEffort/b.Attendance:1;
            if(Day%3==0)b.ServiceCredit=Math.Min(2,b.ServiceCredit+efficiency);
            if(adults>=2&&herd.Count<Rules.AnimalsPerPen&&Day%3==0&&b.ServiceCredit>=.999f&&ConsumeLocal(b,new(){["Food"]=2}))
            {b.ServiceCredit=Math.Max(0,b.ServiceCredit-1);Animals.Add(new(){Id=NextId++,Type=b.Plan.Animal,Home=b.Id,X=b.X,Z=b.Z,Health=20,Age=0});Log(L.F("A baby {0} was born.",L.Entity(b.Plan.Animal)));}
            if(adults>=2){if(b.Plan.Animal=="sheep")RecordProduction("Wool",Receive(b,"Wool",adults*efficiency));if(b.Plan.Animal=="chicken")RecordProduction("Eggs",Receive(b,"Eggs",adults*efficiency));if(b.Plan.Animal=="pig")RecordProduction("Food",Receive(b,"Food",adults*efficiency));}
        }
        foreach(var a in Animals)a.Age++;
        foreach(var b in Buildings.Where(b=>b.Worked&&b.Plan.CreatesGuardian&&b.Plan.Defender!=""))
        {
            if(Day%3==0)b.ServiceCredit=Math.Min(2,b.ServiceCredit+(b.Attendance>0?b.WorkEffort/b.Attendance:1));
            if(Defenders.Count(d=>d.Home==b.Id)<Rules.GolemsPerWorkshop&&Day%3==0&&b.ServiceCredit>=.999f&&ConsumeLocal(b,b.Plan.Input))
            {b.ServiceCredit=Math.Max(0,b.ServiceCredit-1);var type=b.Plan.Defender;Defenders.Add(new(){Id=NextId++,Type=type,Home=b.Id,X=b.X,Z=b.Z,Health=Catalog.Data.Units[type].Health});Log(L.F("{0} awakened.",L.Entity(type)));}
        }
        foreach(var b in Buildings){b.Worked=false;b.WorkEffort=0;b.Attendance=0;}
        foreach(var v in Citizens){v.HasWorked=false;if(fed&&watered)v.Health=Math.Min(50,v.Health+3);}
        if(Rules.RaidsEnabled&&Day%Rules.RaidInterval==0)SpawnWave();
        else if(!fed)Message=L.T("Food shortage! Assign farmers or trade for supplies.");
        else if(!watered)Message=L.T("Water shortage! Build more connected wells.");
        scripts.Flush();
    }
    void UpdateAnimals(float dt)
    {
        foreach(var a in Animals){var b=Buildings.FirstOrDefault(b=>b.Id==a.Home);if(b==null)continue;
            var dest=new Vector2(b.X+MathF.Sin(DayTime*.5f+a.Age+a.Home)*.28f,b.Z+MathF.Cos(DayTime*.4f+a.Home)*.25f);
            Move(a,Toward(a.Position,dest,dt*.2f));}
    }
    public void SpawnNight()
    {
        string[] types=["zombie","skeleton","creeper"];
        if(Day<=PeaceDays)return;
        var sites=Buildings.Where(b=>!b.Plan.Path&&!b.Plan.Natural&&!b.Plan.Indestructible).ToArray();if(sites.Length==0)return;
        int count=Math.Min(Rules.NightCount,Math.Max(0,Rules.MaxRaiders-Enemies.Count));
        for(int i=0;i<count;i++)
        {
            var b=sites[(Day+i*7)%sites.Length];string type=types[(Day+i)%types.Length];
            SpawnMonster(type,Math.Clamp(b.X+(i%2==0?-2:2),0,Width-1),Math.Clamp(b.Z+2,0,Height-1),night:true);
        }
        if(count>0)Log(L.F("Night falls: {0} monsters approach. Sunlight burns unprotected undead.",count));
    }
    public void SpawnWave()
    {
        Waves++;int count=Math.Max(0,Math.Min(Rules.MaxRaiders-Enemies.Count,Rules.WaveBase+Waves*Rules.WaveGrowth));string[] types=["zombie","skeleton","creeper","piglin","piglin_brute","zombified_piglin","blaze"];
        for(int i=0;i<count;i++){string type=i==0?"nether_warlord":types[(i-1)%types.Length];SpawnMonster(type,Math.Max(0,AnchorX-18),Math.Clamp(AnchorZ-12+i*2%26,0,Height-1),bossWave:true);}
        Log(L.F("RAID {0}! {1} hostiles led by a Nether warlord are attacking.",Waves,count));
    }
    void Fight(Citizen v,string type,float dt)
    {
        var unit=Catalog.Data.Units[type];v.Cooldown-=dt;
        var enemy=Enemies.Where(e=>e.Health>0).OrderBy(e=>Vector2.DistanceSquared(v.Position,e.Position)).FirstOrDefault();if(enemy==null)return;
        if(Vector2.Distance(v.Position,enemy.Position)>unit.Range*.85f)
        {
            var route=Route(v.Position,enemy.Position,false);if(route.Count>0)Move(v,Toward(v.Position,route[0],dt*unit.Speed*TravelSpeed(v.Position,type)));v.Route.Clear();
        }
        if(Vector2.Distance(v.Position,enemy.Position)<=unit.Range&&v.Cooldown<=0&&Sight(v.Position,enemy.Position))
        {DamageMonster(enemy,unit.Damage*Efficiency(v)*(v.Effects.ContainsKey("Weakness")?.7f:1)*(EnchantedArmor?1+Catalog.Data.Economy.EnchantmentDamage:1),v);v.Cooldown=unit.Cooldown;Shots.Add(new(v.Position,enemy.Position,type));}
    }
    void Combat(float dt)
    {
        foreach(var d in Defenders)
        {
            var unit=Catalog.Data.Units[d.Type];d.Cooldown-=dt;
            var enemy=Enemies.Where(e=>e.Health>0).OrderBy(e=>Vector2.DistanceSquared(d.Position,e.Position)).FirstOrDefault();
            var home=Buildings.FirstOrDefault(b=>b.Id==d.Home);
            Vector2 dest=enemy?.Position??new Vector2(Math.Clamp((home?.X??AnchorX)+MathF.Sin(DayTime*.2f)*2,0,Width-1),home?.Z??AnchorZ);
            if(enemy!=null&&Vector2.Distance(d.Position,dest)<=unit.Range&&Sight(d.Position,dest))
            {if(d.Cooldown<=0){DamageMonster(enemy,unit.Damage,d);d.Cooldown=unit.Cooldown;Shots.Add(new(d.Position,dest,d.Type));}}
            else MoveCreature(d,dest,unit.Speed*dt);
        }
        foreach(var e in Enemies.Where(e=>e.Health>0).ToArray())
        {
            var unit=Catalog.Data.Units[e.Type];e.Cooldown-=dt;
            var defender=Defenders.Where(d=>d.Health>0).OrderBy(d=>Vector2.DistanceSquared(e.Position,d.Position)).FirstOrDefault();
            var citizen=Citizens.OrderBy(v=>Vector2.DistanceSquared(e.Position,v.Position)).FirstOrDefault();
            var building=Buildings.Where(b=>!b.Plan.Natural&&!b.Plan.Indestructible&&(!b.Plan.Path||b.Plan.BlocksRaiders)).OrderBy(b=>Vector2.DistanceSquared(e.Position,new(b.X,b.Z))).FirstOrDefault();
            float dd=defender==null||!Sight(e.Position,defender.Position)?float.MaxValue:Vector2.Distance(e.Position,defender.Position);
            float cd=citizen==null||!Sight(e.Position,citizen.Position)?float.MaxValue:Vector2.Distance(e.Position,citizen.Position);
            Vector2 target=dd<6?defender!.Position:cd<12?citizen!.Position:building!=null?new(building.X,building.Z):new(AnchorX,AnchorZ);
            float distance=Vector2.Distance(e.Position,target);
            bool acted=false;
            void Act(string action)
            {
            if(acted||e.Health<=0)return;acted=true;
            if(action=="explode"&&distance<=2.5f)
            {
                foreach(var b in Buildings.Where(b=>Vector2.Distance(e.Position,new(b.X,b.Z))<=2))DamageBuilding(b,unit.Damage,e);
                foreach(var v in Citizens.Where(v=>Vector2.Distance(e.Position,v.Position)<=2))DamageVillager(v,unit.Damage*(1-ArmorReduction),e);
                foreach(var d in Defenders.Where(d=>Vector2.Distance(e.Position,d.Position)<=2))d.Health-=unit.Damage*(1-ArmorReduction);
                Shots.Add(new(e.Position,target,"blaze"));DamageMonster(e,e.Health,e);
            }
            else if(action=="attack"&&distance<=unit.Range&&Sight(e.Position,target)&&e.Cooldown<=0)
            {
                e.Cooldown=unit.Cooldown;Shots.Add(new(e.Position,target,e.Type));
                if(dd<6)defender!.Health-=unit.Damage*(1-ArmorReduction);else if(cd<12)DamageVillager(citizen!,unit.Damage*(1-ArmorReduction),e);else if(building!=null)DamageBuilding(building,unit.Damage,e);
            }
            else if(action=="move")MoveCreature(e,target,unit.Speed*dt);
            }
            void AttackTarget(int id,bool person)
            {
                if(acted||e.Health<=0||e.Cooldown>0)return;
                var v=person?Citizens.FirstOrDefault(v=>v.Id==id&&v.Health>0):null;
                var b=person?null:Buildings.FirstOrDefault(b=>b.Id==id&&b.Health>0);
                if(v==null&&b==null)return;
                Vector2 destination=v?.Position??new Vector2(b!.X,b.Z);
                if(Vector2.Distance(e.Position,destination)>unit.Range||!Sight(e.Position,destination))return;
                acted=true;e.Cooldown=unit.Cooldown;Shots.Add(new(e.Position,destination,e.Type));
                if(v!=null)DamageVillager(v,unit.Damage*(1-ArmorReduction),e);else DamageBuilding(b!,unit.Damage,e);
            }
            void Steer(Vector2 destination){if(!acted&&e.Health>0){acted=true;MoveCreature(e,destination,unit.Speed*dt);}}
            var context=new MobScriptContext(this,e,dt,distance,Sight(e.Position,target),dd<6?null:cd<12?citizen:null,dd<6||cd<12?null:building,Act,Steer,AttackTarget);
            if(!Scripting.UpdateMonster(e,context,dt))
            {
                // Missing/faulted overrides use the basic combat loop instead of freezing the creature.
                Act(distance<=unit.Range&&Sight(e.Position,target)?"attack":"move");
            }

        }
        ResolveDeaths();
    }
    public void ResolveDeaths()
    {
        int dead=Enemies.Count(e=>e.Health<=0);if(dead>0){int burned=Enemies.Count(e=>e.Health<=0&&e.SunKilled);Statistics.SunDeaths+=burned;Kills+=dead;Stock["Goods"]+=(dead-burned)*Catalog.Data.Economy.GoodsPerKill;foreach(var m in Enemies.Where(e=>e.Health<=0).ToArray())RemoveMonster(m,"killed");if(Enemies.Count==0)Log(L.T("Hostiles defeated! Repair and prepare for the next attack."));}
        Defenders.RemoveAll(d=>d.Health<=0);
        var fallen=Citizens.Where(v=>v.Health<=0).ToArray();int lost=fallen.Length;foreach(var v in fallen)RemoveVillager(v,"killed");if(lost>0){Log(L.F("{0} villagers fell during the raid.",lost));Assign();}
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
        var route=Route(v.Position,target,false);
        if(route.Count>0)Move(v,Toward(v.Position,route[0],dt*.85f*TravelSpeed(v.Position,v.Profession)));
        else if(Vector2.Distance(v.Position,target)<.7f)Move(v,Toward(v.Position,target,dt*.85f));
        v.Task="Scripted behavior";
    }
    public bool Trade(string resource,bool buy)
    {
        ReconcileStock();
        if(!Buildings.Any(b=>b.Enabled&&b.Plan.Job=="Trading"&&Citizens.Any(v=>v.Work==b.Id))){Message=L.T("Connect and staff a trading hall.");return false;}
        var definition=Catalog.Data.ResourceDefinitions.FirstOrDefault(r=>r.Id==resource);if(definition==null||Currency(resource))return false;float price=definition.BuyPrice;
        var market=Buildings.First(b=>b.Enabled&&b.Plan.Job=="Trading"&&Citizens.Any(v=>v.Work==b.Id));
        if(buy&&Stock["Emeralds"]>=price&&StorageFree(market)>=20){Stock["Emeralds"]-=price;Receive(market,resource,20);return true;}
        if(!buy)market.RequestedResource=resource;
        if(!buy&&Stored(market,resource)>=20){market.Storage[resource]-=20;Stock[resource]-=20;Stock["Emeralds"]+=definition.SellPrice;return true;}
        if(!buy){Message=L.T("Delivery required: bring 20 items to the trading hall.");return false;}
        Message=L.T("Insufficient supplies for trade.");return false;
    }
    public void Repair(Building b){float amount=Math.Min(25,b.Plan.Health-b.Health);if(amount>0&&Spend(new(){["Stone"]=2,["Wood"]=2}))b.Health+=amount;}
    public void Save(string path)
    {
        ReconcileStock();BinarySave.Write(path,this);
    }
    public static City Load(string path)
    {
        City c;
        bool binary;using(var probe=File.OpenRead(path)){Span<byte> magic=stackalloc byte[4];binary=probe.Read(magic)==4&&magic.SequenceEqual("VCTY"u8);}
        if(!binary&&Path.GetExtension(path).Equals(".json",StringComparison.OrdinalIgnoreCase))
        {
            if(new FileInfo(path).Length>BinarySave.MaxBytes)throw new InvalidDataException("Save is too large.");
            c=JsonSerializer.Deserialize<City>(File.ReadAllText(path),new JsonSerializerOptions{IncludeFields=true})??throw new InvalidDataException("Empty save");
            if(c.Version!=2)throw new InvalidDataException("Unsupported legacy save.");c.Version=3;c.Width=32;c.Height=32;c.PeaceDays=0;c.Statistics=new(){RecordedSinceDay=c.Day,PeakPopulation=c.Citizens.Count};
            if(c.Stock==null)throw new InvalidDataException("Invalid legacy resource data.");
            foreach(string id in Catalog.Resources)c.Stock.TryAdd(id,0);
        }
        else c=BinarySave.Read(path);
        bool importedStorage=!c.StorageInitialized;Validate(c);
        if(importedStorage)c.Log(L.T("Supplies moved into warehouse storage. Train deliverymen to supply workplaces."));
        return c;
    }
    public static void Validate(City c)
    {
        bool finite(float n)=>float.IsFinite(n);
        if(c.Version!=3||c.Width is <32 or >512||c.Height is <32 or >512)throw new InvalidDataException("Unsupported save or map size.");
        c.Terrain??=TerrainData.Flat(c.Width,c.Height);
        if(c.Terrain.Elevation.Length==0)c.Terrain=TerrainData.Flat(c.Width,c.Height);
        c.Terrain.Validate(c.Width,c.Height);
        if(c.PeaceDays is <0 or >100||c.TutorialStep is <0 or >100||c.TutorialTreesHarvested<0)throw new InvalidDataException("Invalid world progress");
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
            Catalog.Resources.Any(r=>!c.Stock.ContainsKey(r)||!finite(c.Stock[r]))||c.Citizens.Select(v=>v.Id).Distinct().Count()!=c.Citizens.Count||c.Citizens.Any(v=>c.Buildings.Any(b=>b.Id==v.Id))||c.ArmorLevel<0||c.ToolLevel<0||c.ArmorLevel>Catalog.Data.Upgrades.Where(u=>u.Kind=="Armor").Select(u=>u.Level).DefaultIfEmpty().Max()||c.ToolLevel>Catalog.Data.Upgrades.Where(u=>u.Kind=="Tools").Select(u=>u.Level).DefaultIfEmpty().Max()||!finite(c.Balance)||!finite(c.Growth)||c.LastNight<0||c.LastNight>c.Day||c.Enemies.Any(e=>!finite(e.BrainTime)||e.BrainTime<0)||!finite(c.DayTime)||c.DayTime<0||c.DayTime>=c.Rules.DayLength||!finite(c.Happiness)||!finite(c.Tax)||c.Day<1)
            throw new InvalidDataException("Invalid village data");
        c.EnsureEntityIds();
        var ids=c.Buildings.Select(b=>b.Id).Concat(c.Citizens.Select(v=>v.Id)).Concat(c.Animals.Concat(c.Defenders).Concat(c.Enemies).Select(m=>m.Id)).ToArray();
        if(ids.Distinct().Count()!=ids.Length||ids.Any(id=>id<=0||id>1000000000))throw new InvalidDataException("Invalid entity IDs");
        foreach(var data in c.Buildings.Select(b=>(b.Tags,b.Behavior)).Concat(c.Citizens.Select(v=>(v.Tags,v.Behavior))).Concat(c.Animals.Concat(c.Defenders).Concat(c.Enemies).Select(m=>(m.Tags,m.Behavior))))
        {EntityTags.Validate(data.Tags);if(data.Behavior==null||data.Behavior.Length>128)throw new InvalidDataException("Invalid behavior reference");}
        c.NextId=Math.Max(c.NextId,c.Buildings.Select(b=>b.Id).Concat(c.Citizens.Select(v=>v.Id)).DefaultIfEmpty().Max()+1);
        c.ValidateLogistics();c.ValidateOutcome();
        c.Tax=Math.Clamp(c.Tax,0,6);foreach(var v in c.Citizens)v.Route=[];c.InvalidateTopology();c.Assign();
    }
}
