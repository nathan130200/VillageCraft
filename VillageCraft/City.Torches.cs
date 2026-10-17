using System.Numerics;
namespace VillageCraft;

public partial class City
{
    bool lightingDirty=true;
    float[] illumination=[];
    public float TorchLightAt(float x,float z)
    {
        if(lightingDirty||illumination.Length!=Width*Height)
        {
            illumination=new float[Width*Height];lightingDirty=false;
            foreach(var torch in Buildings.Where(b=>b.TorchLit&&b.Plan.LightRadius>0))
            {
                float radius=torch.Plan.LightRadius;
                for(int tz=Math.Max(0,(int)(torch.Z-radius));tz<=Math.Min(Height-1,torch.Z+radius);tz++)
                for(int tx=Math.Max(0,(int)(torch.X-radius));tx<=Math.Min(Width-1,torch.X+radius);tx++)
                {
                    float light=1-Vector2.Distance(new(tx,tz),new(torch.X,torch.Z))/radius;
                    illumination[tz*Width+tx]=Math.Max(illumination[tz*Width+tx],light);
                }
            }
        }
        int ix=(int)MathF.Round(x),iz=(int)MathF.Round(z);
        return Land(ix,iz)?illumination[iz*Width+ix]:0;
    }
    public bool SetTorch(Building torch,bool lit)
    {
        if(!Buildings.Contains(torch)||torch.Plan.LightRadius<=0||torch.TorchLit==lit)return false;
        torch.TorchLit=lit;lightingDirty=true;Emit(new("onTorchChanged",torch));return true;
    }
    bool UpdateTorchDuty(Citizen v,float dt)
    {
        bool light=DayPhase>=17f/24||DayPhase<6f/24;
        // Villagers visit the fixtures at dusk and dawn. Their flames never consume fuel.
        var torch=Buildings.FirstOrDefault(b=>b.Id==v.TorchTarget&&b.Plan.LightRadius>0&&b.TorchLit!=light);
        if(torch==null)
        {
            ReleaseTorchDuty(v);
            foreach(var candidate in Buildings.Where(b=>b.Plan.LightRadius>0&&b.TorchLit!=light&&!Citizens.Any(other=>other.Id!=v.Id&&other.TorchTarget==b.Id))
                .OrderBy(b=>Vector2.DistanceSquared(v.Position,new(b.X,b.Z))))
            {
                var route=Route(v.Position,new(candidate.X,candidate.Z),false);
                bool sameCell=MathF.Round(v.X)==candidate.X&&MathF.Round(v.Z)==candidate.Z;
                if(!sameCell&&route.Count==0)continue;
                torch=candidate;v.TorchTarget=torch.Id;v.RouteTarget=torch.Id;v.Route=route;break;
            }
        }
        if(torch==null)return false;
        // A path can disappear while the villager is travelling (e.g. a new wall).
        if(v.Route.Count==0&&(MathF.Round(v.X)!=torch.X||MathF.Round(v.Z)!=torch.Z)&&Route(v.Position,new(torch.X,torch.Z),false).Count==0)
        {ReleaseTorchDuty(v);return false;}
        v.Task=light?"Lighting torches":"Extinguishing torches";
        if(WalkTo(v,torch,dt,tolerance:.4f)){SetTorch(torch,light);ReleaseTorchDuty(v);}
        return true;
    }
    static void ReleaseTorchDuty(Citizen v)
    {
        if(v.TorchTarget<0)return;
        if(v.RouteTarget==v.TorchTarget){v.Route.Clear();v.RouteTarget=-1;}
        v.TorchTarget=-1;
    }
}
