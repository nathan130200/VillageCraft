using System.Diagnostics;
using System.Numerics;
namespace VillageCraft;

static class AiChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("SCRIPTED AI: "+message);}
    public static void Run()
    {
        Check(Catalog.Data.Prefabs.All(p=>!string.IsNullOrEmpty(p.Script))&&Catalog.Data.Prefabs.Select(p=>p.Script).Distinct().Count()==Catalog.Data.Prefabs.Count,"each prefab has its own component");
        Check(Catalog.Data.Game.InitialStock.Keys.All(r=>r!=ResourceType.None&&Enum.IsDefined(r)),"TOML resource maps use enum keys");
        Check(!ResourceIds.TryParse("Food ",out _)&&!ResourceIds.TryParse("2",out _)&&!ResourceIds.TryParse("Imaginary",out _)&&ResourceIds.Parse("Food")==ResourceType.Food,"strict resource boundary validation");
        using(var city=City.New(generateTerrain:false))
        {
            var v=city.Citizens[0];var warehouse=city.Buildings.First(b=>b.Plan.Warehouse);var person=new ScriptVillager(city,v);var store=new ScriptBuilding(city,warehouse);
            Check(!person.collect(store,"Imaginary",1,.1f)&&!person.walkTo(store,float.NaN)&&!person.walkTo(store,1),"actions reject invalid IDs and timesteps");
            v.Position=new(1,1);Check(!person.pickup(store,store,"Food"),"remote pickups cannot teleport resources");
            var api=new ScriptGame(city);Check(!api.takeResource("Imaginary",1)&&api.stock("Imaginary")==0,"unknown script resource names never enter inventory");
        }
        string folder=Path.Combine(Path.GetTempPath(),"villagecraft-ai-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(var source in Scripts.Sources)File.WriteAllText(Path.Combine(folder,source.Key+".js"),source.Value);
            File.WriteAllText(Path.Combine(folder,"custom_farmer.js"),"""
                set_script_type(eST_AI, "farmer");
                function onVillagerUpdate(v, dt) { v.tags = "script_controlled"; v.task = tasks.ScriptedBehavior; v.moveTo(v.x + 1, v.z, dt); }
                """);
            File.WriteAllText(Path.Combine(folder,"custom_guardian.js"),"""
                set_script_type(eST_AI, "iron_golem");
                function onGuardianUpdate(ctx, dt) { ctx.self.tags = "guardian_override"; ctx.moveTo(ctx.x + 1, ctx.z); }
                """);
            File.WriteAllText(Path.Combine(folder,"custom_animal.js"),"""
                set_script_type(eST_AI, "chicken");
                function onAnimalUpdate(ctx, dt) { ctx.self.tags = "animal_override"; ctx.moveTo(ctx.x + 0.2, ctx.z); }
                """);
            File.WriteAllText(Path.Combine(folder,"custom_well.js"),"""
                set_script_type(eST_Building, "well");
                function onBuildingDay(building) { building.repair(5); }
                """);
            Scripts.Load(folder);
            using(var city=City.StartVillage(seed:4,tutorial:false))
            {
                city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;
                var farmer=city.Citizens.First(v=>v.Profession=="farmer");var guardian=new Creature{Id=city.NextId++,Type="iron_golem",Health=250,X=city.AnchorX,Z=city.AnchorZ};city.Defenders.Add(guardian);
                var well=city.Buildings.First(b=>b.Prefab=="well");well.Health=50;city.Tick();
                city.Update(.1f);
                Check(farmer.Tags=="script_controlled"&&farmer.Task==TaskId.ScriptedBehavior,"profession AI override receives typed task constants");
                Check(guardian.Tags=="guardian_override"&&guardian.X>city.AnchorX,"guardian AI is replaceable in JS");
                Check(city.Animals.Any(a=>a.Type=="chicken"&&a.Tags=="animal_override"),"animal AI is replaceable in JS");
                Check(well.Health==55,"building type override runs once instead of the default daily behavior");
                Check(city.Scripting.Errors.Count==0,"custom callbacks run without errors");
            }
            File.WriteAllText(Path.Combine(folder,"custom_farmer.js"),"""
                set_script_type(eST_AI, "farmer");
                function onVillagerUpdate(v, dt) { throw new Error("intentional override failure"); }
                """);
            File.WriteAllText(Path.Combine(folder,"custom_well.js"),"""
                set_script_type(eST_Building, "well");
                function onBuildingDay(building) { throw new Error("intentional building failure"); }
                """);
            Scripts.Load(folder);
            using(var city=City.StartVillage(seed:4,tutorial:false))
            {
                city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;
                city.Update(.1f);var well=city.Buildings.First(b=>b.Prefab=="well");well.Health=50;city.Tick();
                Check(well.Health==52,"failed building override falls back to its prefab component");
                Check(city.Scripting.Errors.Count==2&&city.Citizens.First(v=>v.Profession=="farmer").Task!=TaskId.Resting,"failed villager override falls back to shipped JS");
            }
        }
        finally{Catalog.Load();Directory.Delete(folder,true);}
        using(var large=City.StartVillage(256,256,seed:7,tutorial:false))
        {
            large.Rules.NightMonsters=large.Rules.RaidsEnabled=false;var timer=Stopwatch.StartNew();for(int i=0;i<10;i++)large.Update(.1f);
            Check(large.Scripting.Errors.Count==0,"large-map queries stay within script budgets");
            Console.WriteLine($"PASS: separate building scripts, enum resources, validated actions, JS overrides/fallbacks and 256x256 AI ({timer.ElapsedMilliseconds} ms / 10 steps).");
        }
    }
}
