namespace VillageCraft;
static class VillagerHealthChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("VILLAGER HEALTH: "+message);}
    public static void Run()
    {
        var previous=Catalog.Data.Professions.ToDictionary(e=>e.Key,e=>e.Value.BaseHealth);
        string path=Path.Combine(Path.GetTempPath(),"villagecraft-health-"+Guid.NewGuid()+".vcs");
        try
        {
            int i=0;
            foreach(var plan in Catalog.Data.Professions.Values)
            {
                plan.BaseHealth=60+10*i++;
                var person=new Citizen{Profession=plan.Id};
                Check(person.Health==plan.BaseHealth&&person.MaximumHealth==plan.BaseHealth,"every profession spawns at its own configured health");
            }
            Catalog.Data.Professions["warrior"].BaseHealth=180;Catalog.Data.Professions["farmer"].BaseHealth=80;
            using var city=City.New(generateTerrain:false);city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;
            var villager=city.Citizens[0];villager.ChangeProfession("warrior");villager.Health=90;
            var script=new ScriptVillager(city,villager);
            Check(script.maximumHealth==180&&script.setProfession("farmer")&&villager.Health==40,"script profession changes preserve injury percentage");
            villager.Health=75;script.heal(25);Check(villager.Health==80,"script healing caps at profession maximum");
            villager.ChangeProfession("warrior");villager.Health=175;villager.Inventory[ResourceType.Milk]=1;
            Check(script.drinkMilk()&&villager.Health==180,"milk heals above 50 HP and caps at profession maximum");
            villager.Health=120;city.Save(path);
            using(var loaded=City.Load(path))Check(loaded.Citizens.Single(v=>v.Id==villager.Id).Health==120,"saved injury is not healed or scaled during loading");
            Catalog.Data.Professions["warrior"].BaseHealth=100;
            using(var loaded=City.Load(path))Check(loaded.Citizens.Single(v=>v.Id==villager.Id).Health==100,"reduced content health clamps existing save health");
        }
        finally{foreach(var entry in previous)Catalog.Data.Professions[entry.Key].BaseHealth=entry.Value;File.Delete(path);}
        Console.WriteLine("PASS: per-profession base health, percentage-preserving job changes, healing, milk and saved injuries.");
    }
}
