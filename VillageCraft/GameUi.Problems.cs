using System.Diagnostics;
using ImGuiNET;
namespace VillageCraft;

partial class GameUi
{
    IReadOnlyList<CityProblem> cityProblems=[];
    City? problemsCity;
    long problemsRefresh;
#if DEBUG
    internal void ShowProblems()=>page="Problems";
#endif
    void RefreshProblems(City city)
    {
        long now=Stopwatch.GetTimestamp();
        if(!ReferenceEquals(city,problemsCity)||now-problemsRefresh>Stopwatch.Frequency/2)
        {cityProblems=city.GetProblems();problemsCity=city;problemsRefresh=now;}
    }
    void ProblemsPanel(City city)
    {
        if(cityProblems.Count==0){Wrapped(L.T("problem.none"));return;}
        Wrapped(L.T("problem.help"));
        foreach(var problem in cityProblems)
        {
            string text=problem.Kind==CityProblemKind.MissingInput?L.F("problem.MissingInput",L.Resource(problem.Resource)):L.T("problem."+problem.Kind);
            if(!ImGui.TreeNode(L.F("problem.group",problem.Count,text)+"###problem_"+problem.Kind+problem.Resource))continue;
            foreach(int id in problem.Subjects)
            {
                bool resident=problem.Kind is CityProblemKind.Homeless or CityProblemKind.Hungry or CityProblemKind.WaterShortage;
                if(resident)
                {
                    var v=city.Citizens.FirstOrDefault(v=>v.Id==id);if(v!=null&&ImGui.Selectable(v.FullName+"##problem_"+id))Focus("villager",id);
                }
                else if(problem.Kind==CityProblemKind.Hostiles)
                {
                    var m=city.Enemies.FirstOrDefault(m=>m.Id==id);if(m!=null&&ImGui.Selectable(L.Entity(m.Type)+" #"+id))Focus("monster",id);
                }
                else
                {
                    var b=city.Buildings.FirstOrDefault(b=>b.Id==id);if(b!=null&&ImGui.Selectable(L.Name(b.Plan)+" #"+id))Focus("building",id);
                }
            }
            ImGui.TreePop();
        }
    }
}
