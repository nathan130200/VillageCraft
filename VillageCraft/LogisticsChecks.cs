using System.Numerics;
namespace VillageCraft;

static class LogisticsChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception("LOGISTICS: "+name);}
    static void Advance(City c,float seconds){for(int i=0;i<seconds*10;i++)c.Update(.1f);}
    public static void Run()
    {
        using(var city=City.StartVillage(seed:4,tutorial:false))
        {
            city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;
            int deliveries=0;var supplied=new HashSet<int>();city.Changed+=e=>{if(e.Name=="onDeliveryCompleted"){deliveries++;if(e.Source is Building b)supplied.Add(b.Id);}};
            foreach(var b in city.Buildings.Where(b=>b.Plan.Input.ContainsKey(ResourceType.Food)))b.Storage[ResourceType.Food]=0;
            city.Stock[ResourceType.Food]=city.Buildings.Sum(b=>b.Storage.GetValueOrDefault(ResourceType.Food));
            var consumers=city.Buildings.Where(b=>b.Plan.Input.ContainsKey(ResourceType.Food)&&b.Enabled&&city.Connected(b,city.Roads())).ToArray();
            Advance(city,180);
            Check(consumers.All(b=>supplied.Contains(b.Id)),"all food consumers receive deliveries instead of a nearby buffer monopolizing couriers: "+string.Join(",",consumers.Select(b=>b.Prefab+":"+supplied.Contains(b.Id))));
            Check(deliveries>=3&&city.Scripting.Errors.Count==0,"couriers perform real deliveries");city.ValidateLogistics();
            foreach(var v in city.Citizens.Where(v=>v.Profession=="deliveryman"))
            {
                var warehouse=city.Buildings.First(b=>b.Plan.Warehouse);v.Position=new(warehouse.X,warehouse.Z);v.Route.Clear();v.RouteTarget=-1;
                var target=consumers.First();var wrapper=new ScriptVillager(city,v);wrapper.walkTo(new(city,target),.1f);
                Check(v.Route.All(step=>city.At((int)step.X,(int)step.Y)?.Plan.Path==true||step==new Vector2(target.X,target.Z)),"delivery route follows roads and destination entrance");
            }
            var pig=city.Buildings.First(b=>b.Prefab=="pig_pen");
            pig.Storage[ResourceType.Food]=0;city.ReconcileStock();city.Receive(pig,ResourceType.Food,3);Check(city.SupplyAvailable(pig,ResourceType.Food)==0,"feed buffer is not collected as surplus output");
        }
        foreach(string profession in Catalog.Data.Professions.Keys)
        {
            using var city=City.New(generateTerrain:false);city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;city.Citizens.Clear();
            var home=city.Buildings.First(b=>b.Plan.Homes>0);var person=new Citizen{Id=city.NextId++,Home=home.Id,X=home.X,Z=home.Z,Profession=profession};city.Citizens.Add(person);person.Inventory[ResourceType.Food]=2;
            city.Place("well",9,15);var well=city.At(9,15)!;well.Health=well.Plan.Health-20;int repairs=0;city.Changed+=e=>{if(e.Name=="onBuildingRepaired")repairs++;};
            float wood=city.Stock[ResourceType.Wood],stone=city.Stock[ResourceType.Stone];Advance(city,90);
            Check(well.Health==well.Plan.Health&&repairs==1,profession+" automatically collects materials and repairs");
            Check(city.Stock[ResourceType.Wood]==wood-2&&city.Stock[ResourceType.Stone]==stone-2,profession+" consumes physical repair materials exactly once");
        }
        Console.WriteLine("PASS: delivery fairness, protected feed buffers, road routes and automatic physical repairs by every profession.");
    }
}
