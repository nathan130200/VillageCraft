using System.Numerics;
using Raylib_cs;
namespace VillageCraft;

static class VisualChecks
{
    internal static void Capture(string name)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"VillageCraft","VillageCraft.csproj")))root=root.Parent;
        if(root==null)throw new InvalidOperationException("Capture checks require a source checkout.");
        string path=Path.Combine(Directory.CreateDirectory(Path.Combine(root.FullName,"screenshots")).FullName,Path.GetFileName(name));
        var image=Raylib.LoadImageFromScreen();
        try{if(!Raylib.ExportImage(image,path))throw new IOException("Screenshot export failed: "+name);}
        finally{Raylib.UnloadImage(image);}
    }

    public static void Nether()
    {
        string[] models=["piglin","piglin_brute","zombified_piglin","blaze","nether_warlord"];
        var target=Raylib.LoadRenderTexture(244,270);Raylib.SetWindowSize(1280,660);
        try
        {
            for(int frame=0;frame<8;frame++)
            {
                Raylib.BeginDrawing();Raylib.ClearBackground(new Color(24,37,43,255));
                for(int i=0;i<10;i++)
                {
                    var camera=new Camera3D{Position=new(2.5f,2.8f,6),Target=new(0,1.9f,0),Up=Vector3.UnitY,FovY=42,Projection=CameraProjection.Perspective};
                    Raylib.BeginTextureMode(target);Raylib.ClearBackground(new Color(182,199,202,255));Raylib.BeginMode3D(camera);
                    Raylib.DrawPlane(Vector3.Zero,new(8,8),new Color(122,147,101,255));
                    Scene.Model(models[i%5],0,0,heading:i<5?0:MathF.PI);
                    Raylib.EndMode3D();Raylib.EndTextureMode();
                    int x=10+i%5*254,y=10+i/5*320;
                    Raylib.DrawText(models[i%5]+(i<5?" front":" rear"),x,y,16,Color.White);
                    Raylib.DrawTextureRec(target.Texture,new(0,0,244,-270),new Vector2(x,y+25),Color.White);
                }
                Raylib.EndDrawing();
            }
            Capture("nether-models.png");
            Console.WriteLine("PASS: Nether models rendered from front and rear.");
        }
        finally{Raylib.UnloadRenderTexture(target);}
    }

    public static void Sunlight()
    {
        var target=Raylib.LoadRenderTexture(600,290);Raylib.SetWindowSize(1280,720);
        try
        {
            for(int frame=0;frame<8;frame++)
            {
                Raylib.BeginDrawing();Raylib.ClearBackground(new Color(24,37,43,255));
                for(int i=0;i<4;i++)
                {
                    bool helmet=i%2==1;string type=i<2?"zombie":"skeleton";
                    var m=new Creature{Type=type,Health=40,LeatherHelmet=helmet,BurningInSun=!helmet};
                    var camera=new Camera3D{Position=new(2.8f,2.4f,4.5f),Target=new(0,1.3f,0),Up=Vector3.UnitY,FovY=40,Projection=CameraProjection.Perspective};
                    Raylib.BeginTextureMode(target);Raylib.ClearBackground(new Color(159,191,196,255));Raylib.BeginMode3D(camera);
                    Raylib.DrawPlane(Vector3.Zero,new(8,8),new Color(122,147,101,255));Scene.Creature(m,true);
                    Raylib.EndMode3D();Raylib.EndTextureMode();
                    int x=20+i%2*630,y=14+i/2*350;
                    Raylib.DrawText(type+(helmet?" / leather helmet":" / burning in sunlight"),x,y,20,Color.White);
                    Raylib.DrawTextureRec(target.Texture,new(0,0,600,-290),new Vector2(x,y+28),Color.White);
                }
                Raylib.EndDrawing();
            }
            VisualChecks.Capture("sunlight-models.png");
            Console.WriteLine("PASS: burning and helmet-protected zombies/skeletons rendered.");
        }
        finally{Raylib.UnloadRenderTexture(target);}
    }
    public static void Materials(bool beta=false)
    {
        Transparency();
        string[] models=beta?["warehouse","large_warehouse","cow_shed","cow","deliveryman"]:["market","pen","quarry","coop","wild_flower","wild_grass"];
        var target=Raylib.LoadRenderTexture(400,310);
        Raylib.SetWindowSize(1280,760);
        try
        {
            foreach(float light in new[]{1f,.45f})
            {
                Scene.Daylight=light;
                for(int frame=0;frame<5;frame++)
                {
                    Raylib.BeginDrawing();Raylib.ClearBackground(new Color(24,37,43,255));
                    for(int i=0;i<models.Length;i++)
                    {
                        bool plant=!beta&&i>=4;
                        var camera=new Camera3D{Position=plant?new(1,1.1f,1.5f):new(6,5,7),Target=plant?new(0,.45f,0):new(0,2.2f,0),Up=Vector3.UnitY,FovY=plant?40:42,Projection=CameraProjection.Perspective};
                        Raylib.BeginTextureMode(target);Raylib.ClearBackground(new Color(182,199,202,255));Raylib.BeginMode3D(camera);
                        Raylib.DrawPlane(Vector3.Zero,new(8,8),new Color(122,147,101,255));
                        Scene.Model(models[i],0,0);
                        Raylib.EndMode3D();Raylib.EndTextureMode();
                        int x=20+i%3*420,y=12+i/3*370;
                        Raylib.DrawText(models[i],x,y,20,Color.White);
                        Raylib.DrawTextureRec(target.Texture,new(0,0,400,-310),new Vector2(x,y+28),Color.White);
                    }
                    Raylib.EndDrawing();
                }
                VisualChecks.Capture(beta?(light==1?"beta105-models-day.png":"beta105-models-night.png"):(light==1?"materials-day.png":"materials-night.png"));
            }
            Console.WriteLine(beta?"PASS: warehouses, cow shed, cow skin, deliveryman rendered in daylight and at night.":"PASS: market, troughs, mine and crossed plants rendered in daylight and at night.");
        }
        finally{Scene.Daylight=1;Raylib.UnloadRenderTexture(target);}
    }
    public static void Models()
    {
        Transparency();
        string[] labels=["Chicken / front","Chicken / left","Chicken / back","Chicken / right","Lumber camp / front","Lumber camp / rear"];
        var targets=Enumerable.Range(0,6).Select(_=>Raylib.LoadRenderTexture(460,380)).ToArray();
        try
        {
            for(int frame=0;frame<10&&!Raylib.WindowShouldClose();frame++)
            {
                Raylib.BeginDrawing();Raylib.ClearBackground(new Color(24,37,43,255));
                for(int i=0;i<6;i++)
                {
                    bool bird=i<4;
                    var camera=new Camera3D{Position=bird?new(0,.6f,1.65f):new(6,5,7),Target=bird?new(0,.35f,0):new(0,2.6f,0),Up=Vector3.UnitY,FovY=bird?35:42,Projection=CameraProjection.Perspective};
                    Raylib.BeginTextureMode(targets[i]);Raylib.ClearBackground(new Color(182,199,202,255));Raylib.BeginMode3D(camera);
                    Raylib.DrawPlane(Vector3.Zero,new(8,8),new Color(122,147,101,255));
                    Scene.Model(bird?"chicken":"lumber",0,0,size:bird?.7f:1,heading:bird?i*MathF.PI/2:(i-4)*MathF.PI);
                    Raylib.EndMode3D();Raylib.EndTextureMode();
                    int x=10+i%3*475,y=20+i/3*440;
                    Raylib.DrawText(labels[i],x,y,20,Color.White);
                    Raylib.DrawTextureRec(targets[i].Texture,new(0,0,460,-380),new Vector2(x,y+30),Color.White);
                }
                Raylib.EndDrawing();
            }
            VisualChecks.Capture("models-smoke.png");Console.WriteLine("PASS: chicken rendered from four directions; lumber foliage rendered from both sides.");
        }
        finally{foreach(var t in targets)Raylib.UnloadRenderTexture(t);}
    }
    static void Transparency()
    {
        const string leaf="__cutout_check",backdrop="__cutout_backdrop";
        Catalog.Data.Models[leaf]=[new(){Position=[0,.5f,0],Size=[1,1,.001f],Color="#FFFFFF",Texture="block/oak_leaves.png"}];
        Catalog.Data.Models[backdrop]=[new(){Position=[0,.5f,-.2f],Size=[1,1,.01f],Color="#FF00FF"}];
        var camera=new Camera3D{Position=new(0,2,10),Target=new(0,2,0),Up=Vector3.UnitY,FovY=4.4f,Projection=CameraProjection.Orthographic};
        var targets=new[]{Raylib.LoadRenderTexture(128,128),Raylib.LoadRenderTexture(128,128)};
        var images=new Image[2];
        try
        {
            for(int i=0;i<2;i++)
            {
                Raylib.BeginTextureMode(targets[i]);Raylib.ClearBackground(Color.Black);Raylib.BeginMode3D(camera);
                Scene.Model(i==0?leaf:backdrop,0,0);Scene.Model(i==0?backdrop:leaf,0,0);
                Raylib.EndMode3D();Raylib.EndTextureMode();images[i]=Raylib.LoadImageFromTexture(targets[i].Texture);
            }
            int visibleBehind=0,visibleLeaves=0;
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                var a=Raylib.GetImageColor(images[0],x,y);var b=Raylib.GetImageColor(images[1],x,y);
                if(a.R!=b.R||a.G!=b.G||a.B!=b.B)throw new InvalidOperationException("Cutout rendering changes with object draw order.");
                if(a.R>150&&a.G<50&&a.B>150)visibleBehind++;
                if(a.G>70&&a.G>a.R&&a.B<130)visibleLeaves++;
            }
            if(visibleBehind<20||visibleLeaves<20)throw new InvalidOperationException("Foliage must show both leaves and objects through its holes.");
            Console.WriteLine($"PASS: transparent depth/order regression ({visibleBehind} pixels show the object behind foliage).");
        }
        finally
        {
            foreach(var image in images)if(image.Width>0)Raylib.UnloadImage(image);
            foreach(var t in targets)Raylib.UnloadRenderTexture(t);
            Catalog.Data.Models.Remove(leaf);Catalog.Data.Models.Remove(backdrop);
        }
    }
}
