namespace VillageCraft;
static class PopulationChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("POPULATION: "+message);}
    public static void Run()
    {
        using var city=City.New(generateTerrain:false);city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;city.Rules.BirthChance=1;
        int before=city.Citizens.Count;
        int vacancies=city.Buildings.Where(b=>b.Plan.Homes>0&&city.Citizens.Count(v=>v.Home==b.Id)>=2).Sum(b=>Math.Max(0,b.Plan.Homes-city.Citizens.Count(v=>v.Home==b.Id)));
        city.Tick();
        Check(city.Babies.Count==vacancies&&city.Citizens.Count==before,"100% per-bed rolls reserve all eligible vacant beds; newborns remain hidden");
        Check(!city.ScheduleBirth(city.Babies[0].Home),"pending births reserve beds and prevent overfilling");
        string save=Path.Combine(Path.GetTempPath(),"villagecraft-babies-"+Guid.NewGuid()+".vcs");
        try
        {
            city.Save(save);using var loaded=City.Load(save);
            Check(loaded.Babies.Count==vacancies&&loaded.Rules.BirthChance==1,"pending births and chance survive binary save");
            loaded.Tick();Check(loaded.Statistics.Births==vacancies&&loaded.Citizens.Count>=before+vacancies&&loaded.Babies.Count==0,"babies fully mature next day");
        }
        finally{File.Delete(save);}
        using var none=City.New(generateTerrain:false);none.Rules.NightMonsters=none.Rules.RaidsEnabled=false;none.Rules.BirthChance=0;none.Tick();Check(none.Babies.Count==0,"zero chance disables births");
        using var jobs=City.StartVillage(seed:4,tutorial:false);
        foreach(var v in jobs.Citizens)v.Profession="citizen";
        jobs.ReassignProfessions();
        Check(jobs.Citizens.Any(v=>v.Profession=="deliveryman"&&v.Work>=0)&&jobs.Citizens.Any(v=>v.Profession=="farmer"&&v.Work>=0),"bulk action creates courier and farmer specialists");
        Check(jobs.Citizens.All(v=>v.Work<0||v.Profession!="citizen"),"bulk action fills assigned workplaces with specialists");
        jobs.Citizens.RemoveRange(2,jobs.Citizens.Count-2);
        jobs.SetProfessionPriority("farmer",1);jobs.SetProfessionPriority("keeper",5);
        Check(jobs.Citizens.Any(v=>v.Profession=="deliveryman")&&jobs.Citizens.Any(v=>v.Profession=="farmer"),"highest priority farmer gets scarce labor after reserving courier");
        jobs.SetProfessionPriority("farmer",5);jobs.SetProfessionPriority("keeper",1);
        Check(jobs.Citizens.Any(v=>v.Profession=="deliveryman")&&jobs.Citizens.Any(v=>v.Profession=="keeper"),"changing priorities automatically reallocates scarce labor");
        jobs.Assign();Check(jobs.Citizens.Any(v=>v.Profession=="keeper"),"automatic assignment continues to respect the policy");
        string prioritySave=Path.Combine(Path.GetTempPath(),"villagecraft-priorities-"+Guid.NewGuid()+".vcs");
        try{jobs.Save(prioritySave);using var loaded=City.Load(prioritySave);Check(loaded.ProfessionPriority("keeper")==1&&loaded.ProfessionPriority("farmer")==5,"profession priorities survive binary save/load");}
        finally{File.Delete(prioritySave);}
        Check(!Catalog.Resources.Any(r=>r.ToString()=="Torches")&&!Catalog.Data.Prefabs.Any(p=>p.Id.StartsWith("torch",StringComparison.Ordinal)),"torch resources and prefabs removed");
        Check(jobs.Scripting.Errors.Count==0,"population and employment scripts run cleanly");
        Console.WriteLine("PASS: configurable births, hidden one-day growth, binary persistence, bulk profession reassignment and torch removal.");
    }
}
