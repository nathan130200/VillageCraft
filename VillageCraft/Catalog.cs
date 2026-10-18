using Tomlyn;
namespace VillageCraft;

public class Part
{
    public string Shape {get;set;}="cube";
    public float Pitch {get;set;}
    public string TextureMode {get;set;}="repeat";
    public float[] Position { get; set; } = [0,0,0];
    public float[] Size { get; set; } = [1,1,1];
    public string Color { get; set; } = "#FFFFFF";
    public string Texture {get;set;}=string.Empty;
    public float[]? UvOrigin {get;set;}
    public float[]? UvSize {get;set;}
}
public class Plan
{
    public int StorageCapacity {get;set;}=200;
    public bool SheltersFromSun {get;set;}
    public bool Indoor {get;set;}
    public bool Warehouse {get;set;}
    public bool Indestructible {get;set;}
    public bool Natural {get;set;}
    public Dictionary<ResourceType,float> Harvest {get;set;}=[];
    public int RegrowDays {get;set;}=3;
    public string HarvestSource {get;set;}=string.Empty;
    public int HarvestRadius {get;set;}=7;
    public string Script {get;set;}=string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string WorkerModel { get; set; } = "villager";
    public int Jobs { get; set; }
    public int Homes { get; set; }
    public int Water { get; set; }
    public int Joy { get; set; }
    public int Upkeep { get; set; }
    public float Health { get; set; } = 120;
    public bool Path { get; set; }
    public bool NeedsPath { get; set; } = true;
    public bool BlocksRaiders { get; set; }
    public Dictionary<ResourceType,float> Cost { get; set; } = [];
    public Dictionary<ResourceType,float> Input { get; set; } = [];
    public Dictionary<ResourceType,float> Output { get; set; } = [];
    public string Animal { get; set; } = string.Empty;
    public string Defender { get; set; } = string.Empty;
    public bool CreatesGuardian { get; set; }
    public float MoveSpeed {get;set;}=1;
    public Dictionary<string,float> JobSpeed {get;set;}=[];
    public string[] Professions {get;set;}=[];
    public bool UpgradeStation {get;set;}
}
public class UnitPlan
{
    public bool BurnsInSun {get;set;}
    public string Behavior {get;set;}="melee";
    public string Model { get; set; } = string.Empty;
    public float Health { get; set; } = 50;
    public float Damage { get; set; } = 8;
    public float Range { get; set; } = 1;
    public float Speed { get; set; } = 1;
    public float Cooldown { get; set; } = 1;
}
public class Catalog
{
    public List<Plan> Prefabs { get; set; } = [];
    public Dictionary<string,List<Part>> Models { get; set; } = [];
    public Dictionary<string,UnitPlan> Units { get; set; } = [];
    public Dictionary<string,ProfessionPlan> Professions {get;set;}=[];
    public List<ResourcePlan> ResourceDefinitions {get;set;}=[];
    public List<UpgradePlan> Upgrades {get;set;}=[];
    public GameDefaults Game {get;set;}=new();
    public AudioSettings Audio {get;set;}=new();
    public RenderingSettings Rendering {get;set;}=new();
    public SunlightSettings Sunlight {get;set;}=new();
    public EconomySettings Economy {get;set;}=new();
    public TerrainSettings Terrain {get;set;}=new();
    public TutorialFile Tutorial {get;set;}=new();
    public Dictionary<ResourceType,string> ResourceIcons {get;set;}=[];
    Dictionary<string,Plan> byId=[];
    public static Catalog Data { get; private set; } = null!;
    public static ResourceType[] Resources {get;private set;}=[];
    public static void Load(string? path=null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory,"Content");
        var data=new Catalog {Game=TomlFile.Read<GameDefaults>(Path.Combine(path,"game.toml"))};
        data.Tutorial=TomlFile.Read<TutorialFile>(Path.Combine(path,"tutorial.toml"));
        data.Terrain=TomlFile.Read<TerrainSettings>(Path.Combine(path,"terrain.toml"));
        data.Sunlight=TomlFile.Read<SunlightSettings>(Path.Combine(path,"sunlight.toml"));
        data.Economy=TomlFile.Read<EconomySettings>(Path.Combine(path,"economy.toml"));
        data.Audio=TomlFile.Read<AudioSettings>(Path.Combine(path,"audio.toml"));data.Audio.Validate();
        data.Rendering=TomlFile.Read<RenderingSettings>(Path.Combine(path,"rendering.toml"));
        data.ResourceDefinitions=TomlFile.Read<ResourceFile>(Path.Combine(path,"resources.toml")).Resources;
        foreach(var file in Directory.GetFiles(Path.Combine(path,"Prefabs"),"*.toml").Order())data.Prefabs.Add(TomlFile.Read<Plan>(file));
        foreach(var file in Directory.GetFiles(Path.Combine(path,"Models"),"*.toml").Order())
        {var model=TomlFile.Read<ModelFile>(file);if(!data.Models.TryAdd(model.Id,model.Parts))throw new InvalidDataException("Duplicate model: "+model.Id);}
        foreach(var file in Directory.GetFiles(Path.Combine(path,"Units"),"*.toml").Order())
        {var unit=TomlFile.Read<UnitFile>(file);if(!data.Units.TryAdd(unit.Id,unit))throw new InvalidDataException("Duplicate unit: "+unit.Id);}
        foreach(var file in Directory.GetFiles(Path.Combine(path,"Professions"),"*.toml").Order())
        {var profession=TomlFile.Read<ProfessionPlan>(file);if(!data.Professions.TryAdd(profession.Id,profession))throw new InvalidDataException("Duplicate profession: "+profession.Id);}
        data.Upgrades=TomlFile.Read<UpgradeFile>(Path.Combine(path,"upgrades.toml")).Upgrades;
        ResourceType[] resourceIds=data.ResourceDefinitions.Select(r=>r.Id).ToArray();
        void Require(bool ok,string reason){if(!ok)throw new InvalidDataException("Prefab catalog: "+reason);}
        Require(data.Prefabs.Count>0&&data.Prefabs.Count<=256,"expected 1..256 prefabs");
        Require(data.Game.FormatVersion==1,"unsupported content version");
        Require(float.IsFinite(data.Rendering.AlphaCutoff)&&data.Rendering.AlphaCutoff>0&&data.Rendering.AlphaCutoff<1&&!string.IsNullOrWhiteSpace(data.Rendering.CutoutFragment),"invalid cutout rendering settings");
        Require(System.Text.RegularExpressions.Regex.IsMatch(data.Rendering.WaterTint,@"^#[0-9A-Fa-f]{6}$")&&!string.IsNullOrWhiteSpace(data.Rendering.WaterFragment),"invalid water rendering settings");
        Require(data.Prefabs.Select(p=>p.Id).Distinct().Count()==data.Prefabs.Count,"duplicate prefab ids");
        foreach(var model in data.Models.Values)foreach(var p in model)
        {
            Require(p.TextureMode is "repeat" or "stretch","invalid model texture mode");
            Require(p.Shape is "cube" or "cross","invalid model shape");
            Require(float.IsFinite(p.Pitch)&&Math.Abs(p.Pitch)<=360,"invalid part pitch");
            Require(p.Position.Length==3&&p.Size.Length==3&&p.Position.All(float.IsFinite)&&p.Size.All(v=>float.IsFinite(v)&&v>0)&&p.Color.Length==7&&p.Color[0]=='#'&&int.TryParse(p.Color[1..],System.Globalization.NumberStyles.HexNumber,null,out _),"invalid model part");
            if(p.UvOrigin?.Length==0&&p.UvSize?.Length==0){p.UvOrigin=null;p.UvSize=null;}
            Require((p.UvOrigin==null&&p.UvSize==null)||(p.UvOrigin?.Length==2&&p.UvSize?.Length==3&&p.UvOrigin.All(v=>float.IsFinite(v)&&v>=0)&&p.UvSize.All(v=>float.IsFinite(v)&&v>0)),"invalid skin UV mapping");
        }
        foreach(var p in data.Prefabs)
        {
            Require(!string.IsNullOrWhiteSpace(p.Id)&&data.Models.ContainsKey(p.Model)&&data.Models.ContainsKey(p.WorkerModel),"missing id or model");
            Require(p.StorageCapacity is >=0 and <=100000&&(!p.Warehouse||p.StorageCapacity>0),"invalid storage settings");
            Require(p.Jobs>=0&&p.Homes>=0&&p.Water>=0&&p.Health>0&&float.IsFinite(p.Health)&&p.Upkeep>=0,"invalid prefab values");
            foreach(var map in new[]{p.Cost,p.Input,p.Output,p.Harvest})Require(map.All(e=>resourceIds.Contains(e.Key)&&float.IsFinite(e.Value)&&e.Value>=0),"unknown resource or negative amount");
            Require(p.Input.Where(e=>!City.Currency(e.Key)).Sum(e=>e.Value)<=p.StorageCapacity&&p.Output.Where(e=>!City.Currency(e.Key)).Sum(e=>e.Value)<=p.StorageCapacity,"storage is smaller than a production cycle");
            Require(p.Defender==string.Empty||data.Units.ContainsKey(p.Defender),"unknown defender");
            Require(p.Animal==string.Empty||data.Models.ContainsKey(p.Animal),"unknown animal model");
            Require(float.IsFinite(p.MoveSpeed)&&p.MoveSpeed>0&&p.MoveSpeed<=4&&p.JobSpeed.Values.All(v=>float.IsFinite(v)&&v>0&&v<=4),"invalid road speed");
            Require(p.Professions.All(data.Professions.ContainsKey),"unknown building profession");
        }
        foreach(var u in data.Units.Values)Require(data.Models.ContainsKey(u.Model)&&new[]{u.Health,u.Damage,u.Range,u.Speed,u.Cooldown}.All(v=>float.IsFinite(v)&&v>0),"invalid unit");
        foreach(string id in new[]{"path","cottage","farm","well","lumber","quarry","sheep_pen","pig_pen","chicken_coop"})Require(data.Prefabs.Any(p=>p.Id==id),"missing starter prefab "+id);
        foreach(string id in new[]{"zombie","piglin","blaze"})Require(data.Units.ContainsKey(id),"missing raider "+id);
        Require(resourceIds.All(r=>r!=ResourceType.None&&Enum.IsDefined(r))&&resourceIds.Distinct().Count()==resourceIds.Length&&resourceIds.Length is >0 and <=64,"duplicate or too many resources");
        foreach(ResourceType id in new[]{ResourceType.Emeralds,ResourceType.Food,ResourceType.Wood,ResourceType.Stone,ResourceType.Iron,ResourceType.Tools,ResourceType.Wool,ResourceType.Eggs})Require(resourceIds.Contains(id),"missing resource "+id);
        foreach(var p in data.Professions.Values)
        {
            Require(data.Models.ContainsKey(p.Model)&&float.IsFinite(p.Efficiency)&&p.Efficiency>0&&p.Efficiency<=2&&float.IsFinite(p.BaseHealth)&&p.BaseHealth>=1&&p.BaseHealth<=10000,"invalid profession "+p.Id);
            Require(p.TrainingCost.Concat(p.ResourceBonus).All(e=>resourceIds.Contains(e.Key)&&float.IsFinite(e.Value)&&e.Value>=0),"invalid profession recipe");
        }
        Require(data.Professions.TryGetValue("citizen",out var citizen)&&citizen.Efficiency==.5f,"citizens must have 50% efficiency");
        Require(data.Game.ImmigrationProfessions.Length>0&&data.Game.ImmigrationProfessions.All(data.Professions.ContainsKey),"invalid immigration professions");
        Require(data.Game.DefaultMapWidth is >=32 and <=512&&data.Game.DefaultMapHeight is >=32 and <=512,"map dimensions must be 32..512");
        Require(float.IsFinite(data.Game.MigrationThreshold)&&data.Game.MigrationThreshold>0&&float.IsFinite(data.Game.CitizenGrowthSupport)&&data.Game.CitizenGrowthSupport>=0,"invalid growth settings");
        foreach(var s in data.Game.Starter)Require(data.Prefabs.Any(p=>p.Id==s.Prefab)&&s.Residents.All(data.Professions.ContainsKey)&&s.Rotation is >=0 and <=3,"invalid starter placement");
        foreach(var u in data.Upgrades)Require(Enum.IsDefined(u.Kind)&&u.Level>0&&float.IsFinite(u.Effect)&&u.Effect>0&&u.Cost.All(e=>resourceIds.Contains(e.Key)&&float.IsFinite(e.Value)&&e.Value>=0),"invalid upgrade");
        Require(data.Upgrades.Select(u=>u.Id).Distinct().Count()==data.Upgrades.Count,"duplicate upgrade");
        foreach(var kind in Enum.GetValues<EquipmentKind>())Require(data.Upgrades.Where(u=>u.Kind==kind).OrderBy(u=>u.Level).Select(u=>u.Level).SequenceEqual(Enumerable.Range(1,data.Upgrades.Count(u=>u.Kind==kind))),"upgrade levels must be unique and consecutive");
        Require(data.Game.InitialStock.All(e=>resourceIds.Contains(e.Key)&&float.IsFinite(e.Value)&&e.Value>=0),"invalid starting supplies");
        Require(data.Game.Starter.Select(s=>(s.X,s.Z)).Distinct().Count()==data.Game.Starter.Count&&data.Game.Starter.All(s=>s.X is >=-16 and <16&&s.Z is >=-16 and <16&&s.Residents.Length<=data.Prefabs.First(p=>p.Id==s.Prefab).Homes),"invalid starter footprints or housing");
        Require(data.ResourceDefinitions.All(r=>r.BuyPrice>0&&r.SellPrice>=0&&float.IsFinite(r.BuyPrice)&&float.IsFinite(r.SellPrice)),"invalid trade prices");
        Require(data.Tutorial.Stages.Count>0&&data.Tutorial.Stages.All(s=>new[]{"inspect","road","trees","water","homes","defense"}.Contains(s.Goal)),"invalid tutorial goal");
        Require(data.Terrain.MaxMountainHeight is >=4 and <=16&&data.Terrain.LakeRadius is >=2 and <=6&&data.Terrain.TreeDensity is >=0 and <=.2f&&data.Terrain.GrassDensity is >=0 and <=.5f,"invalid terrain settings");
        Require(data.Prefabs.Any(p=>p.Id==data.Terrain.TreePrefab&&p.Natural),"unknown natural tree prefab");
        foreach(var p in data.Prefabs)Require(p.RegrowDays is >=1 and <=30&&p.HarvestRadius is >=1 and <=32&&(p.HarvestSource==string.Empty||data.Prefabs.Any(t=>t.Id==p.HarvestSource&&t.Harvest.Count>0)),"invalid forestry settings");
        Require(float.IsFinite(data.Sunlight.RegularHelmetChance)&&data.Sunlight.RegularHelmetChance is >=0 and <=1&&float.IsFinite(data.Sunlight.DamagePerSecond)&&data.Sunlight.DamagePerSecond is >0 and <=100,"invalid sunlight settings");
        var economy=data.Economy;
        Require(new[]{economy.GoodsPerWorkCycle,economy.GoodsPerKill,economy.EmeraldsPerGood,economy.DeliveryCapacity,economy.InputBufferCycles,economy.MilkHealing,economy.EnchantmentProtection,economy.EnchantmentDamage}.All(n=>float.IsFinite(n)&&n>0)&&economy.DeliveryCapacity<=24&&economy.EnchantmentProtection<1&&economy.EnchantmentDamage<=2&&economy.InputBufferCycles<=100,"invalid economy settings");
        Require(float.IsFinite(economy.RepairWorkSeconds)&&economy.RepairWorkSeconds is >=.1f and <=120&&float.IsFinite(economy.RepairAmount)&&economy.RepairAmount is >0 and <=100&&economy.RepairCost.Values.Sum()<=24&&economy.RepairCost.All(e=>resourceIds.Contains(e.Key)&&!City.Currency(e.Key)&&float.IsFinite(e.Value)&&e.Value>0),"invalid automatic repair settings");
        Require(economy.EnchantmentCost.All(e=>resourceIds.Contains(e.Key)&&float.IsFinite(e.Value)&&e.Value>=0),"invalid enchantment cost");
        Require(float.IsFinite(data.Rendering.NightAmbient)&&data.Rendering.NightAmbient is >=.15f and <=.6f,"night ambient must be 0.15..0.6");
        data.Game.Rules.Validate();data.byId=data.Prefabs.ToDictionary(p=>p.Id);data.ResourceIcons=data.ResourceDefinitions.ToDictionary(r=>r.Id,r=>r.Icon);
        CitizenNames.Load(Path.Combine(path,"names.toml"));
        L.Load(Path.Combine(path,"Locales"));
        Scripts.Load(Path.Combine(path,"Scripts"));
        foreach(string required in new[]{"_ai","villager","guardian","animal","building","employment","population"})Scripts.Require(required);
        foreach(var u in data.Units.Values)Scripts.Require(u.Behavior);
        foreach(var plan in data.Prefabs)if(plan.Script!=string.Empty)Scripts.Require(plan.Script);
        Resources=resourceIds;Data=data;
    }
    public static Plan Get(string id)=>Data.byId.TryGetValue(id,out var plan)?plan:throw new InvalidDataException("Unknown prefab: "+id);
}
public class ModelFile {public string Id {get;set;}=string.Empty;public List<Part> Parts {get;set;}=[];}
public class RenderingSettings {public float NightAmbient {get;set;}= .4f;public float AlphaCutoff {get;set;}=.1f;public string CutoutFragment {get;set;}=string.Empty;public string EnhancedFragment {get;set;}=string.Empty;public string LightingVertex {get;set;}=string.Empty;public string LightingFragment {get;set;}=string.Empty;public string ShadowFragment {get;set;}=string.Empty;public string WaterFragment {get;set;}=string.Empty;public string WaterTint {get;set;}="#3F76E4";}
public class UnitFile:UnitPlan {public string Id {get;set;}=string.Empty;}
public class ResourceFile {public List<ResourcePlan> Resources {get;set;}=[];}
public class UpgradeFile {public List<UpgradePlan> Upgrades {get;set;}=[];}
public class ResourcePlan
{
    public ResourceType Id {get;set;}=ResourceType.None;
    public string Icon {get;set;}=string.Empty;
    public float BuyPrice {get;set;}=30;
    public float SellPrice {get;set;}=21;
}
public class ProfessionPlan
{
    public float BaseHealth {get;set;}=50;
    public string Id {get;set;}=string.Empty;
    public string Name {get;set;}=string.Empty;
    public string Model {get;set;}="villager";
    public string[] Jobs {get;set;}=[];
    public float Efficiency {get;set;}=1;
    public Dictionary<ResourceType,float> TrainingCost {get;set;}=[];
    public Dictionary<ResourceType,float> ResourceBonus {get;set;}=[];
}
public class UpgradePlan
{
    public string Id {get;set;}=string.Empty;
    public string Name {get;set;}=string.Empty;
    public EquipmentKind Kind {get;set;}=EquipmentKind.Armor;
    public int Level {get;set;}=1;
    public float Effect {get;set;}=.1f;
    public Dictionary<ResourceType,float> Cost {get;set;}=[];
}
public class StarterPlacement
{
    public string Prefab {get;set;}="path";
    public int X {get;set;}
    public int Z {get;set;}
    public int Rotation {get;set;}
    public bool Industry {get;set;}
    public string[] Residents {get;set;}=[];
}
public class GameDefaults
{
    public int FormatVersion {get;set;}=1;
    public string[] Categories {get;set;}=[];
    public int DefaultMapWidth {get;set;}=32;
    public int DefaultMapHeight {get;set;}=32;
    public string GroundTexture {get;set;}="block/grass_block_top.png";
    public string GroundTint {get;set;}="#92B968";
    public string DirtTexture {get;set;}="block/dirt.png";
    public float MigrationThreshold {get;set;}=3;
    public float CitizenGrowthSupport {get;set;}=.2f;
    public string[] ImmigrationProfessions {get;set;}=["citizen"];
    public Dictionary<ResourceType,float> InitialStock {get;set;}=[];
    public List<StarterPlacement> Starter {get;set;}=[];
    public VillageRules Rules {get;set;}=new();
}
public static class TomlFile
{
    static readonly TomlSerializerOptions options=new(){Converters=[new ResourceTableConverter()]};
    public static string Serialize<T>(T value)=>TomlSerializer.Serialize(value,options);
    public static T Read<T>(string path)
    {
        try{return TomlSerializer.Deserialize<T>(File.ReadAllText(path),options)??throw new InvalidDataException("Empty TOML file");}
        catch(Exception e){throw new InvalidDataException($"{path}: {e.Message}",e);}
    }
}
