using System.Numerics;
using ImGuiNET;
using Raylib_cs;
namespace VillageCraft;

partial class GameUi
{
    readonly record struct Panel(string Kind,int Id);
    readonly record struct UiRect(float X,float Y,float Width,float Height)
    {public bool Contains(Vector2 p)=>p.X>=X&&p.Y>=Y&&p.X<X+Width&&p.Y<Y+Height;}
    readonly List<UiRect> rects=[];
    readonly HashSet<Panel> pinned=[];
    readonly HashSet<string> pinnedManagement=[];
    public int InspectedMonster=-1;
    bool buildOpen;
    int panelOrder;
    static uint Color(float value,float alpha=1)=>ImGui.ColorConvertFloat4ToU32(new(value,value,value,alpha));
    static uint GoldColor=>ImGui.ColorConvertFloat4ToU32(new(1,.85f,.4f,1));
    public void Theme()
    {
        UiIcons.Load();
        var s=ImGui.GetStyle();s.WindowRounding=s.FrameRounding=s.ChildRounding=s.PopupRounding=s.ScrollbarRounding=s.GrabRounding=0;
        s.WindowPadding=new(8,5);s.FramePadding=new(6,3);s.ItemSpacing=new(6,4);s.ItemInnerSpacing=new(4,3);
        s.WindowBorderSize=2;s.PopupBorderSize=2;s.FrameBorderSize=1;s.ScrollbarSize=10;s.GrabMinSize=8;
        s.Colors[(int)ImGuiCol.WindowBg]=new(.12f,.12f,.12f,.96f);
        s.Colors[(int)ImGuiCol.ChildBg]=new(.08f,.08f,.08f,.92f);
        s.Colors[(int)ImGuiCol.PopupBg]=new(.12f,.12f,.12f,.99f);
        s.Colors[(int)ImGuiCol.Border]=new(.025f,.025f,.025f,1);
        s.Colors[(int)ImGuiCol.ModalWindowDimBg]=new(0,0,0,.64f);
        s.Colors[(int)ImGuiCol.TitleBg]=new(.23f,.23f,.23f,1);
        s.Colors[(int)ImGuiCol.TitleBgActive]=new(.32f,.32f,.32f,1);
        s.Colors[(int)ImGuiCol.Button]=new(.43f,.43f,.43f,1);
        s.Colors[(int)ImGuiCol.ButtonHovered]=new(.55f,.55f,.65f,1);
        s.Colors[(int)ImGuiCol.ButtonActive]=new(.31f,.31f,.38f,1);
        s.Colors[(int)ImGuiCol.Header]=new(.37f,.37f,.37f,1);
        s.Colors[(int)ImGuiCol.HeaderHovered]=new(.5f,.5f,.58f,1);
        s.Colors[(int)ImGuiCol.HeaderActive]=new(.42f,.42f,.5f,1);
        s.Colors[(int)ImGuiCol.FrameBg]=new(.055f,.055f,.055f,1);
        s.Colors[(int)ImGuiCol.FrameBgHovered]=new(.19f,.19f,.19f,1);
        s.Colors[(int)ImGuiCol.FrameBgActive]=new(.24f,.24f,.24f,1);
        s.Colors[(int)ImGuiCol.Text]=new(.94f,.94f,.94f,1);
        s.Colors[(int)ImGuiCol.ResizeGrip]=new(.35f,.35f,.35f,1);
        s.Colors[(int)ImGuiCol.ResizeGripHovered]=new(.65f,.65f,.65f,1);
        s.Colors[(int)ImGuiCol.ResizeGripActive]=new(.8f,.75f,.45f,1);
        s.Colors[(int)ImGuiCol.PlotHistogram]=new(.45f,.72f,.2f,1);
        s.Colors[(int)ImGuiCol.ScrollbarGrab]=new(.4f,.4f,.4f,1);
        s.Colors[(int)ImGuiCol.SliderGrab]=new(.66f,.66f,.66f,1);
        s.Colors[(int)ImGuiCol.CheckMark]=new(.7f,.9f,.5f,1);
    }
    void CaptureRect()
    {
        var pos=ImGui.GetWindowPos();var size=ImGui.GetWindowSize();rects.Add(new(pos.X,pos.Y,size.X,size.Y));
    }
    static void Bevel(Vector2 min,Vector2 max,bool selected=false)
    {
        var draw=ImGui.GetWindowDrawList();uint light=selected?GoldColor:Color(.72f),dark=Color(.1f);
        draw.AddLine(min+new Vector2(1,1),new(max.X-2,min.Y+1),light,1);
        draw.AddLine(min+new Vector2(1,1),new(min.X+1,max.Y-2),light,1);
        draw.AddLine(new(min.X+1,max.Y-2),max-new Vector2(2,2),dark,2);
        draw.AddLine(new(max.X-2,min.Y+1),max-new Vector2(2,2),dark,2);
        if(selected)draw.AddRect(min,max,GoldColor,0,ImDrawFlags.None,2);
    }
    static bool Button(string text,Vector2 size=default)
    {
        float available=ImGui.GetContentRegionAvail().X,limit=size.X>0?size.X:available;
        string label=text;
        if(ImGui.CalcTextSize(text).X+ImGui.GetStyle().FramePadding.X*2>limit)
        {
            while(label.Length>0&&ImGui.CalcTextSize(label+"...").X+ImGui.GetStyle().FramePadding.X*2>limit)label=label[..^1];
            label+="...";if(size.X==0)size.X=limit;
        }
        bool clicked=ImGui.Button(label==text?text:label+"###"+text,size);
        Bevel(ImGui.GetItemRectMin(),ImGui.GetItemRectMax());if(label!=text)Tooltip(text);return clicked;
    }
    static bool SmallButton(string text)
    {
        bool clicked=ImGui.SmallButton(text);Bevel(ImGui.GetItemRectMin(),ImGui.GetItemRectMax());return clicked;
    }
    static bool IconButton(string id,string icon,string hint,bool selected=false,float size=28)
    {
        ImGui.PushID(id);
        if(selected)ImGui.PushStyleColor(ImGuiCol.Button,new Vector4(.44f,.4f,.24f,1));
        bool clicked=ImGui.Button("##icon",new(size,size));
        var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
        if(selected)ImGui.PopStyleColor();
        Bevel(min,max,selected);UiIcons.Draw(icon,min+new Vector2((size-16)/2));
        Tooltip(hint);ImGui.PopID();return clicked;
    }
    static void Stat(string icon,string value,string hint,string texture="")
    {
        var point=ImGui.GetCursorScreenPos();ImGui.Dummy(new(18,18));
        if(texture!="")
        {
            var asset=Assets.Get(texture);
            if(asset.Id!=0)ImGui.GetWindowDrawList().AddImage((IntPtr)asset.Id,point,point+new Vector2(18));
            else UiIcons.Draw(icon,point);
        }
        else UiIcons.Draw(icon,point);
        Tooltip(hint);ImGui.SameLine(0,4);ImGui.TextUnformatted(value);Tooltip(hint);ImGui.SameLine(0,14);
    }
    public static int ClockFrame(float phase)=>((int)(Math.Clamp(phase,0,.999999f)*64)+32)%64;
    void DrawTutorial(City city)
    {
        if(!city.TutorialEnabled)return;
        var stages=Catalog.Data.Tutorial.Stages;
        Window("Village guide",8,hudHeight+16,304,158);
        if(city.TutorialStep>=stages.Count)
        {
            Title("Tutorial complete");Hint("Your village is ready to grow.");
            if(Button(L.T("Close")))city.TutorialEnabled=false;ImGui.End();return;
        }
        var stage=stages[city.TutorialStep];Title(L.Get("tutorial."+stage.Id+".title",stage.Title));
        Wrapped(L.Get("tutorial."+stage.Id+".description",stage.Description));
        bool done=city.TutorialGoalMet(stage.Goal);
        Colored(done?Accent:Muted,done?L.T("Goal complete"):L.T("Goal in progress"));
        ImGui.BeginDisabled(!done);if(Button(L.T("Continue")))city.TutorialStep++;ImGui.EndDisabled();
        ImGui.SameLine();if(Button(L.T("Skip tutorial"))){city.TutorialEnabled=false;GraphicsOptions.Current.TutorialDismissed=true;GraphicsOptions.Current.Save();newTutorial=false;}
        ImGui.End();
    }
    int hudHeight=58;
    void DrawTopBar(City city,int w)
    {
        var resources=Catalog.Resources.Where(id=>id!="Torches").ToArray();
        float rowWidth=0;int rows=1;
        foreach(string id in resources){float size=18+6+ImGui.CalcTextSize(city.Stock[id].ToString("0")).X+8;if(rowWidth+size>w-36){rows++;rowWidth=0;}rowWidth+=size;}
        bool separateMenus=w<850;
        hudHeight=58+(rows-1)*22+(separateMenus?28:0);
        Window("Village HUD",8,6,w-16,hudHeight);rowWidth=0;
        foreach(string id in resources)
        {
            float size=18+6+ImGui.CalcTextSize(city.Stock[id].ToString("0")).X+8;
            if(rowWidth+size>w-36){ImGui.NewLine();rowWidth=0;}
            Resource(id,city.Stock[id]);ImGui.SameLine(0,8);rowWidth+=size;
        }
        ImGui.NewLine();
        string clock=city.ClockTime;
        Stat("map",$"{city.Day}  {clock}",L.F("Day {0}: {1}",city.Day,clock),$"item/clock_{ClockFrame(city.DayPhase):00}.png");
        Stat("people",$"{city.Citizens.Count}/{city.Capacity}",L.T("Residents"));
        Stat("happy",$"{city.Happiness:0}%",L.T("Happiness"));
        Stat("water",$"{city.Water}/{city.Citizens.Count}",L.T("Water"));
        Stat("raid",city.Enemies.Count>0?city.Enemies.Count.ToString():city.RaidIn<0?"-":city.RaidIn.ToString(),city.Enemies.Count>0?L.T("Hostiles"):L.T("Days until raid")+(city.Day<=city.PeaceDays?"\n"+L.F("Night monsters begin on day {0}",city.PeaceDays+1):""));
        Stat("map",$"{city.Width}x{city.Height}",L.T("Map size"));
        ImGui.SetCursorScreenPos(new(w-176,32+(rows-1)*22+(separateMenus?26:0)));
        foreach(var (tab,icon) in new[]{("People","people"),("Economy","economy"),("Defense","shield"),("Journal","book")})
        {
            if(IconButton("open_"+tab,icon,L.T(tab),page==tab,24)){page=page==tab?"Build":tab;}
            ImGui.SameLine(0,4);
        }
        if(IconButton("menu","menu",L.T("MENU [Esc]"),false,24))Escape();
        ImGui.End();
    }
    internal void ShowBuildCategory(string id){category=id;buildOpen=true;}
    public void UseInspectTool(){InspectMode=true;Selected="";buildOpen=false;}
    public void ToggleTool(){if(IsBuilding)UseInspectTool();else buildOpen=!buildOpen;}
    void CloseTransientPanels(){page="Build";Inspected=InspectedCitizen=InspectedMonster=-1;}
    public void ResetPanels(){CloseTransientPanels();pinned.Clear();pinnedManagement.Clear();UseInspectTool();}
    void Focus(string kind,int id)
    {
        CloseTransientPanels();UseInspectTool();
        if(kind=="villager")InspectedCitizen=id;else if(kind=="monster")InspectedMonster=id;else Inspected=id;
    }
    public void PinCurrent()
    {
        if(Inspected>=0)pinned.Add(new("building",Inspected));
        if(InspectedCitizen>=0)pinned.Add(new("villager",InspectedCitizen));
        if(InspectedMonster>=0)pinned.Add(new("monster",InspectedMonster));
    }
    public int PinnedCount=>pinned.Count;
    bool BeginPanel(string key,string title,int x,int y,int height)
    {
        ImGui.SetNextWindowPos(new(x,y),ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new(264,height),ImGuiCond.FirstUseEver);
        bool open=true;
        ImGui.Begin(title+"###"+key,ref open,ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoSavedSettings);
        CaptureRect();return open;
    }
    void DrawManagement(City city,int w,int h)
    {
        int order=0;
        foreach(string tab in pinnedManagement.Concat(page=="Build"?Array.Empty<string>():new[]{page}).Distinct().ToArray())
        {
            bool open=BeginPanel("management_"+tab,L.T(tab),8+order++*272,hudHeight+16,Math.Min(460,h-240));
            if(IconButton("pin","pin",pinnedManagement.Contains(tab)?L.T("Unpin panel"):L.T("Pin panel"),pinnedManagement.Contains(tab),22))
            {if(!pinnedManagement.Remove(tab))pinnedManagement.Add(tab);}
            ImGui.Separator();
            if(tab=="People")PeoplePanel(city);
            if(tab=="Economy")EconomyPanel(city);
            if(tab=="Defense")DefensePanel(city);
            if(tab=="Journal"){foreach(string entry in city.Events){Wrapped(entry);ImGui.Separator();}}
            ImGui.End();
            if(!open){pinnedManagement.Remove(tab);if(page==tab)page="Build";}
        }
    }
    void DrawInspectors(City city,int w,int h)
    {
        panelOrder=0;var all=new List<Panel>(pinned);
        if(Inspected>=0)all.Add(new("building",Inspected));
        if(InspectedCitizen>=0)all.Add(new("villager",InspectedCitizen));
        if(InspectedMonster>=0)all.Add(new("monster",InspectedMonster));
        foreach(var panel in all.Distinct().ToArray())
        {
            var building=panel.Kind=="building"?city.Buildings.FirstOrDefault(b=>b.Id==panel.Id):null;
            var person=panel.Kind=="villager"?city.Citizens.FirstOrDefault(v=>v.Id==panel.Id):null;
            var monster=panel.Kind=="monster"?city.Enemies.Concat(city.Defenders).Concat(city.Animals).FirstOrDefault(m=>m.Id==panel.Id):null;
            if(building==null&&person==null&&monster==null){Close(panel);continue;}
            string name=building!=null?L.Name(building.Plan):person!=null?person.FullName:L.Entity(monster!.Type);
            int columns=Math.Max(1,(w-16)/272),column=panelOrder%columns,row=panelOrder/columns;panelOrder++;
            bool open=BeginPanel("inspect_"+panel.Kind+"_"+panel.Id,name,w-272-column*272,hudHeight+16+row*36,Math.Min(430,h-240));
            if(IconButton("pin","pin",pinned.Contains(panel)?L.T("Unpin panel"):L.T("Pin panel"),pinned.Contains(panel),22))
            {if(!pinned.Remove(panel))pinned.Add(panel);}
            ImGui.Separator();
            if(building!=null)Inspector(city,building,w,h);
            if(person!=null)VillagerInspector(city,person,w,h);
            if(monster!=null)MonsterInspector(city,monster);
            ImGui.End();if(!open)Close(panel);
        }
    }
    void Close(Panel panel)
    {
        pinned.Remove(panel);
        if(panel.Kind=="building"&&Inspected==panel.Id)Inspected=-1;
        if(panel.Kind=="villager"&&InspectedCitizen==panel.Id)InspectedCitizen=-1;
        if(panel.Kind=="monster"&&InspectedMonster==panel.Id)InspectedMonster=-1;
    }
    void MonsterInspector(City city,Creature monster)
    {
        float max=Catalog.Data.Units.TryGetValue(monster.Type,out var unit)?unit.Health:20;
        if(monster.LeatherHelmet)Hint("Leather helmet / sunlight protection");
        if(monster.BurningInSun)Hint("Burning in sunlight");
        if(monster.BossWave)Hint("Boss wave");
        ImGui.TextUnformatted(L.F("Health {0:0}/{1:0}",monster.Health,max));ImGui.ProgressBar(monster.Health/max,new(-1,8),"");
        if(unit!=null)
        {
            ImGui.TextUnformatted(L.F("Damage {0:0} / Range {1:0.0}",unit.Damage,unit.Range));
            ImGui.TextUnformatted(L.F("Speed {0:0.00} / Cooldown {1:0.0}s",unit.Speed,monster.Cooldown));
        }
        Hint(city.Enemies.Contains(monster)?L.T("Hostile"):L.T("Friendly"));
        if(monster.Tags!="")Wrapped(monster.Tags);
    }
    void DrawHotbar(City city,int w,int h)
    {
        float width=Math.Min(1160,w-16),left=(w-width)/2;
        if(buildOpen)
        {
            Window("Build inventory",left,h-152,width,102);
            ImGui.SetNextItemWidth(160);ImGui.InputTextWithHint("##search",L.T("Search..."),ref search,80);
            ImGui.SameLine();Hint(IsBuilding?L.Name(Catalog.Get(Selected)):L.T("Select a building"));
            ImGui.BeginChild("Prefab slots",new(width-245,66),ImGuiChildFlags.None,ImGuiWindowFlags.HorizontalScrollbar);
            foreach(var p in Catalog.Data.Prefabs.Where(p=>(search!=""||p.Category==category)&&L.Name(p).Contains(search,StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p.Cost.GetValueOrDefault("Emeralds")))
            {
                ImGui.PushID(p.Id);ImGui.BeginGroup();
                bool selected=IsBuilding&&Selected==p.Id;
                ImGui.PushStyleColor(ImGuiCol.Button,selected?new Vector4(.44f,.4f,.24f,1):new Vector4(.22f,.22f,.22f,1));
                bool clicked=thumbnails.TryGetValue(p.Id,out var t)&&ImGui.ImageButton("##prefab",(IntPtr)t.Texture.Id,new(64,42),new(0,1),new(1,0));
                if(TestActivatePrefab==p.Id){clicked=true;TestActivatePrefab=null;TestActivationCount++;}
                ImGui.PopStyleColor();Bevel(ImGui.GetItemRectMin(),ImGui.GetItemRectMax(),selected);
                Tooltip(L.Name(p)+"\n"+L.Description(p)+"\n"+string.Join(" / ",p.Cost.Where(e=>e.Value>0).Select(e=>L.F("{0:0} {1}",e.Value,L.Resource(e.Key)))));
                if(clicked)SelectPrefab(p.Id);
                ImGui.EndGroup();ImGui.PopID();ImGui.SameLine();
            }
            ImGui.EndChild();ImGui.SameLine();ImGui.BeginGroup();
            if(IsBuilding)
            {
                foreach(var cost in Catalog.Get(Selected).Cost.Where(e=>e.Value>0)){Resource(cost.Key,cost.Value,city.Stock[cost.Key]>=cost.Value);ImGui.SameLine();}ImGui.NewLine();
                if(IconButton("rotate","rotate",L.F("Rotate [R]  {0} deg",Rotation*90)))Rotation=(Rotation+1)%4;
                ImGui.SameLine();if(IconButton("cancel","close",L.T("Inspect tool")))UseInspectTool();
            }
            ImGui.EndGroup();ImGui.End();
        }
        Window("Hotbar",left,h-46,width,40);
        if(IconButton("inspect","inspect",L.T("Inspect tool"),InspectMode))UseInspectTool();ImGui.SameLine(0,5);
        var categories=new Dictionary<string,string>{{"Roads","road"},{"Housing","home"},{"Production","production"},{"Infrastructure","services"},{"Economy","economy"},{"Animals","animals"},{"Defense","shield"}};
        foreach(string c in Catalog.Data.Game.Categories.Concat(Catalog.Data.Prefabs.Select(p=>p.Category)).Distinct())
        {
            if(IconButton("category_"+c,categories.GetValueOrDefault(c,"build"),L.T(c),buildOpen&&category==c))
            {if(buildOpen&&category==c)buildOpen=false;else{buildOpen=true;category=c;}InspectMode=true;Selected="";}
            ImGui.SameLine(0,4);
        }
        ImGui.Dummy(new(10,0));ImGui.SameLine(0,4);
        if(IconButton("pause",Paused?"play":"pause",Paused?L.T("PLAY"):L.T("PAUSE"),Paused))Paused=!Paused;
        for(int i=1;i<=4;i++)
        {
            ImGui.SameLine(0,4);int speed=i switch{3=>4,4=>16,_=>i};
            if(IconButton("speed"+i,"speed"+speed,$"{speed}x [{i}]",!Paused&&Speed==speed))SetSpeed(i);
        }
        ImGui.SameLine(0,12);
        string message=city.Message;float available=ImGui.GetContentRegionAvail().X-8;
        while(message.Length>0&&ImGui.CalcTextSize(message+"...").X>available)message=message[..^1];
        if(message.Length<city.Message.Length)message+="...";
        ImGui.AlignTextToFramePadding();ImGui.TextUnformatted(message);Tooltip(city.Message);ImGui.End();
    }
}
