using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using VillageCraft;

GameLog.Initialize();
try{Catalog.Load();}catch(Exception e){GameLog.Write(GameLogLevel.Critical,"Cannot load TOML content: "+e);Environment.ExitCode=1;return;}
#if DEBUG
if(args.Contains("--spacing-test")){try{EntityOffsetsChecks.Run();CameraDragChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--health-test")){try{VillagerHealthChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--save-preview-test")){try{SavePreviewChecks.Run();LocalizationChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--audio-test")){try{AudioChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}finally{Assets.Dispose();}return;}
if(args.Contains("--village-improvements-test")){try{VillageImprovementsChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--profile-test")){try{ProfilerChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--profile-benchmark")){try{ProfilerChecks.Benchmark();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--asset-cache-test")){try{AssetCacheChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--presentation-test")){try{LocalizationChecks.Run();NightChecks.Run();LoggingChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--population-test")){try{PopulationChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--logistics-test")){try{LogisticsChecks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
if(args.Contains("--self-test")){try{Checks.Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}return;}
bool smoke=args.Contains("--smoke-test"),raidSmoke=args.Contains("--raid-smoke");
bool savePreviewSmoke=args.Contains("--save-preview-smoke");if(savePreviewSmoke)smoke=true;
bool enhancedSmoke=args.Contains("--enhanced-smoke");if(enhancedSmoke)smoke=true;
bool trafficSmoke=args.Contains("--traffic-smoke");bool crowdSmoke=args.Contains("--crowd-smoke")||trafficSmoke;if(crowdSmoke)smoke=true;
bool audioSmoke=args.Contains("--audio-smoke");if(audioSmoke)smoke=true;
bool problemsSmoke=args.Contains("--problems-smoke");if(problemsSmoke)smoke=true;
bool profileSmoke=args.Contains("--profile-smoke")||args.Contains("--profile-callbacks-smoke");if(profileSmoke)smoke=true;
bool peopleSmoke=args.Contains("--people-smoke");if(peopleSmoke)smoke=true;
bool menuSmoke=args.Contains("--menu-smoke"),optionsSmoke=args.Contains("--options-smoke");
bool uiSmoke=args.Contains("--ui-smoke");
bool caveSmoke=args.Contains("--cave-smoke"),newMapSmoke=args.Contains("--newmap-smoke");
bool villagerModels=args.Contains("--villager-models-smoke");
bool netherModels=args.Contains("--nether-models-smoke");
bool sunlightModels=args.Contains("--sunlight-models-smoke");
bool gameOverSmoke=args.Contains("--gameover-smoke");if(gameOverSmoke)smoke=true;
bool modelsSmoke=args.Contains("--models-smoke")||sunlightModels||netherModels||villagerModels;
bool beta105Models=args.Contains("--beta105-models-smoke");
bool materialsSmoke=args.Contains("--materials-smoke")||beta105Models;if(materialsSmoke)modelsSmoke=true;
bool englishSmoke=args.Contains("--english-smoke");
bool beta105Smoke=args.Contains("--beta105-smoke");if(beta105Smoke)smoke=true;
bool terrainSmoke=args.Contains("--terrain-smoke");if(terrainSmoke)smoke=true;
bool displaySmoke=args.Contains("--display-smoke");if(displaySmoke){smoke=true;optionsSmoke=true;}
bool compactSmoke=args.Contains("--compact-smoke");if(compactSmoke)smoke=true;
bool occupantsSmoke=args.Contains("--occupants-smoke");if(occupantsSmoke)smoke=true;
bool panelsSmoke=args.Contains("--panels-smoke"),clockSmoke=args.Contains("--clock-smoke");if(panelsSmoke||clockSmoke)smoke=true;
bool citizenSmoke=args.Contains("--citizen-smoke");if(englishSmoke)smoke=true;
bool nightSmoke=args.Contains("--night-smoke");if(citizenSmoke)smoke=true;if(nightSmoke)smoke=true;
if(smoke||raidSmoke||menuSmoke||optionsSmoke||uiSmoke||caveSmoke||newMapSmoke||modelsSmoke)Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
GraphicsOptions.Load();if(audioSmoke)GraphicsOptions.Current.MasterVolume=.04f;if(args.Contains("--scale-smoke")){GraphicsOptions.Current.UiScale=1.5f;smoke=true;}if(englishSmoke)L.Set("en-US");Assets.Prepare();
#else
if(args.Length>0){Console.Error.WriteLine("Development command-line modes require a Debug build.");Environment.ExitCode=1;return;}
GraphicsOptions.Load();Assets.Prepare();
#endif
GameLog.CaptureRenderer();
Raylib.SetConfigFlags(ConfigFlags.ResizableWindow|ConfigFlags.Msaa4xHint);
Raylib.InitWindow(1024,640,"VillageCraft 1.0.8 | The Living Overworld");DisplaySettings.Apply(false);Raylib.SetTargetFPS(GraphicsOptions.Current.Fps);Raylib.SetExitKey(KeyboardKey.Null);
#if DEBUG
if(smoke||uiSmoke)Raylib.TraceLog(TraceLogLevel.Warning,"Visual check renderer logging is active.");
#endif
Rlgl.SetClipPlanes(1,10000);
rlImGui.Setup(true);
string font=Path.Combine(AppContext.BaseDirectory,"Content","Fonts","mojangles.ttf");
UiFonts.Load(font);
#if DEBUG
if(compactSmoke)Raylib.SetWindowSize(1024,640);
if(modelsSmoke)
{
    try{if(villagerModels)VisualChecks.Villagers();else if(netherModels)VisualChecks.Nether();else if(sunlightModels)VisualChecks.Sunlight();else if(materialsSmoke)VisualChecks.Materials(beta105Models);else VisualChecks.Models();}finally{Assets.Dispose();rlImGui.Shutdown();Raylib.CloseWindow();}return;
}
#endif
using var audio=new GameAudio(Catalog.Data.Audio);audio.Initialize();
var ui=new GameUi();ui.Theme();
#if DEBUG
if(profileSmoke)ui.ToggleProfiler();
#endif
ui.BuildThumbnails();
#if DEBUG
if(menuSmoke||optionsSmoke){ui.MenuOpen=true;ui.MenuPage=optionsSmoke?"Options":"Main";}
if(newMapSmoke){ui.MenuOpen=true;ui.MenuPage="New Game";}
var city=args.Contains("--demo")||raidSmoke||caveSmoke?City.New():City.StartVillage();if(args.Contains("--demo")||raidSmoke||caveSmoke)Checks.Demo(city);
if(compactSmoke){ui.SelectPrefab("farm");ui.Inspected=city.Buildings.First(b=>b.Prefab=="farm").Id;ui.Paused=true;}
if(citizenSmoke){ui.InspectMode=true;ui.InspectedCitizen=city.Citizens[0].Id;ui.Paused=true;}
if(occupantsSmoke)
{
    city.TutorialEnabled=false;ui.Paused=true;
    ui.Inspected=city.Buildings.First(b=>b.Plan.Homes>0).Id;ui.PinCurrent();
    ui.Inspected=city.Buildings.First(b=>b.Plan.Jobs>0&&city.Citizens.Any(v=>v.Work==b.Id)).Id;
}
if(panelsSmoke){ui.InspectedCitizen=city.Citizens[0].Id;ui.PinCurrent();var m=city.SpawnMonster("zombie",city.AnchorX-3,city.AnchorZ+3)!;ui.InspectAt(city,m.Position);ui.Paused=true;}
if(problemsSmoke){ui.ShowProblems();ui.Paused=true;foreach(var b in city.Buildings.Where(b=>b.Plan.Input.Count>0||!string.IsNullOrEmpty(b.Plan.Animal)))b.Storage.Clear();}
if(peopleSmoke){city.Rules.BirthChance=1;city.Tick();ui.ShowPeople();ui.Paused=true;}
if(clockSmoke)ui.Paused=true;
if(nightSmoke){city.Day=6;city.DayTime=city.Rules.DayLength*.9f;ui.Paused=true;city.SpawnNight();}
if(enhancedSmoke)ui.Paused=true;
if(crowdSmoke)
{
    var road=city.Buildings.Where(b=>b.Plan.Path).OrderBy(b=>Math.Abs(b.X-city.AnchorX)+Math.Abs(b.Z-city.AnchorZ)).First();
    foreach(var v in city.Citizens.Take(4)){v.Position=new(road.X,road.Z);v.Work=-1;}
    var pen=city.Buildings.First(b=>!string.IsNullOrEmpty(b.Plan.Animal));
    foreach(var a in city.Animals.Where(a=>a.Home==pen.Id))a.Position=new(pen.X,pen.Z);
    ui.Paused=true;city.TutorialEnabled=false;
}
if(caveSmoke){ui.SelectPrefab("cave");ui.Inspected=city.Buildings.First(b=>b.Prefab=="cave").Id;}
if(beta105Smoke)
{
    foreach(ResourceType id in Catalog.Resources)city.Stock[id]=Math.Max(city.Stock[id],100);city.ReconcileStock();
    city.Place("cow_shed",city.AnchorX-3,city.AnchorZ-1);
    city.Place("large_warehouse",city.AnchorX+5,city.AnchorZ-1);
    city.DayTime=city.Rules.DayLength*.8f;ui.Paused=true;city.TutorialEnabled=false;
    ui.Inspected=city.Buildings.First(b=>b.Plan.Warehouse).Id;
}
#else
var city=City.StartVillage();
#endif
_=city.Scripting;city.Scripting.Flush();audio.Bind(city);
#if DEBUG
if(raidSmoke)for(int i=0;i<240;i++)city.Update(.1f);
string? previewSave=null;
if(gameOverSmoke)
{
    previewSave="gameover-preview-"+Guid.NewGuid().ToString("N");city.Day=32;city.Statistics.RecordedSinceDay=1;city.Kills=42;city.Waves=1;city.Statistics.Deliveries=37;city.Statistics.EmeraldsFromGoods=550;city.Save(SaveStore.PathFor(previewSave));ui.RememberSave(previewSave);
    foreach(var v in city.Citizens.ToArray())city.RemoveVillager(v,"killed");
}

#endif
float yaw=.85f,distance=102,time=0;var focus=new Vector3(city.AnchorX*4,0,city.AnchorZ*4);City cameraCity=city;
var cameraDrag=new CameraDrag();
var visuals=new EnhancedVisuals();
#if DEBUG
int frames=0;distance=peopleSmoke||nightSmoke||beta105Smoke?62:terrainSmoke?145:102;
Raylib_cs.Color[]? shadowBaseline=null;
if(crowdSmoke)distance=36;
#endif
try
{
    while(!Raylib.WindowShouldClose()&&!ui.ExitRequested)
    {
        if(Raylib.IsKeyPressed(KeyboardKey.F3))ui.ToggleProfiler();
        Profiler.BeginFrame();
        try
        {
        float dt;bool capture;Vector2 mouse;Camera3D camera;int hx,hz;Ray ray;Vector3 hit;
        using(Profiler.Measure(ProfileArea.Input))
        {
        dt=Math.Min(Raylib.GetFrameTime(),.1f);time+=dt;
        if(!ReferenceEquals(cameraCity,city)){audio.Bind(city);cameraCity.Dispose();_=city.Scripting;city.Scripting.Flush();focus=new(city.AnchorX*4,0,city.AnchorZ*4);distance=102;cameraCity=city;}
#if DEBUG
        if(clockSmoke)city.DayTime=city.Rules.DayLength*frames/64f;
        ui.TestTooltip=(uiSmoke&&frames>=50)||compactSmoke;
        if(optionsSmoke&&frames==20)L.Set("en-US");if(optionsSmoke&&frames==40)L.Set("pt-BR");
        if(uiSmoke&&frames%5==0){ui.ShowBuildCategory("Roads");ui.TestActivatePrefab=Catalog.Data.Prefabs.Where(p=>p.Category=="Roads").ElementAt(frames/5%6).Id;distance=frames<30?55:145;}
#endif
        if(Raylib.IsKeyPressed(KeyboardKey.Escape))ui.Escape();
#if DEBUG
        if(audioSmoke&&frames==12){city.DayTime=city.Rules.DayLength*.25f-.001f;city.LastSettlementDay=city.Day-1;city.Stock[ResourceType.Goods]=5;}
        if(audioSmoke&&frames==36)city.SpawnWave();
#endif
        using(Profiler.Measure(ProfileArea.Update))
        if(!ui.Paused&&!ui.MenuOpen&&!city.GameOver){city.PlaySeconds+=Raylib.GetFrameTime();city.Update(dt*ui.Speed);}
#if DEBUG
        if(trafficSmoke)
        {
            int index=0;
            foreach(var v in city.Citizens.Take(8))
            {
                float t=frames*.012f-1.44f;v.Work=-1;
                v.Position=(index%4) switch{0=>new(city.AnchorX+t,city.AnchorZ+index/4*.4f),1=>new(city.AnchorX-t,city.AnchorZ+index/4*.4f),2=>new(city.AnchorX+index/4*.4f,city.AnchorZ+t),_=>new(city.AnchorX+index/4*.4f,city.AnchorZ-t)};
                index++;
            }
        }
#endif
#if DEBUG
        if(trafficSmoke)RenderEffectsChecks.TrackCrowd(city,frames,dt);
#endif
        mouse=Raylib.GetMousePosition();capture=city.GameOver||ui.OverUi(mouse)||ImGui.GetIO().WantCaptureMouse;
        cameraDrag.Update(mouse,Raylib.IsMouseButtonDown(MouseButton.Left),Raylib.IsMouseButtonDown(MouseButton.Right),capture);
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
        if(!capture)
        {
            var forward=new Vector3(-MathF.Cos(yaw),0,-MathF.Sin(yaw));var right=new Vector3(-forward.Z,0,forward.X);
            float units=distance*1.0507f/Math.Max(1,Raylib.GetScreenHeight());
            focus+=(-right*cameraDrag.Pan.X+forward*cameraDrag.Pan.Y*1.626f)*units;
            yaw+=cameraDrag.Rotation*.006f*(Raylib.IsKeyDown(KeyboardKey.LeftShift)||Raylib.IsKeyDown(KeyboardKey.RightShift)?3:1);
        }
        if(!capture)distance=Math.Clamp(distance-Raylib.GetMouseWheelMove()*Math.Max(7,distance*.06f),25,Math.Max(city.Width,city.Height)*5);
        focus.X=Math.Clamp(focus.X,0,(city.Width-1)*4);focus.Z=Math.Clamp(focus.Z,0,(city.Height-1)*4);
        camera=new Camera3D{Position=focus+new Vector3(MathF.Cos(yaw)*distance,distance*.78f,MathF.Sin(yaw)*distance),Target=focus,Up=Vector3.UnitY,FovY=45,Projection=CameraProjection.Perspective};
        ray=Raylib.GetScreenToWorldRay(mouse,camera);using(Profiler.Measure(ProfileArea.Picking))hit=TerrainScene.PickGround(city,ray);
        hx=(int)MathF.Floor(hit.X+.5f);hz=(int)MathF.Floor(hit.Z+.5f);
        if(!capture)
        {
            if(cameraDrag.RightClick||(ui.InspectMode&&cameraDrag.LeftClick))ui.InspectRay(city,ray,new Vector2(hit.X,hit.Z));
            else if(ui.IsBuilding&&cameraDrag.LeftClick)city.Place(ui.Selected,hx,hz,ui.Rotation);
        }
        if(ui.RefreshThumbnails)ui.BuildThumbnails();Scene.Time=time;
        Scene.Daylight=city.Daylight;
        audio.Update(city,new(focus.X/Scene.Scale,focus.Z/Scene.Scale),new(MathF.Sin(yaw),-MathF.Cos(yaw)),dt,ui.Paused||ui.MenuOpen);
        }
        using(Profiler.Measure(ProfileArea.Render))
        {
        TerrainScene.ViewCenter=new(focus.X/Scene.Scale,focus.Z/Scene.Scale);TerrainScene.ViewRadius=Math.Min(45,distance/Scene.Scale*1.2f);
        Raylib.BeginDrawing();visuals.Begin(city,focus,camera.Position);Raylib.ClearBackground(new Color((int)(159*city.Daylight),(int)(191*city.Daylight),(int)(196*city.Daylight),255));Raylib.BeginMode3D(camera);Scene.World=city;
        using(Profiler.Measure(ProfileArea.Terrain))Scene.Terrain(city);
        var roads=city.Roads();
        using(Profiler.Measure(ProfileArea.Buildings))foreach(var b in city.Buildings)Scene.Build(city,b,roads);
        using(Profiler.Measure(ProfileArea.Residents))foreach(var v in city.Citizens)Scene.Villager(city,v,time);
        using(Profiler.Measure(ProfileArea.Creatures))
        {foreach(var a in city.Animals)Scene.Creature(a);foreach(var d in city.Defenders)Scene.Creature(d);foreach(var e in city.Enemies)Scene.Creature(e,true);}
        using(Profiler.Measure(ProfileArea.Effects))
        {
        Scene.Projectiles(city);
        if(!capture&&ui.IsBuilding)Scene.Preview(city,ui.Selected,hx,hz,ui.Rotation);
        var inspected=city.Buildings.FirstOrDefault(b=>b.Id==ui.Inspected);
        if(inspected!=null)Raylib.DrawCubeWires(new Vector3(inspected.X,.6f+city.SurfaceY(inspected.X,inspected.Z),inspected.Z)*4,4,5,4,Scene.ColorOf("#E8C478"));
        var selectedCitizen=city.Citizens.FirstOrDefault(v=>v.Id==ui.InspectedCitizen);
        if(selectedCitizen!=null&&Scene.VillagerVisible(city,selectedCitizen)){var pos=Scene.VillagerPosition(city,selectedCitizen);Raylib.DrawCubeWires(new Vector3(pos.X,.3f+city.SurfaceY(pos.X,pos.Y),pos.Y)*4,1.1f,2.4f,1.1f,Scene.ColorOf("#E8C478"));}
        var selectedMonster=city.Enemies.Concat(city.Defenders).Concat(city.IsNight?Enumerable.Empty<Creature>():city.Animals).FirstOrDefault(m=>m.Id==ui.InspectedMonster);
        if(selectedMonster!=null){var p=Scene.CreaturePosition(city,selectedMonster);Raylib.DrawCubeWires(new Vector3(p.X,.4f+city.SurfaceY(p.X,p.Y),p.Y)*4,1.4f,3.2f,1.4f,Scene.ColorOf("#E8C478"));}
        }
        Raylib.EndMode3D();Scene.World=null;
        visuals.End(city.Daylight);
        }
#if DEBUG
        if(args.Contains("--scale-smoke")&&frames==5)Raylib.SetMousePosition(Raylib.GetScreenWidth()-30,Raylib.GetScreenHeight()-30);
        if(args.Contains("--scale-smoke")&&frames is 20 or 40)
            GraphicsOptions.Current.UiScale=frames==20?2:1;
#endif
        using(Profiler.Measure(ProfileArea.Ui))
        {
        ui.BeginFrame();
#if DEBUG
        if(savePreviewSmoke&&frames==5)ui.SavePreviewSmoke(city);
        if(savePreviewSmoke&&frames==35)ui.SavePreviewSmokeOverwrite();
        if(args.Contains("--scale-smoke")&&Math.Abs(ImGui.GetFont().FontSize-16*GraphicsOptions.Current.UiScale)>.01f)
            throw new InvalidOperationException("Font atlas resolution does not match HUD scale.");
        if(args.Contains("--scale-smoke")&&frames==10&&Vector2.Distance(ImGui.GetIO().MousePos,Raylib.GetMousePosition()/GraphicsOptions.Current.UiScale)>2)
            throw new InvalidOperationException("Scaled mouse input does not match UI coordinates.");
#endif
        ui.Draw(ref city,hx,hz);ui.DrawProfiler(city,audio);
        }
        using(Profiler.Measure(ProfileArea.UiSubmit))ui.EndFrame();
        using(Profiler.Measure(ProfileArea.PresentAndPacing))Raylib.EndDrawing();
        using(Profiler.Measure(ProfileArea.DisplayChanges))DisplaySettings.ApplyPending();
#if DEBUG
        if(displaySmoke&&frames is 10 or 25 or 40)
        {
            int mode=frames==10?1:frames==25?2:0;GraphicsOptions.Current.DisplayMode=mode;DisplaySettings.Apply(false);
            bool valid=mode==1?Raylib.IsWindowFullscreen():mode==2?Raylib.IsWindowState(ConfigFlags.BorderlessWindowMode):!Raylib.IsWindowFullscreen()&&!Raylib.IsWindowState(ConfigFlags.BorderlessWindowMode);
            if(!valid){Console.Error.WriteLine("Display mode transition failed: "+mode);Environment.ExitCode=1;}
            Console.WriteLine($"Display mode {mode}: {Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}");
        }
        if(profileSmoke&&frames==50)ui.ProfileSmoke(args.Contains("--profile-callbacks-smoke"));
        if(clockSmoke&&frames==33)VisualChecks.Capture("clock-noon.png");
        if(clockSmoke&&frames==1)VisualChecks.Capture("clock-first.png");
        if(uiSmoke&&frames==29)VisualChecks.Capture("ui-near-smoke.png");
        if(savePreviewSmoke&&frames==30)VisualChecks.Capture("save-browser.png");
        if(savePreviewSmoke&&frames==55)VisualChecks.Capture("save-overwrite.png");
        if(crowdSmoke&&frames==55)VisualChecks.Capture("crowd-spacing.png");
        if(trafficSmoke&&frames is 50 or 100 or 150 or 200)VisualChecks.Capture("traffic-"+frames+".png");
        if(enhancedSmoke&&frames==5)GraphicsOptions.Current.EnhancedVisuals=true;
        if(enhancedSmoke&&frames==6)city.DayTime=city.Rules.DayLength*.5f;
        if(enhancedSmoke&&frames==8)GraphicsOptions.Current.Shadows=false;
        if(enhancedSmoke&&frames==10){shadowBaseline=RenderEffectsChecks.ReadFrame();VisualChecks.Capture("enhanced-no-shadows.png");}
        if(enhancedSmoke&&frames==11)GraphicsOptions.Current.Shadows=true;
        if(enhancedSmoke&&frames==11)GraphicsOptions.Current.SmoothShadows=false;
        if(enhancedSmoke&&frames==14){if(!visuals.LightingReady)throw new InvalidOperationException("World lighting failed to load.");RenderEffectsChecks.Shadows(shadowBaseline!,RenderEffectsChecks.ReadFrame());VisualChecks.Capture("enhanced-with-shadows.png");}
        if(enhancedSmoke&&frames==16)GraphicsOptions.Current.SmoothShadows=true;
        if(enhancedSmoke&&frames==20){if(!visuals.Active)throw new InvalidOperationException("Enhanced renderer failed to activate.");VisualChecks.Capture("enhanced-day.png");city.DayTime=city.Rules.DayLength*.9f;}
        if(enhancedSmoke&&frames==30){VisualChecks.Capture("enhanced-night.png");GraphicsOptions.Current.EnhancedVisuals=false;}
        if(enhancedSmoke&&frames==40){if(visuals.Active)throw new InvalidOperationException("Enhanced renderer failed to disable.");GraphicsOptions.Current.EnhancedVisuals=true;Raylib.SetWindowSize(1100,680);}
        if(enhancedSmoke&&frames==50&&!visuals.Active)throw new InvalidOperationException("Enhanced renderer failed after resize.");
        if((smoke||raidSmoke||menuSmoke||optionsSmoke||uiSmoke||caveSmoke||newMapSmoke)&&++frames>=(trafficSmoke?240:60))
        {
            VisualChecks.Capture(occupantsSmoke?"building-occupants.png":audioSmoke?"audio-smoke.png":problemsSmoke?"city-problems.png":profileSmoke?(args.Contains("--profile-callbacks-smoke")?"profiler-callbacks.png":"profiler-smoke.png"):nightSmoke?"night-village.png":peopleSmoke?"workforce-priorities.png":gameOverSmoke?"gameover-smoke.png":beta105Smoke?"beta105-smoke.png":raidSmoke?"raid-smoke.png":menuSmoke?"menu-smoke.png":optionsSmoke?"options-smoke.png":uiSmoke?"ui-smoke.png":caveSmoke?"cave-smoke.png":newMapSmoke?"newmap-smoke.png":citizenSmoke?"citizen-smoke.png":panelsSmoke?"panels-smoke.png":clockSmoke?"clock-smoke.png":compactSmoke?"compact-smoke.png":terrainSmoke?"terrain-smoke.png":englishSmoke?"english-smoke.png":"smoke-test.png");
            if(audioSmoke){Console.WriteLine($"Audio smoke: {audio.LoadedSounds} decoded, {audio.Played} played, {audio.Dropped} rate-limited.");if(audio.LoadedSounds==0||audio.PlayedCount(AudioTrigger.Settlement)==0||audio.PlayedCount(AudioTrigger.BossWave)==0||audio.PlayedCount(AudioTrigger.Footstep)==0)Environment.ExitCode=1;}
            if(uiSmoke&&ui.TestActivationCount!=12){Console.Error.WriteLine("UI activation count mismatch: "+ui.TestActivationCount);Environment.ExitCode=1;}
            Console.WriteLine($"PASS: rendered {frames} frames, loaded {Assets.TextureCount} textures"+(uiSmoke?", activated construction cards 12 times across two zoom levels.":"."));break;
        }
#endif
        }
        finally{Profiler.EndFrame();}
    }
}
#if DEBUG
catch(Exception e)when(smoke||raidSmoke||menuSmoke||optionsSmoke||uiSmoke||caveSmoke||newMapSmoke)
{GameLog.Write(GameLogLevel.Error,"Validation failed: "+e);Environment.ExitCode=1;}
#endif
finally{
#if DEBUG
if(previewSave!=null)SaveStore.Delete(previewSave);
#endif
visuals.Dispose();TerrainScene.Dispose();audio.Dispose();city.Dispose();ui.Dispose();Assets.Dispose();rlImGui.Shutdown();Raylib.CloseWindow();}
