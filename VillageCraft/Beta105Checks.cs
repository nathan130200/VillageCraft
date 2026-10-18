using System.Numerics;
namespace VillageCraft;

static class Beta105Checks
{
    static void Check(bool value,string name){if(!value)throw new Exception("1.0.5-beta: "+name);}
    static City Empty()
    {
        var c=City.New(generateTerrain:false);c.Citizens.Clear();c.Rules.NightMonsters=false;c.Rules.RaidsEnabled=false;
        return c;
    }
    static Citizen Person(City c,string profession,Vector2 position)
    {
        var v=c.AddVillager(profession,c.Buildings.First(b=>b.Plan.Homes>0).Id)!;
        v.Position=position;c.Spend(new(){[ResourceType.Food]=2});v.Inventory[ResourceType.Food]=2;c.Assign();return v;
    }
    static void Advance(City c,float seconds){for(int i=0;i<seconds*10;i++)c.Update(.1f);}
    public static void Run()
    {
        using(var c=Empty())
        {
            Check(c.Buildings.Any(b=>b.Plan.Warehouse)&&c.Buildings.All(b=>!b.Storage.ContainsKey(ResourceType.Emeralds)&&!b.Storage.ContainsKey(ResourceType.Goods)),"physical starter stock excludes currency");
            c.Stock[ResourceType.Goods]=10;c.Day=2;c.DayTime=c.Rules.DayLength*.25f-.05f;float money=c.Stock[ResourceType.Emeralds]-c.Buildings.Sum(b=>b.Plan.Upkeep);
            c.Update(.1f);Check(c.Stock[ResourceType.Emeralds]==money+10&&c.Stock[ResourceType.Goods]==0&&c.LastSettlementDay==2,"Goods convert at 06:00");
            c.Update(.1f);Check(c.Stock[ResourceType.Emeralds]==money+10,"settlement occurs once per day");
            c.Enemies.Add(new(){Id=c.NextId++,Type="zombie",Health=0,X=10,Z=16});c.ResolveDeaths();
            Check(c.Stock[ResourceType.Goods]==Catalog.Data.Economy.GoodsPerKill&&c.Stock[ResourceType.Emeralds]==money+10,"combat earns Goods rather than instant emeralds");
        }
        using(var c=Empty())
        {
            Check(c.Place("forge",9,15)&&c.Place("lumber",10,15),"logistics fixtures");
            var forge=c.At(9,15)!;var source=c.At(10,15)!;var warehouse=c.Buildings.First(b=>b.Plan.Warehouse);
            var courier=Person(c,"deliveryman",new(10,16));
            Check(c.FindSupply(courier,ResourceType.Wood)==warehouse,"warehouse preferred over closer source");
            c.Spend(new(){[ResourceType.Wood]=c.Stock[ResourceType.Wood],[ResourceType.Iron]=c.Stock[ResourceType.Iron]});c.Receive(source,ResourceType.Wood,12);
            for(int i=0;i<800&&courier.Cargo.Count==0;i++)c.Update(.1f);
            Check(courier.Cargo.GetValueOrDefault(ResourceType.Wood)>0&&source.Storage[ResourceType.Wood]<12&&forge.Storage.GetValueOrDefault(ResourceType.Wood)==0,"pickup happens at source before delivery: "+courier.Task+" "+courier.Position+" from="+courier.PickupSource+" to="+courier.DeliveryTarget+" resource="+courier.DeliveryResource+" cargo="+string.Join(",",courier.Cargo)+" errors="+string.Join(";",c.Scripting.Errors));
            Check(Math.Abs(c.Stock[ResourceType.Wood]+courier.Cargo.GetValueOrDefault(ResourceType.Wood)-12)<.001f,"in-transit cargo conserves resources");
            string save=Path.Combine(Path.GetTempPath(),"villagecraft-105-"+Guid.NewGuid()+".vcs");
            try
            {
                c.EnchantedArmor=true;c.ArmorLevel=1;courier.Effects["Weakness"]=10;
                c.Save(save);using var restored=City.Load(save);var carrier=restored.Citizens.Single();
                Check(carrier.Cargo.GetValueOrDefault(ResourceType.Wood)==courier.Cargo[ResourceType.Wood]&&carrier.DeliveryTarget==forge.Id,"in-transit save roundtrip");
                Check(restored.EnchantedArmor&&carrier.Effects["Weakness"]==10,"enchantment and effects persist");
                Advance(restored,50);var delivered=restored.Buildings.First(b=>b.Id==forge.Id);
                Check(delivered.Storage.GetValueOrDefault(ResourceType.Wood)==12&&carrier.Cargo.Count==0,"delivery completes at destination after loading");
                Check(Math.Abs(restored.Stock[ResourceType.Wood]-12)<.001f,"delivery neither creates nor loses resources");
            }
            finally{File.Delete(save);}
        }
        using(var c=Empty())
        {
            Check(c.Place("cow_shed",9,15),"cow shed placement");var shed=c.At(9,15)!;
            var keeper=Person(c,"keeper",new(9,15));shed.Priority=3;c.Assign();
            Advance(c,2.2f);Check(shed.Storage.GetValueOrDefault(ResourceType.Milk)==0,"workplace cannot consume remote global food");
            c.Spend(new(){[ResourceType.Food]=4});c.Receive(shed,ResourceType.Food,4);Advance(c,2.1f);
            Check(shed.Storage.GetValueOrDefault(ResourceType.Milk)>0&&shed.Storage.GetValueOrDefault(ResourceType.Leather)>0&&c.Animals.Count(a=>a.Type=="cow")==2,"cows produce leather and milk with delivered feed");
            keeper.Health=30;keeper.Effects["Burning"]=10;keeper.Inventory[ResourceType.Milk]=1;
            c.Update(.1f);Check(keeper.Effects.Count==0&&keeper.Health>41&&keeper.Inventory[ResourceType.Milk]==0,"milk clears effects and heals injuries");
        }
        using(var c=Empty())
        {
            Check(c.Place("mage_hall",9,15),"enchantment mage hall");Person(c,"mage",new(9,15));c.ArmorLevel=1;
            var warehouse=c.Buildings.First(b=>b.Plan.Warehouse);c.Receive(warehouse,ResourceType.Leather,20);c.Receive(warehouse,ResourceType.Gold,8);
            float armor=c.ArmorReduction;Check(c.EnchantArmor()&&c.ArmorReduction>armor,"staffed wizard enchants leather-equipped armor");
            Check(!c.EnchantArmor(),"enchantment cannot be charged twice");
        }
        using(var c=City.StartVillage(seed:4,tutorial:false))
        {
            c.Rules.NightMonsters=false;c.Rules.RaidsEnabled=false;int deliveries=0,settlements=0;
            c.Changed+=evt=>{if(evt.Name=="onDeliveryCompleted")deliveries++;if(evt.Name=="onGoodsConverted")settlements++;};
            Advance(c,600);c.ValidateLogistics();
            Check(deliveries>3&&settlements==1&&c.Citizens.All(v=>v.Health>0),"starter village operates with physical supplies through its first dawn: "+deliveries+" / "+settlements+" "+string.Join("; ",c.Citizens.Select(v=>v.Profession+":"+v.Task+" cargo="+v.Cargo.Values.Sum()+" pos="+v.Position)));
        }
        using(var c=Empty())
        {
            c.DayTime=0;Check(c.ClockTime=="00:00"&&c.IsNight&&c.Daylight==Catalog.Data.Rendering.NightAmbient&&GameUi.ClockFrame(c.DayPhase)==32,"midnight clock and moon icon match dark lighting");
            c.DayTime=c.Rules.DayLength*.25f;Check(c.ClockTime=="06:00"&&!c.IsNight&&c.Daylight>.2f&&c.Daylight<1,"06:00 sunrise");
            c.DayTime=c.Rules.DayLength*.5f;Check(c.ClockTime=="12:00"&&!c.IsNight&&c.Daylight==1&&GameUi.ClockFrame(c.DayPhase)==0,"noon clock and sun icon match daylight");
            c.DayTime=c.Rules.DayLength*.75f;Check(c.ClockTime=="18:00"&&c.IsNight&&c.Daylight<1,"18:00 sunset starts night duty");
        }
        bool dismissed=GraphicsOptions.Current.TutorialDismissed;
        try
        {
            GraphicsOptions.Current.TutorialDismissed=true;using var skipped=City.New(generateTerrain:false,tutorial:true);Check(!skipped.TutorialEnabled,"skipped tutorial stays skipped in new worlds");
            GraphicsOptions.Current.TutorialDismissed=false;using var reset=City.New(generateTerrain:false,tutorial:true);Check(reset.TutorialEnabled,"reset restores tutorial in new worlds");
        }
        finally{GraphicsOptions.Current.TutorialDismissed=dismissed;}
        Console.WriteLine("PASS: 1.0.5-beta Goods settlement, physical deliveries, in-transit saves, cattle, milk and tutorial preference.");
    }
}
