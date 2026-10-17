using System.Numerics;
using ImGuiNET;
namespace VillageCraft;

partial class GameUi
{
    City? defeatedCity;
    string retrySave="";
    string[] retrySaves=[];
    bool confirmDelete;
    static void Statistic(string label,object value)=>ImGui.TextUnformatted(L.T(label)+": "+value);
    internal void RememberSave(string name)=>currentSave=name;
    internal bool Retry(ref City city,string name)
    {
        var loaded=City.Load(SaveStore.LoadPathFor(name));
        if(loaded.GameOver){loaded.Dispose();menuError=L.T("This save already contains a defeated village. Choose an earlier save.");return false;}
        city=loaded;currentSave=name;ResetPanels();Paused=false;MenuOpen=false;menuError="";return true;
    }
    void DrawGameOver(ref City city,int w,int h)
    {
        if(!ReferenceEquals(defeatedCity,city))
        {
            defeatedCity=city;MenuOpen=false;Paused=true;ResetPanels();menuError="";confirmDelete=false;retrySave=currentSave;
            try{retrySaves=SaveStore.List();}catch(Exception e)when(e is IOException or UnauthorizedAccessException){menuError=e.Message;retrySaves=[];}
        }
        ImGui.GetBackgroundDrawList().AddRectFilled(Vector2.Zero,ImGui.GetIO().DisplaySize,ImGui.ColorConvertFloat4ToU32(new(0,0,0,.7f)));
        float width=Math.Min(680,w-24),height=Math.Min(680,h-24);
        Window("Village lost",(w-width)/2,(h-height)/2,width,height);
        Colored(new(1,.3f,.25f,1),L.T("VILLAGE LOST"));Wrapped(L.T("All villagers have been eliminated."));
        Hint(currentSave==""?L.T("Unsaved village"):L.F("Save: {0}",currentSave));
        ImGui.BeginChild("Final statistics",new(0,Math.Max(60,height-238-(confirmDelete?24:0)-(menuError!=""?36:0))),ImGuiChildFlags.Borders);
        Title("Village statistics");var s=city.Statistics;
        Statistic("Day / time",city.Day+" / "+city.ClockTime);
        Statistic("Recorded since day",s.RecordedSinceDay);
        Statistic("Simulated minutes",(s.SimulatedSeconds/60).ToString("0.0"));
        Statistic("Map / seed",$"{city.Width}x{city.Height} / {city.Terrain.Seed}");
        Statistic("Peak population",s.PeakPopulation);Statistic("New arrivals",s.Arrivals);Statistic("Villagers lost",s.VillagersLost);
        Statistic("Waves / monsters defeated",$"{city.Waves} / {city.Kills}");Statistic("Monsters lost to sunlight",s.SunDeaths);
        Statistic("Remaining monsters",city.Enemies.Count);Statistic("Animals / guardians",$"{city.Animals.Count} / {city.Defenders.Count}");
        Statistic("Buildings placed / destroyed",$"{s.BuildingsPlaced} / {s.BuildingsDestroyed}");
        Statistic("Deliveries completed",s.Deliveries);Statistic("Emeralds from Goods",s.EmeraldsFromGoods.ToString("0.##"));
        Statistic("Daily balance",city.Balance.ToString("+0.##;-0.##;0"));Statistic("Happiness",city.Happiness.ToString("0")+"%");
        Statistic("Housing / water",$"{city.Capacity} / {city.Water}");Statistic("Immigration progress",city.Growth.ToString("0.##"));
        Statistic("Armor / tools",$"{city.ArmorLevel} / {city.ToolLevel}");Statistic("Enchantment",L.T(city.EnchantedArmor?"Yes":"No"));
        Statistic("Boss wave interval",L.F("{0} days",city.Rules.RaidInterval));
        Statistic("Day duration",L.F("{0:0.##} minutes",city.Rules.DayLength/60));
        ImGui.Separator();Title("Resources remaining");
        foreach(string id in Catalog.Resources.Where(id=>id!="Torches"))Statistic(L.Resource(id),city.Stock[id].ToString("0.##"));
        ImGui.Separator();Title("Total production");
        foreach(var item in s.Production.OrderBy(e=>e.Key))Statistic(L.Resource(item.Key),item.Value.ToString("0.##"));
        ImGui.Separator();Title("Buildings remaining");
        foreach(var group in city.Buildings.GroupBy(b=>b.Prefab).OrderBy(g=>g.Key))Statistic(L.Name(Catalog.Get(group.Key)),group.Count());
        foreach(var group in city.Animals.Concat(city.Defenders).Concat(city.Enemies).GroupBy(m=>m.Type).OrderBy(g=>g.Key))Statistic(L.Entity(group.Key),group.Count());
        ImGui.Separator();Title("Journal");foreach(string entry in city.Events){Wrapped(entry);ImGui.Separator();}
        ImGui.EndChild();
        Hint("Retry save");ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.BeginCombo("##retrySave",retrySave==""?L.T("Select a save"):retrySave))
        {
            foreach(string name in retrySaves)if(ImGui.Selectable(name,retrySave==name)){retrySave=name;confirmDelete=false;menuError="";}
            ImGui.EndCombo();
        }
        bool exists=retrySave!=""&&File.Exists(SaveStore.LoadPathFor(retrySave));
        ImGui.BeginDisabled(!exists);float buttonWidth=(width-24)/2;
        bool retry=Button(L.T("Reload save / Retry"),new(buttonWidth,26));ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button,new Vector4(.6f,.09f,.07f,1));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered,new Vector4(.8f,.16f,.12f,1));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,new Vector4(.4f,.04f,.03f,1));
        bool delete=Button(L.T(confirmDelete?"Confirm delete save":"Delete save"),new(buttonWidth,26));
        ImGui.PopStyleColor(3);ImGui.EndDisabled();
        try
        {
            if(retry)Retry(ref city,retrySave);
            if(delete)
            {
                if(!confirmDelete)confirmDelete=true;
                else
                {
                    SaveStore.Delete(retrySave);if(currentSave==retrySave)currentSave="";
                    retrySave="";retrySaves=SaveStore.List();confirmDelete=false;menuError=L.T("Save deleted.");
                }
            }
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException){menuError=e.Message;}
        if(confirmDelete){Hint(L.F("Permanently delete {0}?",retrySave));ImGui.SameLine();if(SmallButton(L.T("Cancel")))confirmDelete=false;}
        if(Button(L.T("New Game"),new(buttonWidth,26))){MenuOpen=true;MenuPage="New Game";}
        ImGui.SameLine();if(Button(L.T("Exit"),new(buttonWidth,26)))ExitRequested=true;
        if(menuError!="")Wrapped(menuError);
        ImGui.End();
    }
}
