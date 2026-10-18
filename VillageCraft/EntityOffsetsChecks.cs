using System.Numerics;
namespace VillageCraft;
static class EntityOffsetsChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("ENTITY OFFSETS: "+message);}
    public static void Run()
    {
        using var city=City.New(generateTerrain:false);
        Citizen a=new(){Id=100,Position=new(3,3)},b=new(){Id=101,Position=new(3,3)};
        city.Citizens.Add(a);city.Citizens.Add(b);
        Vector2 offset=Scene.VillagerPosition(city,a)-a.Position;
        Check(Scene.VillagerPosition(city,a)!=Scene.VillagerPosition(city,b),"co-located villagers use different fixed lanes");
        for(int frame=0;frame<200;frame++)
        {
            a.Z+=.01f;b.Z-=.01f;
            Check(Vector2.Distance(Scene.VillagerPosition(city,a)-a.Position,offset)<.0001f,"approaching and passing another entity never pushes or changes the offset");
        }
        city.Citizens.Remove(b);Check(Vector2.Distance(Scene.VillagerPosition(city,a)-a.Position,offset)<.0001f,"offset does not depend on neighboring entities");
        var house=city.Buildings.First(b=>b.Plan.Homes>0);a.Home=house.Id;a.Position=new(house.X,house.Z);
        Check(!Scene.VillagerVisible(city,a),"indoor residents are hidden instead of protruding through walls");
        var entrance=new Vector2(house.Entrance.Item1,house.Entrance.Item2);a.Position=entrance;
        Check(Scene.VillagerVisible(city,a),"residents outside the entrance are visible");
        Vector2 previous=Scene.VillagerPosition(city,a);
        for(int frame=1;frame<=100;frame++)
        {
            a.Position=Vector2.Lerp(entrance,new(house.X,house.Z),frame/100f);var p=Scene.VillagerPosition(city,a);
            Check(Vector2.Distance(p,previous)<.04f,"entrance offset fades continuously without pushing");previous=p;
        }
        using var farm=City.StartVillage(tutorial:false);var pen=farm.Buildings.First(p=>!string.IsNullOrEmpty(p.Plan.Animal));
        var pair=farm.Animals.Where(m=>m.Home==pen.Id).Take(2).ToArray();
        Check(pair.All(m=>Scene.CreaturePosition(farm,m)==m.Position),"animals are excluded from offsets");
        farm.DayTime=farm.Rules.DayLength*.9f;var sleeping=pair.Select(m=>m.Position).ToArray();
        for(int frame=0;frame<20;frame++)foreach(var animal in pair)farm.Scripting.UpdateAnimal(animal,.1f);
        Check(sleeping.SequenceEqual(pair.Select(m=>m.Position)),"animals stop wandering at night");
        Console.WriteLine("PASS: fixed entity offsets, no pushing, continuous entrances, hidden indoor residents and sleeping animals.");
    }
}
