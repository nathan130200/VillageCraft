using System.Numerics;
namespace VillageCraft;

static class VillageImprovementsChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAILED: "+message);}
    public static void Run()
    {
        using var city=City.New(generateTerrain:false);city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;city.Rules.BirthChance=0;
        var plans=Catalog.Data.Prefabs.Where(p=>p.Input.GetValueOrDefault(ResourceType.Food)>0).ToArray();
        Check(plans.Length>0,"food-consuming building definition exists");
        var plan=plans[0];var added=new List<Building>();
        for(int i=0;i<4;i++)
        {
            var building=new Building{Id=city.NextId++,Prefab=plan.Id,X=3+i,Z=3,Health=plan.Health};city.Buildings.Add(building);added.Add(building);
        }
        city.InvalidateTopology();
        var before=city.GetProblems().Single(p=>p.Kind==CityProblemKind.MissingInput&&p.Resource==ResourceType.Food);
        Check(added.All(b=>before.Subjects.Contains(b.Id)),"four food shortages share one counted group");
        foreach(var b in added)b.Storage[ResourceType.Food]=100;
        var after=city.GetProblems().FirstOrDefault(p=>p.Kind==CityProblemKind.MissingInput&&p.Resource==ResourceType.Food);
        Check(after==null||added.All(b=>!after.Subjects.Contains(b.Id)),"resolved shortages disappear instead of accumulating");
        city.Buildings.RemoveAll(b=>added.Contains(b));city.InvalidateTopology();
        foreach(string type in new[]{"iron_golem","snow_golem"})
        {
            Building? forge=null;
            foreach(var road in city.Roads())
            {
                for(int rotation=0;rotation<4;rotation++)
                {
                    var offset=rotation switch{0=>(0,1),1=>(-1,0),2=>(0,-1),_=>(1,0)};
                    int x=road.Item1-offset.Item1,z=road.Item2-offset.Item2;
                    if(city.At(x,z)!=null||!city.Walkable(x,z))continue;
                    string prefab=type=="iron_golem"?"iron_foundry":"snow_shrine";
                    forge=new Building{Id=city.NextId++,Prefab=prefab,X=x,Z=z,Rotation=rotation,Health=Catalog.Get(prefab).Health};break;
                }
                if(forge!=null)break;
            }
            Check(forge!=null,"guardian forge fixture has an entrance on the road");
            city.Buildings.Add(forge!);city.InvalidateTopology();
            var guardian=new Creature{Id=city.NextId++,Type=type,Home=forge!.Id,X=forge.X,Z=forge.Z,Health=Catalog.Data.Units[type].Health};city.Defenders.Add(guardian);
            var visited=new HashSet<(int,int)>();float farthest=0;
            for(int i=0;i<1000;i++)
            {
                city.Update(.1f);visited.Add(((int)MathF.Round(guardian.X),(int)MathF.Round(guardian.Z)));
                farthest=Math.Max(farthest,Vector2.Distance(guardian.Position,new(city.AnchorX,city.AnchorZ)));
                Check(float.IsFinite(guardian.X)&&city.Walkable((int)MathF.Round(guardian.X),(int)MathF.Round(guardian.Z)),"guardian remains on traversable terrain");
            }
            Check(visited.Count>=6&&farthest>3,type+" patrols beyond its former forge tether");
            var hostile=city.SpawnMonster("zombie",guardian.X,guardian.Z)!;hostile.LeatherHelmet=true;
            city.Update(.1f);Check(guardian.Cooldown>0,"combat interrupts the guardian patrol");
            city.Enemies.Clear();city.Defenders.Remove(guardian);city.Buildings.Remove(forge);city.InvalidateTopology();
        }
        foreach(var profession in Catalog.Data.Professions.Values)
        {
            var parts=Catalog.Data.Models[profession.Model];
            Check(parts.Any(p=>p.Texture=="entity/villager/villager.png"),"profession retains base skin: "+profession.Id);
            Check(parts.Any(p=>p.Texture=="entity/villager/type/plains.png"),"profession retains clothing layer: "+profession.Id);
            Check(parts.All(p=>p.Texture is not "entity/villager/farmer.png" and not "entity/villager/smith.png" and not "entity/villager/butcher.png" and not "entity/villager/priest.png"),"no obsolete full-body profession lookup");
        }
        Console.WriteLine("PASS: grouped/resolving city problems, roaming iron/snow golems and layered villager skins.");
    }
}
