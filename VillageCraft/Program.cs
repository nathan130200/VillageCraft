using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using VillageCraft;

try{Catalog.Load();}catch(Exception e){Console.Error.WriteLine("Cannot load TOML content: "+e.Message);Environment.ExitCode=1;return;}
if(args.Contains("--self-test")){try{Checks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
bool smoke=args.Contains("--smoke-test"),raidSmoke=args.Contains("--raid-smoke");
bool menuSmoke=args.Contains("--menu-smoke"),optionsSmoke=args.Contains("--options-smoke");
bool uiSmoke=args.Contains("--ui-smoke");
bool caveSmoke=args.Contains("--cave-smoke"),newMapSmoke=args.Contains("--newmap-smoke");
bool sunlightModels=args.Contains("--sunlight-models-smoke");
bool gameOverSmoke=args.Contains("--gameover-smoke");if(gameOverSmoke)smoke=true;
bool modelsSmoke=args.Contains("--models-smoke")||sunlightModels;
bool beta105Models=args.Contains("--beta105-models-smoke");
bool materialsSmoke=args.Contains("--materials-smoke")||beta105Models;if(materialsSmoke)modelsSmoke=true;
bool englishSmoke=args.Contains("--english-smoke");
bool beta105Smoke=args.Contains("--beta105-smoke");if(beta105Smoke)smoke=true;
bool terrainSmoke=args.Contains("--terrain-smoke");if(terrainSmoke)smoke=true;
bool displaySmoke=args.Contains("--display-smoke");if(displaySmoke){smoke=true;optionsSmoke=true;}
bool compactSmoke=args.Contains("--compact-smoke");if(compactSmoke)smoke=true;
bool panelsSmoke=args.Contains("--panels-smoke"),clockSmoke=args.Contains("--clock-smoke");if(panelsSmoke||clockSmoke)smoke=true;
bool citizenSmoke=args.Contains("--citizen-smoke");if(englishSmoke)smoke=true;
bool nightSmoke=args.Contains("--night-smoke");if(citizenSmoke)smoke=true;if(nightSmoke)smoke=true;
if(smoke||raidSmoke||menuSmoke||optionsSmoke||uiSmoke||caveSmoke||newMapSmoke||modelsSmoke)Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
GraphicsOptions.Load();if(args.Contains("--scale-smoke")){GraphicsOptions.Current.UiScale=1.5f;smoke=true;}if(englishSmoke)L.Set("en-US");Assets.Discover();
Raylib.SetConfigFlags(ConfigFlags.ResizableWindow|ConfigFlags.Msaa4xHint);
Raylib.InitWindow(1024,640,"VillageCraft 1.0.5-beta | The Living Overworld");DisplaySettings.Apply(false);Raylib.SetTargetFPS(GraphicsOptions.Current.Fps);Raylib.SetExitKey(KeyboardKey.Null);
Rlgl.SetClipPlanes(.1,10000);
rlImGui.Setup(true);
string font=Path.Combine(AppContext.BaseDirectory,"Content","Fonts","mojangles.ttf");
if(!File.Exists(font))throw new FileNotFoundException("Missing Mojangles UI font",font);
ImGui.GetIO().Fonts.Clear();ImGui.GetIO().Fonts.AddFontFromFileTTF(font,16);rlImGui.ReloadFonts();
Rlgl.TextureParameters((uint)ImGui.GetIO().Fonts.TexID,0x2800,0x2600);
Rlgl.TextureParameters((uint)ImGui.GetIO().Fonts.TexID,0x2801,0x2600);
if(compactSmoke)Raylib.SetWindowSize(1024,640);
if(modelsSmoke)
{
    try{if(sunlightModels)VisualChecks.Sunlight();else if(materialsSmoke)VisualChecks.Materials(beta105Models);else VisualChecks.Models();}finally{Assets.Dispose();rlImGui.Shutdown();Raylib.CloseWindow();}return;
}
var ui=new GameUi();ui.Theme();
ui.BuildThumbnails();if(menuSmoke||optionsSmoke){ui.MenuOpen=true;ui.MenuPage=optionsSmoke?"Options":"Main";}
if(newMapSmoke){ui.MenuOpen=true;ui.MenuPage="New Game";}
var city=args.Contains("--demo")||raidSmoke||caveSmoke?City.New():City.StartVillage();if(args.Contains("--demo")||raidSmoke||caveSmoke)Checks.Demo(city);
if(compactSmoke){ui.SelectPrefab("farm");ui.Inspected=city.Buildings.First(b=>b.Prefab=="farm").Id;ui.Paused=true;}
if(citizenSmoke){ui.InspectMode=true;ui.InspectedCitizen=city.Citizens[0].Id;ui.Paused=true;}
if(panelsSmoke){ui.InspectedCitizen=city.Citizens[0].Id;ui.PinCurrent();var m=city.SpawnMonster("zombie",city.AnchorX-3,city.AnchorZ+3)!;ui.InspectAt(city,m.Position);ui.Paused=true;}
if(clockSmoke)ui.Paused=true;
if(nightSmoke){city.Day=6;city.DayTime=city.Rules.DayLength*.78f;ui.Paused=true;city.SpawnNight();}
if(caveSmoke){ui.SelectPrefab("cave");ui.Inspected=city.Buildings.First(b=>b.Prefab=="cave").Id;}
if(beta105Smoke)
{
    foreach(string id in Catalog.Resources)city.Stock[id]=Math.Max(city.Stock[id],100);city.ReconcileStock();
    city.Place("cow_shed",city.AnchorX-3,city.AnchorZ-1);city.Place("torch_workshop",city.AnchorX+3,city.AnchorZ-1);
    city.Place("large_warehouse",city.AnchorX+5,city.AnchorZ-1);
    foreach(var (dx,dz) in new[]{(-3,1),(3,1),(2,2)})if(city.Place("torch",city.AnchorX+dx,city.AnchorZ+dz))city.SetTorch(city.At(city.AnchorX+dx,city.AnchorZ+dz)!,true);
    city.DayTime=city.Rules.DayLength*.8f;ui.Paused=true;city.TutorialEnabled=false;
    ui.Inspected=city.Buildings.First(b=>b.Plan.Warehouse).Id;
}
_=city.Scripting;city.Scripting.Flush();
if(raidSmoke)for(int i=0;i<240;i++)city.Update(.1f);
string? previewSave=null;
if(gameOverSmoke)
{
    previewSave="gameover-preview-"+Guid.NewGuid().ToString("N");city.Day=32;city.Statistics.RecordedSinceDay=1;city.Kills=42;city.Waves=1;city.Statistics.Deliveries=37;city.Statistics.EmeraldsFromGoods=550;city.Save(SaveStore.PathFor(previewSave));ui.RememberSave(previewSave);
    foreach(var v in city.Citizens.ToArray())city.RemoveVillager(v,"killed");
}
float yaw=.85f,distance=beta105Smoke?62:terrainSmoke?145:102,time=0;var focus=new Vector3(city.AnchorX*4,0,city.AnchorZ*4);int frames=0;City cameraCity=city;
try
{
    while(!Raylib.WindowShouldClose()&&!ui.ExitRequested)
    {
        float dt=Math.Min(Raylib.GetFrameTime(),.1f);time+=dt;
        if(!ReferenceEquals(cameraCity,city)){cameraCity.Dispose();_=city.Scripting;city.Scripting.Flush();focus=new(city.AnchorX*4,0,city.AnchorZ*4);distance=102;cameraCity=city;}
        if(clockSmoke)city.DayTime=city.Rules.DayLength*frames/64f;
        ui.TestTooltip=(uiSmoke&&frames>=50)||compactSmoke;
        if(optionsSmoke&&frames==20)L.Set("en-US");if(optionsSmoke&&frames==40)L.Set("pt-BR");
        if(uiSmoke&&frames%5==0){ui.ShowBuildCategory("Roads");ui.TestActivatePrefab=Catalog.Data.Prefabs.Where(p=>p.Category=="Roads").ElementAt(frames/5%6).Id;distance=frames<30?55:145;}
        if(Raylib.IsKeyPressed(KeyboardKey.Escape))ui.Escape();
        if(!ui.Paused&&!ui.MenuOpen)city.Update(dt*ui.Speed);
        var mouse=Raylib.GetMousePosition();bool capture=city.GameOver||ui.OverUi(mouse)||ImGui.GetIO().WantCaptureMouse;
        if(!city.GameOver&&!ImGui.GetIO().WantCaptureKeyboard&&!ui.MenuOpen)
        {
            var forward=new Vector3(-MathF.Cos(yaw),0,-MathF.Sin(yaw));var right=new Vector3(-forward.Z,0,forward.X);
            if(Raylib.IsKeyDown(KeyboardKey.W))focus+=forward*dt*40;
            if(Raylib.IsKeyDown(KeyboardKey.S))focus-=forward*dt*40;
            if(Raylib.IsKeyDown(KeyboardKey.D))focus+=right*dt*40;
            if(Raylib.IsKeyDown(KeyboardKey.A))focus-=right*dt*40;
            float rotationSpeed=dt*.7f*(Raylib.IsKeyDown(KeyboardKey.LeftShift)||Raylib.IsKeyDown(KeyboardKey.RightShift)?3:1);
            if(Raylib.IsKeyDown(KeyboardKey.Q))yaw-=rotationSpeed;
            if(Raylib.IsKeyDown(KeyboardKey.E))yaw+=rotationSpeed;
            if(Raylib.IsKeyPressed(KeyboardKey.R))ui.Rotation=(ui.Rotation+1)%4;
            if(Raylib.IsKeyPressed(KeyboardKey.Tab))ui.ToggleTool();
            if(Raylib.IsKeyPressed(KeyboardKey.Space))ui.Paused=!ui.Paused;
            if(Raylib.IsKeyPressed(KeyboardKey.One))ui.SetSpeed(1);
            if(Raylib.IsKeyPressed(KeyboardKey.Two))ui.SetSpeed(2);
            if(Raylib.IsKeyPressed(KeyboardKey.Three))ui.SetSpeed(3);
            if(Raylib.IsKeyPressed(KeyboardKey.Four))ui.SetSpeed(4);
        }
        if(!capture)distance=Math.Clamp(distance-Raylib.GetMouseWheelMove()*Math.Max(7,distance*.06f),25,Math.Max(city.Width,city.Height)*5);
        focus.X=Math.Clamp(focus.X,0,(city.Width-1)*4);focus.Z=Math.Clamp(focus.Z,0,(city.Height-1)*4);
        var camera=new Camera3D{Position=focus+new Vector3(MathF.Cos(yaw)*distance,distance*.78f,MathF.Sin(yaw)*distance),Target=focus,Up=Vector3.UnitY,FovY=45,Projection=CameraProjection.Perspective};
        var ray=Raylib.GetScreenToWorldRay(mouse,camera);var hit=TerrainScene.PickGround(city,ray);
        int hx=(int)MathF.Floor(hit.X+.5f),hz=(int)MathF.Floor(hit.Z+.5f);
        if(!capture)
        {
            if(Raylib.IsMouseButtonPressed(MouseButton.Right)||(ui.InspectMode&&Raylib.IsMouseButtonPressed(MouseButton.Left)))ui.InspectRay(city,ray,new Vector2(hit.X,hit.Z));
            else if(ui.IsBuilding&&Raylib.IsMouseButtonPressed(MouseButton.Left))city.Place(ui.Selected,hx,hz,ui.Rotation);
        }
        if(ui.RefreshThumbnails)ui.BuildThumbnails();Scene.Time=time;
        Scene.Daylight=city.Daylight;
        Raylib.BeginDrawing();Raylib.ClearBackground(new Color((int)(159*city.Daylight),(int)(191*city.Daylight),(int)(196*city.Daylight),255));Raylib.BeginMode3D(camera);Scene.World=city;TerrainScene.ViewCenter=new(focus.X/Scene.Scale,focus.Z/Scene.Scale);TerrainScene.ViewRadius=Math.Min(45,distance/Scene.Scale*1.2f);
        Scene.Terrain(city);var roads=city.Roads();foreach(var b in city.Buildings)Scene.Build(city,b,roads);
        foreach(var v in city.Citizens)Scene.Villager(city,v,time);
        foreach(var a in city.Animals)Scene.Creature(a);foreach(var d in city.Defenders)Scene.Creature(d);foreach(var e in city.Enemies)Scene.Creature(e,true);
        Scene.Projectiles(city);
        if(!capture&&ui.IsBuilding)Scene.Preview(city,ui.Selected,hx,hz,ui.Rotation);
        var inspected=city.Buildings.FirstOrDefault(b=>b.Id==ui.Inspected);
        if(inspected!=null)Raylib.DrawCubeWires(new Vector3(inspected.X,.6f+city.SurfaceY(inspected.X,inspected.Z),inspected.Z)*4,4,5,4,Scene.ColorOf("#E8C478"));
        var selectedCitizen=city.Citizens.FirstOrDefault(v=>v.Id==ui.InspectedCitizen);
        if(selectedCitizen!=null){var pos=Scene.VillagerPosition(city,selectedCitizen);Raylib.DrawCubeWires(new Vector3(pos.X,.3f+city.SurfaceY(pos.X,pos.Y),pos.Y)*4,1.1f,2.4f,1.1f,Scene.ColorOf("#E8C478"));}
        var selectedMonster=city.Enemies.Concat(city.Defenders).Concat(city.Animals).FirstOrDefault(m=>m.Id==ui.InspectedMonster);
        if(selectedMonster!=null)Raylib.DrawCubeWires(new Vector3(selectedMonster.X,.4f+city.SurfaceY(selectedMonster.X,selectedMonster.Z),selectedMonster.Z)*4,1.4f,3.2f,1.4f,Scene.ColorOf("#E8C478"));
        Raylib.EndMode3D();Scene.World=null;
        if(args.Contains("--scale-smoke")&&frames==5)Raylib.SetMousePosition(Raylib.GetScreenWidth()-30,Raylib.GetScreenHeight()-30);
        ui.BeginFrame();
        if(args.Contains("--scale-smoke")&&frames==10&&Vector2.Distance(ImGui.GetIO().MousePos,Raylib.GetMousePosition()/GraphicsOptions.Current.UiScale)>2)
            throw new InvalidOperationException("Scaled mouse input does not match UI coordinates.");
        ui.Draw(ref city,hx,hz);ui.EndFrame();Raylib.EndDrawing();DisplaySettings.ApplyPending();
        if(displaySmoke&&frames is 10 or 25 or 40)
        {
            int mode=frames==10?1:frames==25?2:0;GraphicsOptions.Current.DisplayMode=mode;DisplaySettings.Apply(false);
            bool valid=mode==1?Raylib.IsWindowFullscreen():mode==2?Raylib.IsWindowState(ConfigFlags.BorderlessWindowMode):!Raylib.IsWindowFullscreen()&&!Raylib.IsWindowState(ConfigFlags.BorderlessWindowMode);
            if(!valid){Console.Error.WriteLine("Display mode transition failed: "+mode);Environment.ExitCode=1;}
            Console.WriteLine($"Display mode {mode}: {Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}");
        }
        if(clockSmoke&&frames==33)Raylib.TakeScreenshot(Path.Combine(Environment.CurrentDirectory,"clock-noon.png"));
        if(clockSmoke&&frames==1)Raylib.TakeScreenshot(Path.Combine(Environment.CurrentDirectory,"clock-first.png"));
        if(uiSmoke&&frames==29)Raylib.TakeScreenshot(Path.Combine(Environment.CurrentDirectory,"ui-near-smoke.png"));
        if((smoke||raidSmoke||menuSmoke||optionsSmoke||uiSmoke||caveSmoke||newMapSmoke)&&++frames>=60)
        {
            Raylib.TakeScreenshot(Path.Combine(Environment.CurrentDirectory,gameOverSmoke?"gameover-smoke.png":beta105Smoke?"beta105-smoke.png":raidSmoke?"raid-smoke.png":menuSmoke?"menu-smoke.png":optionsSmoke?"options-smoke.png":uiSmoke?"ui-smoke.png":caveSmoke?"cave-smoke.png":newMapSmoke?"newmap-smoke.png":citizenSmoke?"citizen-smoke.png":panelsSmoke?"panels-smoke.png":clockSmoke?"clock-smoke.png":compactSmoke?"compact-smoke.png":terrainSmoke?"terrain-smoke.png":englishSmoke?"english-smoke.png":"smoke-test.png"));
            if(uiSmoke&&ui.TestActivationCount!=12){Console.Error.WriteLine("UI activation count mismatch: "+ui.TestActivationCount);Environment.ExitCode=1;}
            Console.WriteLine($"PASS: rendered {frames} frames, loaded {Assets.TextureCount} textures"+(uiSmoke?", activated construction cards 12 times across two zoom levels.":"."));break;
        }
    }
}
finally{if(previewSave!=null)SaveStore.Delete(previewSave);city.Dispose();ui.Dispose();Assets.Dispose();rlImGui.Shutdown();Raylib.CloseWindow();}
