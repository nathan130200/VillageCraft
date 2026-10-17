namespace VillageCraft;
public sealed class TutorialFile {public List<TutorialStage> Stages {get;set;}=[];}
public sealed class TutorialStage
{
    public string Id {get;set;}=string.Empty;
    public string Title {get;set;}=string.Empty;
    public string Description {get;set;}=string.Empty;
    public string Goal {get;set;}=string.Empty;
}
public partial class City
{
    public bool TutorialRoadBuilt {get;set;}
    public int TutorialTreesHarvested {get;set;}
    public bool TutorialGoalMet(string goal)=>goal switch
    {
        "inspect"=>TutorialInspected,
        "road"=>TutorialRoadBuilt,
        "trees"=>TutorialTreesHarvested>0,
        "water"=>Water>=Citizens.Count,
        "homes"=>Capacity>=Citizens.Count+4,
        "defense"=>Buildings.Any(b=>b.Enabled&&!b.Plan.CreatesGuardian&&b.Plan.Defender!=string.Empty&&Connected(b,Roads())&&Citizens.Any(v=>v.Work==b.Id)),
        _=>false
    };
}
