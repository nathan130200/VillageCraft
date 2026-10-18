using System.Numerics;
using ImGuiNET;
namespace VillageCraft;

partial class GameUi
{
    City? defeatedCity;
    string retrySave=string.Empty;
    string[] retrySaves=[];
    bool confirmDelete;
    static void Statistic(string label,object value)=>ImGui.TextUnformatted(L.T(label)+": "+value);
#if DEBUG
    internal void RememberSave(string name)=>currentSave=name;
#endif
    internal bool Retry(ref City city,string name)
    {
        var loaded=City.Load(SaveStore.LoadPathFor(name));
        if(loaded.GameOver){loaded.Dispose();menuError=L.T("ui.this_save_already_contains_a_defeated_village_choose");return false;}
        city=loaded;currentSave=name;ResetPanels();Paused=false;MenuOpen=false;menuError=string.Empty;return true;
    }
    void DrawGameOver(ref City city,int w,int h)
    {
        if(!ReferenceEquals(defeatedCity,city))
        {
            defeatedCity=city;MenuOpen=false;Paused=true;ResetPanels();menuError=string.Empty;confirmDelete=false;retrySave=currentSave;
            try{retrySaves=SaveStore.List();}catch(Exception e)when(e is IOException or UnauthorizedAccessException){menuError=e.Message;retrySaves=[];}
        }
        ImGui.GetBackgroundDrawList().AddRectFilled(Vector2.Zero,ImGui.GetIO().DisplaySize,ImGui.ColorConvertFloat4ToU32(new(0,0,0,.7f)));
        float width=Math.Min(680,w-24),height=Math.Min(680,h-24);
        Window("Village lost",(w-width)/2,(h-height)/2,width,height);
        Colored(new(1,.3f,.25f,1),L.T("gameover.title"));Wrapped(L.T("gameover.description"));
        Hint(currentSave==string.Empty?L.T("ui.unsaved_village"):L.F("ui.save",currentSave));
        ImGui.BeginChild("Final statistics",new(0,Math.Max(60,height-238-(confirmDelete?24:0)-(menuError!=string.Empty?36:0))),ImGuiChildFlags.Borders);
        Title("ui.village_statistics");var s=city.Statistics;
        Statistic("ui.day_time",city.Day+" / "+city.ClockTime);
        Statistic("ui.recorded_since_day",s.RecordedSinceDay);
        Statistic("ui.simulated_minutes",(s.SimulatedSeconds/60).ToString("0.0"));
        Statistic("ui.map_seed",$"{city.Width}x{city.Height} / {city.Terrain.Seed}");
        Statistic("ui.peak_population",s.PeakPopulation);Statistic("ui.new_arrivals",s.Arrivals);Statistic("ui.villagers_lost",s.VillagersLost);
        Statistic("ui.waves_monsters_defeated",$"{city.Waves} / {city.Kills}");Statistic("ui.monsters_lost_to_sunlight",s.SunDeaths);
        Statistic("ui.remaining_monsters",city.Enemies.Count);Statistic("ui.animals_guardians_2",$"{city.Animals.Count} / {city.Defenders.Count}");
        Statistic("ui.buildings_placed_destroyed",$"{s.BuildingsPlaced} / {s.BuildingsDestroyed}");
        Statistic("statistics.births",s.Births);Statistic("statistics.growing_babies",city.Babies.Count);
        Statistic("ui.deliveries_completed",s.Deliveries);Statistic("ui.emeralds_from_goods",s.EmeraldsFromGoods.ToString("0.##"));
        Statistic("ui.daily_balance",city.Balance.ToString("+0.##;-0.##;0"));Statistic("ui.happiness",city.Happiness.ToString("0")+"%");
        Statistic("ui.housing_water",$"{city.Capacity} / {city.Water}");Statistic("ui.immigration_progress",city.Growth.ToString("0.##"));
        Statistic("ui.armor_tools",$"{city.ArmorLevel} / {city.ToolLevel}");Statistic("ui.enchantment",L.T(city.EnchantedArmor?"ui.yes":"ui.no"));
        Statistic("ui.boss_wave_interval",L.F("ui.days",city.Rules.RaidInterval));
        Statistic("ui.day_duration",L.F("ui.minutes",city.Rules.DayLength/60));
        ImGui.Separator();Title("ui.resources_remaining");
        foreach(ResourceType id in Catalog.Resources)Statistic(L.Resource(id),city.Stock[id].ToString("0.##"));
        ImGui.Separator();Title("ui.total_production");
        foreach(var item in s.Production.OrderBy(e=>e.Key))Statistic(L.Resource(item.Key),item.Value.ToString("0.##"));
        ImGui.Separator();Title("ui.buildings_remaining");
        foreach(var group in city.Buildings.GroupBy(b=>b.Prefab).OrderBy(g=>g.Key))Statistic(L.Name(Catalog.Get(group.Key)),group.Count());
        foreach(var group in city.Animals.Concat(city.Defenders).Concat(city.Enemies).GroupBy(m=>m.Type).OrderBy(g=>g.Key))Statistic(L.Entity(group.Key),group.Count());
        ImGui.Separator();Title("ui.journal");foreach(string entry in city.Events){Wrapped(entry);ImGui.Separator();}
        ImGui.EndChild();
        Hint("ui.retry_save");ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if(ImGui.BeginCombo("##retrySave",retrySave==string.Empty?L.T("ui.select_a_save"):retrySave))
        {
            foreach(string name in retrySaves)if(ImGui.Selectable(name,retrySave==name)){retrySave=name;confirmDelete=false;menuError=string.Empty;}
            ImGui.EndCombo();
        }
        bool exists=retrySave!=string.Empty&&File.Exists(SaveStore.LoadPathFor(retrySave));
        ImGui.BeginDisabled(!exists);float buttonWidth=(width-24)/2;
        bool retry=Button(L.T("ui.reload_save_retry"),new(buttonWidth,26));ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button,new Vector4(.6f,.09f,.07f,1));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered,new Vector4(.8f,.16f,.12f,1));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,new Vector4(.4f,.04f,.03f,1));
        bool delete=Button(L.T(confirmDelete?"ui.confirm_delete_save":"ui.delete_save"),new(buttonWidth,26));
        ImGui.PopStyleColor(3);ImGui.EndDisabled();
        try
        {
            if(retry)Retry(ref city,retrySave);
            if(delete)
            {
                if(!confirmDelete)confirmDelete=true;
                else
                {
                    SaveStore.Delete(retrySave);if(currentSave==retrySave)currentSave=string.Empty;
                    retrySave=string.Empty;retrySaves=SaveStore.List();confirmDelete=false;menuError=L.T("ui.save_deleted");
                }
            }
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException){menuError=e.Message;}
        if(confirmDelete){Hint(L.F("ui.permanently_delete",retrySave));ImGui.SameLine();if(SmallButton(L.T("ui.cancel")))confirmDelete=false;}
        if(Button(L.T("ui.new_game"),new(buttonWidth,26))){MenuOpen=true;MenuPage="New Game";}
        ImGui.SameLine();if(Button(L.T("menu.exit"),new(buttonWidth,26)))ExitRequested=true;
        if(menuError!=string.Empty)Wrapped(menuError);
        ImGui.End();
    }
}
