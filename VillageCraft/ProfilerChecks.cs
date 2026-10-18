using System.Diagnostics;
namespace VillageCraft;

static class ProfilerChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAILED: "+text);}
    static void Burn(double milliseconds)
    {long end=Stopwatch.GetTimestamp()+(long)(milliseconds*Stopwatch.Frequency/1000);while(Stopwatch.GetTimestamp()<end)Thread.SpinWait(5);}
    public static void Run()
    {
        Profiler.Enabled=true;Profiler.Paused=false;Profiler.RequestReset();Profiler.BeginFrame();
        using(Profiler.Measure(ProfileArea.Update))
        {
            Burn(.5);
            using(Profiler.Measure(ProfileArea.Pathfinding)){Burn(1);GC.KeepAlive(new byte[4096]);}
        }
        try{using var scope=Profiler.Measure(ProfileArea.ManagedActions);throw new InvalidOperationException("intentional scope unwind");}catch(InvalidOperationException){}
        Profiler.EndFrame();
        var rows=Profiler.Snapshot();var frame=rows.Single(r=>r.Metric.Area==ProfileArea.Frame);var update=rows.Single(r=>r.Metric.Area==ProfileArea.Update);var path=rows.Single(r=>r.Metric.Area==ProfileArea.Pathfinding);
        Check(path.Mean>=1&&update.Mean>=path.Mean,"high-resolution elapsed measurement");
        Check(Math.Abs(update.Mean-update.Self-path.Mean)<.000001,"exclusive time subtracts nested scopes exactly");
        Check(Math.Abs(rows.Sum(r=>r.Self)-frame.Mean)<.000001,"self scopes partition frame without double counting");
        Check(path.Bytes>=4096&&update.Bytes<4096,"nested allocations charged to child only");
        Check(path.Calls==1&&path.PerCall==path.Mean,"call count and per-call mean");
        var callback=Profiler.GetScript("profiling_check","onUpdate");
        Profiler.BeginFrame();
        using(Profiler.Measure(callback))
        {
            Burn(.2);
            using(Profiler.Measure(ProfileArea.Pathfinding)){Burn(.2);}
        }
        Profiler.EndFrame();
        var callbackRow=Profiler.Snapshot().Single(r=>r.Metric==callback);
        Check(callbackRow.Calls==1&&callbackRow.Mean>callbackRow.Self&&callbackRow.PeakCall>=callbackRow.PerCall,"Jint boundary accounting excludes nested managed work from self time");
        int samples=Profiler.Samples;Profiler.Paused=true;Profiler.BeginFrame();using(Profiler.Measure(ProfileArea.Pathfinding)){}Profiler.EndFrame();
        Check(Profiler.Samples==samples,"freeze preserves captured samples");
        Profiler.Paused=false;Profiler.Enabled=false;Profiler.BeginFrame();
        for(int i=0;i<10;i++){using var warm=Profiler.Measure(ProfileArea.Pathfinding);}
        long allocation=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<1000;i++){using var off=Profiler.Measure(ProfileArea.Pathfinding);}
        Check(GC.GetAllocatedBytesForCurrentThread()==allocation,"disabled scope path allocates zero bytes");Profiler.EndFrame();
        Profiler.Enabled=true;
        for(int i=0;i<Profiler.History+2;i++){Profiler.BeginFrame();Profiler.EndFrame();}
        Check(Profiler.Samples==Profiler.History&&Profiler.Snapshot().Single(r=>r.Metric.Area==ProfileArea.Pathfinding).Calls==0,"rolling window includes zero frames and evicts old calls");
        Profiler.RequestReset();Profiler.BeginFrame();Profiler.EndFrame();Check(Profiler.Samples==1,"reset starts a fresh capture");
        Profiler.Enabled=false;Console.WriteLine("PASS: nested/exclusive time, exact accounting, allocations, scope unwinding, freeze/reset, rolling zeros and zero-allocation disabled scopes.");
    }
    public static void Benchmark()
    {
        Run();
        using var city=City.New(256,256,generateTerrain:false);city.Rules.NightMonsters=city.Rules.RaidsEnabled=false;city.Rules.BirthChance=0;
        for(int i=0;i<20;i++)city.Update(.016f);
        Profiler.Enabled=true;Profiler.RequestReset();
        for(int i=0;i<240;i++){Profiler.BeginFrame();using(Profiler.Measure(ProfileArea.Update))city.Update(.016f);Profiler.EndFrame();}
        Console.WriteLine("Measured 240 simulation frames, flat 256x256 starter village (Debug; no rendering):");
        foreach(var row in Profiler.Snapshot().Where(r=>r.Calls>0&&r.Metric.Area!=ProfileArea.Frame).OrderByDescending(r=>r.Self).Take(12))
            Console.WriteLine($"  {row.Metric.Key}: inclusive {row.Mean:F4} ms/frame, self {row.Self:F4}, p95 {row.P95:F4}, calls {row.Calls}, self allocation {row.Bytes:F0} bytes/frame");
        Console.WriteLine($"Timer tick: {Profiler.TimerNanoseconds:F1} ns; last collector pass: {Profiler.CollectorMs:F4} ms.");
        Profiler.Enabled=false;
    }
}
