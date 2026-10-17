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
    public float MilkHealing {get;set;}=12;
    public float EnchantmentProtection {get;set;}=.1f;
    public float EnchantmentDamage {get;set;}=.15f;
    public Dictionary<string,float> EnchantmentCost {get;set;}=[];
}

public partial class City
{
    public bool StorageInitialized {get;set;}
    public int LastSettlementDay {get;set;}=1;
    public bool EnchantedArmor {get;set;}
    public static bool Currency(string resource)=>resource is "Emeralds" or "Goods";
    static float Stored(Building b,string resource)=>b.Storage.GetValueOrDefault(resource);
    public float StorageFree(Building b)=>Math.Max(0,b.Plan.StorageCapacity-b.Storage.Values.Sum());
    public void EnsureStorage()
    {
        foreach(string id in Catalog.Resources)Stock.TryAdd(id,0);
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
        foreach(string resource in Catalog.Resources.Where(r=>!Currency(r)))
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
    void Withdraw(string resource,float amount)
    {
        foreach(var b in Buildings.OrderByDescending(b=>b.Plan.Warehouse))
        {
            float take=Math.Min(amount,Stored(b,resource));
            if(take<=0)continue;
            b.Storage[resource]-=take;amount-=take;
            if(amount<.0001f)break;
        }
    }
    public float Receive(Building b,string resource,float amount)
    {
        if(Currency(resource)){Stock[resource]+=amount;return amount;}
        float accepted=Math.Min(amount,StorageFree(b));
        b.Storage[resource]=Stored(b,resource)+accepted;Stock[resource]+=accepted;return accepted;
    }
    bool ConsumeLocal(Building b,Dictionary<string,float> recipe)
    {
        if(recipe.Any(e=>Currency(e.Key)?Stock[e.Key]<e.Value:Stored(b,e.Key)<e.Value))return false;
        foreach(var e in recipe)
        {
            if(!Currency(e.Key))b.Storage[e.Key]-=e.Value;
            Stock[e.Key]-=e.Value;
        }
        return true;
    }
    public Building? FindSupply(Citizen villager,string resource,int exclude=-1)
    {
        // Warehouses take priority, then original sources; only reachable supplies qualify.
        foreach(var b in Buildings.Where(b=>b.Id!=exclude&&b.Enabled&&Stored(b,resource)>.001f)
            .OrderByDescending(b=>b.Plan.Warehouse).ThenBy(b=>Vector2.DistanceSquared(villager.Position,new(b.X,b.Z))).ThenBy(b=>b.Id))
            if(Vector2.DistanceSquared(villager.Position,new(b.X,b.Z))<.2f||Route(villager.Position,new(b.X,b.Z),false).Count>0)return b;
        return null;
    }
    bool WalkTo(Citizen v,Building b,float dt,bool roadsOnly=false,float tolerance=.18f)
    {
        var target=new Vector2(b.X,b.Z);
        if(Vector2.Distance(v.Position,target)<tolerance)return true;
        if(v.RouteTarget!=b.Id){v.Route.Clear();v.RouteTarget=b.Id;}
        if(v.Route.Count==0)v.Route=Route(v.Position,target,roadsOnly);
        if(v.Route.Count>0)
        {
            Move(v,Toward(v.Position,v.Route[0],dt*(roadsOnly?.85f:.9f)*TravelSpeed(v.Position,roadsOnly?b.Plan.Job:v.Profession)));
            if(Vector2.Distance(v.Position,v.Route[0])<.005f)v.Route.RemoveAt(0);
        }
        else if(MathF.Round(v.X)==b.X&&MathF.Round(v.Z)==b.Z)
            Move(v,Toward(v.Position,target,dt*.9f));
        return false;
    }
    bool Collect(Citizen v,Building source,string resource,float amount,float dt)
    {
        if(!WalkTo(v,source,dt))return false;
        float take=Math.Min(Math.Min(amount,Stored(source,resource)),Math.Max(0,32-v.Inventory.Values.Sum()-v.Cargo.Values.Sum()));
        if(take<=0)return false;
        source.Storage[resource]-=take;Stock[resource]-=take;
        v.Inventory[resource]=v.Inventory.GetValueOrDefault(resource)+take;
        Emit(new("onResourceCollected",v,Amount:take,Source:source));return true;
    }
    bool UpdatePersonalSupplies(Citizen v,float dt)
    {
        if(v.Health<50||v.Effects.Count>0)
        {
            if(v.Inventory.GetValueOrDefault("Milk")>=1)
            {v.Inventory["Milk"]--;v.Effects.Clear();v.Health=Math.Min(50,v.Health+Catalog.Data.Economy.MilkHealing);}
            else if(FindSupply(v,"Milk") is {} milk)
            {v.Task="Collecting milk";Collect(v,milk,"Milk",1,dt);return true;}
        }
        if(v.Inventory.GetValueOrDefault("Food")<1&&FindSupply(v,"Food") is {} food)
        {v.Task="Collecting food";Collect(v,food,"Food",2-v.Inventory.GetValueOrDefault("Food"),dt);return true;}
        return false;
    }
    static IEnumerable<string> ProducedResources(Building b)=>b.Plan.Output.Keys.Concat(b.Plan.Animal switch
    {"sheep"=>new[]{"Wool"},"chicken"=>new[]{"Eggs"},"pig"=>new[]{"Food"},_=>Array.Empty<string>()}).Distinct();
    static float InputTarget(Building b,string resource)
    {
        float recipeSize=b.Plan.Input.Where(e=>!Currency(e.Key)).Sum(e=>e.Value);
        float cycles=Math.Max(1,Math.Min(Catalog.Data.Economy.InputBufferCycles,b.Plan.StorageCapacity*.5f/Math.Max(.001f,recipeSize)));
        return b.Plan.Input.GetValueOrDefault(resource)*cycles;
    }
    void UpdateDelivery(Citizen v,Building workplace,float dt)
    {
        if(v.Cargo.Values.Sum()>0)
        {
            var destination=Buildings.FirstOrDefault(b=>b.Id==v.DeliveryTarget&&b.Enabled&&StorageFree(b)>0);
            destination??=Buildings.Where(b=>b.Plan.Warehouse&&b.Enabled&&StorageFree(b)>0)
                .OrderBy(b=>Vector2.DistanceSquared(v.Position,new(b.X,b.Z))).FirstOrDefault();
            if(destination==null){v.Task="Waiting for storage space";return;}
            v.DeliveryTarget=destination.Id;v.Task="Delivering resources";
            if(!WalkTo(v,destination,dt))return;
            foreach(var item in v.Cargo.ToArray())
            {float moved=Receive(destination,item.Key,item.Value);v.Cargo[item.Key]-=moved;}
            foreach(string key in v.Cargo.Where(e=>e.Value<.001f).Select(e=>e.Key).ToArray())v.Cargo.Remove(key);
            Stock["Goods"]+=Catalog.Data.Economy.GoodsPerWorkCycle;v.HasWorked=true;Statistics.Deliveries++;
            Emit(new("onDeliveryCompleted",v,Source:destination));return;
        }
        Building? from=Buildings.FirstOrDefault(b=>b.Id==v.PickupSource&&b.Enabled);
        Building? to=Buildings.FirstOrDefault(b=>b.Id==v.DeliveryTarget&&b.Enabled);
        string resource=v.DeliveryResource;
        if(from==null||to==null||Stored(from,resource)<.01f||StorageFree(to)<.01f)
        {
            from=null;to=null;
            // Supply workplace input buffers before collecting surplus output for warehouses.
            foreach(var consumer in Buildings.Where(b=>b.Enabled&&StorageFree(b)>.01f&&(b.Plan.Input.Count>0||b.RequestedResource!="")&&Connected(b,Roads()))
                .OrderByDescending(b=>b.Priority).ThenBy(b=>Vector2.DistanceSquared(v.Position,new(b.X,b.Z))))
            {
                foreach(var input in consumer.Plan.Input.Select(e=>new KeyValuePair<string,float>(e.Key,InputTarget(consumer,e.Key))).Concat(consumer.RequestedResource==""?[]:[new KeyValuePair<string,float>(consumer.RequestedResource,20)]).Where(e=>!Currency(e.Key)&&Stored(consumer,e.Key)<e.Value-.001f))
                {
                    if(Citizens.Any(other=>other.Id!=v.Id&&other.DeliveryTarget==consumer.Id&&other.DeliveryResource==input.Key))continue;
                    var source=FindSupply(v,input.Key,consumer.Id);if(source==null)continue;
                    if(Route(new(source.X,source.Z),new(consumer.X,consumer.Z),false).Count==0)continue;
                    from=source;to=consumer;resource=input.Key;break;
                }
                if(from!=null)break;
            }
            if(from==null)
            {
                foreach(var source in Buildings.Where(b=>!b.Plan.Warehouse&&b.Enabled&&ProducedResources(b).Any(r=>Stored(b,r)>0))
                    .OrderBy(b=>Vector2.DistanceSquared(v.Position,new(b.X,b.Z))))
                {
                    var warehouse=Buildings.Where(b=>b.Plan.Warehouse&&b.Enabled&&StorageFree(b)>0&&Connected(b,Roads()))
                        .OrderBy(b=>Vector2.DistanceSquared(new(source.X,source.Z),new(b.X,b.Z)))
                        .FirstOrDefault(b=>Route(new(source.X,source.Z),new(b.X,b.Z),false).Count>0);
                    if(warehouse==null)continue;
                    from=source;to=warehouse;resource=ProducedResources(source).First(r=>Stored(source,r)>0);break;
                }
            }
            if(from==null||to==null){v.Task="Waiting for deliveries";return;}
            v.PickupSource=from.Id;v.DeliveryTarget=to.Id;v.DeliveryResource=resource;
        }
        v.Task="Collecting delivery";
        if(!WalkTo(v,from,dt))return;
        float amount=Math.Min(Math.Min(Stored(from,resource),StorageFree(to)),Catalog.Data.Economy.DeliveryCapacity*Efficiency(v));
        if(to.Plan.Input.ContainsKey(resource))amount=Math.Min(amount,Math.Max(0,InputTarget(to,resource)-Stored(to,resource)));
        amount=Math.Min(amount,Math.Max(0,32-v.Inventory.Values.Sum()));
        if(amount<=.001f){v.PickupSource=-1;return;}
        from.Storage[resource]-=amount;Stock[resource]-=amount;v.Cargo[resource]=amount;
        v.PickupSource=-1;v.Route.Clear();
    }
    void SettleAtDawn()
    {
        if(LastSettlementDay>=Day||DayPhase<.25f)return;
        LastSettlementDay=Day;
        float earned=Stock["Goods"]*Catalog.Data.Economy.EmeraldsPerGood;
        Stock["Goods"]=0;float upkeep=Buildings.Sum(b=>b.Plan.Upkeep);
        Stock["Emeralds"]+=earned-upkeep;Balance=earned-upkeep;Statistics.EmeraldsFromGoods+=earned;
        Emit(new("onGoodsConverted",Amount:earned));Emit(new("onDawn"));
        Log(L.F("06:00: {0:0.##} emeralds earned from Goods; upkeep {1:0.##}.",earned,upkeep));
    }
    public void ValidateLogistics()
    {
        bool ValidMap(Dictionary<string,float>? map)=>map!=null&&map.Count<=64&&map.All(e=>Catalog.Resources.Contains(e.Key)&&!Currency(e.Key)&&float.IsFinite(e.Value)&&e.Value>=0&&e.Value<=1000000000);
        if(LastSettlementDay<0||LastSettlementDay>Day||Buildings.Any(b=>!ValidMap(b.Storage)||b.RequestedResource!=""&&(!Catalog.Resources.Contains(b.RequestedResource)||Currency(b.RequestedResource))||b.TorchLit&&b.Plan.LightRadius<=0)||
            Citizens.Any(v=>!ValidMap(v.Cargo)||v.Inventory.Values.Sum()+v.Cargo.Values.Sum()>32.001f||v.Effects==null||v.Effects.Count>16||v.Effects.Any(e=>!(e.Key is "Burning" or "Poison" or "Slowness" or "Weakness")||!float.IsFinite(e.Value)||e.Value<0||e.Value>3600)||v.DeliveryResource!=""&&(!Catalog.Resources.Contains(v.DeliveryResource)||Currency(v.DeliveryResource))))
            throw new InvalidDataException("Invalid storage, delivery or effects data.");
        EnsureStorage();
        if(Catalog.Resources.Where(r=>!Currency(r)).Any(r=>Math.Abs(Stock[r]-Buildings.Sum(b=>Stored(b,r)))>.1f))throw new InvalidDataException("Resource stock does not match physical storage.");
    }
    public bool EnchantArmor()
    {
        if(EnchantedArmor||ArmorLevel==0||!Buildings.Any(b=>b.Enabled&&Connected(b,Roads())&&b.Plan.Defender=="mage"&&Citizens.Any(v=>v.Work==b.Id&&v.Profession=="mage"))){Message=L.T("A staffed mage hall and armor are required. Enchanting improves protection and damage.");return false;}
        if(!Spend(Catalog.Data.Economy.EnchantmentCost)){Message=L.T("Not enough materials for this upgrade.");return false;}
        EnchantedArmor=true;Emit(new("onArmorEnchanted"));return true;
    }
}
