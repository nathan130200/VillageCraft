using Jint;
using Jint.Native;
namespace VillageCraft;

public enum ScriptType { Mod, AI, Building }
public sealed record WorldEvent(string Name,object? Subject=null,float Amount=0,string Reason="",object? Source=null);

/// <summary>Standalone per-world scripting component. Owns engines, registrations, callbacks and queued mutations.</summary>
public sealed class ScriptRuntime:IDisposable
{
    sealed class Component
    {
        public readonly Dictionary<string,Profiler.Metric> Timings=[];
        public required string Id;public required Engine Engine;
        public ScriptType Type;public string Target=string.Empty;public bool Failed,Registered;
    }
    readonly City city;
    readonly Dictionary<string,Component> components=[];
    readonly Queue<Action> pending=[];
    bool draining,disposed,closing,starting=true;
    const int QueueLimit=2048,WorkPerFlush=256;
    public IReadOnlyList<string> Errors=>errors;
    readonly List<string> errors=[];
    public int PendingCount=>pending.Count;
    internal IEnumerable<(string Id,bool Failed)> ProfileScripts=>components.Values.Select(c=>(c.Id,c.Failed));
    public ScriptRuntime(City city)
    {
        this.city=city;city.runtime?.Dispose();city.runtime=this;city.EnsureEntityIds();city.Changed+=OnEvent;
        foreach(var pair in Scripts.Sources.Where(p=>!p.Key.StartsWith("_")))
        {
            var engine=new Engine(o=>o.MaxStatements(200000).LimitMemory(32_000_000).TimeoutInterval(TimeSpan.FromMilliseconds(250)));
            var script=new Component{Id=pair.Key,Engine=engine};
            components.Add(pair.Key,script);
            bool registrationOpen=true;
            engine.SetValue("eST_Mod",0).SetValue("eST_AI",1).SetValue("eST_Building",2);
            engine.SetValue("__register",new Action<int,string>((kind,target)=>
            {
                if(!registrationOpen||script.Registered)return;
                script.Registered=true;script.Type=Enum.IsDefined(typeof(ScriptType),kind)?(ScriptType)kind:ScriptType.Mod;
                script.Target=script.Type==ScriptType.Mod?string.Empty:target;
            }));
            engine.Execute("function set_script_type(type, target) { __register(type === eST_AI || type === eST_Building ? type : eST_Mod, target === undefined ? '' : String(target)); }");
            engine.SetValue("world",new ScriptWorld(city,this,engine,script.Id));
            engine.SetValue("__resourceIds",Catalog.Resources.Select(ResourceIds.Name).ToArray());
            engine.Execute("const resources = Object.freeze(Object.fromEntries(Array.from(__resourceIds).map(id => [id, id])));");
            engine.SetValue("__taskIds",Enum.GetNames<TaskId>());
            engine.Execute("const tasks = Object.freeze(Object.fromEntries(Array.from(__taskIds).map(id => [id, id])));");
            try{if(Scripts.Sources.TryGetValue("_ai",out var library))engine.Execute(library);engine.Execute(pair.Value);}catch(Exception e){Fail(script,e);}
            registrationOpen=false;
        }
        foreach(var c in components.Values.Where(c=>!c.Failed))
        {
            bool valid=string.IsNullOrEmpty(c.Target)||c.Type==ScriptType.Mod||
                (c.Type==ScriptType.Building&&Catalog.Data.Prefabs.Any(p=>p.Id==c.Target))||
                (c.Type==ScriptType.AI&&(c.Target is "villager" or "guardian" or "animal"||Catalog.Data.Units.ContainsKey(c.Target)||Catalog.Data.Professions.ContainsKey(c.Target)||Catalog.Data.Prefabs.Any(p=>p.Animal==c.Target)));
            if(!valid)Fail(c,new InvalidDataException("Unknown script target "+c.Target));
        }
        // Explicit targets override data-file defaults. Duplicate overrides fail visibly instead of depending on filename order.
        foreach(var group in components.Values.Where(c=>!c.Failed&&c.Type!=ScriptType.Mod&&!string.IsNullOrEmpty(c.Target)).GroupBy(c=>(c.Type,c.Target)))
            if(group.Count()>1)foreach(var c in group)Fail(c,new InvalidDataException("Duplicate script target "+group.Key));
        foreach(var script in components.Values)
        {
            Invoke(script,"onInit");
            if(script.Type==ScriptType.Mod)Invoke(script,"onWorldLoaded");
        }
        foreach(var mob in city.Enemies.ToArray())Dispatch(new("onMonsterAttached",mob));
        foreach(var mob in city.Defenders.ToArray())Dispatch(new("onGuardianAttached",mob));
        foreach(var mob in city.Animals.ToArray())Dispatch(new("onAnimalAttached",mob));
        foreach(var v in city.Citizens.ToArray())Dispatch(new("onVillagerAttached",v));
        foreach(var b in city.Buildings.ToArray())Dispatch(new("onBuildingAttached",b));
        starting=false;
    }
    void Fail(Component c,Exception error)
    {
        c.Failed=true;string message=$"Script {c.Id}: {error.Message}";
        errors.Add(message);if(errors.Count>40)errors.RemoveAt(0);GameLog.Script(c.Id,GameLogLevel.Error,error.ToString());
    }
    bool Has(Component c,string callback)
    {
        using var profile=Profiler.Measure(ProfileArea.ScriptLookup);
        return !c.Failed&&c.Engine.GetValue(callback) is Jint.Native.Function.Function;
    }
    JsValue Invoke(Component c,string callback,params object?[] args)
    {
        if(disposed||!Has(c,callback))return JsValue.Undefined;
        using var profile=Profiler.Active?Profiler.Measure(Timing(c,callback)):default;
        try{return c.Engine.Invoke(callback,args);}catch(Exception e){Fail(c,e);return JsValue.Undefined;}
    }
    static Profiler.Metric Timing(Component c,string callback)
    {
        if(c.Timings.TryGetValue(callback,out var metric))return metric;
        metric=Profiler.GetScript(c.Id,callback);c.Timings.Add(callback,metric);return metric;
    }
    public bool Queue(Action mutation)
    {
        if(disposed||closing||pending.Count>=QueueLimit)return false;pending.Enqueue(mutation);return true;
    }
    void OnEvent(WorldEvent evt)=>Queue(()=>Dispatch(evt));
    object? Bind(object? value)=>value switch
    {
        Building b=>new ScriptBuilding(city,b),Citizen v=>new ScriptVillager(city,v),
        Creature m=>new ScriptMonster(city,m),_=>value
    };
    bool Matches(Component c,WorldEvent evt)
    {
        if(c.Type==ScriptType.Mod)return true;
        return evt.Subject switch
        {
            Building b=>c.Type==ScriptType.Building&&ResolveBuilding(b)==c,
            Creature m=>c.Type==ScriptType.AI&&(evt.Name.StartsWith("onGuardian")?ResolveGuardian(m):evt.Name.StartsWith("onAnimal")?ResolveAnimal(m):ResolveMonster(m))==c,
            Citizen v=>c.Type==ScriptType.AI&&ResolveVillager(v)==c,
            _=>false
        };
    }
    void Dispatch(WorldEvent evt)
    {
        using var profile=Profiler.Measure(ProfileArea.ScriptDispatch);
        foreach(var c in components.Values.Where(c=>!c.Failed&&Matches(c,evt)))
        {
            if(evt.Name=="onBuildingDay"&&c.Type!=ScriptType.Mod)continue;
            if(evt.Name=="onCommandFailed")Invoke(c,evt.Name,evt.Reason);
            else if(evt.Name.EndsWith("Damaged")||evt.Name=="onBuildingRepaired")Invoke(c,evt.Name,Bind(evt.Subject),evt.Amount,Bind(evt.Source));
            else if(evt.Name is "onGoodsConverted" or "onBossWaveStarted")Invoke(c,evt.Name,evt.Amount);
            else if(evt.Name=="onResourceCollected")Invoke(c,evt.Name,Bind(evt.Subject),evt.Amount,Bind(evt.Source));
            else if(evt.Name.EndsWith("Attack")||evt.Name is "onDeliveryCompleted" or "onBuildingWork")Invoke(c,evt.Name,Bind(evt.Subject),Bind(evt.Source));
            else if(evt.Name=="onTreeHarvested")Invoke(c,evt.Name,Bind(evt.Subject),Bind(evt.Source));
            else if(evt.Name.EndsWith("Removed"))Invoke(c,evt.Name,Bind(evt.Subject),evt.Reason);
            else if(evt.Subject==null)Invoke(c,evt.Name);
            else Invoke(c,evt.Name,Bind(evt.Subject));
        }
    }
    Component? Resolve(ScriptType type,string instance,string target,string fallback,string generic="")
    {
        if(!string.IsNullOrEmpty(instance)&&components.TryGetValue(instance,out var attached)&&attached.Type==type&&!attached.Failed)return attached;
        var script=components.Values.FirstOrDefault(c=>!c.Failed&&c.Type==type&&c.Target==target&&!string.IsNullOrEmpty(target));
        if(script!=null)return script;
        if(!string.IsNullOrEmpty(generic)){script=components.Values.FirstOrDefault(c=>!c.Failed&&c.Type==type&&c.Target==generic);if(script!=null)return script;}
        return !string.IsNullOrEmpty(fallback)&&components.TryGetValue(fallback,out var data)&&data.Type==type&&!data.Failed?data:null;
    }
    Component? ResolveMonster(Creature m)=>Resolve(ScriptType.AI,m.Behavior,m.Type,Catalog.Data.Units[m.Type].Behavior);
    Component? ResolveGuardian(Creature m)=>Resolve(ScriptType.AI,m.Behavior,m.Type,"guardian","guardian");
    Component? ResolveAnimal(Creature m)=>Resolve(ScriptType.AI,m.Behavior,m.Type,"animal","animal");
    Component? ResolveVillager(Citizen v)=>Resolve(ScriptType.AI,v.Behavior,v.Profession,"villager","villager");
    Component? ResolveBuilding(Building b)=>Resolve(ScriptType.Building,b.Behavior,b.Prefab,string.IsNullOrEmpty(b.Plan.Script)?"building":b.Plan.Script);
    // Fallback runs the shipped JS component, never a second hidden C# decision tree.
    bool UpdateAI(Component? chosen,string fallback,string callback,object context,float dt)
    {
        if(chosen!=null&&Has(chosen,callback))
        {
            Invoke(chosen,callback=="onMonsterUpdate"&&Has(chosen,"__monsterUpdate")?"__monsterUpdate":callback,context,dt);
            if(!chosen.Failed)return true;
        }
        if(components.TryGetValue(fallback,out var basic)&&basic!=chosen&&Has(basic,callback))
        {Invoke(basic,callback=="onMonsterUpdate"&&Has(basic,"__monsterUpdate")?"__monsterUpdate":callback,context,dt);return !basic.Failed;}
        return false;
    }
    public bool UpdateMonster(Creature mob,MobScriptContext ctx,float dt)=>UpdateAI(ResolveMonster(mob),Catalog.Data.Units[mob.Type].Behavior,"onMonsterUpdate",ctx,dt);
    public bool UpdateVillager(Citizen v,float dt)=>UpdateAI(ResolveVillager(v),"villager","onVillagerUpdate",new ScriptVillager(city,v),dt);
    public bool UpdateGuardian(Creature mob,float dt)=>UpdateAI(ResolveGuardian(mob),"guardian","onGuardianUpdate",new MobScriptContext(city,mob,dt),dt);
    public bool UpdateAnimal(Creature mob,float dt)=>UpdateAI(ResolveAnimal(mob),"animal","onAnimalUpdate",new MobScriptContext(city,mob,dt),dt);
    public void ReassignProfessions(){if(components.TryGetValue("employment",out var script))Invoke(script,"onReassignProfessions");}
    public void AssignJobs()
    {
        if(components.TryGetValue("employment",out var script))Invoke(script,"onAssignJobs");
    }
    Component? DefaultBuilding(Building b)
    {
        if(!string.IsNullOrEmpty(b.Plan.Script)&&components.TryGetValue(b.Plan.Script,out var specific)&&!specific.Failed)return specific;
        return components.GetValueOrDefault("building");
    }
    public bool Produce(Building b,Citizen worker)
    {
        var c=ResolveBuilding(b);
        if(c!=null&&Has(c,"onBuildingProduce"))
        {var result=Invoke(c,"onBuildingProduce",new ScriptBuilding(city,b),new ScriptVillager(city,worker));if(!c.Failed)return Jint.Runtime.TypeConverter.ToBoolean(result);}
        var basic=DefaultBuilding(b);return basic!=null&&basic!=c&&Jint.Runtime.TypeConverter.ToBoolean(Invoke(basic,"onBuildingProduce",new ScriptBuilding(city,b),new ScriptVillager(city,worker)));
    }
    public void BuildingDay(Building b)
    {
        var c=ResolveBuilding(b);
        if(c!=null&&Has(c,"onBuildingDay")){Invoke(c,"onBuildingDay",new ScriptBuilding(city,b));if(!c.Failed)return;}
        var basic=DefaultBuilding(b);if(basic!=null&&basic!=c)Invoke(basic,"onBuildingDay",new ScriptBuilding(city,b));
    }
    public void BuildingWork(Building b,Citizen worker)=>city.Notify(new("onBuildingWork",b,Source:worker));
    public void Tick(float dt)
    {
        using var profile=Profiler.Measure(ProfileArea.ScriptTick);
        if(disposed)return;Flush();
        foreach(var c in components.Values)Invoke(c,"onUpdate",dt);
        foreach(var b in city.Buildings.Where(b=>b.Enabled).ToArray())
        {
            var c=ResolveBuilding(b);if(c!=null)Invoke(c,"onBuildingUpdate",new ScriptBuilding(city,b),dt);
        }
        Flush();
    }
    public void Flush()
    {
        if(disposed||draining||starting)return;
        using var profile=Profiler.Measure(ProfileArea.ScriptQueue);draining=true;
        try{for(int i=0;i<WorkPerFlush&&pending.TryDequeue(out var work);i++)try{work();}catch(Exception e){GameLog.Write(GameLogLevel.Error,"Script queued command failed: "+e);}}
        finally{try{city.ResolveDeaths();}finally{draining=false;}}
    }
    public bool Attach(object entity,string script)
    {
        var type=entity is Building?ScriptType.Building:ScriptType.AI;
        if(!string.IsNullOrEmpty(script)&&(!components.TryGetValue(script,out var c)||c.Type!=type||c.Failed))return false;
        return Queue(()=>
        {
            string name;
            switch(entity)
            {
                case Building b when city.Buildings.Contains(b):b.Behavior=script;name="onBuildingAttached";break;
                case Citizen v when city.Citizens.Contains(v):v.Behavior=script;name="onVillagerAttached";break;
                case Creature m when city.Enemies.Contains(m):m.Behavior=script;name="onMonsterAttached";break;
                case Creature m when city.Defenders.Contains(m):m.Behavior=script;name="onGuardianAttached";break;
                case Creature m when city.Animals.Contains(m):m.Behavior=script;name="onAnimalAttached";break;
                default:return;
            }
            Dispatch(new(name,entity));
        });
    }
    public void Dispose()
    {
        if(disposed)return;closing=true;
        foreach(var c in components.Values){bool failed=c.Failed;c.Failed=false;Invoke(c,"onShutdown");c.Failed|=failed;}
        disposed=true;city.Changed-=OnEvent;pending.Clear();
    }
}
