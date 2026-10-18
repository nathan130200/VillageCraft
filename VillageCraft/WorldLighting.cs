using System.Numerics;
using Raylib_cs;
namespace VillageCraft;

sealed class WorldLighting:IDisposable
{
    public static WorldLighting? Current {get;private set;}
    public static bool Active=>Current!=null;
    public bool ShadowPass {get;private set;}
    Shader light,depth;
    RenderTexture2D shadows;
    bool failed;
    int surface;
    int shadowSampler;
    readonly Dictionary<string,int> locations=[];
    public bool Ready=>light.Id!=0&&!failed;
    public void Prepare(City city,Vector3 focus,Vector3 eye)
    {
        if(failed)return;
        if(light.Id==0)
        {
            var settings=Catalog.Data.Rendering;
            light=Raylib.LoadShaderFromMemory(settings.LightingVertex,settings.LightingFragment);
            depth=Raylib.LoadShaderFromMemory(settings.LightingVertex,settings.ShadowFragment);
            surface=Raylib.GetShaderLocation(light,"waterSurface");
            shadowSampler=Raylib.GetShaderLocation(light,"shadowMap");
            if(surface<0||Raylib.GetShaderLocation(depth,"shadowPassCheck")<0)
            {failed=true;GameLog.Write(GameLogLevel.Warning,"World lighting shader unavailable; using standard rendering.");return;}
            shadows=Raylib.LoadRenderTexture(1536,1536);
            if(!Raylib.IsRenderTextureValid(shadows)){failed=true;return;}
            Raylib.SetTextureFilter(shadows.Texture,TextureFilter.Point);
        }
        Current=this;
        float angle=(city.DayPhase-.25f)*MathF.Tau;
        Vector3 sun=Vector3.Normalize(new(MathF.Cos(angle),Math.Max(.25f,MathF.Sin(angle)),.4f));
        var camera=new Camera3D{Position=focus+sun*220,Target=focus,Up=Vector3.UnitY,FovY=200,Projection=CameraProjection.Orthographic};
        var view=Raylib.GetCameraMatrix(camera);var projection=Raymath.MatrixOrtho(-100,100,-100,100,1,10000);
        var matrix=Raymath.MatrixMultiply(view,projection);
        Raylib.SetShaderValueMatrix(light,Location("lightSpace"),matrix);
        Set("sunDirection",sun);Set("eyePosition",eye);
        Set("dayAmount",city.SunStrength);Set("ambientFloor",Catalog.Data.Rendering.NightAmbient);
        Set("clock",GraphicsOptions.Current.AnimateWater?Scene.Time:0);Set("shadowTexel",1f/shadows.Texture.Width);
        Set("shadowEnabled",GraphicsOptions.Current.Shadows&&city.SunStrength>.05f?1f:0f);
        Set("smoothShadows",GraphicsOptions.Current.SmoothShadows?1f:0f);
        Set("colDiffuse",Vector4.One);
        if(GraphicsOptions.Current.Shadows&&city.SunStrength>.05f)
        {
            using var profile=Profiler.Measure(ProfileArea.ShadowMap);
            ShadowPass=true;var previousWorld=Scene.World;Scene.World=city;
            Raylib.BeginTextureMode(shadows);Raylib.ClearBackground(Color.White);Raylib.BeginMode3D(camera);
            Scene.Terrain(city);
            foreach(var b in city.Buildings)
            {
                if(Vector2.DistanceSquared(new(b.X*Scene.Scale,b.Z*Scene.Scale),new(focus.X,focus.Z))>110*110)continue;
                Scene.Model(b.Plan.Harvest.Count>0&&b.GrowthDays>0?"tree_stump":b.Plan.Model,b.X,b.Z,b.Rotation);
            }
            foreach(var v in city.Citizens)Scene.Villager(city,v,Scene.Time);
            foreach(var m in city.Animals.Concat(city.Defenders).Concat(city.Enemies))Scene.Creature(m);
            Raylib.EndMode3D();Raylib.EndTextureMode();Scene.World=previousWorld;ShadowPass=false;
        }
    }
    public void Bind(bool water=false)
    {
        if(ShadowPass)Raylib.BeginShaderMode(depth);
        else
        {
            Raylib.BeginShaderMode(light);Raylib.SetShaderValue(light,surface,water?1f:0f,ShaderUniformDataType.Float);
            Raylib.SetShaderValueTexture(light,shadowSampler,shadows.Texture);
        }
    }
    int Location(string key){if(!locations.TryGetValue(key,out int value))locations[key]=value=Raylib.GetShaderLocation(light,key);return value;}
    void Set(string key,float v)=>Raylib.SetShaderValue(light,Location(key),v,ShaderUniformDataType.Float);
    void Set(string key,Vector3 v)=>Raylib.SetShaderValue(light,Location(key),v,ShaderUniformDataType.Vec3);
    void Set(string key,Vector4 v)=>Raylib.SetShaderValue(light,Location(key),v,ShaderUniformDataType.Vec4);
    public void End(){if(Current==this)Current=null;}
    public void Dispose()
    {
        End();if(shadows.Id!=0)Raylib.UnloadRenderTexture(shadows);
        if(light.Id!=0&&light.Id!=Rlgl.GetShaderIdDefault())Raylib.UnloadShader(light);
        if(depth.Id!=0&&depth.Id!=Rlgl.GetShaderIdDefault())Raylib.UnloadShader(depth);
    }
}
