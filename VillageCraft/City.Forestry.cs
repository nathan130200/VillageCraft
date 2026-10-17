using System.Numerics;
namespace VillageCraft;

public partial class City
{
    void PopulateTrees()
    {
        var settings=Catalog.Data.Terrain;var rng=new Random(Terrain.Seed^0x51A7);var occupied=Buildings.Select(b=>b.Cell).ToHashSet();
        bool Free(int x,int z)=>Walkable(x,z)&&!occupied.Contains((x,z))&&(Math.Abs(x-AnchorX)>settings.StarterHalfWidth||Math.Abs(z-AnchorZ)>settings.StarterHalfDepth)&&Elevation(x,z)<3;
        for(int z=0;z<Height;z++)for(int x=0;x<Width;x++)
            if(Free(x,z)&&rng.NextDouble()<settings.TreeDensity)
            {var tree=Add(settings.TreePrefab,x,z);tree.Tags="natural tree";occupied.Add((x,z));}
        // A dependable starter grove within reach of the initial lumber camp.
        foreach(var (x,z) in new[]{(AnchorX-11,AnchorZ-1),(AnchorX-11,AnchorZ+1),(AnchorX-10,AnchorZ)})
            if(Free(x,z)){var tree=Add(settings.TreePrefab,x,z);tree.Tags="natural tree";occupied.Add((x,z));}
    }
    void UpdateForester(Citizen v,Building camp,float dt)
    {
        var tree=Buildings.FirstOrDefault(b=>b.Id==v.ResourceTarget&&b.Prefab==camp.Plan.HarvestSource&&b.GrowthDays==0);
        if(tree==null)
        {
            var reserved=Citizens.Where(other=>other.Id!=v.Id).Select(other=>other.ResourceTarget).ToHashSet();
            foreach(var candidate in Buildings.Where(b=>b.Prefab==camp.Plan.HarvestSource&&b.GrowthDays==0&&!reserved.Contains(b.Id)&&Vector2.DistanceSquared(new(camp.X,camp.Z),new(b.X,b.Z))<=camp.Plan.HarvestRadius*camp.Plan.HarvestRadius)
                .OrderBy(b=>Vector2.DistanceSquared(v.Position,new(b.X,b.Z))))
            {
                var route=Route(v.Position,new(candidate.X,candidate.Z),false);
                if(route.Count==0&&Vector2.Distance(v.Position,new(candidate.X,candidate.Z))>.25f)continue;
                tree=candidate;v.Route=route;v.ResourceTarget=tree.Id;v.RouteTarget=tree.Id;break;
            }
        }
        if(tree==null){v.Task=TaskId.WaitingTreeRegrowth;v.HasWorked=true;return;}
        var target=new Vector2(tree.X,tree.Z+.24f);float distance=Vector2.Distance(v.Position,target);
        if(distance>.15f)
        {
            v.Task=TaskId.GoingToTrees;
            if(v.Route.Count==0)v.Route=Route(v.Position,target,false);
            if(v.Route.Count>0){Move(v,Toward(v.Position,v.Route[0],dt*.85f*TravelSpeed(v.Position,"Woodcutting")));if(Vector2.Distance(v.Position,v.Route[0])<.005f)v.Route.RemoveAt(0);}
            else if(distance<.7f)Move(v,Toward(v.Position,target,dt*.85f));
            return;
        }
        v.Task=TaskId.ChoppingTree;v.WorkTime+=dt*Efficiency(v)*ToolEfficiency;
        camp.Attendance+=dt;camp.WorkEffort+=dt*Efficiency(v)*ToolEfficiency;
        if(v.WorkTime<Rules.ProductionCycle)return;
        v.WorkTime-=Rules.ProductionCycle;
        if(HarvestTree(tree,v))
        {camp.Worked=true;v.HasWorked=true;Scripting.BuildingWork(camp,v);}
        v.ResourceTarget=-1;v.Route.Clear();
    }
    public bool HarvestTree(Building tree,Citizen worker)
    {
        if(!Buildings.Contains(tree)||tree.Plan.Harvest.Count==0||tree.GrowthDays>0||!Citizens.Contains(worker)||Vector2.Distance(worker.Position,new(tree.X,tree.Z))>.45f)return false;
        var camp=Buildings.FirstOrDefault(b=>b.Id==worker.Work&&b.Plan.HarvestSource==tree.Prefab);
        var store=camp??tree;
        var profession=Catalog.Data.Professions[worker.Profession];
        if(StorageFree(store)<tree.Plan.Harvest.Sum(e=>e.Value*profession.ResourceBonus.GetValueOrDefault(e.Key,1)))return false;
        tree.GrowthDays=tree.Plan.RegrowDays;TutorialTreesHarvested++;
        foreach(var resource in tree.Plan.Harvest)RecordProduction(resource.Key,Receive(store,resource.Key,resource.Value*profession.ResourceBonus.GetValueOrDefault(resource.Key,1)));
        Stock["Goods"]+=Catalog.Data.Economy.GoodsPerWorkCycle;
        Emit(new("onTreeHarvested",tree,Source:worker));return true;
    }
}
