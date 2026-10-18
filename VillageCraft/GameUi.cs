using System.Numerics;
using ImGuiNET;
using Raylib_cs;
namespace VillageCraft;

partial class GameUi
{
    public string Selected=string.Empty;
    public int Rotation,Inspected=-1,InspectedCitizen=-1,Speed=1;
    bool inspectMode=true;
    public bool InspectMode {get=>inspectMode||string.IsNullOrEmpty(Selected);set=>inspectMode=value;}
    public bool IsBuilding=>!InspectMode&&!string.IsNullOrEmpty(Selected);
    public bool Paused,MenuOpen,ExitRequested,RefreshThumbnails;
    public string MenuPage="Main";
#if DEBUG
    internal string? TestActivatePrefab;
    internal bool TestTooltip;
    internal int TestActivationCount;
#endif
    string page="Build",category="Roads",search=string.Empty,currentSave=string.Empty,saveName=L.T("save.default_name"),loadName=string.Empty,menuError=string.Empty;
    ResourceType tradeResource=ResourceType.Food;
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
    public void Escape(){MenuOpen=!MenuOpen;MenuPage="Main";menuError=string.Empty;overwrite=false;}
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
    static void Resource(ResourceType id,float value,bool enough=true)
    {
        var texture=Assets.Get(Assets.Icons[id]);
        if(texture.Id!=0){ImGui.Image((IntPtr)texture.Id,new(18,18));Tooltip(L.Resource(id));}else ImGui.TextUnformatted(L.T("ui.unknown"));
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
#if DEBUG
        if(TestTooltip&&IsBuilding)
        {
            var p=Catalog.Get(Selected);ShowTooltip(L.Name(p)+"\n"+L.Description(p)+"\n"+string.Join(" / ",p.Cost.Where(e=>e.Value>0).Select(e=>L.F("format.resource_amount",e.Value,L.Resource(e.Key)))));
        }
        else
#endif
        if(!OverUi(Raylib.GetMousePosition())&&city.Land(hx,hz)&&IsBuilding)
        {
            var issue=city.PlacementIssue(Selected,hx,hz,Rotation);
            ShowTooltip((string.IsNullOrEmpty(issue)?L.T("ui.click_to_build"):L.T(issue))+"\n"+L.T("ui.r_rotate_tab_inspect_right_click_inspect_shift"));
        }
        DrawMenu(ref city,w,h);
    }
    void PeoplePanel(City city)
    {
        Title("ui.workforce");ImGui.TextUnformatted(L.F("ui.employed", city.Citizens.Count(v=>v.Work>=0), city.Citizens.Count));ImGui.TextUnformatted(L.F("ui.animals_guardians", city.Animals.Count, city.Defenders.Count));
        Wrapped(L.T("ui.unemployed_citizens_help_families_attract_residents_they_can"));
        if(Button(L.T("people.reassign_professions"),new(-1,26)))city.ReassignProfessions();
        Hint(L.T("ui.assign_specialists_to_connected_workplaces_surplus_villagers_become"));
        Hint(L.F("people.growing_babies",city.Babies.Count));
        ImGui.Separator();foreach(var v in city.Citizens)
        {
            ImGui.PushID(v.Id);if(ImGui.Selectable(L.F("villager.name_health", v.FullName, v.Health))){Focus("villager",v.Id);}
            var profession=Catalog.Data.Professions[v.Profession];
            if(ImGui.BeginCombo("##profession",L.Name(profession)+" / "+(city.Efficiency(v)*100).ToString("0")+"%"))
            {
        foreach(var p in Catalog.Data.Professions.Values){if(ImGui.Selectable(L.Name(p),p.Id==v.Profession))city.Train(v,p.Id);Tooltip(string.Join(" / ",p.TrainingCost.Select(e=>L.F("format.resource_amount", e.Value, L.Resource(e.Key)))));}ImGui.EndCombo();
            }
            Hint(L.Task(v.Task));ImGui.PopID();
        }
    }
    void EconomyPanel(City city)
    {
        Title("ui.economy");Hint("ui.goods_convert_to_emeralds_every_day_at_06");ImGui.TextUnformatted(L.F("ui.balance_day", city.Balance));float tax=city.Tax;if(ImGui.SliderFloat(L.T("ui.daily_goods_contribution"),ref tax,0,6,"%.1f"))city.Tax=tax;
        ImGui.TextUnformatted(L.F("ui.food_reserve_days", city.FoodDays));ImGui.Separator();Title("ui.trading_hall");Hint("ui.sell_requests_are_delivered_to_the_trading_hall");
        if(ImGui.BeginCombo(L.T("ui.resource"),L.Resource(tradeResource))){foreach(ResourceType r in Catalog.Resources.Where(r=>!City.Currency(r)))if(ImGui.Selectable(L.Resource(r),tradeResource==r))tradeResource=r;ImGui.EndCombo();}
        var resource=Catalog.Data.ResourceDefinitions.First(r=>r.Id==tradeResource);float price=resource.BuyPrice;
        if(Button(L.F("ui.buy_20_emeralds", price),new(-1,26)))city.Trade(tradeResource,true);
        if(Button(L.F("ui.sell_20_emeralds", resource.SellPrice),new(-1,26)))city.Trade(tradeResource,false);
        ImGui.Separator();Title(L.F("ui.equipment_armor_tools", city.ArmorLevel, city.ToolLevel));
        Hint(L.F("ui.damage_reduction_work_rate_x", city.ArmorReduction*100, city.ToolEfficiency));
        foreach(var u in Catalog.Data.Upgrades.Where(u=>u.Level==(u.Kind==EquipmentKind.Armor?city.ArmorLevel:city.ToolLevel)+1))
        {if(Button(L.Name(u),new(-1,26)))city.Upgrade(u.Id);Tooltip(string.Join(" / ",u.Cost.Select(e=>L.F("format.resource_amount", e.Value, L.Resource(e.Key))))+L.T("ui.requires_a_staffed_forge"));}
        if(city.EnchantedArmor)Hint("ui.enchantment_active");
        else {if(Button(L.T("ui.enchant_armor"),new(-1,26)))city.EnchantArmor();Tooltip(string.Join(" / ",Catalog.Data.Economy.EnchantmentCost.Select(e=>L.F("format.resource_amount",e.Value,L.Resource(e.Key))))); }
        Hint(L.T("ui.a_staffed_mage_hall_and_armor_are_required"));
        ImGui.Separator();Title("ui.animal_care");foreach(string type in new[]{"sheep","pig","chicken","cow"})ImGui.TextUnformatted(L.F("ui.young", L.Entity(type), city.Animals.Count(a=>a.Type==type), city.Animals.Count(a=>a.Type==type&&a.Age<3)));
    }
    void DefensePanel(City city)
    {
        Title("ui.village_defense");ImGui.TextUnformatted(city.RaidIn<0?L.T("ui.raids_disabled"):L.F("ui.next_raid_day", city.Day+city.RaidIn));ImGui.TextUnformatted(L.F("ui.waves_kills", city.Waves, city.Kills));ImGui.TextUnformatted(L.F("ui.hostiles", city.Enemies.Count));
        foreach(var g in city.Defenders.GroupBy(d=>d.Type))ImGui.TextUnformatted(L.F("ui.label_2", L.Entity(g.Key), g.Count()));ImGui.Separator();
        foreach(var b in city.Buildings.Where(b=>!string.IsNullOrEmpty(b.Plan.Defender))){if(ImGui.Selectable(L.F("ui.label", L.Name(b.Plan), b.Id))){Focus("building",b.Id);}Hint(L.F("ui.staff", city.Citizens.Count(v=>v.Work==b.Id), b.Plan.Jobs));}
        Wrapped(L.T("ui.walls_and_gates_block_raiders_warriors_intercept_archers"));
    }
    public void InspectRay(City city,Ray ray,Vector2 tile)
    {
        InspectAt(city,tile);float nearest=float.MaxValue;string kind=string.Empty;int id=-1;
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
        ImGui.TextUnformatted(L.F("villager.health", v.Health));ImGui.ProgressBar(v.Health/50,new(-1,10),string.Empty);
        Hint(L.Task(v.Task));ImGui.TextUnformatted(L.F("ui.efficiency", city.Efficiency(v)*100));
        ImGui.TextUnformatted(L.F("ui.work_progress_s", v.WorkTime, city.Rules.ProductionCycle));
        ImGui.TextUnformatted(L.F("ui.damage_reduction_work_rate_x", city.ArmorReduction*100, city.ToolEfficiency));
        var home=city.Buildings.FirstOrDefault(b=>b.Id==v.Home);var work=city.Buildings.FirstOrDefault(b=>b.Id==v.Work);
        Wrapped(L.F("ui.home",home==null?L.T("ui.homeless"):L.Name(home.Plan)));
        Wrapped(L.F("ui.workplace",work==null?L.T("ui.unemployed"):L.Name(work.Plan)));
        ImGui.Separator();Title("ui.inventory");
        Hint("ui.carried_items_are_separate_from_village_stock_food");
        foreach(var item in v.Inventory.Where(e=>e.Value>0)) {Resource(item.Key,item.Value);ImGui.SameLine();ImGui.TextUnformatted(L.Resource(item.Key));}
        if(!v.Inventory.Any(e=>e.Value>0))Hint("ui.empty_inventory");
        foreach(var item in v.Cargo.Where(e=>e.Value>0))Wrapped(L.F("ui.cargo",item.Value,L.Resource(item.Key)));
        foreach(var effect in v.Effects)Wrapped(L.F("ui.effect_s",L.Effect(effect.Key),effect.Value));
        Hint("ui.villagers_collect_supplies_from_nearby_warehouses_or_sources");

    }
    void Inspector(City city,Building b,int w,int h)
    {
        Hint(L.F("ui.tile", b.X, b.Z));
        Wrapped(L.Description(b.Plan));ImGui.Separator();
        if(b.Plan.StorageCapacity>0){Title("ui.storage");Hint(L.F("ui.storage_2",b.Storage.Values.Sum(),b.Plan.StorageCapacity));foreach(var item in b.Storage.Where(e=>e.Value>0))Wrapped(L.F("format.resource_stock",L.Resource(item.Key),item.Value));}
        if(b.Plan.Harvest.Count>0)Hint(b.GrowthDays==0?L.T("ui.ready_to_harvest"):L.F("ui.regrows_in_days",b.GrowthDays));
        Colored(city.Connected(b,city.Roads())?Accent:Gold,city.Connected(b,city.Roads())?L.T("ui.connected"):L.T("ui.entrance_disconnected"));
        ImGui.TextUnformatted(L.F("building.health", b.Health, b.Plan.Health));ImGui.ProgressBar(b.Health/b.Plan.Health,new(-1,10),string.Empty);
        if(Button(L.T("ui.repair_2_wood_2_stone"),new(-1,26)))city.Repair(b);
        if(b.Plan.Path){ImGui.TextUnformatted(L.F("ui.base_travel_speed_x", b.Plan.MoveSpeed));foreach(var e in b.Plan.JobSpeed)Wrapped(L.F("ui.x", L.Activity(e.Key), b.Plan.MoveSpeed*e.Value));}
        else
        {
            ImGui.TextUnformatted(L.F("ui.workers", city.Citizens.Count(v=>v.Work==b.Id), b.Plan.Jobs));bool enabled=b.Enabled;if(ImGui.Checkbox(L.T("ui.workplace_enabled"),ref enabled)){b.Enabled=enabled;city.Assign();}
            int priority=b.Priority;if(ImGui.SliderInt(L.T("ui.priority"),ref priority,0,3)){b.Priority=priority;city.Assign();}
            foreach(var e in b.Plan.Input)ImGui.TextUnformatted(L.F("ui.uses_cycle", e.Value, L.Resource(e.Key)));foreach(var e in b.Plan.Output)ImGui.TextUnformatted(L.F("ui.makes_cycle", e.Value, L.Resource(e.Key)));
        }
        if(!b.Plan.Indestructible&&Button(L.T("ui.demolish_half_emerald_refund"),new(-1,26))){city.Remove(b);if(!city.Buildings.Contains(b))Inspected=-1;}
    }
    void Save(City city,string name)
    {city.Save(SaveStore.PathFor(name));currentSave=Path.GetFileNameWithoutExtension(SaveStore.PathFor(name));city.Log(L.F("ui.saved_as",currentSave));MenuOpen=false;ImGui.CloseCurrentPopup();}
    void DrawMenu(ref City city,int w,int h)
    {
        if(MenuOpen&&!ImGui.IsPopupOpen(L.T("menu.title")+"###GameMenu"))ImGui.OpenPopup(L.T("menu.title")+"###GameMenu");
        ImGui.SetNextWindowPos(new(w*.5f,h*.5f),ImGuiCond.Always,new(.5f,.5f));ImGui.SetNextWindowSize(new(460,Math.Min(660,h-48)));
        bool open=MenuOpen;
        if(ImGui.BeginPopupModal(L.T("menu.title")+"###GameMenu",ref open,ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoSavedSettings))
        {
            if(!MenuOpen){ImGui.CloseCurrentPopup();ImGui.EndPopup();return;}
            Title("VillageCraft / "+(MenuPage=="Main"?L.T("menu.paused"):L.Ui(MenuPage)));ImGui.Separator();
            try
            {
                if(MenuPage=="Main")
                {
                    if(Button(L.T("menu.resume"),new(-1,26))){MenuOpen=false;ImGui.CloseCurrentPopup();}
                    if(Button(L.T("ui.new_game"),new(-1,26)))MenuPage="New Game";
                    if(Button(L.T("ui.load_game"),new(-1,26))){saves=SaveStore.List();MenuPage="Load Game";loadName=saves.FirstOrDefault()??string.Empty;menuError=string.Empty;}
                    ImGui.BeginDisabled(city.GameOver);
                    if(Button(L.T("ui.save_game"),new(-1,26))){if(string.IsNullOrEmpty(currentSave))MenuPage="Save Game As";else Save(city,currentSave);}
                    if(Button(L.T("ui.save_game_as"),new(-1,26))){MenuPage="Save Game As";saveName=string.IsNullOrEmpty(currentSave)?L.T("save.default_name"):currentSave;overwrite=false;}
                    ImGui.EndDisabled();
                    if(Button(L.T("ui.options"),new(-1,26)))MenuPage="Options";
                    if(Button(L.T("menu.exit"),new(-1,26)))ExitRequested=true;
                    Hint("ui.escape_closes_this_menu_the_simulation_is_paused");
                }
                else
                {
                    if(Button(L.T("ui.back"))){MenuPage="Main";menuError=string.Empty;}ImGui.Separator();
                    if(MenuPage=="Load Game")
                    {
                        Hint("ui.saves_in_executable_userdata");ImGui.BeginChild("Save list",new(0,300),ImGuiChildFlags.Borders);
                        foreach(string name in saves)if(ImGui.Selectable(name,loadName==name))loadName=name;
                        if(saves.Length==0)Wrapped(L.T("ui.no_saved_villages_yet"));ImGui.EndChild();
                        if(!string.IsNullOrEmpty(loadName)&&Button(L.T("ui.load_selected_village"),new(-1,28))){var loaded=City.Load(SaveStore.LoadPathFor(loadName));city=loaded;currentSave=loadName;ResetPanels();Paused=false;MenuOpen=false;ImGui.CloseCurrentPopup();city.Log(L.F("ui.loaded",currentSave));}
                    }
                    if(MenuPage=="Save Game As")
                    {
                        ImGui.TextUnformatted(L.T("ui.village_save_name"));ImGui.SetNextItemWidth(-1);if(ImGui.InputText("##saveName",ref saveName,70))overwrite=false;
                        Hint("ui.stored_as_userdata_name_vcs_binary");
                        if(Button(overwrite?L.T("ui.confirm_overwrite"):L.T("ui.save_village"),new(-1,28)))
                        {string path=SaveStore.PathFor(saveName);if(File.Exists(path)&&!overwrite){overwrite=true;menuError=L.T("ui.a_save_with_this_name_exists_confirm_overwrite");}else Save(city,saveName);}
                    }
                    if(MenuPage=="Options")Options(city);
                    if(MenuPage=="New Game")
                    {
                        Title("ui.create_a_village");ImGui.InputInt(L.T("ui.width_tiles"),ref newWidth);ImGui.InputInt(L.T("ui.height_tiles"),ref newHeight);
                        foreach(int size in new[]{32,64,128,256,512}){if(SmallButton(size+"x"+size))newWidth=newHeight=size;if(size!=512)ImGui.SameLine();}
                        Wrapped(L.F("ui.choose_32512_tiles_per_axis_default_x_lakes", Catalog.Data.Game.DefaultMapWidth, Catalog.Data.Game.DefaultMapHeight));
                        ImGui.Checkbox(L.T("ui.guided_tutorial"),ref newTutorial);ImGui.SetNextItemWidth(180);ImGui.InputInt(L.T("ui.seed_0_random"),ref newSeed);
                        if(Button(L.T("ui.create_village_replace_session"),new(-1,28))){city=City.StartVillage(newWidth,newHeight,seed:newSeed==0?null:newSeed,tutorial:newTutorial);currentSave=string.Empty;ResetPanels();Paused=false;MenuOpen=false;ImGui.CloseCurrentPopup();}
                    }
                }
            }
            catch(Exception e)when(e is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException or InvalidDataException){menuError=e.Message;}
            if(!string.IsNullOrEmpty(menuError))Wrapped(menuError);ImGui.EndPopup();
        }
        if(!open)MenuOpen=false;
    }
    static void IntOption(string label,int value,int min,int max,Action<int> set){if(ImGui.SliderInt(L.T(label),ref value,min,max))set(value);}
    void Options(City city)
    {
        var g=GraphicsOptions.Current;ImGui.PushItemWidth(190);
        if(ImGui.BeginCombo(L.T("ui.language"),L.Languages.First(l=>l.Id==L.Current).Name))
        {
            foreach(var language in L.Languages)if(ImGui.Selectable(language.Name,L.Current==language.Id)){L.Set(language.Id);g.Language=language.Id;g.Save();}
            ImGui.EndCombo();
        }
        float scale=g.UiScale;
        if(ImGui.SliderFloat(L.T("ui.ui_scale"),ref scale,1,2,"%.2fx")){g.UiScale=scale;g.Save();}
        Title("ui.display");ImGui.PushItemWidth(250);
        string[] displayModes=[L.T("ui.windowed"),L.T("ui.fullscreen"),L.T("ui.borderless_fullscreen")];
        if(ImGui.BeginCombo(L.T("ui.display_mode"),displayModes[g.DisplayMode]))
        {
            for(int mode=0;mode<displayModes.Length;mode++)
                if(ImGui.Selectable(displayModes[mode],g.DisplayMode==mode))g.DisplayMode=mode;
            ImGui.EndCombo();
        }
        ImGui.BeginDisabled(g.DisplayMode==2);
        if(ImGui.BeginCombo(L.T("ui.resolution"),$"{g.DisplayWidth}x{g.DisplayHeight}"))
        {
            foreach(var resolution in DisplaySettings.Resolutions())
                if(ImGui.Selectable($"{resolution.Width}x{resolution.Height}",g.DisplayWidth==resolution.Width&&g.DisplayHeight==resolution.Height))
                {g.DisplayWidth=resolution.Width;g.DisplayHeight=resolution.Height;}
            ImGui.EndCombo();
        }
        ImGui.EndDisabled();
        if(g.DisplayMode==2)Hint("ui.borderless_uses_the_desktop_resolution");
        if(Button(L.T("ui.apply_display_settings"),new(-1,26)))DisplaySettings.RequestApply();
        Hint("ui.windowed_size_is_fitted_to_the_screen_to");
        ImGui.PopItemWidth();ImGui.Separator();Title("ui.graphics");int quality=g.Quality;
        if(ImGui.Combo(L.T("ui.quality"),ref quality,L.T("ui.low")+"\0"+L.T("ui.medium")+"\0"+L.T("ui.high")+"\0")){g.Quality=quality;Assets.RefreshFilters();RefreshThumbnails=true;}
        bool textures=g.Textures;if(ImGui.Checkbox(L.T("ui.minecraft_textures"),ref textures)){g.Textures=textures;RefreshThumbnails=true;}
        bool shadows=g.Shadows;if(ImGui.Checkbox(L.T("ui.ground_shadows"),ref shadows))g.Shadows=shadows;
        bool water=g.AnimateWater;if(ImGui.Checkbox(L.T("ui.animated_water"),ref water))g.AnimateWater=water;
        int fps=Array.IndexOf(new[]{30,60,120,0},g.Fps);if(ImGui.Combo(L.T("ui.frame_limit"),ref fps,"30 FPS\0"+"60 FPS\0"+"120 FPS\0"+L.T("ui.unlimited")+"\0")){g.Fps=new[]{30,60,120,0}[fps];Raylib.SetTargetFPS(g.Fps);}
        if(Button(L.T("ui.save_graphics_preferences"),new(-1,26))){g.Save();menuError=L.T("ui.graphics_preferences_saved");}
        ImGui.Separator();Title("ui.village_rules_saved_with_this_village");var r=city.Rules;
        bool raids=r.RaidsEnabled;if(ImGui.Checkbox(L.T("ui.enable_raids"),ref raids))r.RaidsEnabled=raids;
        bool nights=r.NightMonsters;if(ImGui.Checkbox(L.T("ui.nighttime_monsters"),ref nights))r.NightMonsters=nights;
        if(Button(L.T("ui.reset_tutorial"))){g.TutorialDismissed=false;g.Save();city.TutorialEnabled=true;city.TutorialStep=0;city.TutorialInspected=false;city.TutorialRoadBuilt=false;city.TutorialTreesHarvested=0;newTutorial=true;}
        bool tutorial=city.TutorialEnabled;if(ImGui.Checkbox(L.T("ui.guided_tutorial"),ref tutorial))city.TutorialEnabled=tutorial;
        IntOption("ui.peaceful_nights",city.PeaceDays,0,30,v=>city.PeaceDays=v);
        IntOption("options.night_spawn_interval",r.NightSpawnIntervalMinutes,5,660,v=>r.NightSpawnIntervalMinutes=v);
        Hint("options.night_spawn_window");
        IntOption("options.night_group_size",r.NightCount,0,12,v=>r.NightCount=v);
        IntOption("ui.attack_every_n_days",r.RaidInterval,1,1000,v=>r.RaidInterval=v);
        Hint("ui.next_attack_uses_the_next_multiple_of_this");
        IntOption("ui.base_wave_size",r.WaveBase,1,100,v=>r.WaveBase=v);IntOption("ui.growth_per_wave",r.WaveGrowth,0,50,v=>r.WaveGrowth=v);
        IntOption("ui.hostile_limit",r.MaxRaiders,1,256,v=>r.MaxRaiders=v);IntOption("ui.population_limit",r.MaxPopulation,12,1024,v=>r.MaxPopulation=v);
        IntOption("ui.animals_per_pen",r.AnimalsPerPen,2,32,v=>r.AnimalsPerPen=v);IntOption("ui.golems_per_workshop",r.GolemsPerWorkshop,0,8,v=>r.GolemsPerWorkshop=v);
        float minutes=r.DayLength/60;if(ImGui.SliderFloat(L.T("ui.minutes_per_day"),ref minutes,1,60,"%.1f")){float day=minutes*60;city.DayTime*=day/r.DayLength;r.DayLength=day;}
        Hint(L.F("format.game_minute_seconds",r.DayLength/1440));
        float birth=r.BirthChance*100;if(ImGui.SliderFloat(L.T("options.birth_chance"),ref birth,0,100,"%.0f%%"))r.BirthChance=birth/100;
        float cycle=r.ProductionCycle;if(ImGui.SliderFloat(L.T("ui.production_seconds"),ref cycle,.5f,10,"%.1f"))r.ProductionCycle=cycle;
        Wrapped(L.T("ui.rules_apply_immediately_and_persist_when_you_save"));
        ImGui.PopItemWidth();
    }
}
