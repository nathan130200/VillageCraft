using System.Numerics;
using System.Text.Json.Serialization;
namespace VillageCraft;

public sealed class EconomySettings
{
    public float GoodsPerWorkCycle {get;set;}=.5f;
    public float GoodsPerKill {get;set;}=3;
    public float EmeraldsPerGood {get;set;}=1;
    public float DeliveryCapacity {get;set;}=12;
    public float InputBufferCycles {get;set;}=20;
    public float RepairWorkSeconds {get;set;}=4;
    public float RepairAmount {get;set;}=25;
    public Dictionary<ResourceType,float> RepairCost {get;set;}=new(){[ResourceType.Wood]=2,[ResourceType.Stone]=2};
    public float MilkHealing {get;set;}=12;
    public float EnchantmentProtection {get;set;}=.1f;
    public float EnchantmentDamage {get;set;}=.15f;
    public Dictionary<ResourceType,float> EnchantmentCost {get;set;}=[];
}

public partial class City
{
    public bool StorageInitialized {get;set;}
    public int LastSettlementDay {get;set;}=1;
    public bool EnchantedArmor {get;set;}
    public static bool Currency(ResourceType resource)=>resource is ResourceType.Emeralds or ResourceType.Goods;
    static float Stored(Building b,ResourceType resource)=>b.Storage.GetValueOrDefault(resource);
    public float StorageFree(Building b)=>Math.Max(0,b.Plan.StorageCapacity-b.Storage.Values.Sum());
    public void EnsureStorage()
    {
        foreach(ResourceType id in Catalog.Resources)Stock.TryAdd(id,0);
        if(StorageInitialized)return;
        var warehouse=Buildings.FirstOrDefault(b=>b.Plan.Warehouse);
        if(warehouse==null)
        {
            foreach(var road in Roads().OrderBy(p=>Math.Abs(p.Item1-AnchorX)+Math.Abs(p.Item2-AnchorZ)))
            {
                foreach(var (dx,dz,rotation) in new[]{(0,-1,0),(0,1,2),(1,0,1),(-1,0,3)})
                {
                    int x=road.Item1+dx,z=road.Item2+dz;
                    if(Walkable(x,z)&&At(x,z)==null){warehouse=Add("small_warehouse",x,z,rotation);break;}
                }
                if(warehouse!=null)break;
            }
        }
        if(warehouse==null)throw new InvalidDataException("A warehouse location is required to import city supplies.");
        // Preserve all legacy stock, even when it exceeds the new warehouse capacity.
        foreach(var item in Stock.Where(e=>!Currency(e.Key)&&e.Value>0))warehouse.Storage[item.Key]=item.Value;
        StorageInitialized=true;
    }
    public void ReconcileStock()
    {
        EnsureStorage();
        // Keep explicit script/admin stock grants compatible. Ordinary production uses local stores.
        foreach(ResourceType resource in Catalog.Resources.Where(r=>!Currency(r)))
        {
            float difference=Stock[resource]-Buildings.Sum(b=>Stored(b,resource));
            if(difference>.001f)
            {
                var target=Buildings.FirstOrDefault(b=>b.Plan.Warehouse)??Buildings.First(b=>!b.Plan.Path&&b.Plan.StorageCapacity>0);
                target.Storage[resource]=Stored(target,resource)+difference;
            }
            else if(difference<-.001f)Withdraw(resource,-difference);
        }
    }
    void Withdraw(ResourceType resource,float amount)
    {
        foreach(var b in Buildings.OrderByDescending(b=>b.Plan.Warehouse))
        {
            float take=Math.Min(amount,Stored(b,resource));
            if(take<=0)continue;
            b.Storage[resource]-=take;amount-=take;
            if(amount<.0001f)break;
        }
    }
    public float Receive(Building b,ResourceType resource,float amount)
    {
        if(Currency(resource)){Stock[resource]+=amount;return amount;}
        float accepted=Math.Min(amount,StorageFree(b));
        b.Storage[resource]=Stored(b,resource)+accepted;Stock[resource]+=accepted;return accepted;
    }
    bool ConsumeLocal(Building b,Dictionary<ResourceType,float> recipe)
    {
        if(recipe.Any(e=>Currency(e.Key)?Stock[e.Key]<e.Value:Stored(b,e.Key)<e.Value))return false;
        foreach(var e in recipe)
        {
            if(!Currency(e.Key))b.Storage[e.Key]-=e.Value;
            Stock[e.Key]-=e.Value;
        }
        return true;
    }
    public Building? FindSupply(Citizen villager,ResourceType resource,int exclude=-1)
    {
        // Warehouses take priority, then original sources; only reachable supplies qualify.
        foreach(var b in Buildings.Where(b=>b.Id!=exclude&&b.Enabled&&SupplyAvailable(b,resource)>.001f)
            .OrderByDescending(b=>b.Plan.Warehouse).ThenBy(b=>Vector2.DistanceSquared(villager.Position,new(b.X,b.Z))).ThenBy(b=>b.Id))
            if(Vector2.DistanceSquared(villager.Position,new(b.X,b.Z))<.2f||Route(villager.Position,new(b.X,b.Z),false).Count>0)return b;
        return null;
    }
    bool WalkTo(Citizen v,Building b,float dt,bool roadsOnly=false,float tolerance=.18f)
    {
        var target=new Vector2(b.X,b.Z);
        if(Vector2.Distance(v.Position,target)<tolerance)return true;
        if(v.RouteTarget!=b.Id){v.Route.Clear();v.RouteTarget=b.Id;}
        if(v.Route.Count==0)v.Route=roadsOnly?Route(v.Position,target,true):PreferRoads(v.Position,target);
        if(v.Route.Count>0)
        {
            Move(v,Toward(v.Position,v.Route[0],dt*(roadsOnly?.85f:.9f)*TravelSpeed(v.Position,roadsOnly?b.Plan.Job:v.Profession)));
            if(Vector2.Distance(v.Position,v.Route[0])<.005f)v.Route.RemoveAt(0);
        }
        else if(MathF.Round(v.X)==b.X&&MathF.Round(v.Z)==b.Z)
            Move(v,Toward(v.Position,target,dt*.9f));
        return false;
    }
    bool Collect(Citizen v,Building source,ResourceType resource,float amount,float dt)
    {
        if(!WalkTo(v,source,dt))return false;
        float take=Math.Min(Math.Min(amount,Stored(source,resource)),Math.Max(0,32-v.Inventory.Values.Sum()-v.Cargo.Values.Sum()));
        if(take<=0)return false;
        source.Storage[resource]-=take;Stock[resource]-=take;
        v.Inventory[resource]=v.Inventory.GetValueOrDefault(resource)+take;
        Emit(new("onResourceCollected",v,Amount:take,Source:source));return true;
    }
    static IEnumerable<ResourceType> ProducedResources(Building b)=>b.Plan.Output.Keys.Concat(b.Plan.Animal switch
    {"sheep"=>new[]{ResourceType.Wool},"chicken"=>new[]{ResourceType.Eggs},"pig"=>new[]{ResourceType.Food},_=>Array.Empty<ResourceType>()}).Distinct();
    public float SupplyAvailable(Building b,ResourceType resource)=>Math.Max(0,Stored(b,resource)-(b.Plan.Warehouse?0:InputTarget(b,resource)));
    static float InputTarget(Building b,ResourceType resource)
    {
        float recipeSize=b.Plan.Input.Where(e=>!Currency(e.Key)).Sum(e=>e.Value);
        float cycles=Math.Max(1,Math.Min(Catalog.Data.Economy.InputBufferCycles,b.Plan.StorageCapacity*.5f/Math.Max(.001f,recipeSize)));
        return b.Plan.Input.GetValueOrDefault(resource)*cycles;
    }
    void SettleAtDawn()
    {
        if(LastSettlementDay>=Day||DayPhase<.25f)return;
        LastSettlementDay=Day;
        float earned=Stock[ResourceType.Goods]*Catalog.Data.Economy.EmeraldsPerGood;
        Stock[ResourceType.Goods]=0;float upkeep=Buildings.Sum(b=>b.Plan.Upkeep);
        Stock[ResourceType.Emeralds]+=earned-upkeep;Balance=earned-upkeep;Statistics.EmeraldsFromGoods+=earned;
        Emit(new("onGoodsConverted",Amount:earned));Emit(new("onDawn"));
        Log(L.F("message.goods_settlement",earned,upkeep));
    }
    public void ValidateLogistics()
    {
        bool ValidMap(Dictionary<ResourceType,float>? map)=>map!=null&&map.Count<=64&&map.All(e=>Catalog.Resources.Contains(e.Key)&&!Currency(e.Key)&&float.IsFinite(e.Value)&&e.Value>=0&&e.Value<=1000000000);
        if(LastSettlementDay<0||LastSettlementDay>Day||Buildings.Any(b=>!ValidMap(b.Storage)||b.RequestedResource!=ResourceType.None&&(!Catalog.Resources.Contains(b.RequestedResource)||Currency(b.RequestedResource)))||
            Citizens.Any(v=>!ValidMap(v.Cargo)||v.Inventory.Values.Sum()+v.Cargo.Values.Sum()>32.001f||v.Effects==null||v.Effects.Count>16||v.Effects.Any(e=>!(e.Key is "Burning" or "Poison" or "Slowness" or "Weakness")||!float.IsFinite(e.Value)||e.Value<0||e.Value>3600)||v.DeliveryResource!=ResourceType.None&&(!Catalog.Resources.Contains(v.DeliveryResource)||Currency(v.DeliveryResource))))
            throw new InvalidDataException("Invalid storage, delivery or effects data.");
        EnsureStorage();
        if(Catalog.Resources.Where(r=>!Currency(r)).Any(r=>Math.Abs(Stock[r]-Buildings.Sum(b=>Stored(b,r)))>.1f))throw new InvalidDataException("Resource stock does not match physical storage.");
    }
    public bool EnchantArmor()
    {
        if(EnchantedArmor||ArmorLevel==0||!Buildings.Any(b=>b.Enabled&&Connected(b,Roads())&&b.Plan.Defender=="mage"&&Citizens.Any(v=>v.Work==b.Id&&v.Profession=="mage"))){Message=L.T("ui.a_staffed_mage_hall_and_armor_are_required");return false;}
        if(!Spend(Catalog.Data.Economy.EnchantmentCost)){Message=L.T("ui.not_enough_materials_for_this_upgrade");return false;}
        EnchantedArmor=true;Emit(new("onArmorEnchanted"));return true;
    }
}
