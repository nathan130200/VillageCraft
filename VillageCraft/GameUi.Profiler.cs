using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using ImGuiNET;
namespace VillageCraft;

partial class GameUi
{
    bool profilerOpen,profilerScripts;
    Profiler.Row[] profileRows=[];
    long profileRefresh;
    int profileSampleCount;
    string profileStatus=string.Empty;
#if DEBUG
    internal void ProfileSmoke(bool callbacks){profilerScripts=callbacks;ExportProfile();}
#endif
    public void ToggleProfiler()
    {
        profilerOpen=!profilerOpen;
        if(profilerOpen){Profiler.Enabled=true;Profiler.Paused=false;}
        else Profiler.Enabled=false;
    }
    public void DrawProfiler(City city)
    {
        if(!profilerOpen)return;
        using var timing=Profiler.Measure(ProfileArea.ProfilerUi);
        ImGui.SetNextWindowSize(new Vector2(950,580),ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(24,85),ImGuiCond.FirstUseEver);
        bool visible=ImGui.Begin(L.T("profile.title")+"###performance",ref profilerOpen,ImGuiWindowFlags.NoSavedSettings);
        CaptureRect();
        if(!profilerOpen)Profiler.Enabled=false;
        if(!visible){ImGui.End();return;}
        ImGui.Checkbox(L.T("profile.capture"),ref Profiler.Enabled);ImGui.SameLine();
        ImGui.Checkbox(L.T("profile.freeze"),ref Profiler.Paused);ImGui.SameLine();
        if(ImGui.Button(L.T("profile.reset"))){Profiler.RequestReset();profileRefresh=0;}ImGui.SameLine();
        if(ImGui.Button(L.T("profile.export")))ExportProfile();
        if(!string.IsNullOrEmpty(profileStatus))ImGui.TextUnformatted(profileStatus);
        long now=Stopwatch.GetTimestamp();
        if(now-profileRefresh>Stopwatch.Frequency/4||profileRows.Length==0)
        {profileRows=Profiler.Snapshot();profileSampleCount=Profiler.Samples;profileRefresh=now;}
        ImGui.TextUnformatted(L.F("profile.summary",profileSampleCount,Profiler.History,Profiler.TimerNanoseconds));
        ImGui.TextUnformatted(L.F("profile.memory",Profiler.FrameAllocatedBytes/1024d,Profiler.HeapBytes/1048576d,Profiler.Gc0,Profiler.Gc1,Profiler.Gc2));
        ImGui.TextUnformatted(Profiler.CpuReady?L.F("profile.cpu",Profiler.ProcessCpuMsPerFrame,Profiler.CollectorMs):L.T("profile.cpu_pending"));
        ImGui.TextWrapped(L.T("profile.accuracy"));
        var scripts=city.Scripting.ProfileScripts.ToArray();
        int active=scripts.Count(s=>profileRows.Any(r=>r.Metric.Script==s.Id&&r.LastCalls>0));
        ImGui.TextUnformatted(L.F("profile.activity",active,scripts.Length-active,scripts.Count(s=>s.Failed),city.Scripting.PendingCount));
        Tooltip("profile.idle_help");
        if(ImGui.CollapsingHeader(L.T("profile.components")))
        {
            ImGui.BeginChild("script_activity",new Vector2(0,100),ImGuiChildFlags.Borders);
            foreach(var script in scripts)
            {
                long calls=profileRows.Where(r=>r.Metric.Script==script.Id).Sum(r=>r.LastCalls);
                ImGui.TextUnformatted(script.Id+" : "+L.T(script.Failed?"profile.failed":calls>0?"profile.active":"profile.idle")+" ("+calls+")");
            }
            ImGui.EndChild();
        }
        var top=profileRows.Where(r=>r.Metric.Area is not ProfileArea.Frame and not ProfileArea.PresentAndPacing and not ProfileArea.ProfilerUi).OrderByDescending(r=>r.Self).FirstOrDefault();
        if(top.Metric!=null&&top.Self>0)ImGui.TextUnformatted(L.F("profile.hotspot",ProfileName(top.Metric),top.Self));
        double jint=profileRows.Where(r=>!string.IsNullOrEmpty(r.Metric.Script)||r.Metric.Area==ProfileArea.ScriptPredicate).Sum(r=>r.Self);
        double pacing=profileRows.Where(r=>r.Metric.Area==ProfileArea.PresentAndPacing).Sum(r=>r.Self);
        double other=profileRows.Sum(r=>r.Self)-jint-pacing;
        ImGui.TextUnformatted(L.F("profile.split",jint,other,pacing));
        ImGui.Checkbox(L.T("profile.callbacks"),ref profilerScripts);
        ImGui.Separator();
        if(ImGui.BeginTable("profile_timings",10,ImGuiTableFlags.Borders|ImGuiTableFlags.RowBg|ImGuiTableFlags.ScrollY|ImGuiTableFlags.Resizable,new Vector2(0,Math.Max(100,ImGui.GetContentRegionAvail().Y))))
        {
            ImGui.TableSetupColumn(L.T("profile.scope"),ImGuiTableColumnFlags.WidthFixed,250);
            foreach(string key in new[]{"last","mean","self","p95","max","per_call","peak_call","calls","allocation"})ImGui.TableSetupColumn(L.T("profile."+key),ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupScrollFreeze(1,1);ImGui.TableHeadersRow();
            foreach(var row in profileRows.Where(r=>(profilerScripts?!string.IsNullOrEmpty(r.Metric.Script):r.Metric.Area!=null)&&r.Calls>0).OrderByDescending(r=>r.Self))
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(ProfileName(row.Metric));Tooltip(ProfileName(row.Metric));
                foreach(double value in new[]{row.Last,row.Mean,row.Self,row.P95,row.Maximum,row.PerCall,row.PeakCall}){ImGui.TableNextColumn();ImGui.TextUnformatted(value.ToString("0.000",CultureInfo.CurrentCulture));}
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Calls.ToString(CultureInfo.CurrentCulture));
                ImGui.TableNextColumn();ImGui.TextUnformatted((row.Bytes/1024).ToString("0.00",CultureInfo.CurrentCulture));
            }
            ImGui.EndTable();
        }
        ImGui.End();
    }
    static string ProfileName(Profiler.Metric metric)=>metric.Area is {} area?L.T("profile.area."+area):metric.Key;
    void ExportProfile()
    {
        try
        {
            string directory=Path.Combine(SaveStore.Folder,"Profiles");Directory.CreateDirectory(directory);
            string name="profile-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff",CultureInfo.InvariantCulture)+".csv";
            using var file=new StreamWriter(Path.Combine(directory,name),false,new System.Text.UTF8Encoding(false));
            file.WriteLine("scope,sampled_frames,window_calls,last_inclusive_ms,mean_inclusive_ms,mean_self_ms,p95_inclusive_ms,max_inclusive_ms,mean_inclusive_ms_per_call,mean_self_allocated_bytes,peak_inclusive_ms_per_call");
            foreach(var row in Profiler.Snapshot())
                file.WriteLine(string.Join(',',new[]{"\""+row.Metric.Key.Replace("\"","\"\"")+"\"",Profiler.Samples.ToString(CultureInfo.InvariantCulture),row.Calls.ToString(CultureInfo.InvariantCulture)}.Concat(new[]{row.Last,row.Mean,row.Self,row.P95,row.Maximum,row.PerCall,row.Bytes,row.PeakCall}.Select(v=>v.ToString("R",CultureInfo.InvariantCulture)))));
            profileStatus=L.F("profile.exported",name);
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){profileStatus=L.T("profile.export_failed");GameLog.Write(GameLogLevel.Error,"Profile export failed: "+e);}
    }
}
