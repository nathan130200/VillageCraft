namespace VillageCraft;
static class PopulationChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("POPULATION: "+message);}
    public static void Run()
    {
        using var city=City.New(generateTerrain:false);city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;city.Rules.BirthChance=1;
        int before=city.Citizens.Count;city.Tick();
        Check(city.Babies.Count==4&&city.Citizens.Count==before,"birth rolls reserve four free beds; newborns remain hidden");
        string save=Path.Combine(Path.GetTempPath(),"villagecraft-babies-"+Guid.NewGuid()+".vcs");
        try
        {
            city.Save(save);using var loaded=City.Load(save);
            Check(loaded.Babies.Count==4&&loaded.Rules.BirthChance==1,"pending births and chance survive binary save");
            loaded.Tick();Check(loaded.Statistics.Births==4&&loaded.Citizens.Count>=before+4&&loaded.Babies.Count==0,"babies fully mature next day");
        }
        finally{File.Delete(save);}
        using var none=City.New(generateTerrain:false);none.Rules.NightMonsters=none.Rules.RaidsEnabled=false;none.Rules.BirthChance=0;none.Tick();Check(none.Babies.Count==0,"zero chance disables births");
        using var jobs=City.StartVillage(seed:4,tutorial:false);
        foreach(var v in jobs.Citizens)v.Profession="citizen";
        jobs.ReassignProfessions();
        Check(jobs.Citizens.Any(v=>v.Profession=="deliveryman"&&v.Work>=0)&&jobs.Citizens.Any(v=>v.Profession=="farmer"&&v.Work>=0),"bulk action creates courier and farmer specialists");
        Check(jobs.Citizens.All(v=>v.Work<0||v.Profession!="citizen"),"bulk action fills assigned workplaces with specialists");
        Check(!Catalog.Resources.Any(r=>r.ToString()=="Torches")&&!Catalog.Data.Prefabs.Any(p=>p.Id.StartsWith("torch",StringComparison.Ordinal)),"torch resources and prefabs removed");
        Check(jobs.Scripting.Errors.Count==0,"population and employment scripts run cleanly");
        Console.WriteLine("PASS: configurable births, hidden one-day growth, binary persistence, bulk profession reassignment and torch removal.");
    }
}
