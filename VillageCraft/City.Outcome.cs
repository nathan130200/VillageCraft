namespace VillageCraft;

public sealed class SunlightSettings
{
    public float RegularHelmetChance {get;set;}=.65f;
    public float DamagePerSecond {get;set;}=4;
}
public sealed class VillageStatistics
{
    public int RecordedSinceDay {get;set;}=1;
    public double SimulatedSeconds {get;set;}
    public int PeakPopulation {get;set;}
    public int Births {get;set;}
    public int Arrivals {get;set;}
    public int VillagersLost {get;set;}
    public int BuildingsPlaced {get;set;}
    public int BuildingsDestroyed {get;set;}
    public int Deliveries {get;set;}
    public int SunDeaths {get;set;}
    public float EmeraldsFromGoods {get;set;}
    public Dictionary<ResourceType,float> Production {get;set;}=[];
}
public partial class City
{
    public bool GameOver {get;set;}
    public VillageStatistics Statistics {get;set;}=new();
    public bool ExposedToSun(Creature monster)
    {
        // Burning uses solar strength, independent of the visual ambient-light setting.
        if(IsNight||(.16f+.84f*SunStrength)<.65f||IsWater((int)MathF.Round(monster.X),(int)MathF.Round(monster.Z)))return false;
        var cover=At((int)MathF.Round(monster.X),(int)MathF.Round(monster.Z));
        return cover==null||!cover.Plan.SheltersFromSun;
    }
    void UpdateSunlight(float dt)
    {
        foreach(var monster in Enemies.ToArray())
        {
            monster.BurningInSun=Catalog.Data.Units[monster.Type].BurnsInSun&&!monster.LeatherHelmet&&ExposedToSun(monster);
            if(!monster.BurningInSun||monster.Health<=0)continue;
            DamageMonster(monster,Catalog.Data.Sunlight.DamagePerSecond*dt);
            if(monster.Health<=0)monster.SunKilled=true;
        }
    }
    void LoseVillage()
    {
        if(GameOver)return;
        while(DayTime>=Rules.DayLength){DayTime-=Rules.DayLength;Day++;}
        GameOver=true;Emit(new("onGameOver"));Log(L.T("gameover.description"));
    }
    void RecordProduction(ResourceType resource,float amount)
    {Statistics.Production[resource]=Statistics.Production.GetValueOrDefault(resource)+amount;}
    void ValidateOutcome()
    {
        var s=Statistics;
        if(s==null||s.RecordedSinceDay<1||s.RecordedSinceDay>Day||!double.IsFinite(s.SimulatedSeconds)||s.SimulatedSeconds<0||
            new[]{s.PeakPopulation,s.Births,s.Arrivals,s.VillagersLost,s.BuildingsPlaced,s.BuildingsDestroyed,s.Deliveries,s.SunDeaths}.Any(n=>n<0)||
            !float.IsFinite(s.EmeraldsFromGoods)||s.EmeraldsFromGoods<0||s.Production==null||s.Production.Count>64||s.Production.Any(e=>!Catalog.Resources.Contains(e.Key)||!float.IsFinite(e.Value)||e.Value<0))
            throw new InvalidDataException("Invalid village statistics.");
        s.PeakPopulation=Math.Max(s.PeakPopulation,Citizens.Count);
        if(Citizens.Count==0)GameOver=true;
        if(GameOver&&Citizens.Count>0)throw new InvalidDataException("A defeated village cannot contain living villagers.");
    }
}
