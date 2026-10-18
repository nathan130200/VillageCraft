namespace VillageCraft;

public enum CityProblemKind { MissingInput, Disconnected, Unstaffed, Damaged, Homeless, Hungry, WaterShortage, Hostiles, FullStorage }
public sealed record CityProblem(CityProblemKind Kind,ResourceType Resource,int[] Subjects)
{public int Count=>Subjects.Length;}

public partial class City
{
    public IReadOnlyList<CityProblem> GetProblems()
    {
        using var profile=Profiler.Measure(ProfileArea.Diagnostics);
        var groups=new Dictionary<(CityProblemKind,ResourceType),HashSet<int>>();
        void Add(CityProblemKind kind,int id,ResourceType resource=ResourceType.None)
        {
            if(!groups.TryGetValue((kind,resource),out var ids))groups[(kind,resource)]=ids=[];
            ids.Add(id);
        }
        var roads=Roads();var staffed=Citizens.Where(v=>v.Health>0&&v.Work>=0).Select(v=>v.Work).ToHashSet();
        foreach(var b in Buildings.Where(b=>b.Enabled&&!b.Plan.Natural))
        {
            if(b.Plan.NeedsPath&&!Connected(b,roads))Add(CityProblemKind.Disconnected,b.Id);
            if(b.Plan.Jobs>0&&!staffed.Contains(b.Id))Add(CityProblemKind.Unstaffed,b.Id);
            if(b.Health<b.Plan.Health&&!b.Plan.Indestructible)Add(CityProblemKind.Damaged,b.Id);
            foreach(var input in b.Plan.Input.Where(e=>!Currency(e.Key)&&Stored(b,e.Key)+.001f<e.Value))Add(CityProblemKind.MissingInput,b.Id,input.Key);
            if(!string.IsNullOrEmpty(b.Plan.Animal)&&Stored(b,ResourceType.Food)<2)Add(CityProblemKind.MissingInput,b.Id,ResourceType.Food);
            if(b.Plan.Warehouse&&StorageFree(b)<=0)Add(CityProblemKind.FullStorage,b.Id);
        }
        var homes=Buildings.Where(b=>b.Plan.Homes>0).Select(b=>b.Id).ToHashSet();
        foreach(var v in Citizens.Where(v=>v.Health>0))
        {
            if(!homes.Contains(v.Home))Add(CityProblemKind.Homeless,v.Id);
            if(v.Inventory.GetValueOrDefault(ResourceType.Food)<1)Add(CityProblemKind.Hungry,v.Id);
        }
        foreach(var v in Citizens.Where(v=>v.Health>0).Skip(Math.Max(0,Water)))Add(CityProblemKind.WaterShortage,v.Id);
        foreach(var m in Enemies.Where(m=>m.Health>0))Add(CityProblemKind.Hostiles,m.Id);
        return groups.OrderBy(g=>g.Key.Item1).ThenBy(g=>g.Key.Item2).Select(g=>new CityProblem(g.Key.Item1,g.Key.Item2,g.Value.Order().ToArray())).ToArray();
    }
}
