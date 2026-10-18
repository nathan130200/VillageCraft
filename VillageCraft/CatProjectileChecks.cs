using System.Numerics;
namespace VillageCraft;
static class CatProjectileChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception("CATS/PROJECTILES: "+message);}
    public static void Run()
    {
        using(var city=City.New(generateTerrain:false))
        {
            city.Citizens.Clear();city.Enemies.Clear();
            var villager=new Citizen{Id=city.NextId++,X=3,Z=3,Profession="farmer"};city.Citizens.Add(villager);
            var skeleton=city.SpawnMonster("skeleton",3,1)!;float health=villager.Health;
            Check(city.ScriptAttack(skeleton,villager)&&city.Shots.Count==1&&villager.Health==health,"ranged launch has no instant damage");
            var initial=city.Shots[0].Position;city.UpdateProjectiles(.05f);
            Check(city.Shots.Count==1&&city.Shots[0].Position!=initial&&villager.Health==health,"projectile travels before hitting");
            for(int i=0;i<20;i++)city.UpdateProjectiles(.05f);
            Check(villager.Health<health&&city.Shots.Count==0,"projectile applies damage once on impact");
            float hitHealth=villager.Health;city.UpdateProjectiles(1);Check(villager.Health==hitHealth,"removed projectile cannot hit twice");
            skeleton.Cooldown=0;villager.Position=new(3,3);city.ScriptAttack(skeleton,villager);villager.Position=new(6,3);
            for(int i=0;i<100;i++)city.UpdateProjectiles(.05f);
            Check(villager.Health==hitHealth&&city.Shots.Count==0,"moving target can dodge; missed projectiles expire");
        }
        using(var city=City.New(generateTerrain:false))
        {
            var cat=city.SpawnCat()??throw new Exception("No reachable cat spawn");Check((cat.X==0||cat.Z==0||cat.X==city.Width-1||cat.Z==city.Height-1),"living cat spawns at empty border");
            var initial=cat.Position;city.Scripting.UpdateAnimal(cat,.2f);Check(cat.Position!=initial,"cat AI approaches housing");
            var farmer=city.Citizens.First(v=>v.Profession=="farmer");
            var courier=city.Citizens.First(v=>v.Profession=="deliveryman");
            cat.Position=new(3,3);farmer.Position=new(3,1);courier.Position=new(3,5);
            farmer.Inventory[ResourceType.Food]=1;courier.Inventory[ResourceType.Food]=1;
            Check(city.RequestTame(cat)&&city.CatCarers(cat,farmer.Id,courier.Id),"taming needs separate farmer and deliveryman");
            foreach(var store in city.Buildings)store.Storage[ResourceType.Food]=0;
            farmer.Inventory[ResourceType.Food]=courier.Inventory[ResourceType.Food]=0;
            var waiting=farmer.Position;
            Check(!city.HelpCat(farmer,.1f)&&farmer.Position==waiting&&cat.Cat!.Requested,"missing food releases worker while cat waits");
            cat.BrainTime=city.Rules.DayLength*Catalog.Data.Cats.WildStayDays;
            city.Scripting.UpdateAnimal(cat,.1f);Check(!cat.Cat!.Leaving,"requested taming pauses wild departure while waiting for supplies");
            farmer.Inventory[ResourceType.Food]=courier.Inventory[ResourceType.Food]=1;
            city.HelpCat(farmer,.1f);Check(cat.Cat!.Feeds==0,"remote food handoff cannot teleport");
            for(int i=0;i<2000;i++){city.HelpCat(farmer,.1f);city.HelpCat(courier,.1f);}
            Check(cat.Cat!.Feeds==3&&farmer.Inventory[ResourceType.Food]==0&&courier.Inventory[ResourceType.Food]==0,"both villagers walk to cat and hand over food: "+cat.Cat.Feeds+" "+farmer.Position+" / "+courier.Position);
            Check(!city.HelpCat(farmer,.1f),"farmer who already handed over food returns to normal duties");
            float chance=Catalog.Data.Cats.TameChance;
            try{Catalog.Data.Cats.TameChance=0;Check(!city.ResolveTaming(cat)&&!cat.Cat.Requested&&cat.Cat.Feeds==0,"failed taming consumes attempt and allows retry");
                city.RequestTame(cat);city.CatCarers(cat,farmer.Id,courier.Id);farmer.Inventory[ResourceType.Food]=courier.Inventory[ResourceType.Food]=1;
                city.HelpCat(farmer,.1f);city.HelpCat(courier,.1f);Catalog.Data.Cats.TameChance=1;
                Check(city.ResolveTaming(cat)&&cat.Cat.Tamed,"successful taming makes cat resident");}
            finally{Catalog.Data.Cats.TameChance=chance;}
            courier.Inventory[ResourceType.Milk]=1;city.HelpCat(courier,.1f);Check(cat.Cat!.Milk==1&&courier.Inventory[ResourceType.Milk]==0,"courier physically supplies milk");
            var creeper=city.SpawnMonster("creeper",3,3.1f)!;creeper.BrainTime=1;var previous=creeper.Position;
            city.Scripting.UpdateMonster(creeper,new MobScriptContext(city,creeper,.1f),.1f);
            Check(creeper.Health>0&&creeper.BrainTime==0&&Vector2.Distance(creeper.Position,cat.Position)>Vector2.Distance(previous,cat.Position),"creeper cancels fuse and flees cat");
            Check(!city.ScriptExplode(creeper,creeper.Position),"validated explosion refuses cat radius");
            city.DayTime=city.Rules.DayLength*22/24;Check(!city.IsSleepingAnimal(cat),"living cat remains active at night");
            string path=Path.Combine(Path.GetTempPath(),"villagecraft-cat-"+Guid.NewGuid()+".vcs");
            try{city.Save(path);using var loaded=City.Load(path);var saved=loaded.Cats.Single();Check(saved.Cat!.Tamed&&saved.Cat.Courier==courier.Id&&saved.Cat.Milk==cat.Cat.Milk,"cat status and supplies persist");}
            finally{File.Delete(path);}
            cat.Cat.Food=cat.Cat.Milk=0;cat.Cat.Unsupplied=city.Rules.DayLength*Catalog.Data.Cats.SupplyGraceDays;
            city.Scripting.UpdateAnimal(cat,.1f);Check(cat.Cat.Leaving,"unfed resident leaves rather than surviving for free");
            cat.Position=new(0,0);city.Scripting.UpdateAnimal(cat,.1f);city.Scripting.Flush();Check(!city.Animals.Contains(cat),"departing cat despawns at map edge");
            Check(city.Scripting.Errors.Count==0,"cat and creeper scripts remain valid");
        }
        using(var city=City.New(generateTerrain:false))
        {
            float chance=Catalog.Data.Cats.DawnSpawnChance;
            try
            {
                Catalog.Data.Cats.DawnSpawnChance=1;city.Day=2;city.DayTime=city.Rules.DayLength*.25f-.05f;city.Update(.1f);
                Check(city.Cats.Count()==1,"06:00 settlement makes one configured dawn spawn roll");
                city.Update(.1f);Check(city.Cats.Count()==1,"dawn roll does not repeat each frame");
                city.DayTime=city.Rules.DayLength*.75f;city.Update(.1f);Check(city.Cats.Count()==1,"18:00 settlement does not spawn cats");
            }
            finally{Catalog.Data.Cats.DawnSpawnChance=chance;}
        }
        using(var city=City.New(generateTerrain:false))
        {
            var parents=city.Citizens.Take(2).ToArray();city.Citizens.RemoveAll(v=>!parents.Contains(v));
            var home=city.Buildings.First(b=>b.Plan.Homes>=3);foreach(var parent in parents){parent.Home=home.Id;parent.Inventory[ResourceType.Food]=2;}
            parents[0].ChangeProfession("farmer");parents[1].ChangeProfession("deliveryman");
            Check(city.Place("farm",9,15),"newborn demand fixture");
            foreach(var building in city.Buildings)building.Storage[ResourceType.Food]=0;
            city.Stock[ResourceType.Food]=0;city.ReconcileStock();
            int next=city.NextId;Check(city.ScheduleBirth(home.Id),"eligible home schedules growth");city.Tick();
            var grown=city.Citizens.First(v=>v.Id>=next);
            Check(grown.Profession=="farmer"&&grown.Work>=0&&city.Buildings.First(b=>b.Id==grown.Work).Plan.Job=="Farming","grown baby takes vacant farm specialty for food demand: "+grown.Profession+" work="+grown.Work+" food="+city.Stock[ResourceType.Food]+" workers="+string.Join(",",city.Citizens.Select(v=>v.Profession+":"+v.Work))+" errors="+string.Join(";",city.Scripting.Errors));
        }
        Console.WriteLine("PASS: traveling projectiles, impact/misses, live cat AI, physical taming/feeding, creeper avoidance and persisted pet state.");
    }
}
