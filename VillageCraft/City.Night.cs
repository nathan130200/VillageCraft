namespace VillageCraft;

public partial class City
{
    public long LastNightSpawnMinute {get;set;}=-1;
    void UpdateNightEncounters()
    {
        // Absolute game minutes preserve interval phase through midnight, speed changes and loading.
        double hour=DayTime/Rules.DayLength*24;
        if(hour>=5&&hour<18)return;
        long start=(Day-1L)*1440+(hour<5?-360:1080);
        long nightDay=(start-1080)/1440+1;
        double now=(Day-1L)*1440+DayTime/Rules.DayLength*1440;
        long slot=start+(long)Math.Floor((now-start)/Rules.NightSpawnIntervalMinutes)*Rules.NightSpawnIntervalMinutes;
        if(slot<=LastNightSpawnMinute)return;
        LastNightSpawnMinute=slot;
        if(nightDay<=PeaceDays||!Rules.NightMonsters||Rules.NightCount<=0)return;
        SpawnNight();
    }
}
