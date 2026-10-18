namespace VillageCraft;
static class NightChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("NIGHT: "+message);}
    static void At(City c,int day,float hour){c.Day=day;c.DayTime=c.Rules.DayLength*hour/24;c.Update(.001f);}
    public static void Run()
    {
        using var city=City.New(generateTerrain:false);city.Rules.RaidsEnabled=false;city.Rules.NightCount=1;city.Rules.NightSpawnIntervalMinutes=60;
        At(city,5,18);Check(city.Enemies.Count==0,"peaceful nights suppress the complete first five nights");
        At(city,6,1);Check(city.Enemies.Count==0,"night five remains peaceful after midnight");
        At(city,6,18);Check(city.Enemies.Count==1,"first eligible sunset encounter");
        At(city,6,18.5f);Check(city.Enemies.Count==1,"no early duplicate between hourly intervals");
        At(city,6,19);Check(city.Enemies.Count==2,"next interval spawns another group");
        At(city,6,23);Check(city.Enemies.Count==3,"a clock jump spawns the latest due group without a backlog burst");
        At(city,7,0);Check(city.Enemies.Count==4,"intervals continue across midnight");
        At(city,7,4);Check(city.Enemies.Count==5,"encounters continue before 05:00");
        At(city,7,4.9f);At(city,7,5);At(city,7,5.9f);Check(city.Enemies.Count==5,"no encounters at or after 05:00");
        city.Rules.MaxRaiders=5;At(city,7,18);Check(city.Enemies.Count==5,"hostile cap applies to recurring encounters");
        city.Enemies.Clear();At(city,7,18.1f);Check(city.Enemies.Count==0,"capped interval does not replay every frame");
        At(city,7,19);Check(city.Enemies.Count==1,"new interval can spawn after capacity becomes available");
        string path=Path.Combine(Path.GetTempPath(),"villagecraft-night-"+Guid.NewGuid()+".vcs");
        try
        {
            city.Save(path);using var loaded=City.Load(path);At(loaded,7,19.2f);Check(loaded.Enemies.Count==1&&loaded.Rules.NightSpawnIntervalMinutes==60,"saved interval phase prevents duplicate encounters");
            At(loaded,7,20);Check(loaded.Enemies.Count==2,"saved city continues its next scheduled encounter");
            loaded.Rules.NightMonsters=false;At(loaded,7,21);Check(loaded.Enemies.Count==2,"night encounter toggle suppresses recurring spawns");
        }
        finally{File.Delete(path);}
        city.DayTime=0;Check(city.Daylight== .4f&&city.IsNight,"midnight remains readable and still counts as night");
        city.DayTime=city.Rules.DayLength*.5f;Check(city.Daylight==1,"noon retains full brightness");
        Console.WriteLine("PASS: recurring 18:00–05:00 encounters, grace period, caps, midnight/save timing and brighter night lighting.");
    }
}
