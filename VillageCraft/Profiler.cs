using System.Diagnostics;
namespace VillageCraft;

// Main-thread wall-time instrumentation. No timers or allocations on disabled scope paths.
enum ProfileArea
{
    Frame, Input, Update, SimulationStep, DailyTick, Villagers, Animals, Combat, Sunlight,
    Pathfinding, RoadConnectivity, Stock, SupplySearch, Production, ManagedActions, WorldQueries,
    ScriptLookup, ScriptDispatch, ScriptTick, ScriptQueue, ScriptPredicate,
    Render, Terrain, Buildings, Residents, Creatures, Effects, Picking, Thumbnails,
    Ui, UiSubmit, PresentAndPacing, DisplayChanges, ProfilerUi, AssetRead, Save, Load, Diagnostics, Audio, VisualEffects, ShadowMap
}

static class Profiler
{
    public const int History=240;
    public sealed class Metric(string key,ProfileArea? area=null,string script="",string callback="")
    {
        public readonly string Key=key,Script=script,Callback=callback;
        public readonly ProfileArea? Area=area;
        internal readonly double[] Inclusive=new double[History],Self=new double[History],Allocations=new double[History],PeakCall=new double[History];
        internal readonly long[] Counts=new long[History];
        internal long Ticks,SelfTicks,Bytes,Calls,PeakTicks;
        public long LastCalls {get;internal set;}
    }
    public readonly record struct Row(Metric Metric,double Last,double Mean,double Self,double P95,double Maximum,double PerCall,double Bytes,long Calls,long LastCalls,double PeakCall);
    struct Entry {public Metric Metric;public long Start,Allocated,ChildTicks,ChildBytes;}
    public readonly struct Scope(int depth):IDisposable
    {public void Dispose(){if(depth>0)Stop(depth);}}
    static readonly Metric[] areas=Enum.GetValues<ProfileArea>().Select(a=>new Metric(a.ToString(),a)).ToArray();
    static readonly Dictionary<(string,string),Metric> scripts=[];
    static readonly List<Metric> metrics=[..areas];
    static Entry[] stack=new Entry[256];
    static readonly Process process=Process.GetCurrentProcess();
    static int depth,cursor;
    static bool active,reset,previousCapture;
    static long frameAllocationStart;
    static Scope frame;
    static long cpuStamp=Stopwatch.GetTimestamp(),cpuTicks=process.TotalProcessorTime.Ticks,cpuFrames,completedFrames;
    static int gen0,gen1,gen2;
    public static bool Enabled,Paused;
    public static bool Active=>active;
    public static int Samples {get;private set;}
    public static long FrameNumber {get;private set;}
    public static int Gc0 {get;private set;}
    public static int Gc1 {get;private set;}
    public static int Gc2 {get;private set;}
    public static long FrameAllocatedBytes {get;private set;}
    public static long HeapBytes {get;private set;}
    public static bool CpuReady {get;private set;}
    public static double ProcessCpuMsPerFrame {get;private set;}
    public static double CollectorMs {get;private set;}
    public static double TimerNanoseconds=>1e9/Stopwatch.Frequency;
    public static Metric GetScript(string id,string callback)
    {
        if(scripts.TryGetValue((id,callback),out var metric))return metric;
        metric=new Metric(id+" / "+callback,script:id,callback:callback);scripts.Add((id,callback),metric);metrics.Add(metric);return metric;
    }
    public static Scope Measure(ProfileArea area)=>active?Start(areas[(int)area]):default;
    public static Scope Measure(Metric metric)=>active?Start(metric):default;
    static Scope Start(Metric metric)
    {
        if(depth==stack.Length)Array.Resize(ref stack,stack.Length*2);
        long allocated=GC.GetAllocatedBytesForCurrentThread(),stamp=Stopwatch.GetTimestamp();
        stack[depth++]=new(){Metric=metric,Start=stamp,Allocated=allocated};return new Scope(depth);
    }
    static void Stop(int expected)
    {
        long stamp=Stopwatch.GetTimestamp(),allocated=GC.GetAllocatedBytesForCurrentThread();
        if(depth!=expected)throw new InvalidOperationException("Profiling scopes must close in reverse order.");
        var entry=stack[--depth];long ticks=stamp-entry.Start,bytes=allocated-entry.Allocated;
        entry.Metric.Ticks+=ticks;entry.Metric.SelfTicks+=ticks-entry.ChildTicks;
        entry.Metric.PeakTicks=Math.Max(entry.Metric.PeakTicks,ticks);
        entry.Metric.Bytes+=bytes-entry.ChildBytes;entry.Metric.Calls++;
        if(depth>0){stack[depth-1].ChildTicks+=ticks;stack[depth-1].ChildBytes+=bytes;}
    }
    public static void RequestReset()=>reset=true;
    public static void BeginFrame()
    {
        if(depth!=0)throw new InvalidOperationException("Unclosed profiling scope at frame boundary.");
        if(reset)
        {
            foreach(var m in metrics){Array.Clear(m.Inclusive);Array.Clear(m.Self);Array.Clear(m.Allocations);Array.Clear(m.Counts);Array.Clear(m.PeakCall);m.Ticks=m.SelfTicks=m.Bytes=m.Calls=m.LastCalls=m.PeakTicks=0;}
            Samples=cursor=Gc0=Gc1=Gc2=0;CpuReady=false;reset=false;cpuStamp=Stopwatch.GetTimestamp();cpuTicks=process.TotalProcessorTime.Ticks;cpuFrames=completedFrames;
        }
        FrameNumber++;active=Enabled&&!Paused;
        if(!active){previousCapture=false;return;}
        if(!previousCapture){cpuStamp=Stopwatch.GetTimestamp();cpuTicks=process.TotalProcessorTime.Ticks;cpuFrames=completedFrames;CpuReady=false;HeapBytes=GC.GetTotalMemory(false);}
        previousCapture=true;frameAllocationStart=GC.GetAllocatedBytesForCurrentThread();
        gen0=GC.CollectionCount(0);gen1=GC.CollectionCount(1);gen2=GC.CollectionCount(2);
        frame=Measure(ProfileArea.Frame);
    }
    public static void EndFrame()
    {
        if(!active)return;
        frame.Dispose();FrameAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-frameAllocationStart;active=false;long bookkeeping=Stopwatch.GetTimestamp();
        double milliseconds=1000d/Stopwatch.Frequency;
        foreach(var m in metrics)
        {
            m.Inclusive[cursor]=m.Ticks*milliseconds;m.Self[cursor]=m.SelfTicks*milliseconds;
            m.PeakCall[cursor]=m.PeakTicks*milliseconds;
            m.Allocations[cursor]=m.Bytes;m.Counts[cursor]=m.LastCalls=m.Calls;m.Ticks=m.SelfTicks=m.Bytes=m.Calls=m.PeakTicks=0;
        }
        cursor=(cursor+1)%History;Samples=Math.Min(History,Samples+1);completedFrames++;
        Gc0+=GC.CollectionCount(0)-gen0;Gc1+=GC.CollectionCount(1)-gen1;Gc2+=GC.CollectionCount(2)-gen2;
        long stamp=Stopwatch.GetTimestamp();
        if(stamp-cpuStamp>=Stopwatch.Frequency)
        {
            long ticks=process.TotalProcessorTime.Ticks;
            ProcessCpuMsPerFrame=(ticks-cpuTicks)/10000d/Math.Max(1,completedFrames-cpuFrames);
            CpuReady=true;HeapBytes=GC.GetTotalMemory(false);cpuTicks=ticks;cpuStamp=stamp;cpuFrames=completedFrames;
        }
        CollectorMs=(Stopwatch.GetTimestamp()-bookkeeping)*milliseconds;
    }
    public static Row[] Snapshot()
    {
        if(Samples==0)return [];
        int last=(cursor+History-1)%History;
        return metrics.Select(m=>
        {
            double total=0,self=0,bytes=0;long calls=0;
            var values=new double[Samples];
            for(int i=0;i<Samples;i++){values[i]=m.Inclusive[i];total+=values[i];self+=m.Self[i];bytes+=m.Allocations[i];calls+=m.Counts[i];}
            Array.Sort(values);
            return new Row(m,m.Inclusive[last],total/Samples,self/Samples,values[(int)Math.Ceiling(Samples*.95)-1],values[^1],calls>0?total/calls:0,bytes/Samples,calls,m.LastCalls,m.PeakCall.Take(Samples).Max());
        }).ToArray();
    }
}
