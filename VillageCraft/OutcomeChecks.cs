using System.Numerics;
namespace VillageCraft;

static class OutcomeChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception("Sunlight/game over: "+name);}
    static City World()
    {var c=City.New(generateTerrain:false);c.Rules.NightMonsters=false;c.Rules.RaidsEnabled=false;return c;}
    public static void Run()
    {
        float chance=Catalog.Data.Sunlight.RegularHelmetChance;
        try
        {
            using var c=World();c.Citizens.Clear();Catalog.Data.Sunlight.RegularHelmetChance=0;
            var bare=c.SpawnMonster("zombie",2,2,night:true)!;var skeleton=c.SpawnMonster("skeleton",4,2,night:true)!;
            var immune=c.SpawnMonster("zombified_piglin",6,2)!;
            Check(!bare.LeatherHelmet&&!skeleton.LeatherHelmet,"zero chance gives unprotected regular monsters");
            Catalog.Data.Sunlight.RegularHelmetChance=1;var helmet=c.SpawnMonster("zombie",8,2,night:true)!;
            Check(helmet.LeatherHelmet,"configured regular helmet chance");
            c.DayTime=c.Rules.DayLength*.5f;float h=bare.Health,s=skeleton.Health,protectedHealth=helmet.Health,netherHealth=immune.Health;
            c.Update(.2f);
            Check(bare.Health<h&&skeleton.Health<s&&bare.BurningInSun,"exposed zombies and skeletons burn in daylight");
            Check(helmet.Health==protectedHealth&&!helmet.BurningInSun&&immune.Health==netherHealth,"helmet and fire-immune monster protection");
            c.DayTime=0;h=bare.Health;c.Update(.2f);Check(bare.Health==h&&!bare.BurningInSun,"midnight never burns monsters");
            Catalog.Data.Sunlight.RegularHelmetChance=0;c.SpawnWave();
            Check(c.Enemies.Where(m=>m.BossWave&&Catalog.Data.Units[m.Type].BurnsInSun).Any()&&c.Enemies.Where(m=>m.BossWave&&Catalog.Data.Units[m.Type].BurnsInSun).All(m=>m.LeatherHelmet),"boss-wave susceptible monsters always have helmets");
            bare.Health=.1f;c.DayTime=c.Rules.DayLength*.5f;float goods=c.Stock[ResourceType.Goods];c.Update(.2f);
            Check(!c.Enemies.Contains(bare)&&c.Statistics.SunDeaths==1&&c.Stock[ResourceType.Goods]==goods,"sunlight can kill without awarding labor Goods");
            Check(c.Place("cottage",9,15),"shelter fixture");var sheltered=c.SpawnMonster("zombie",9,15)!;
            Check(!c.ExposedToSun(sheltered),"roof provides shade");
        }
        finally{Catalog.Data.Sunlight.RegularHelmetChance=chance;}
        using(var midnight=World())
        {
            foreach(var v in midnight.Citizens.Skip(1).ToArray())midnight.RemoveVillager(v);
            var last=midnight.Citizens.Single();last.Health=.01f;last.Effects["Burning"]=2;midnight.DayTime=midnight.Rules.DayLength-.05f;
            midnight.Update(.1f);Check(midnight.GameOver&&midnight.Day==2&&midnight.DayTime<1,"defeat across midnight preserves a valid frozen clock");
        }
        string name="outcome-check-"+Guid.NewGuid().ToString("N"),path=SaveStore.PathFor(name);
        using var village=World();var ui=new GameUi();
        try
        {
            var helmet=village.SpawnMonster("skeleton",2,2,bossWave:true)!;
            village.Statistics.Deliveries=7;village.Statistics.Production[ResourceType.Wood]=42;village.Save(path);
            using(var saved=City.Load(path))Check(saved.Enemies.Single().LeatherHelmet&&saved.Enemies.Single().BossWave&&saved.Statistics.Deliveries==7&&saved.Statistics.Production[ResourceType.Wood]==42,"helmet, wave and statistics roundtrip");
            int population=village.Citizens.Count;
            foreach(var person in village.Citizens.ToArray()){village.DamageVillager(person,10000);}
            village.ResolveDeaths();
            Check(village.GameOver&&village.Citizens.Count==0&&village.Statistics.VillagersLost==population,"eliminating the final villager ends the game");
            float time=village.DayTime;int day=village.Day;village.Update(100);village.Tick();
            Check(village.DayTime==time&&village.Day==day&&village.Citizens.Count==0,"defeated simulation freezes without immigration");
            City retry=village;Check(ui.Retry(ref retry,name)&&retry!=village&&!retry.GameOver&&retry.Citizens.Count==population,"retry reloads the intact saved village");retry.Dispose();
            village.Save(path);using(var defeated=City.Load(path))Check(defeated.GameOver&&defeated.Statistics.VillagersLost==population,"defeat persists in binary saves");
            City cannotRetry=village;Check(!ui.Retry(ref cannotRetry,name)&&ReferenceEquals(cannotRetry,village),"retry rejects an already defeated snapshot");
            File.WriteAllText(Path.ChangeExtension(path,".json"),"legacy placeholder");
            File.WriteAllText(SaveStore.PathFor(name+"-other"),"unrelated save");
            SaveStore.Delete(name);Check(!File.Exists(path)&&!File.Exists(Path.ChangeExtension(path,".json"))&&File.ReadAllText(SaveStore.PathFor(name+"-other"))=="unrelated save","delete removes only the selected save representations");
        }
        finally{File.Delete(SaveStore.PathFor(name+"-other"));File.Delete(path);File.Delete(Path.ChangeExtension(path,".json"));}
        Console.WriteLine("PASS: sunlight, regular/boss helmets, shade, persistent statistics, defeat freeze, retry and save deletion.");
    }
}
