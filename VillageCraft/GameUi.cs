using System.Numerics;
using ImGuiNET;
using Raylib_cs;
namespace VillageCraft;

partial class GameUi
{
    public string Selected="";
    public int Rotation,Inspected=-1,InspectedCitizen=-1,Speed=1;
    bool inspectMode=true;
    public bool InspectMode {get=>inspectMode||Selected=="";set=>inspectMode=value;}
    public bool IsBuilding=>!InspectMode&&Selected!="";
    public bool Paused,MenuOpen,ExitRequested,RefreshThumbnails;
    public string MenuPage="Main";
    internal string? TestActivatePrefab;
    internal bool TestTooltip;
    internal int TestActivationCount;
    string page="Build",category="Roads",search="",tradeResource="Food",currentSave="",saveName=L.T("My village"),loadName="",menuError="";
    bool overwrite;
    bool newTutorial=!GraphicsOptions.Current.TutorialDismissed;
    int newSeed;
    string[] saves=[];
    int newWidth=Catalog.Data.Game.DefaultMapWidth,newHeight=Catalog.Data.Game.DefaultMapHeight;
    readonly Dictionary<string,RenderTexture2D> thumbnails=[];
    static readonly Vector4 Accent=new(.65f,.85f,.45f,1),Muted=new(.68f,.68f,.68f,1),Gold=new(1,.85f,.35f,1);
    const ImGuiWindowFlags Fixed=ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoScrollbar|ImGuiWindowFlags.NoScrollWithMouse;
    public void BuildThumbnails()
    {
        var previousWorld=Scene.World;var previousLight=Scene.Daylight;Scene.World=null;Scene.Daylight=1;
        foreach(var t in thumbnails.Values)Raylib.UnloadRenderTexture(t);thumbnails.Clear();
        var camera=new Camera3D{Position=new(7,6,7),Target=new(0,2,0),Up=Vector3.UnitY,FovY=7.8f,Projection=CameraProjection.Orthographic};
        foreach(var p in Catalog.Data.Prefabs)
        {
            var target=Raylib.LoadRenderTexture(144,96);Raylib.BeginTextureMode(target);Raylib.ClearBackground(new Color(43,43,43,255));Raylib.BeginMode3D(camera);
            Scene.Model(p.Model,0,0);Raylib.EndMode3D();Raylib.EndTextureMode();thumbnails[p.Id]=target;
        }
        Scene.World=previousWorld;Scene.Daylight=previousLight;RefreshThumbnails=false;
    }
    public void Dispose(){foreach(var t in thumbnails.Values)Raylib.UnloadRenderTexture(t);}
    public bool OverUi(Vector2 p)=>MenuOpen||rects.Any(r=>r.Contains(p/frameScale));
    public void Escape(){MenuOpen=!MenuOpen;MenuPage="Main";menuError="";overwrite=false;}
    public void SetSpeed(int key){Speed=key switch{1=>1,2=>2,3=>4,4=>16,_=>1};Paused=false;}
    public void SelectPrefab(string id){Selected=id;category=Catalog.Get(id).Category;page="Build";InspectMode=false;buildOpen=true;}
    void Window(string name,float x,float y,float w,float h){ImGui.SetNextWindowPos(new(x,y));ImGui.SetNextWindowSize(new(w,h));ImGui.Begin(name,Fixed);CaptureRect();}
    static void Colored(Vector4 color,string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text,color);ImGui.TextUnformatted(text);ImGui.PopStyleColor();
    }
    static void Wrapped(string text)
    {
        ImGui.PushTextWrapPos(0);ImGui.TextUnformatted(text);ImGui.PopTextWrapPos();
    }
    static void Title(string text)=>Colored(Accent,L.T(text).ToUpperInvariant());
    static void Hint(string text){ImGui.PushTextWrapPos(0);Colored(Muted,L.T(text));ImGui.PopTextWrapPos();}
    static void ShowTooltip(string text)
    {
        float width=Math.Min(320,ImGui.GetIO().DisplaySize.X-32);
        ImGui.SetNextWindowSizeConstraints(new(0,0),new(width+20,1000));
        ImGui.PushStyleColor(ImGuiCol.PopupBg,new Vector4(.075f,.025f,.12f,.99f));
        ImGui.PushStyleColor(ImGuiCol.Border,new Vector4(.23f,.08f,.42f,1));
        ImGui.BeginTooltip();ImGui.PushTextWrapPos(ImGui.GetCursorPosX()+width);
        ImGui.TextUnformatted(L.T(text));ImGui.PopTextWrapPos();ImGui.EndTooltip();ImGui.PopStyleColor(2);
    }
    static void Tooltip(string text){if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))ShowTooltip(text);}
    static void Resource(string id,float value,bool enough=true)
    {
        var texture=Assets.Get(Assets.Icons[id]);
        if(texture.Id!=0){ImGui.Image((IntPtr)texture.Id,new(18,18));Tooltip(L.Resource(id));}else ImGui.TextUnformatted(L.T("?"));
        ImGui.SameLine();Colored(enough?Accent:Gold,value.ToString("0"));Tooltip(L.Resource(id)); 
    }
    public void Draw(ref City city,int hx,int hz)
    {
        rects.Clear();int w=(int)(Raylib.GetScreenWidth()/frameScale),h=(int)(Raylib.GetScreenHeight()/frameScale);
        if(city.GameOver){DrawGameOver(ref city,w,h);DrawMenu(ref city,w,h);return;}
        DrawTopBar(city,w);
        if(page=="Build"&&pinnedManagement.Count==0)DrawTutorial(city);
        DrawManagement(city,w,h);
        DrawInspectors(city,w,h);
        DrawHotbar(city,w,h);
        if(TestTooltip&&IsBuilding)
        {
            var p=Catalog.Get(Selected);ShowTooltip(L.Name(p)+"\n"+L.Description(p)+"\n"+string.Join(" / ",p.Cost.Where(e=>e.Value>0).Select(e=>L.F("{0:0} {1}",e.Value,L.Resource(e.Key)))));
        }
        else if(!OverUi(Raylib.GetMousePosition())&&city.Land(hx,hz)&&IsBuilding)
        {
            var issue=city.PlacementIssue(Selected,hx,hz,Rotation);
            ShowTooltip((issue==""?L.T("Click to build"):L.T(issue))+"\n"+L.T("R rotate / Tab inspect / Right click inspect / Shift+Q/E fast camera"));
        }
        DrawMenu(ref city,w,h);
    }
    void PeoplePanel(City city)
    {
        Title("Workforce");ImGui.TextUnformatted(L.F("Employed {0} / {1}", city.Citizens.Count(v=>v.Work>=0), city.Citizens.Count));ImGui.TextUnformatted(L.F("Animals {0} / Guardians {1}", city.Animals.Count, city.Defenders.Count));
        Wrapped(L.T("Unemployed citizens help families attract residents. They can assist any workplace at 50% efficiency. Train specialists using tools and emeralds."));
        ImGui.Separator();foreach(var v in city.Citizens)
        {
            ImGui.PushID(v.Id);if(ImGui.Selectable(L.F("{0} | {1:0} HP", v.FullName, v.Health))){Focus("villager",v.Id);}
            var profession=Catalog.Data.Professions[v.Profession];
            if(ImGui.BeginCombo("##profession",L.Name(profession)+" / "+(city.Efficiency(v)*100).ToString("0")+"%"))
            {
                foreach(var p in Catalog.Data.Professions.Values){if(ImGui.Selectable(L.Name(p),p.Id==v.Profession))city.Train(v,p.Id);Tooltip(string.Join(" / ",p.TrainingCost.Select(e=>L.F("{0:0} {1}", e.Value, L.Resource(e.Key)))));}ImGui.EndCombo();
            }
            Hint(L.Task(v.Task));ImGui.PopID();
        }
    }
    void EconomyPanel(City city)
    {
        Title("Economy");Hint("Goods convert to emeralds every day at 06:00.");ImGui.TextUnformatted(L.F("Balance {0:+0.0;-0.0;0} / day", city.Balance));float tax=city.Tax;if(ImGui.SliderFloat(L.T("Daily Goods contribution"),ref tax,0,6,"%.1f"))city.Tax=tax;
        ImGui.TextUnformatted(L.F("Food reserve: {0:0.0} days", city.FoodDays));ImGui.Separator();Title("Trading hall");Hint("Sell requests are delivered to the trading hall before the exchange.");
        if(ImGui.BeginCombo(L.T("Resource"),L.Resource(tradeResource))){foreach(string r in Catalog.Resources.Where(r=>!City.Currency(r)))if(ImGui.Selectable(L.Resource(r),tradeResource==r))tradeResource=r;ImGui.EndCombo();}
        var resource=Catalog.Data.ResourceDefinitions.First(r=>r.Id==tradeResource);float price=resource.BuyPrice;
        if(Button(L.F("Buy 20 / {0:0} emeralds", price),new(-1,26)))city.Trade(tradeResource,true);
        if(Button(L.F("Sell 20 / {0:0} emeralds", resource.SellPrice),new(-1,26)))city.Trade(tradeResource,false);
        ImGui.Separator();Title(L.F("Equipment / armor {0} / tools {1}", city.ArmorLevel, city.ToolLevel));
        Hint(L.F("Damage reduction {0:0}% / Work rate {1:0.00}x", city.ArmorReduction*100, city.ToolEfficiency));
        foreach(var u in Catalog.Data.Upgrades.Where(u=>u.Level==(u.Kind=="Armor"?city.ArmorLevel:city.ToolLevel)+1))
        {if(Button(L.Name(u),new(-1,26)))city.Upgrade(u.Id);Tooltip(string.Join(" / ",u.Cost.Select(e=>L.F("{0:0} {1}", e.Value, L.Resource(e.Key))))+L.T(". Requires a staffed forge."));}
        if(city.EnchantedArmor)Hint("Enchantment active");
        else {if(Button(L.T("Enchant armor"),new(-1,26)))city.EnchantArmor();Tooltip(string.Join(" / ",Catalog.Data.Economy.EnchantmentCost.Select(e=>L.F("{0:0} {1}",e.Value,L.Resource(e.Key))))); }
        Hint(L.T("A staffed mage hall and armor are required. Enchanting improves protection and damage."));
        ImGui.Separator();Title("Animal care");foreach(string type in new[]{"sheep","pig","chicken","cow"})ImGui.TextUnformatted(L.F("{0}: {1} ({2} young)", L.Entity(type), city.Animals.Count(a=>a.Type==type), city.Animals.Count(a=>a.Type==type&&a.Age<3)));
    }
    void DefensePanel(City city)
    {
        Title("Village defense");ImGui.TextUnformatted(city.RaidIn<0?L.T("Raids disabled"):L.F("Next raid: day {0}", city.Day+city.RaidIn));ImGui.TextUnformatted(L.F("Waves {0} / Kills {1}", city.Waves, city.Kills));ImGui.TextUnformatted(L.F("Hostiles {0}", city.Enemies.Count));
        foreach(var g in city.Defenders.GroupBy(d=>d.Type))ImGui.TextUnformatted(L.F("{0}: {1}", L.Entity(g.Key), g.Count()));ImGui.Separator();
        foreach(var b in city.Buildings.Where(b=>b.Plan.Defender!="")){if(ImGui.Selectable(L.F("{0}##{1}", L.Name(b.Plan), b.Id))){Focus("building",b.Id);}Hint(L.F("Staff {0}/{1}", city.Citizens.Count(v=>v.Work==b.Id), b.Plan.Jobs));}
        Wrapped(L.T("Walls and gates block raiders. Warriors intercept; archers, mages and snow golems shoot. Configure attack timing and limits in Options."));
    }
    public void InspectRay(City city,Ray ray,Vector2 tile)
    {
        InspectAt(city,tile);float nearest=float.MaxValue;string kind="";int id=-1;
        void Hit(string type,int entity,Vector3 min,Vector3 max)
        {
            float baseHeight=city.SurfaceY((min.X+max.X)*.5f,(min.Z+max.Z)*.5f);min.Y+=baseHeight;max.Y+=baseHeight;
            var collision=Raylib.GetRayCollisionBox(ray,new BoundingBox(min*Scene.Scale,max*Scene.Scale));
            if(collision.Hit&&collision.Distance<nearest){nearest=collision.Distance;kind=type;id=entity;}
        }
        foreach(var b in city.Buildings.Where(b=>!b.Plan.Path))Hit("building",b.Id,new(b.X-.45f,0,b.Z-.45f),new(b.X+.45f,1.6f,b.Z+.45f));
        foreach(var v in city.Citizens){var pos=Scene.VillagerPosition(city,v);Hit("villager",v.Id,new(pos.X-.18f,0,pos.Y-.18f),new(pos.X+.18f,.6f,pos.Y+.18f));}
        foreach(var m in city.Enemies.Concat(city.Defenders).Concat(city.Animals))Hit("monster",m.Id,new(m.X-.2f,0,m.Z-.2f),new(m.X+.2f,.85f,m.Z+.2f));
        if(id>=0){city.TutorialInspected=true;Focus(kind,id);}
    }
    public void InspectAt(City city,Vector2 tile)
    {
        UseInspectTool();CloseTransientPanels();
        var villager=city.Citizens.Where(v=>Vector2.DistanceSquared(Scene.VillagerPosition(city,v),tile)<.35f*.35f).OrderBy(v=>Vector2.DistanceSquared(Scene.VillagerPosition(city,v),tile)).FirstOrDefault();
        var monster=city.Enemies.Concat(city.Defenders).Concat(city.Animals).Where(m=>Vector2.DistanceSquared(m.Position,tile)<.35f*.35f).OrderBy(m=>Vector2.DistanceSquared(m.Position,tile)).FirstOrDefault();
        if(villager!=null){city.TutorialInspected=true;Focus("villager",villager.Id);}
        else if(monster!=null){city.TutorialInspected=true;Focus("monster",monster.Id);}
        else if(city.At((int)MathF.Round(tile.X),(int)MathF.Round(tile.Y)) is {} b){city.TutorialInspected=true;Focus("building",b.Id);}
    }
    void VillagerInspector(City city,Citizen v,int w,int h)
    {
        ImGui.TextUnformatted(L.Name(Catalog.Data.Professions[v.Profession]));
        ImGui.TextUnformatted(L.F("Health {0:0}/50", v.Health));ImGui.ProgressBar(v.Health/50,new(-1,10),"");
        Hint(L.Task(v.Task));ImGui.TextUnformatted(L.F("Efficiency {0:0}%", city.Efficiency(v)*100));
        ImGui.TextUnformatted(L.F("Work progress {0:0.0}/{1:0.0}s", v.WorkTime, city.Rules.ProductionCycle));
        ImGui.TextUnformatted(L.F("Damage reduction {0:0}% / Work rate {1:0.00}x", city.ArmorReduction*100, city.ToolEfficiency));
        var home=city.Buildings.FirstOrDefault(b=>b.Id==v.Home);var work=city.Buildings.FirstOrDefault(b=>b.Id==v.Work);
        Wrapped(L.F("Home: {0}",home==null?L.T("Homeless"):L.Name(home.Plan)));
        Wrapped(L.F("Workplace: {0}",work==null?L.T("Unemployed"):L.Name(work.Plan)));
        ImGui.Separator();Title("Inventory");
        Hint("Carried items are separate from village stock. Food is consumed daily.");
        foreach(var item in v.Inventory.Where(e=>e.Value>0)) {Resource(item.Key,item.Value);ImGui.SameLine();ImGui.TextUnformatted(L.Resource(item.Key));}
        if(!v.Inventory.Any(e=>e.Value>0))Hint("Empty inventory");
        foreach(var item in v.Cargo.Where(e=>e.Value>0))Wrapped(L.F("Cargo: {0:0.##} {1}",item.Value,L.Resource(item.Key)));
        foreach(var effect in v.Effects)Wrapped(L.F("Effect: {0} ({1:0}s)",L.T(effect.Key),effect.Value));
        Hint("Villagers collect supplies from nearby warehouses or sources.");
        
    }
    void Inspector(City city,Building b,int w,int h)
    {
        Hint(L.F("Tile {0}, {1}", b.X, b.Z));
        Wrapped(L.Description(b.Plan));ImGui.Separator();
        if(b.Plan.StorageCapacity>0){Title("Storage");Hint(L.F("Storage {0:0.##}/{1}",b.Storage.Values.Sum(),b.Plan.StorageCapacity));foreach(var item in b.Storage.Where(e=>e.Value>0))Wrapped(L.F("{0}: {1:0.##}",L.Resource(item.Key),item.Value));}
        if(b.Plan.LightRadius>0)Hint(b.TorchLit?"Lit":"Extinguished");
        if(b.Plan.Harvest.Count>0)Hint(b.GrowthDays==0?L.T("Ready to harvest"):L.F("Regrows in {0} days",b.GrowthDays));
        Colored(city.Connected(b,city.Roads())?Accent:Gold,city.Connected(b,city.Roads())?L.T("Connected"):L.T("Entrance disconnected"));
        ImGui.TextUnformatted(L.F("Health {0:0}/{1:0}", b.Health, b.Plan.Health));ImGui.ProgressBar(b.Health/b.Plan.Health,new(-1,10),"");
        if(Button(L.T("Repair / 2 wood + 2 stone"),new(-1,26)))city.Repair(b);
        if(b.Plan.Path){ImGui.TextUnformatted(L.F("Base travel speed: {0:0.00}x", b.Plan.MoveSpeed));foreach(var e in b.Plan.JobSpeed)Wrapped(L.F("{0}: {1:0.00}x", L.Resource(e.Key), b.Plan.MoveSpeed*e.Value));}
        else
        {
            ImGui.TextUnformatted(L.F("Workers {0}/{1}", city.Citizens.Count(v=>v.Work==b.Id), b.Plan.Jobs));bool enabled=b.Enabled;if(ImGui.Checkbox(L.T("Workplace enabled"),ref enabled)){b.Enabled=enabled;city.Assign();}
            int priority=b.Priority;if(ImGui.SliderInt(L.T("Priority"),ref priority,0,3)){b.Priority=priority;city.Assign();}
            foreach(var e in b.Plan.Input)ImGui.TextUnformatted(L.F("Uses {0:0.##} {1} / cycle", e.Value, L.Resource(e.Key)));foreach(var e in b.Plan.Output)ImGui.TextUnformatted(L.F("Makes {0:0.##} {1} / cycle", e.Value, L.Resource(e.Key)));
        }
        if(!b.Plan.Indestructible&&Button(L.T("Demolish / half emerald refund"),new(-1,26))){city.Remove(b);if(!city.Buildings.Contains(b))Inspected=-1;}
    }
    void Save(City city,string name)
    {city.Save(SaveStore.PathFor(name));currentSave=Path.GetFileNameWithoutExtension(SaveStore.PathFor(name));city.Log(L.F("Saved as {0}.",currentSave));MenuOpen=false;ImGui.CloseCurrentPopup();}
    void DrawMenu(ref City city,int w,int h)
    {
        if(MenuOpen&&!ImGui.IsPopupOpen(L.T("Game menu")+"###GameMenu"))ImGui.OpenPopup(L.T("Game menu")+"###GameMenu");
        ImGui.SetNextWindowPos(new(w*.5f,h*.5f),ImGuiCond.Always,new(.5f,.5f));ImGui.SetNextWindowSize(new(460,Math.Min(660,h-48)));
        bool open=MenuOpen;
        if(ImGui.BeginPopupModal(L.T("Game menu")+"###GameMenu",ref open,ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoSavedSettings))
        {
            if(!MenuOpen){ImGui.CloseCurrentPopup();ImGui.EndPopup();return;}
            Title("VillageCraft / "+L.T(MenuPage=="Main"?"Paused":MenuPage));ImGui.Separator();
            try
            {
                if(MenuPage=="Main")
                {
                    if(Button(L.T("Resume game"),new(-1,26))){MenuOpen=false;ImGui.CloseCurrentPopup();}
                    if(Button(L.T("New Game"),new(-1,26)))MenuPage="New Game";
                    if(Button(L.T("Load Game"),new(-1,26))){saves=SaveStore.List();MenuPage="Load Game";loadName=saves.FirstOrDefault()??"";menuError="";}
                    ImGui.BeginDisabled(city.GameOver);
                    if(Button(L.T("Save Game"),new(-1,26))){if(currentSave=="")MenuPage="Save Game As";else Save(city,currentSave);}
                    if(Button(L.T("Save Game As"),new(-1,26))){MenuPage="Save Game As";saveName=currentSave==""?L.T("My village"):currentSave;overwrite=false;}
                    ImGui.EndDisabled();
                    if(Button(L.T("Options"),new(-1,26)))MenuPage="Options";
                    if(Button(L.T("Exit"),new(-1,26)))ExitRequested=true;
                    Hint("Escape closes this menu. The simulation is paused.");
                }
                else
                {
                    if(Button(L.T("< Back"))){MenuPage="Main";menuError="";}ImGui.Separator();
                    if(MenuPage=="Load Game")
                    {
                        Hint("Saves in executable/UserData");ImGui.BeginChild("Save list",new(0,300),ImGuiChildFlags.Borders);
                        foreach(string name in saves)if(ImGui.Selectable(name,loadName==name))loadName=name;
                        if(saves.Length==0)Wrapped(L.T("No saved villages yet."));ImGui.EndChild();
                        if(loadName!=""&&Button(L.T("Load selected village"),new(-1,28))){var loaded=City.Load(SaveStore.LoadPathFor(loadName));city=loaded;currentSave=loadName;ResetPanels();Paused=false;MenuOpen=false;ImGui.CloseCurrentPopup();city.Log(L.F("Loaded {0}.",currentSave));}
                    }
                    if(MenuPage=="Save Game As")
                    {
                        ImGui.TextUnformatted(L.T("Village save name"));ImGui.SetNextItemWidth(-1);if(ImGui.InputText("##saveName",ref saveName,70))overwrite=false;
                        Hint("Stored as UserData/<name>.vcs (binary)");
                        if(Button(overwrite?L.T("Confirm overwrite"):L.T("Save village"),new(-1,28)))
                        {string path=SaveStore.PathFor(saveName);if(File.Exists(path)&&!overwrite){overwrite=true;menuError=L.T("A save with this name exists. Confirm overwrite or change the name.");}else Save(city,saveName);}
                    }
                    if(MenuPage=="Options")Options(city);
                    if(MenuPage=="New Game")
                    {
                        Title("Create a village");ImGui.InputInt(L.T("Width (tiles)"),ref newWidth);ImGui.InputInt(L.T("Height (tiles)"),ref newHeight);
                        foreach(int size in new[]{32,64,128,256,512}){if(SmallButton(size+"x"+size))newWidth=newHeight=size;if(size!=512)ImGui.SameLine();}
                        Wrapped(L.F("Choose 32–512 tiles per axis. Default: {0}x{1}. Lakes, gentle mountains and natural trees surround a flat starter village. Save before replacing the current session.", Catalog.Data.Game.DefaultMapWidth, Catalog.Data.Game.DefaultMapHeight));
                        ImGui.Checkbox(L.T("Guided tutorial"),ref newTutorial);ImGui.SetNextItemWidth(180);ImGui.InputInt(L.T("Seed (0 = random)"),ref newSeed);
                        if(Button(L.T("Create village / replace session"),new(-1,28))){city=City.StartVillage(newWidth,newHeight,seed:newSeed==0?null:newSeed,tutorial:newTutorial);currentSave="";ResetPanels();Paused=false;MenuOpen=false;ImGui.CloseCurrentPopup();}
                    }
                }
            }
            catch(Exception e)when(e is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException or InvalidDataException){menuError=e.Message;}
            if(menuError!="")Wrapped(menuError);ImGui.EndPopup();
        }
        if(!open)MenuOpen=false;
    }
    static void IntOption(string label,int value,int min,int max,Action<int> set){if(ImGui.SliderInt(L.T(label),ref value,min,max))set(value);}
    void Options(City city)
    {
        var g=GraphicsOptions.Current;ImGui.PushItemWidth(190);
        if(ImGui.BeginCombo(L.T("Language"),L.Languages.First(l=>l.Id==L.Current).Name))
        {
            foreach(var language in L.Languages)if(ImGui.Selectable(language.Name,L.Current==language.Id)){L.Set(language.Id);g.Language=language.Id;g.Save();}
            ImGui.EndCombo();
        }
        float scale=g.UiScale;
        if(ImGui.SliderFloat(L.T("UI scale"),ref scale,1,2,"%.2fx")){g.UiScale=scale;g.Save();}
        Title("Display");ImGui.PushItemWidth(250);
        string[] displayModes=[L.T("Windowed"),L.T("Fullscreen"),L.T("Borderless fullscreen")];
        if(ImGui.BeginCombo(L.T("Display mode"),displayModes[g.DisplayMode]))
        {
            for(int mode=0;mode<displayModes.Length;mode++)
                if(ImGui.Selectable(displayModes[mode],g.DisplayMode==mode))g.DisplayMode=mode;
            ImGui.EndCombo();
        }
        ImGui.BeginDisabled(g.DisplayMode==2);
        if(ImGui.BeginCombo(L.T("Resolution"),$"{g.DisplayWidth}x{g.DisplayHeight}"))
        {
            foreach(var resolution in DisplaySettings.Resolutions())
                if(ImGui.Selectable($"{resolution.Width}x{resolution.Height}",g.DisplayWidth==resolution.Width&&g.DisplayHeight==resolution.Height))
                {g.DisplayWidth=resolution.Width;g.DisplayHeight=resolution.Height;}
            ImGui.EndCombo();
        }
        ImGui.EndDisabled();
        if(g.DisplayMode==2)Hint("Borderless uses the desktop resolution.");
        if(Button(L.T("Apply display settings"),new(-1,26)))DisplaySettings.RequestApply();
        Hint("Windowed size is fitted to the screen to keep the title bar visible.");
        ImGui.PopItemWidth();ImGui.Separator();Title("Graphics");int quality=g.Quality;
        if(ImGui.Combo(L.T("Quality"),ref quality,L.T("Low")+"\0"+L.T("Medium")+"\0"+L.T("High")+"\0")){g.Quality=quality;Assets.RefreshFilters();RefreshThumbnails=true;}
        bool textures=g.Textures;if(ImGui.Checkbox(L.T("Minecraft textures"),ref textures)){g.Textures=textures;RefreshThumbnails=true;}
        bool shadows=g.Shadows;if(ImGui.Checkbox(L.T("Ground shadows"),ref shadows))g.Shadows=shadows;
        bool water=g.AnimateWater;if(ImGui.Checkbox(L.T("Animated water"),ref water))g.AnimateWater=water;
        int fps=Array.IndexOf(new[]{30,60,120,0},g.Fps);if(ImGui.Combo(L.T("Frame limit"),ref fps,"30 FPS\0"+"60 FPS\0"+"120 FPS\0"+L.T("Unlimited")+"\0")){g.Fps=new[]{30,60,120,0}[fps];Raylib.SetTargetFPS(g.Fps);}
        if(Button(L.T("Save graphics preferences"),new(-1,26))){g.Save();menuError=L.T("Graphics preferences saved.");}
        ImGui.Separator();Title("Village rules / saved with this village");var r=city.Rules;
        bool raids=r.RaidsEnabled;if(ImGui.Checkbox(L.T("Enable raids"),ref raids))r.RaidsEnabled=raids;
        bool nights=r.NightMonsters;if(ImGui.Checkbox(L.T("Nighttime monsters"),ref nights))r.NightMonsters=nights;
        if(Button(L.T("Reset tutorial"))){g.TutorialDismissed=false;g.Save();city.TutorialEnabled=true;city.TutorialStep=0;city.TutorialInspected=false;city.TutorialRoadBuilt=false;city.TutorialTreesHarvested=0;newTutorial=true;}
        bool tutorial=city.TutorialEnabled;if(ImGui.Checkbox(L.T("Guided tutorial"),ref tutorial))city.TutorialEnabled=tutorial;
        IntOption("Peaceful nights",city.PeaceDays,0,30,v=>city.PeaceDays=v);
        IntOption("Monsters per night",r.NightCount,0,12,v=>r.NightCount=v);
        IntOption("Attack every N days",r.RaidInterval,1,1000,v=>r.RaidInterval=v);
        Hint("Next attack uses the next multiple of this interval.");
        IntOption("Base wave size",r.WaveBase,1,100,v=>r.WaveBase=v);IntOption("Growth per wave",r.WaveGrowth,0,50,v=>r.WaveGrowth=v);
        IntOption("Hostile limit",r.MaxRaiders,1,256,v=>r.MaxRaiders=v);IntOption("Population limit",r.MaxPopulation,12,1024,v=>r.MaxPopulation=v);
        IntOption("Animals per pen",r.AnimalsPerPen,2,32,v=>r.AnimalsPerPen=v);IntOption("Golems per workshop",r.GolemsPerWorkshop,0,8,v=>r.GolemsPerWorkshop=v);
        float minutes=r.DayLength/60;if(ImGui.SliderFloat(L.T("Minutes per day"),ref minutes,1,60,"%.1f")){float day=minutes*60;city.DayTime*=day/r.DayLength;r.DayLength=day;}
        Hint(L.F("1 game minute = {0:0.00}s at 1x",r.DayLength/1440));
        float cycle=r.ProductionCycle;if(ImGui.SliderFloat(L.T("Production seconds"),ref cycle,.5f,10,"%.1f"))r.ProductionCycle=cycle;
        Wrapped(L.T("Rules apply immediately and persist when you save the village. Lower caps stop new spawns without deleting existing villagers or animals. Map dimensions are chosen under New Game."));
        ImGui.PopItemWidth();
    }
}
