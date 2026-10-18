using System.Numerics;
namespace VillageCraft;
static class TerrainChecks
{
    static void Check(bool valid,string message){if(!valid)throw new Exception("TERRAIN CHECK: "+message);}
    public static void Run()
    {
        for(int seed=1;seed<=20;seed++)
        {
            using var c=City.StartVillage(seed:seed);
            Check(c.Width==32&&c.Height==32&&c.Daylight>.99f&&c.Hour>=6,"bright default morning on 32x32");
            c.Terrain.Validate(c.Width,c.Height);
            Check(c.Terrain.Water.Any(v=>v==1)&&c.Terrain.Elevation.Max()>=3&&c.Terrain.Grass.Any(v=>v>0),"water, mountains, grasses");
            Check(c.Buildings.Any(b=>b.Plan.Natural),"natural tree entities");
            Check(c.Buildings.Where(b=>!b.Plan.Natural).All(b=>c.Walkable(b.X,b.Z)&&c.Elevation(b.X,b.Z)==0),"starter is flat and dry");
            var replica=TerrainData.Generate(32,32,seed,Catalog.Data.Terrain);
            Check(replica.Elevation.SequenceEqual(c.Terrain.Elevation)&&replica.Water.SequenceEqual(c.Terrain.Water),"repeatable seed");
            var water=Array.FindIndex(c.Terrain.Water,v=>v==1);
            Check(!c.Place("path",water%32,water/32)&&c.Route(new(c.AnchorX,c.AnchorZ),new(water%32,water/32),false).Count==0,"water cannot be built or crossed");
            var peak=Array.IndexOf(c.Terrain.Elevation,c.Terrain.Elevation.Max());
            var route=c.Route(new(c.AnchorX,c.AnchorZ),new(peak%32,peak/32),false);
            Check(route.Count>0,"mountain reachable using one-block steps");
            Vector2 previous=new(c.AnchorX,c.AnchorZ);
            foreach(var step in route){Check(Math.Abs(c.Elevation((int)step.X,(int)step.Y)-c.Elevation((int)previous.X,(int)previous.Y))<=1,"NPC route honors one-block rise");previous=step;}
        }
        using(var grace=City.StartVillage(seed:7))
        {
            for(int day=1;day<=5;day++){grace.Day=day;grace.SpawnNight();Check(grace.Enemies.Count==0,"five safe nights");}
            grace.Day=6;grace.SpawnNight();Check(grace.Enemies.Count==3,"monsters arrive on night six");
        }
        using(var forest=City.StartVillage(seed:3))
        {
            forest.Rules.NightMonsters=false;float wood=forest.Stock[ResourceType.Wood];
            for(int i=0;i<400;i++)forest.Update(.1f);
            Check(forest.Stock[ResourceType.Wood]>wood&&forest.Buildings.Any(b=>b.Plan.Harvest.Count>0&&b.GrowthDays>0),"woodcutter travels and harvests a real tree");
            var stump=forest.Buildings.First(b=>b.Plan.Harvest.Count>0&&b.GrowthDays>0);
            var worker=forest.Citizens.First(v=>v.Profession=="woodcutter");worker.Position=new(stump.X,stump.Z);
            Check(!forest.HarvestTree(stump,worker),"cannot harvest depleted tree twice");
            var camp=forest.Buildings.First(b=>b.Id==worker.Work);
            var next=forest.Buildings.First(b=>b.Id!=stump.Id&&b.Plan.Harvest.Count>0&&System.Numerics.Vector2.Distance(new(b.X,b.Z),new(camp.X,camp.Z))<=camp.Plan.HarvestRadius);
            next.GrowthDays=0;foreach(var person in forest.Citizens)person.ResourceTarget=-1;
            worker.ResourceTarget=stump.Id;worker.Inventory[ResourceType.Food]=2;worker.Health=worker.MaximumHealth;
            forest.Scripting.UpdateVillager(worker,.1f);
            Check(worker.ResourceTarget>0&&worker.ResourceTarget!=stump.Id&&worker.Task is TaskId.GoingToTrees or TaskId.ChoppingTree,"woodcutter releases a stump and immediately selects another grown tree");
            for(int i=0;i<stump.Plan.RegrowDays;i++)forest.Tick();
            Check(stump.GrowthDays==0&&forest.HarvestTree(stump,worker),"tree regrows and can be harvested again");
            Check(forest.TutorialTreesHarvested>0&&forest.TutorialGoalMet("trees"),"tutorial observes actual harvest");
            string file=Path.Combine(Path.GetTempPath(),"villagecraft-terrain-"+Guid.NewGuid()+".vcs");
            try
            {
                forest.TutorialStep=2;forest.TutorialInspected=true;forest.Save(file);
                using var loaded=City.Load(file);
                Check(loaded.Terrain.Seed==forest.Terrain.Seed&&loaded.Terrain.Elevation.SequenceEqual(forest.Terrain.Elevation)&&loaded.TutorialStep==2&&loaded.TutorialInspected&&loaded.TutorialTreesHarvested==forest.TutorialTreesHarvested,"terrain/tutorial state persists");
                Check(loaded.Buildings.First(b=>b.Id==stump.Id).GrowthDays==stump.GrowthDays,"tree growth persists");
            }
            finally{File.Delete(file);}
        }
        Check(Catalog.Data.Models["quarry"].Where(p=>p.Texture=="block/iron_ore.png").All(p=>p.TextureMode=="stretch"),"ore faces use complete UVs");
    }
}
