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
        v.Position=position;c.Spend(new(){["Food"]=2});v.Inventory["Food"]=2;c.Assign();return v;
    }
    static void Advance(City c,float seconds){for(int i=0;i<seconds*10;i++)c.Update(.1f);}
    public static void Run()
    {
        using(var c=Empty())
        {
            Check(c.Buildings.Any(b=>b.Plan.Warehouse)&&c.Buildings.All(b=>!b.Storage.ContainsKey("Emeralds")&&!b.Storage.ContainsKey("Goods")),"physical starter stock excludes currency");
            c.Stock["Goods"]=10;c.Day=2;c.DayTime=c.Rules.DayLength*.25f-.05f;float money=c.Stock["Emeralds"]-c.Buildings.Sum(b=>b.Plan.Upkeep);
            c.Update(.1f);Check(c.Stock["Emeralds"]==money+10&&c.Stock["Goods"]==0&&c.LastSettlementDay==2,"Goods convert at 06:00");
            c.Update(.1f);Check(c.Stock["Emeralds"]==money+10,"settlement occurs once per day");
            c.Enemies.Add(new(){Id=c.NextId++,Type="zombie",Health=0,X=10,Z=16});c.ResolveDeaths();
            Check(c.Stock["Goods"]==Catalog.Data.Economy.GoodsPerKill&&c.Stock["Emeralds"]==money+10,"combat earns Goods rather than instant emeralds");
        }
        using(var c=Empty())
        {
            Check(c.Place("forge",9,15)&&c.Place("lumber",10,15),"logistics fixtures");
            var forge=c.At(9,15)!;var source=c.At(10,15)!;var warehouse=c.Buildings.First(b=>b.Plan.Warehouse);
            var courier=Person(c,"deliveryman",new(10,16));
            Check(c.FindSupply(courier,"Wood")==warehouse,"warehouse preferred over closer source");
            c.Spend(new(){["Wood"]=c.Stock["Wood"],["Iron"]=c.Stock["Iron"]});c.Receive(source,"Wood",12);
            for(int i=0;i<800&&courier.Cargo.Count==0;i++)c.Update(.1f);
            Check(courier.Cargo.GetValueOrDefault("Wood")>0&&source.Storage["Wood"]<12&&forge.Storage.GetValueOrDefault("Wood")==0,"pickup happens at source before delivery");
            Check(Math.Abs(c.Stock["Wood"]+courier.Cargo.GetValueOrDefault("Wood")-12)<.001f,"in-transit cargo conserves resources");
            string save=Path.Combine(Path.GetTempPath(),"villagecraft-105-"+Guid.NewGuid()+".vcs");
            try
            {
                c.Place("torch",10,14);var fixture=c.At(10,14)!;c.SetTorch(fixture,true);c.EnchantedArmor=true;c.ArmorLevel=1;courier.Effects["Weakness"]=10;
                c.Save(save);using var restored=City.Load(save);var carrier=restored.Citizens.Single();
                Check(carrier.Cargo.GetValueOrDefault("Wood")==courier.Cargo["Wood"]&&carrier.DeliveryTarget==forge.Id,"in-transit save roundtrip");
                Check(restored.EnchantedArmor&&restored.Buildings.First(b=>b.Id==fixture.Id).TorchLit&&carrier.Effects["Weakness"]==10,"enchantment, torch and effects persist");
                Advance(restored,50);var delivered=restored.Buildings.First(b=>b.Id==forge.Id);
                Check(delivered.Storage.GetValueOrDefault("Wood")==12&&carrier.Cargo.Count==0,"delivery completes at destination after loading");
                Check(Math.Abs(restored.Stock["Wood"]-12)<.001f,"delivery neither creates nor loses resources");
            }
            finally{File.Delete(save);}
        }
        using(var c=Empty())
        {
            Check(c.Place("cow_shed",9,15),"cow shed placement");var shed=c.At(9,15)!;
            var keeper=Person(c,"keeper",new(9,15));shed.Priority=3;c.Assign();
            Advance(c,2.2f);Check(shed.Storage.GetValueOrDefault("Milk")==0,"workplace cannot consume remote global food");
            c.Spend(new(){["Food"]=4});c.Receive(shed,"Food",4);Advance(c,2.1f);
            Check(shed.Storage.GetValueOrDefault("Milk")>0&&shed.Storage.GetValueOrDefault("Leather")>0&&c.Animals.Count(a=>a.Type=="cow")==2,"cows produce leather and milk with delivered feed");
            keeper.Health=30;keeper.Effects["Burning"]=10;keeper.Inventory["Milk"]=1;
            c.Update(.1f);Check(keeper.Effects.Count==0&&keeper.Health>41&&keeper.Inventory["Milk"]==0,"milk clears effects and heals injuries");
        }
        using(var c=Empty())
        {
            Check(c.Place("torch",10,15),"torch placement");var torch=c.At(10,15)!;
            var citizen=Person(c,"citizen",new(10.45f,15.45f));c.DayTime=c.Rules.DayLength*17.1f/24;Advance(c,3);
            Check(torch.TorchLit&&c.TorchLightAt(10,15)>.9f&&citizen.TorchTarget==-1,"villager finishes lighting from inside the torch tile");
            c.DamageBuilding(torch,5);Check(torch.Health==torch.Plan.Health-5,"torches take damage");
            c.DayTime=c.Rules.DayLength*6.1f/24;citizen.Position=new(10.45f,15.45f);Advance(c,3);
            Check(!torch.TorchLit&&c.TorchLightAt(10,15)==0&&citizen.TorchTarget==-1,"villager finishes extinguishing without retrying");
            c.Remove(torch);Check(!c.Buildings.Contains(torch),"torch can be demolished");
            Check(c.Place("torch",10,15),"replacement torch");torch=c.At(10,15)!;c.SetTorch(torch,true);
            c.DamageBuilding(torch,10000);c.ResolveDeaths();Check(!c.Buildings.Contains(torch)&&c.TorchLightAt(10,15)==0,"destroyed torches stop emitting light");
        }
        using(var c=Empty())
        {
            Check(c.Place("mage_hall",9,15),"enchantment mage hall");Person(c,"mage",new(9,15));c.ArmorLevel=1;
            var warehouse=c.Buildings.First(b=>b.Plan.Warehouse);c.Receive(warehouse,"Leather",20);c.Receive(warehouse,"Gold",8);
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
            c.Place("barracks",9,15);c.Place("torch",10,15);var torch=c.At(10,15)!;
            var guard=Person(c,"warrior",new(9,15));c.DayTime=c.Rules.DayLength*17.1f/24;c.Update(.1f);
            Check(guard.TorchTarget==torch.Id,"torch visit reserves one target");
            var helper=Person(c,"citizen",new(10.45f,15.45f));
            c.Enemies.Add(new(){Id=c.NextId++,Type="zombie",X=9,Z=15.2f,Health=1000});Advance(c,2);
            Check(guard.TorchTarget==-1&&torch.TorchLit,"combat releases torch claim for another villager");
        }
        using(var c=Empty())
        {
            c.DayTime=0;Check(c.ClockTime=="00:00"&&c.IsNight&&c.Daylight<.2f&&GameUi.ClockFrame(c.DayPhase)==32,"midnight clock and moon icon match dark lighting");
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
        Console.WriteLine("PASS: 1.0.5-beta Goods settlement, physical deliveries, in-transit saves, cattle, milk, torch visits and tutorial preference.");
    }
}
