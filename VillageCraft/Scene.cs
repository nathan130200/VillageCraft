using System.Numerics;
using Raylib_cs;
namespace VillageCraft;

static class Scene
{
    public const float Scale=4;
    public static City? World;
    public static float Time;
    public static float Daylight=1;
    static float modelLight=-1;
    public static float LightAt(float x,float z)=>WorldLighting.Active?1:Daylight;
    static Color Lit(Color c,float? illumination=null)
    {float light=illumination??(modelLight>=0?modelLight:Daylight);return new((int)(c.R*light),(int)(c.G*light),(int)(c.B*(.25f+.75f*light)),c.A);}
    public static Color ColorOf(string hex,byte alpha=255)=>new(Convert.ToByte(hex.Substring(1,2),16),Convert.ToByte(hex.Substring(3,2),16),Convert.ToByte(hex.Substring(5,2),16),alpha);
    static void Box(float x,float y,float z,float w,float h,float d,string color,byte alpha=255,string texture="")
    {
        y+=World?.SurfaceY(x,z)??0;
        if(GraphicsOptions.Current.Textures&&texture!=string.Empty)
        {
            var asset=Assets.Get(texture);if(asset.Id!=0)
            {
                float frame=asset.Height>asset.Width&&GraphicsOptions.Current.AnimateWater?(int)(Time*5)%(asset.Height/asset.Width)*(float)asset.Width/asset.Height:0;
                Assets.Cube(asset,new Vector3(x,y,z)*Scale,new Vector3(w,h,d)*Scale,Lit(ColorOf(color,alpha),World==null&&modelLight>=0?modelLight:LightAt(x,z)),frame:frame);return;
            }
        }
        Raylib.DrawCube(new Vector3(x,y,z)*Scale,w*Scale,h*Scale,d*Scale,Lit(ColorOf(color,alpha),World==null&&modelLight>=0?modelLight:LightAt(x,z)));
    }
    public static void Model(string model,float x,float z,int rotation=0,float size=1,byte alpha=255,float y=0,float? heading=null)
    {
        float terrainY=World?.SurfaceY(x,z)??0;
        float previousLight=modelLight;modelLight=LightAt(x,z);
        Assets.BeginCutouts();
        foreach(var part in Catalog.Data.Models[model])
        {
            float px=part.Position[0]*size,pz=part.Position[2]*size;float w=part.Size[0]*size,d=part.Size[2]*size;
            float angle=heading??rotation*MathF.PI/2;float cosine=MathF.Cos(angle),sine=MathF.Sin(angle);(px,pz)=(px*cosine-pz*sine,px*sine+pz*cosine);
            if(GraphicsOptions.Current.Textures&&part.Texture!=string.Empty&&Assets.Get(part.Texture).Id!=0)
            {
                var tint=part.Texture.StartsWith("models/armor/leather")?ColorOf(part.Color,alpha):part.Texture.Contains("grass")||part.Texture.Contains("leaves")?ColorOf("#85B455",alpha):new Color(255,255,255,(int)alpha);
                if(part.Shape=="cross")
                {
                    Assets.Cross(Assets.Get(part.Texture),new Vector3(x+px,y+terrainY+part.Position[1]*size,z+pz)*Scale,new Vector3(part.Size[0],part.Size[1],part.Size[2])*size*Scale,Lit(tint),angle);
                    continue;
                }
                Assets.Cube(Assets.Get(part.Texture),new Vector3(x+px,y+terrainY+part.Position[1]*size,z+pz)*Scale,new Vector3(part.Size[0],part.Size[1],part.Size[2])*size*Scale,Lit(tint),part.UvOrigin,part.UvSize,rotation,heading:angle,stretch:part.TextureMode=="stretch",pitch:part.Pitch);
            }
            else
            {
                Rlgl.PushMatrix();Rlgl.Translatef((x+px)*Scale,(y+terrainY+part.Position[1]*size)*Scale,(z+pz)*Scale);Rlgl.Rotatef(-angle*180/MathF.PI,0,1,0);Rlgl.Rotatef(part.Pitch,1,0,0);
                var world=World;World=null;Box(0,0,0,w,part.Size[1]*size,d,part.Color,alpha);World=world;Rlgl.PopMatrix();
            }
        }
        Assets.EndCutouts();modelLight=previousLight;
    }
    public static void Terrain(City city)=>TerrainScene.Draw(city);
    public static void Build(City city,Building b,HashSet<(int,int)> roads)
    {
        if(b.Plan.Natural&&Vector2.DistanceSquared(new(b.X,b.Z),TerrainScene.ViewCenter)>TerrainScene.ViewRadius*TerrainScene.ViewRadius*2.25f)return;
        if(GraphicsOptions.Current.Shadows&&GraphicsOptions.Current.Quality>0&&!b.Plan.Path)Box(b.X+.12f,.002f,b.Z+.14f,1.05f,.007f,1.05f,"#18291C",65);
        if(b.Plan.Harvest.Count>0&&b.GrowthDays>0)
        {
            Model("tree_stump",b.X,b.Z);
            float progress=1-(float)b.GrowthDays/b.Plan.RegrowDays;
            if(progress>0)Model(b.Plan.Model,b.X,b.Z,size:.2f+progress*.5f);
        }
        else Model(b.Plan.Model,b.X,b.Z,b.Rotation);
        if(b.Health<b.Plan.Health){float ratio=b.Health/b.Plan.Health;Box(b.X,1.65f,b.Z,.8f,.035f,.04f,"#493D36");Box(b.X-(1-ratio)*.4f,1.66f,b.Z,.8f*ratio,.04f,.05f,"#CA795C");}
        if(b.Plan.NeedsPath&&!b.Plan.Path&&!city.Connected(b,roads))Box(b.X,1.5f,b.Z,.13f,.13f,.13f,"#E9AF56");
    }
    static Building? VillagerRoom(City city,Citizen v)
    {
        int x=(int)MathF.Round(v.X),z=(int)MathF.Round(v.Z);Building? nearest=null;float distance=1.21f;
        for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
        {
            var b=city.At(x+dx,z+dz);if(b?.Plan.Indoor!=true)continue;
            float d=Vector2.DistanceSquared(v.Position,new(b.X,b.Z));if(d<distance){distance=d;nearest=b;}
        }
        return nearest;
    }
    public static bool VillagerVisible(City city,Citizen v)
    {
        var room=VillagerRoom(city,v);
        return room==null||Vector2.DistanceSquared(v.Position,new(room.X,room.Z))>.62f*.62f;
    }
    public static Vector2 VillagerPosition(City city,Citizen v)
    {
        var room=VillagerRoom(city,v);
        float strength=room==null?1:Math.Clamp((Vector2.Distance(v.Position,new(room.X,room.Z))-.62f)/.48f,0,1);
        return EntityOffsets.Position(city,v.Position,v.Id,strength);
    }
    public static Vector2 CreaturePosition(City city,Creature c)=>city.Animals.Contains(c)?c.Position:EntityOffsets.Position(city,c.Position,c.Id);
    public static void Villager(City city,Citizen v,float time)
    {
        if(!VillagerVisible(city,v))return;
        var b=city.Buildings.FirstOrDefault(b=>b.Id==v.Work);string model=Catalog.Data.Professions[v.Profession].Model;
        var pos=v.Position;bool working=b!=null&&Vector2.Distance(pos,new(b.X,b.Z))<.1f;
        float heading=working?(b!.Rotation+2)*MathF.PI/2:v.Facing;
        pos=VillagerPosition(city,v);
        float bob=Tasks.Moving(v.Task)?MathF.Abs(MathF.Sin(time*7+v.Id))*.02f:0;
        if(GraphicsOptions.Current.Shadows&&GraphicsOptions.Current.Quality==2&&!WorldLighting.Active)Box(pos.X,.008f,pos.Y,.18f,.006f,.18f,"#18291C",65);
        Model(model,pos.X,pos.Y,0,.46f,255,bob,heading);
        if(working&&b!.Plan.Output.Count>0)
        {
            float spark=MathF.Abs(MathF.Sin(time*5+v.Id));
            Box(pos.X+.13f,.22f+spark*.18f,pos.Y+.12f,.04f,.04f,.04f,b.Plan.Job=="Forging"?"#FFBB59":"#D4BF87");
        }
    }
    public static void Creature(Creature c,bool enemy=false)
    {
        if(World?.IsNight==true&&World.Animals.Contains(c))return;
        var pos=World==null?c.Position:CreaturePosition(World,c);
        string model=Catalog.Data.Units.TryGetValue(c.Type,out var unit)?unit.Model:c.Type;
        float scale=unit!=null?.52f:c.Age<3?.42f:.7f;
        Model(model,pos.X,pos.Y,0,scale,heading:c.Facing);
        if(enemy&&c.LeatherHelmet)Model(c.Type is "piglin" or "piglin_brute" or "zombified_piglin" or "nether_warlord"?"leather_helmet_piglin":"leather_helmet",pos.X,pos.Y,0,scale,heading:c.Facing);
        if(enemy&&c.BurningInSun)Model("monster_fire",pos.X,pos.Y,0,scale,heading:c.Facing);
        if(enemy)Box(pos.X,.8f,pos.Y,.25f,.025f,.03f,"#DC715D");
    }
    public static void Preview(City city,string id,int x,int z,int rotation)
    {
        if(!city.Land(x,z))return;
        bool valid=city.PlacementIssue(id,x,z,rotation)==string.Empty;var color=ColorOf(valid?"#A7E29C":"#E37E69");
        Model(Catalog.Get(id).Model,x,z,rotation,1,120);
        Raylib.DrawCubeWires(new Vector3(x,.05f+city.SurfaceY(x,z),z)*Scale,Scale,.15f,Scale,color);
        if(Catalog.Get(id).NeedsPath&&!Catalog.Get(id).Path)
        {
            var b=new Building{X=x,Z=z,Rotation=rotation};var p=b.Entrance;
            Raylib.DrawLine3D(new Vector3(x,.13f+city.SurfaceY(x,z),z)*Scale,new Vector3(p.Item1,.13f+city.SurfaceY(p.Item1,p.Item2),p.Item2)*Scale,color);
            Raylib.DrawCubeWires(new Vector3(p.Item1,.07f+city.SurfaceY(p.Item1,p.Item2),p.Item2)*Scale,Scale*.8f,.12f,Scale*.8f,color);
        }
    }
    public static void Projectiles(City city)
    {
        // Short, bounded block fragments on actual hits; no permanent particle entities.
        foreach(var effect in city.BuildingDamageEffects)
        {
            var building=city.Buildings.FirstOrDefault(b=>b.Id==effect.Key);if(building==null)continue;
            float age=.7f-effect.Value;
            for(int i=0;i<8;i++)
            {
                float angle=i*MathF.Tau/8+building.Id;
                var p=new Vector3(building.X+MathF.Cos(angle)*(.25f+age*.8f),
                    city.SurfaceY(building.X,building.Z)+.4f+age*1.4f-age*age*2,
                    building.Z+MathF.Sin(angle)*(.25f+age*.8f))*Scale;
                var color=new Color((byte)130,(byte)110,(byte)85,(byte)(255*effect.Value/.7f));
                Raylib.DrawCube(p,.06f*Scale,.06f*Scale,.06f*Scale,color);
            }
        }
        foreach(var s in city.Shots)
        {
            var color=ColorOf(s.Type is "mage"?"#C697EA":s.Type=="blaze"?"#FBA04E":s.Type=="snow_golem"?"#ECF4F3":"#D2B87C");
            Raylib.DrawLine3D(new Vector3(s.From.X,.3f+city.SurfaceY(s.From.X,s.From.Y),s.From.Y)*Scale,new Vector3(s.To.X,.3f+city.SurfaceY(s.To.X,s.To.Y),s.To.Y)*Scale,color);
        }
    }
}
