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
    public bool HarvestTree(Building tree,Citizen worker)
    {
        if(!Buildings.Contains(tree)||tree.Plan.Harvest.Count==0||tree.GrowthDays>0||!Citizens.Contains(worker)||Vector2.Distance(worker.Position,new(tree.X,tree.Z))>.45f)return false;
        var camp=Buildings.FirstOrDefault(b=>b.Id==worker.Work&&b.Plan.HarvestSource==tree.Prefab);
        var store=camp??tree;
        var profession=Catalog.Data.Professions[worker.Profession];
        if(StorageFree(store)<tree.Plan.Harvest.Sum(e=>e.Value*profession.ResourceBonus.GetValueOrDefault(e.Key,1)))return false;
        tree.GrowthDays=tree.Plan.RegrowDays;TutorialTreesHarvested++;
        foreach(var resource in tree.Plan.Harvest)RecordProduction(resource.Key,Receive(store,resource.Key,resource.Value*profession.ResourceBonus.GetValueOrDefault(resource.Key,1)));
        Stock[ResourceType.Goods]+=Catalog.Data.Economy.GoodsPerWorkCycle;
        Emit(new("onTreeHarvested",tree,Source:worker));return true;
    }
}
