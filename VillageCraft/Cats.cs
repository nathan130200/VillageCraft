using System.Numerics;
using System.Text.Json.Serialization;
namespace VillageCraft;
public static class CreatureIds { public const string Cat="cat"; }
public sealed class CatSettings
{
    public float DawnSpawnChance {get;set;}=.1f;
    public float TameChance {get;set;}=.25f;
    public float WildStayDays {get;set;}=1;
    public float SupplyGraceDays {get;set;}=1;
    public int MaxCats {get;set;}=16;
    public float CreeperAvoidRadius {get;set;}=5;
}
public sealed class CatState
{
    public bool Tamed,Requested,Leaving;
    public int Farmer=-1,Courier=-1,Feeds;
    public float Food,Milk,Unsupplied;
}
public partial class City
{
    public IEnumerable<Creature> Cats=>Animals.Where(a=>a.Type==CreatureIds.Cat);
    public bool IsSleepingAnimal(Creature c)=>IsNight&&c.Type!=CreatureIds.Cat&&Animals.Contains(c);
    public Creature? SpawnCat()
    {
        if(Cats.Count()>=Catalog.Data.Cats.MaxCats||!Buildings.Any(b=>b.Plan.Homes>0))return null;
        var homes=Buildings.Where(b=>b.Plan.Homes>0&&b.Enabled).ToArray();
        var sites=BorderSpawnSites().OrderBy(_=>Random.Shared.Next()).Take(64);
        foreach(var site in sites)
        {
            var position=site;
            if(!homes.Any(h=>ScriptReach(position,new(h.X,h.Z))))continue;
            var cat=new Creature{Id=NextId++,Type=CreatureIds.Cat,X=position.X,Z=position.Y,Health=Catalog.Data.Units[CreatureIds.Cat].Health,Cat=new()};
            Animals.Add(cat);Emit(new("onAnimalSpawned",cat));return cat;
        }
        return null;
    }
    public bool RequestTame(Creature cat)
    {
        if(!Animals.Contains(cat)||cat.Type!=CreatureIds.Cat||cat.Health<=0||cat.Cat is not {} state||state.Tamed||state.Leaving)return false;
        state.Requested=true;return true;
    }
    internal bool CatCarers(Creature cat,int farmer,int courier)
    {
        if(cat.Cat is not {} state||!Animals.Contains(cat)||state.Leaving||(!state.Requested&&!state.Tamed))return false;
        bool Valid(int id,string profession)=>Citizens.Any(v=>v.Id==id&&v.Health>0&&v.Profession==profession)&&!Cats.Any(c=>c!=cat&&c.Cat is {} s&&(s.Farmer==id||s.Courier==id));
        if((!state.Tamed&&!Valid(farmer,"farmer"))||(farmer>=0&&!Valid(farmer,"farmer"))||!Valid(courier,"deliveryman"))return false;
        state.Farmer=farmer;state.Courier=courier;return true;
    }
    internal bool ResolveTaming(Creature cat)
    {
        if(cat.Cat is not {} state||!Animals.Contains(cat)||!state.Requested||state.Leaving||state.Tamed||state.Feeds!=3)return false;
        state.Feeds=0;state.Requested=false;
        state.Tamed=Random.Shared.NextSingle()<Catalog.Data.Cats.TameChance;
        if(state.Tamed){state.Food=2;state.Unsupplied=0;Emit(new("onAnimalTamed",cat));}
        else {state.Farmer=state.Courier=-1;Emit(new("onAnimalTamingFailed",cat));}
        return state.Tamed;
    }
    internal bool HelpCat(Citizen villager,float dt)
    {
        var cat=Cats.FirstOrDefault(c=>c.Cat is {} s&&!s.Leaving&&(s.Requested||s.Tamed)&&(s.Farmer==villager.Id||s.Courier==villager.Id));
        if(cat?.Cat is not {} state)return false;
        bool farmer=state.Farmer==villager.Id;
        if(villager.Profession!=(farmer?"farmer":"deliveryman"))return false;
        int bit=farmer?1:2;
        if(!state.Tamed&&(state.Feeds&bit)!=0)return false;
        ResourceType resource=state.Tamed&&!farmer&&state.Milk<1?ResourceType.Milk:ResourceType.Food;
        if(state.Tamed&&(resource==ResourceType.Milk?state.Milk:state.Food)>=1)return false;
        villager.Task=state.Tamed?TaskId.FeedingCat:TaskId.TamingCat;
        if(villager.Inventory.GetValueOrDefault(resource)<1)
        {
            float missing=1-villager.Inventory.GetValueOrDefault(resource);
            // The cat keeps waiting while carers return to their regular work.
            if(villager.Inventory.Values.Sum()+villager.Cargo.Values.Sum()+missing>32||Buildings.Where(b=>b.Enabled).Sum(b=>SupplyAvailable(b,resource))<missing)return false;
            var source=FindSupply(villager,resource);if(source==null)return false;
            Collect(villager,source,resource,Math.Min(missing,SupplyAvailable(source,resource)),dt);return true;
        }
        if(Vector2.DistanceSquared(villager.Position,cat.Position)>.35f*.35f){ScriptPatrol(villager,cat.X,cat.Z,dt);return true;}
        villager.Inventory[resource]-=1;
        if(state.Tamed){if(resource==ResourceType.Milk)state.Milk=Math.Min(3,state.Milk+1);else state.Food=Math.Min(3,state.Food+1);}
        else state.Feeds|=bit;
        Emit(new("onAnimalFed",cat,Source:villager));return true;
    }
}
public sealed partial class ScriptVillager
{
    public bool helpCat(float dt)=>Active&&Step(dt)&&city.HelpCat(citizen,dt);
}
