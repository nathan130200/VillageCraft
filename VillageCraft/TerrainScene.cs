using System.Numerics;
using Raylib_cs;
namespace VillageCraft;

static class TerrainScene
{
    readonly record struct Quad(Vector3 A,Vector3 B,Vector3 C,Vector3 D,float U,float V,float Shade);
    static City? cached;
    static readonly Dictionary<string,List<Quad>> surfaces=[];
    public static Vector2 ViewCenter;
    public static float ViewRadius=50;
    static void Add(string material,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float u,float v,float shade=1)
    {
        if(shade<1)(a,b,c,d)=(b,a,d,c);
        if(!surfaces.TryGetValue(material,out var list))surfaces[material]=list=[];
        list.Add(new(a*Scene.Scale,b*Scene.Scale,c*Scene.Scale,d*Scene.Scale,u,v,shade));
    }
    static void Cache(City city)
    {
        cached=city;surfaces.Clear();
        for(int z=0;z<city.Height;z++)
        {
            // Merge equal-height top surfaces across each row; large flat regions stay inexpensive.
            for(int x=0;x<city.Width;)
            {
                int end=x+1,h=city.Elevation(x,z);bool water=city.IsWater(x,z);
                while(end<city.Width&&end-x<4&&city.Elevation(end,z)==h&&city.IsWater(end,z)==water)end++;
                float y=h*TerrainData.BlockHeight;
                string texture=water?"block/sand.png":h>=3?"block/stone.png":Catalog.Data.Game.GroundTexture;
                Add(texture,new(x-.5f,y,z+.5f),new(end-.5f,y,z+.5f),new(end-.5f,y,z-.5f),new(x-.5f,y,z-.5f),(end-x)*4,4);
                if(water)Add("block/water_still.png",new(x-.5f,.015f,z+.5f),new(end-.5f,.015f,z+.5f),new(end-.5f,.015f,z-.5f),new(x-.5f,.015f,z-.5f),(end-x)*4,4);
                x=end;
            }
            for(int x=0;x<city.Width;x++)
            {
                float y=city.Elevation(x,z)*TerrainData.BlockHeight;
                string side=city.Elevation(x,z)>=3?"block/stone.png":Catalog.Data.Game.DirtTexture;
                void Face(int nx,int nz,int direction)
                {
                    float low=city.Land(nx,nz)?city.Elevation(nx,nz)*TerrainData.BlockHeight:-1;
                    if(low>=y)return;
                    if(direction==0)Add(side,new(x+.5f,low,z+.5f),new(x-.5f,low,z+.5f),new(x-.5f,y,z+.5f),new(x+.5f,y,z+.5f),4,(y-low)*4,.8f);
                    if(direction==1)Add(side,new(x-.5f,low,z-.5f),new(x+.5f,low,z-.5f),new(x+.5f,y,z-.5f),new(x-.5f,y,z-.5f),4,(y-low)*4,.72f);
                    if(direction==2)Add(side,new(x-.5f,low,z+.5f),new(x-.5f,low,z-.5f),new(x-.5f,y,z-.5f),new(x-.5f,y,z+.5f),4,(y-low)*4,.65f);
                    if(direction==3)Add(side,new(x+.5f,low,z-.5f),new(x+.5f,low,z+.5f),new(x+.5f,y,z+.5f),new(x+.5f,y,z-.5f),4,(y-low)*4,.88f);
                }
                Face(x,z+1,0);Face(x,z-1,1);Face(x-1,z,2);Face(x+1,z,3);
            }
        }
    }
    public static Vector3 PickGround(City city,Ray ray)
    {
        var bounds=new BoundingBox(new Vector3(-.5f,-1,-.5f)*Scene.Scale,new Vector3(city.Width-.5f,city.Terrain.MaximumHeight*TerrainData.BlockHeight+.5f,city.Height-.5f)*Scene.Scale);
        var collision=Raylib.GetRayCollisionBox(ray,bounds);
        if(collision.Hit)
        {
            const float step=.4f;float start=Math.Max(0,collision.Distance-step),last=start;
            float limit=start+(city.Width+city.Height+32)*Scene.Scale*2;
            float Ground(Vector3 point)
            {
                int x=(int)MathF.Floor(point.X/Scene.Scale+.5f),z=(int)MathF.Floor(point.Z/Scene.Scale+.5f);
                return city.Land(x,z)?(city.IsWater(x,z)?.015f:city.Elevation(x,z)*TerrainData.BlockHeight)*Scene.Scale:float.NegativeInfinity;
            }
            for(float distance=start;distance<limit;distance+=step)
            {
                var point=ray.Position+ray.Direction*distance;
                if(point.Y<=Ground(point))
                {
                    float low=last,high=distance;
                    for(int i=0;i<10;i++){float mid=(low+high)*.5f;var p=ray.Position+ray.Direction*mid;if(p.Y<=Ground(p))high=mid;else low=mid;}
                    return (ray.Position+ray.Direction*high)/Scene.Scale;
                }
                if(point.Y<-Scene.Scale)break;last=distance;
            }
        }
        return (ray.Position+ray.Direction*(-ray.Position.Y/ray.Direction.Y))/Scene.Scale;
    }
    public static void Draw(City city)
    {
        if(!ReferenceEquals(city,cached))Cache(city);
        foreach(var surface in surfaces.OrderBy(s=>s.Key=="block/water_still.png"?1:0))
        {
            bool water=surface.Key=="block/water_still.png";var texture=Assets.Get(surface.Key);
            var tint=surface.Key==Catalog.Data.Game.GroundTexture?Scene.ColorOf(Catalog.Data.Game.GroundTint):Color.White;
            if(water)tint=new(90,151,235,215);
            for(int begin=0;begin<surface.Value.Count;begin+=512)
            {
                int end=Math.Min(begin+512,surface.Value.Count);
                Rlgl.CheckRenderBatchLimit((end-begin)*4);
                Rlgl.SetTexture(GraphicsOptions.Current.Textures?texture.Id:0);Rlgl.Begin((int)DrawMode.Quads);
                for(int i=begin;i<end;i++)
                {
                    var q=surface.Value[i];
                    float flow=water&&GraphicsOptions.Current.AnimateWater?Scene.Time*.015f:0;
                    void Vertex(Vector3 v,float u,float t){float light=Scene.LightAt(v.X/Scene.Scale,v.Z/Scene.Scale)*q.Shade;Rlgl.Color4ub((byte)(tint.R*light),(byte)(tint.G*light),(byte)(tint.B*light),tint.A);Rlgl.TexCoord2f(u+flow,t+flow);Rlgl.Vertex3f(v.X,v.Y,v.Z);}
                    Vertex(q.A,0,q.V);Vertex(q.B,q.U,q.V);Vertex(q.C,q.U,0);Vertex(q.D,0,0);
                }
                Rlgl.End();Rlgl.SetTexture(0);
            }
        }
        // Decoration is culled once it becomes subpixel at a distance.
        if(GraphicsOptions.Current.Quality==0)return;
        for(int z=Math.Max(0,(int)(ViewCenter.Y-ViewRadius));z<Math.Min(city.Height,ViewCenter.Y+ViewRadius);z++)
        for(int x=Math.Max(0,(int)(ViewCenter.X-ViewRadius));x<Math.Min(city.Width,ViewCenter.X+ViewRadius);x++)
        {
            int kind=city.Terrain.Grass[z*city.Width+x];if(kind==0||city.At(x,z)!=null)continue;
            string model=kind==1?"wild_grass":kind==2?"wild_flower":"wild_grass";
            Scene.Model(model,x,z);
        }
    }
}
