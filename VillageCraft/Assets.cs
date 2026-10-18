using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using Raylib_cs;
namespace VillageCraft;

static class Assets
{
    static readonly Dictionary<string,Texture2D> textures=new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string,string> loose=new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string,string> indexed=new(StringComparer.OrdinalIgnoreCase);
    static readonly List<ZipArchive> packs=[];
    static readonly List<string> roots=[];
    static Shader cutoutShader;
    static AssetCache? cache;
    public static int TextureCount=>textures.Count;
    public static Dictionary<ResourceType,string> Icons=>Catalog.Data.ResourceIcons;
    public static void Discover()
    {
        if(cache!=null)return;
        cache=new AssetCache(Path.Combine(SaveStore.Folder,"cache.zip"));cache.Load();
    }
    public static void Prepare()
    {
        Discover();
        // Resolve referenced files once, without decoding or uploading every texture to the GPU.
        var names=Catalog.Data.Models.Values.SelectMany(parts=>parts).Select(p=>p.Texture)
            .Concat(Icons.Values).Concat(new[]{Catalog.Data.Game.GroundTexture,Catalog.Data.Game.DirtTexture,"block/sand.png","block/stone.png","block/water_still.png"})
            .Concat(Enumerable.Range(0,64).Select(i=>$"item/clock_{i:00}.png"));
        foreach(string name in names.Where(n=>!string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase))Read(name);
        cache!.Save();
    }
    public static void FlushCache()=>cache?.Save();
    static void DiscoverSources()
    {
        if(roots.Count>0)return;
        string? configured=Environment.GetEnvironmentVariable("VILLAGECRAFT_ASSETS");
        if(!string.IsNullOrWhiteSpace(configured))
        {
            string folder=Path.GetFullPath(configured);
            if(!Directory.Exists(folder))throw new DirectoryNotFoundException("VILLAGECRAFT_ASSETS: "+folder);
            roots.Add(folder);
        }
        else
        {
            for(var d=new DirectoryInfo(AppContext.BaseDirectory);d!=null;d=d.Parent)if(File.Exists(Path.Combine(d.FullName,"assets_index.json")))roots.Add(d.FullName);
            if(!roots.Contains(Environment.CurrentDirectory))roots.Add(Environment.CurrentDirectory);
        }
        foreach(var root in roots.ToArray())
        {
            foreach(string soundRoot in new[]{Path.Combine(root,"sounds"),Path.Combine(root,"assets","minecraft","sounds")})
                if(Directory.Exists(soundRoot))foreach(string file in Directory.EnumerateFiles(soundRoot,"*.ogg",SearchOption.AllDirectories))
                    loose.TryAdd("minecraft/sounds/"+Path.GetRelativePath(soundRoot,file).Replace('\\','/'),file);
            foreach(var folder in new[]{Path.Combine(root,"textures"),Path.Combine(root,"assets"),Path.Combine(root,"Content","Assets")})
            {
                if(!Directory.Exists(folder))continue;
                foreach(var file in Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories).Where(f=>f.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||f.EndsWith(".ogg",StringComparison.OrdinalIgnoreCase)))
                // Keep full relative paths: a profession overlay must not impersonate a legacy full-body skin by basename.
                {string rel=Path.GetRelativePath(folder,file).Replace('\\','/');loose.TryAdd(rel,file);}
            }
            foreach(string indexPath in new[]{Path.Combine(root,"assets_index.json"),Path.Combine(root,"assets","assets_index.json")})
            {
                if(!File.Exists(indexPath))continue;
                using var json=JsonDocument.Parse(File.ReadAllText(indexPath));var map=json.RootElement;
                if(map.TryGetProperty("objects",out var objects))map=objects;
                foreach(var entry in map.EnumerateObject())
                {
                    string? file=null;var value=entry.Value;
                    if(value.ValueKind==JsonValueKind.String)file=FindPath(root,value.GetString()!)??FindPath(Path.GetDirectoryName(indexPath)!,value.GetString()!);
                    else if(value.ValueKind==JsonValueKind.Object)
                    {
                        if(value.TryGetProperty("path",out var path))file=FindPath(root,path.GetString()!)??FindPath(Path.GetDirectoryName(indexPath)!,path.GetString()!);
                        else if(value.TryGetProperty("hash",out var hash))
                        {
                            string h=hash.GetString()??string.Empty;if(h.Length==40&&h.All(Uri.IsHexDigit))file=FindPath(root,"objects/"+h[..2]+"/"+h)??FindPath(root,"assets/objects/"+h[..2]+"/"+h);
                        }
                    }
                    if(file==null)continue;indexed.TryAdd(entry.Name,file);
                    if(entry.Name.EndsWith(".zip",StringComparison.OrdinalIgnoreCase))
                    {try{packs.Add(ZipFile.OpenRead(file));}catch(InvalidDataException){Console.Error.WriteLine("Invalid asset pack: "+entry.Name);}}
                }
            }
        }
        Console.WriteLine($"Assets: {loose.Count} loose aliases, {indexed.Count} indexed objects, {packs.Count} resource packs.");
    }
    static string? FindPath(string root,string relative)
    {
        string path=Path.GetFullPath(Path.Combine(root,relative));
        return path.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&File.Exists(path)?path:null;
    }
    public static byte[]? Read(string name)
    {
        using var profile=Profiler.Measure(ProfileArea.AssetRead);
        if(name=="block/dirt_path.png")name="block/grass_path_top.png";
        Discover();string logical=name.StartsWith("minecraft/")?name:name.StartsWith("sounds/")?"minecraft/"+name:"minecraft/textures/"+name;
        return cache!.Read(logical,()=>ReadSource(name,logical));
    }
    static byte[]? ReadSource(string name,string logical)
    {
        DiscoverSources();
        foreach(string key in new[]{logical,"assets/"+logical,name,Path.GetFileName(name),logical.Replace('/','_')})
            if(loose.TryGetValue(key,out var path))return File.ReadAllBytes(path);
        foreach(string key in new[]{logical,"assets/"+logical,name})if(indexed.TryGetValue(key,out var path))return File.ReadAllBytes(path);
        foreach(var pack in packs)
        {
            var entry=pack.GetEntry("assets/"+logical)??pack.GetEntry(logical);if(entry==null)continue;
            using var stream=entry.Open();using var bytes=new MemoryStream();stream.CopyTo(bytes);return bytes.ToArray();
        }
        return null;
    }
    public static Texture2D Get(string name)
    {
        if(textures.TryGetValue(name,out var result))return result;
        var bytes=Read(name);if(bytes!=null)
        {
            var image=Raylib.LoadImageFromMemory(".png",bytes);
            if(name.StartsWith("block/")&&image.Height>image.Width&&image.Height%image.Width==0)Raylib.ImageCrop(ref image,new Rectangle(0,0,image.Width,image.Width));
            result=Raylib.LoadTextureFromImage(image);Raylib.UnloadImage(image);
            if(name.StartsWith("block/")||name.StartsWith("entity/")||name.StartsWith("models/armor/"))Raylib.GenTextureMipmaps(ref result);
            Filter(result,name);Raylib.SetTextureWrap(result,TextureWrap.Repeat);
        }
        else GameLog.Write(GameLogLevel.Warning,"Missing texture: "+name+". Using model fallback colors; supply this skin through VILLAGECRAFT_ASSETS.");
        textures[name]=result;return result;
    }
    static void Filter(Texture2D texture,string name)
    {
        Raylib.SetTextureFilter(texture,TextureFilter.Point);
        if(name.StartsWith("block/")||name.StartsWith("entity/")||name.StartsWith("models/armor/"))
        {
            if(GraphicsOptions.Current.Quality>0)Raylib.SetTextureFilter(texture,GraphicsOptions.Current.Quality==2?TextureFilter.Anisotropic8X:TextureFilter.Anisotropic4X);
            // Nearest pixel magnification with blended mip levels keeps close textures crisp and distant surfaces stable.
            Rlgl.TextureParameters(texture.Id,0x2801,0x2702);
        }
    }
    public static void RefreshFilters(){foreach(var e in textures)if(e.Value.Id!=0)Filter(e.Value,e.Key);}
    public static void BeginCutouts()
    {
        if(WorldLighting.Current is {} lighting){lighting.Bind();Rlgl.DisableBackfaceCulling();return;}
        if(cutoutShader.Id==0)
        {
            cutoutShader=Raylib.LoadShaderFromMemory(null,Catalog.Data.Rendering.CutoutFragment);
            int cutoff=Raylib.GetShaderLocation(cutoutShader,"alphaCutoff");
            if(cutoff<0)throw new InvalidOperationException("The alpha-cutout shader failed to compile.");
            Raylib.SetShaderValue(cutoutShader,cutoff,Catalog.Data.Rendering.AlphaCutoff,ShaderUniformDataType.Float);
            Raylib.SetShaderValue(cutoutShader,Raylib.GetShaderLocation(cutoutShader,"colDiffuse"),Vector4.One,ShaderUniformDataType.Vec4);
        }
        Raylib.BeginShaderMode(cutoutShader);
        // Entity skins and foliage use two-sided cutouts: their reverse face can be visible through holes.
        Rlgl.DisableBackfaceCulling();
    }
    public static void EndCutouts(){Raylib.EndShaderMode();Rlgl.EnableBackfaceCulling();}
    public static void Dispose()
    {cache?.Save();cache=null;if(cutoutShader.Id!=0){Raylib.UnloadShader(cutoutShader);cutoutShader=default;}foreach(var t in textures.Values)if(t.Id!=0)Raylib.UnloadTexture(t);textures.Clear();foreach(var p in packs)p.Dispose();packs.Clear();roots.Clear();loose.Clear();indexed.Clear();}

    public static void Cross(Texture2D texture,Vector3 center,Vector3 size,Color tint,float heading=0)
    {
        // Two flat, two-sided plant faces; no textured box edges or end caps.
        Rlgl.SetTexture(texture.Id);Rlgl.Begin((int)DrawMode.Quads);
        Rlgl.Color4ub(tint.R,tint.G,tint.B,tint.A);
        for(int face=0;face<2;face++)
        {
            float angle=heading+MathF.PI/4+face*MathF.PI/2;
            var side=new Vector3(MathF.Cos(angle)*size.X*.5f,0,MathF.Sin(angle)*size.Z*.5f);
            var up=new Vector3(0,size.Y*.5f,0);
            Rlgl.Normal3f(-MathF.Sin(angle),0,MathF.Cos(angle));
            Vector3[] points=[center-side-up,center+side-up,center+side+up,center-side+up];
            Vector2[] uv=[new(0,1),new(1,1),new(1,0),new(0,0)];
            for(int i=0;i<4;i++){Rlgl.TexCoord2f(uv[i].X,uv[i].Y);Rlgl.Vertex3f(points[i].X,points[i].Y,points[i].Z);}
        }
        Rlgl.End();Rlgl.SetTexture(0);
    }
    public static void Cube(Texture2D texture,Vector3 center,Vector3 size,Color tint,float[]? origin=null,float[]? skinSize=null,int rotation=0,float frame=0,float? heading=null,bool stretch=false,float pitch=0)
    {
        if(texture.Id==0)return;
        Vector3[] vertices=[new(-1,-1,-1),new(1,-1,-1),new(1,1,-1),new(-1,1,-1),new(-1,-1,1),new(1,-1,1),new(1,1,1),new(-1,1,1)];
        int[][] faces=[[4,5,6,7],[1,0,3,2],[0,4,7,3],[5,1,2,6],[7,6,2,3],[0,1,5,4]];
        float[] shade=[.88f,.72f,.68f,.82f,1,.55f];
        float tilt=pitch*MathF.PI/180,tiltCos=MathF.Cos(tilt),tiltSin=MathF.Sin(tilt);
        float angle=heading??rotation*MathF.PI/2,cosine=MathF.Cos(angle),sine=MathF.Sin(angle);
        Rlgl.SetTexture(texture.Id);Rlgl.Begin((int)DrawMode.Quads);
        for(int face=0;face<6;face++)
        {
            float u=0,v=frame,w=face is 2 or 3?size.Z:size.X,h=face is 4 or 5?size.Z:size.Y;
            if(stretch){w=1;h=1;}
            if(texture.Height>texture.Width){h=(float)texture.Width/texture.Height;w=1;}
            if(origin!=null&&skinSize!=null)
            {
                float x=origin[0],y=origin[1],a=skinSize[0],b=skinSize[1],d=skinSize[2];
                (float px,float py,float pw,float ph)=face switch{0=>(x+d,y+d,a,b),1=>(x+2*d+a,y+d,a,b),2=>(x,y+d,d,b),3=>(x+d+a,y+d,d,b),4=>(x+d,y,a,d),_=>(x+d+a,y,a,d)};
                u=px/texture.Width;v=py/texture.Height;w=pw/texture.Width;h=ph/texture.Height;
            }
            Vector2[] uv=[new(u,v+h),new(u+w,v+h),new(u+w,v),new(u,v)];
            Vector3 normal=face switch{0=>Vector3.UnitZ,1=>-Vector3.UnitZ,2=>-Vector3.UnitX,3=>Vector3.UnitX,4=>Vector3.UnitY,_=>-Vector3.UnitY};
            (normal.Y,normal.Z)=(normal.Y*tiltCos-normal.Z*tiltSin,normal.Y*tiltSin+normal.Z*tiltCos);
            (normal.X,normal.Z)=(normal.X*cosine-normal.Z*sine,normal.X*sine+normal.Z*cosine);
            Rlgl.Normal3f(normal.X,normal.Y,normal.Z);
            float light=WorldLighting.Active?1:shade[face];Rlgl.Color4ub((byte)(tint.R*light),(byte)(tint.G*light),(byte)(tint.B*light),tint.A);
            for(int i=0;i<4;i++)
            {
                var p=vertices[faces[face][i]]*size*.5f;
                (p.Y,p.Z)=(p.Y*tiltCos-p.Z*tiltSin,p.Y*tiltSin+p.Z*tiltCos);(p.X,p.Z)=(p.X*cosine-p.Z*sine,p.X*sine+p.Z*cosine);
                p+=center;Rlgl.TexCoord2f(uv[i].X,uv[i].Y);Rlgl.Vertex3f(p.X,p.Y,p.Z);
            }
        }
        Rlgl.End();Rlgl.SetTexture(0);
    }
}
