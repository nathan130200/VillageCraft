namespace VillageCraft;

public sealed class GrowingBaby
{
    public int Home {get;set;}
    public int DueDay {get;set;}
}
public partial class City
{
    public Dictionary<string,int> ProfessionPriorities {get;set;}=[];
    public int ProfessionPriority(string id)=>ProfessionPriorities.GetValueOrDefault(id,3);
    public void SetProfessionPriority(string id,int priority)
    {
        if(!Catalog.Data.Professions.ContainsKey(id)||priority is <1 or >5)throw new ArgumentException("Invalid profession priority.");
        ProfessionPriorities[id]=priority;ReassignProfessions();
    }
    public List<GrowingBaby> Babies {get;set;}=[];
    public bool ScheduleBirth(int homeId)
    {
        if(GameOver||Citizens.Count+Babies.Count>=Rules.MaxPopulation)return false;
        var home=Buildings.FirstOrDefault(b=>b.Id==homeId&&b.Enabled&&b.Plan.Homes>0&&Connected(b,Roads()));
        int parents=Citizens.Count(v=>v.Home==homeId&&v.Health>0);
        if(home==null||parents<2||parents+Babies.Count(b=>b.Home==homeId)>=home.Plan.Homes)return false;
        Babies.Add(new(){Home=homeId,DueDay=Day+1});Emit(new("onBabyBorn",home));return true;
    }
    void MatureBabies()
    {
        foreach(var baby in Babies.Where(b=>b.DueDay<=Day).ToArray())
        {
            Babies.Remove(baby);if(Citizens.Count>=Rules.MaxPopulation)continue;
            var home=Buildings.FirstOrDefault(b=>b.Id==baby.Home&&b.Plan.Homes>Citizens.Count(v=>v.Home==b.Id))??Buildings.FirstOrDefault(b=>b.Plan.Homes>Citizens.Count(v=>v.Home==b.Id));
            var v=new Citizen{Id=NextId++,Home=home?.Id??-1,X=home?.X??AnchorX,Z=home?.Z??AnchorZ,Profession="citizen"};
            CitizenNames.Ensure(v);Citizens.Add(v);Statistics.Births++;Statistics.PeakPopulation=Math.Max(Statistics.PeakPopulation,Citizens.Count);
            Emit(new("onVillagerBorn",v));Emit(new("onVillagerSpawned",v,Reason:"born"));
        }
    }
    public void ReassignProfessions()=>Scripting.ReassignProfessions();
}
