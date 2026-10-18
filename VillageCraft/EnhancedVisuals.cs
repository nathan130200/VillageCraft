using System.Numerics;
using Raylib_cs;
namespace VillageCraft;

sealed class EnhancedVisuals:IDisposable
{
    Shader shader;
    RenderTexture2D target;
    int resolution,day;
    bool active,failed;
    readonly WorldLighting lighting=new();
    public bool Active=>active;
    public bool LightingReady=>lighting.Ready;
    public void Begin(City city,Vector3 focus,Vector3 eye)
    {
        active=false;
        if(!GraphicsOptions.Current.EnhancedVisuals||failed)return;
        using var profile=Profiler.Measure(ProfileArea.VisualEffects);
        if(shader.Id==0)
        {
            shader=Raylib.LoadShaderFromMemory(null,Catalog.Data.Rendering.EnhancedFragment);
            resolution=Raylib.GetShaderLocation(shader,"resolution");day=Raylib.GetShaderLocation(shader,"dayAmount");
            if(resolution<0||day<0){failed=true;GameLog.Write(GameLogLevel.Warning,"Enhanced visuals shader unavailable; using standard rendering.");return;}
        }
        int w=Raylib.GetScreenWidth(),h=Raylib.GetScreenHeight();
        if(target.Texture.Width!=w||target.Texture.Height!=h)
        {
            if(target.Id!=0)Raylib.UnloadRenderTexture(target);
            target=Raylib.LoadRenderTexture(w,h);
            if(!Raylib.IsRenderTextureValid(target)){failed=true;GameLog.Write(GameLogLevel.Warning,"Enhanced visuals target unavailable; using standard rendering.");return;}
            Raylib.SetTextureFilter(target.Texture,TextureFilter.Bilinear);
        }
        lighting.Prepare(city,focus,eye);
        Raylib.BeginTextureMode(target);active=true;
    }
    public void End(float daylight)
    {
        if(!active)return;
        lighting.End();
        using var profile=Profiler.Measure(ProfileArea.VisualEffects);
        Raylib.EndTextureMode();
        Raylib.SetShaderValue(shader,resolution,new Vector2(target.Texture.Width,target.Texture.Height),ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(shader,day,daylight,ShaderUniformDataType.Float);
        Raylib.BeginShaderMode(shader);
        Raylib.DrawTextureRec(target.Texture,new Rectangle(0,0,target.Texture.Width,-target.Texture.Height),Vector2.Zero,Color.White);
        Raylib.EndShaderMode();
    }
    public void Dispose()
    {
        lighting.Dispose();
        if(target.Id!=0)Raylib.UnloadRenderTexture(target);
        if(shader.Id!=0&&shader.Id!=Rlgl.GetShaderIdDefault())Raylib.UnloadShader(shader);
    }
}
