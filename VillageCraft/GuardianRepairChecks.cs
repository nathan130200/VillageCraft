using System.Numerics;
namespace VillageCraft;
static class GuardianRepairChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception("GUARDS/INSPECTORS: "+message);}
    public static void Run()
    {
        using(var city=City.New(generateTerrain:false))
        {
            city.Enemies.Clear();city.Defenders.Clear();
            var a=new Creature{Id=city.NextId++,Type="iron_golem",X=1,Z=1,Health=100};
            var b=new Creature{Id=city.NextId++,Type="iron_golem",X=1,Z=1,Health=100};
            city.Defenders.Add(a);city.Defenders.Add(b);
            var first=city.SpawnMonster("zombie",1,1.2f)!;var second=city.SpawnMonster("zombie",1,1.3f)!;
            float hp1=first.Health,hp2=second.Health;
            city.Scripting.UpdateGuardian(a,.1f);city.Scripting.UpdateGuardian(b,.1f);
            Check(first.Health<hp1&&second.Health<hp2,"two guardians cover different living enemies");
            city.Enemies.Clear();city.SpawnMonster("zombie",30,1);a.Cooldown=0;
            var start=a.Position;city.Scripting.UpdateGuardian(a,.1f);
            Check(a.Position!=start,"guardian acquires distant map threats");
            Check(city.Scripting.Errors.Count==0,"guardian script remains valid");
        }
        using(var city=City.New(generateTerrain:false))
        {
            var home=city.Buildings.First(b=>b.Plan.Homes>0&&b.Cell!=(city.AnchorX,city.AnchorZ));
            var resident=city.Citizens.First();resident.Home=home.Id;
            city.Remove(home);
            Check(!city.Buildings.Contains(home)&&resident.Home!=home.Id,"occupied home demolition rehouses or marks homeless");
            var target=city.Buildings.First(b=>!b.Plan.Indestructible&&!b.Plan.Natural);
            city.DamageBuilding(target,1);Check(city.BuildingDamageEffects.ContainsKey(target.Id),"actual damage emits bounded block fragments");
            city.ProfessionPriorities["farmer"]=1;
            Check(city.Train(resident,"archer"),"inspector training changes profession");city.Assign();
            Check(resident.Profession=="archer"&&resident.ManualProfession,"automatic assignment preserves explicit profession");
            string file=Path.Combine(Path.GetTempPath(),"villagecraft-manual-"+Guid.NewGuid()+".vcs");
            try{city.Save(file);using var loaded=City.Load(file);loaded.Assign();Check(loaded.Citizens.First(v=>v.Id==resident.Id).ManualProfession&&loaded.Citizens.First(v=>v.Id==resident.Id).Profession=="archer","manual profession survives save/load");}
            finally{File.Delete(file);}
        }
        using(var city=City.New(generateTerrain:false))
        {
            city.DayTime=city.Rules.DayLength*22/24;
            var v=city.Citizens.First();city.Citizens.RemoveAll(other=>other!=v);
            v.ChangeProfession("farmer");v.Work=-1;v.Inventory[ResourceType.Food]=2;
            var home=city.Buildings.First(b=>b.Id==v.Home);v.Position=new(home.X,home.Z);
            var damaged=city.Buildings.First(b=>!b.Plan.Indestructible&&b.Id!=home.Id&&!b.Plan.Natural);
            damaged.Health-=5;
            foreach(var store in city.Buildings)store.Storage.Clear();
            city.Scripting.UpdateVillager(v,.1f);
            Check(v.RepairTarget<0&&v.Task!=TaskId.CollectingRepairSupplies,"night repair does not leave without the whole recipe");
            foreach(var item in Catalog.Data.Economy.RepairCost)v.Inventory[item.Key]=item.Value;
            city.Scripting.UpdateVillager(v,.1f);
            Check(v.RepairTarget==damaged.Id,"night villager wakes when repair materials are carried");
            for(int i=0;i<2000&&damaged.Health<damaged.Plan.Health;i++)city.Scripting.UpdateVillager(v,.1f);
            Check(damaged.Health==damaged.Plan.Health,"night repair physically reaches and repairs the building");
        }
        Console.WriteLine("PASS: distributed guardians, distant acquisition, occupied demolition, manual professions and night repair supplies.");
    }
}
