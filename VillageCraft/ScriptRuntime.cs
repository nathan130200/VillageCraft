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
        public required string Id;public required Engine Engine;
        public ScriptType Type;public string Target="";public bool Failed,Registered;
    }
    readonly City city;
    readonly Dictionary<string,Component> components=[];
    readonly Queue<Action> pending=[];
    bool draining,disposed,closing,starting=true;
    const int QueueLimit=2048,WorkPerFlush=256;
    public IReadOnlyList<string> Errors=>errors;
    readonly List<string> errors=[];
    public int PendingCount=>pending.Count;
    public ScriptRuntime(City city)
    {
        this.city=city;city.runtime?.Dispose();city.runtime=this;city.EnsureEntityIds();city.Changed+=OnEvent;
        foreach(var pair in Scripts.Sources)
        {
            var engine=new Engine(o=>o.MaxStatements(10000).LimitMemory(32_000_000).TimeoutInterval(TimeSpan.FromMilliseconds(250)));
            var script=new Component{Id=pair.Key,Engine=engine};
            components.Add(pair.Key,script);
            bool registrationOpen=true;
            engine.SetValue("eST_Mod",0).SetValue("eST_AI",1).SetValue("eST_Building",2);
            engine.SetValue("__register",new Action<int,string>((kind,target)=>
            {
                if(!registrationOpen||script.Registered)return;
                script.Registered=true;script.Type=Enum.IsDefined(typeof(ScriptType),kind)?(ScriptType)kind:ScriptType.Mod;
                script.Target=script.Type==ScriptType.Mod?"":target;
            }));
            engine.Execute("function set_script_type(type, target) { __register(type === eST_AI || type === eST_Building ? type : eST_Mod, target === undefined ? '' : String(target)); }");
            engine.SetValue("world",new ScriptWorld(city,this,engine));
            try{engine.Execute(pair.Value);}catch(Exception e){Fail(script,e);}
            registrationOpen=false;
        }
        foreach(var c in components.Values.Where(c=>!c.Failed))
        {
            bool valid=c.Target==""||c.Type==ScriptType.Mod||
                (c.Type==ScriptType.Building&&Catalog.Data.Prefabs.Any(p=>p.Id==c.Target))||
                (c.Type==ScriptType.AI&&(c.Target=="villager"||Catalog.Data.Units.ContainsKey(c.Target)||Catalog.Data.Professions.ContainsKey(c.Target)));
            if(!valid)Fail(c,new InvalidDataException("Unknown script target "+c.Target));
        }
        // Explicit targets override data-file defaults. Duplicate overrides fail visibly instead of depending on filename order.
        foreach(var group in components.Values.Where(c=>!c.Failed&&c.Type!=ScriptType.Mod&&c.Target!="").GroupBy(c=>(c.Type,c.Target)))
            if(group.Count()>1)foreach(var c in group)Fail(c,new InvalidDataException("Duplicate script target "+group.Key));
        foreach(var script in components.Values)
        {
            Invoke(script,"onInit");
            if(script.Type==ScriptType.Mod)Invoke(script,"onWorldLoaded");
        }
        foreach(var mob in city.Enemies.ToArray())Dispatch(new("onMonsterAttached",mob));
        foreach(var v in city.Citizens.ToArray())Dispatch(new("onVillagerAttached",v));
        foreach(var b in city.Buildings.ToArray())Dispatch(new("onBuildingAttached",b));
        starting=false;
    }
    void Fail(Component c,Exception error)
    {
        c.Failed=true;string message=$"Script {c.Id}: {error.Message}";
        errors.Add(message);if(errors.Count>40)errors.RemoveAt(0);Console.Error.WriteLine(message);
    }
    bool Has(Component c,string callback)=>!c.Failed&&c.Engine.GetValue(callback) is Jint.Native.Function.Function;
    void Invoke(Component c,string callback,params object?[] args)
    {
        if(disposed||!Has(c,callback))return;
        try{c.Engine.Invoke(callback,args);}catch(Exception e){Fail(c,e);}
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
            Creature m=>c.Type==ScriptType.AI&&ResolveMonster(m)==c,
            Citizen v=>c.Type==ScriptType.AI&&ResolveVillager(v)==c,
            _=>false
        };
    }
    void Dispatch(WorldEvent evt)
    {
        foreach(var c in components.Values.Where(c=>!c.Failed&&Matches(c,evt)))
        {
            if(evt.Name=="onCommandFailed")Invoke(c,evt.Name,evt.Reason);
            else if(evt.Name.EndsWith("Damaged"))Invoke(c,evt.Name,Bind(evt.Subject),evt.Amount,Bind(evt.Source));
            else if(evt.Name=="onGoodsConverted")Invoke(c,evt.Name,evt.Amount);
            else if(evt.Name=="onResourceCollected")Invoke(c,evt.Name,Bind(evt.Subject),evt.Amount,Bind(evt.Source));
            else if(evt.Name=="onDeliveryCompleted")Invoke(c,evt.Name,Bind(evt.Subject),Bind(evt.Source));
            else if(evt.Name=="onTreeHarvested")Invoke(c,evt.Name,Bind(evt.Subject),Bind(evt.Source));
            else if(evt.Name.EndsWith("Removed"))Invoke(c,evt.Name,Bind(evt.Subject),evt.Reason);
            else if(evt.Subject==null)Invoke(c,evt.Name);
            else Invoke(c,evt.Name,Bind(evt.Subject));
        }
    }
    Component? Resolve(ScriptType type,string instance,string target,string fallback,string generic="")
    {
        if(instance!=""&&components.TryGetValue(instance,out var attached)&&attached.Type==type&&!attached.Failed)return attached;
        var script=components.Values.FirstOrDefault(c=>!c.Failed&&c.Type==type&&c.Target==target&&target!="");
        if(script!=null)return script;
        if(generic!=""){script=components.Values.FirstOrDefault(c=>!c.Failed&&c.Type==type&&c.Target==generic);if(script!=null)return script;}
        return fallback!=""&&components.TryGetValue(fallback,out var data)&&data.Type==type&&!data.Failed?data:null;
    }
    Component? ResolveMonster(Creature m)=>Resolve(ScriptType.AI,m.Behavior,m.Type,Catalog.Data.Units[m.Type].Behavior);
    Component? ResolveVillager(Citizen v)=>Resolve(ScriptType.AI,v.Behavior,v.Profession,"","villager");
    Component? ResolveBuilding(Building b)=>Resolve(ScriptType.Building,b.Behavior,b.Prefab,b.Plan.Script);
    public bool UpdateMonster(Creature mob,MobScriptContext ctx,float dt)
    {
        var c=ResolveMonster(mob);if(c==null||!Has(c,"onMonsterUpdate"))return false;
        Invoke(c,"onMonsterUpdate",ctx,dt);return !c.Failed;
    }
    public bool UpdateVillager(Citizen v,float dt)
    {
        var c=ResolveVillager(v);if(c==null||!Has(c,"onVillagerUpdate"))return false;
        Invoke(c,"onVillagerUpdate",new ScriptVillager(city,v),dt);return !c.Failed;
    }
    public void BuildingWork(Building b,Citizen worker)
    {
        var c=ResolveBuilding(b);if(c!=null)Invoke(c,"onBuildingWork",new ScriptBuilding(city,b),new ScriptVillager(city,worker));
        foreach(var mod in components.Values.Where(c=>c.Type==ScriptType.Mod))Invoke(mod,"onBuildingWork",new ScriptBuilding(city,b),new ScriptVillager(city,worker));
    }
    public void Tick(float dt)
    {
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
        if(disposed||draining||starting)return;draining=true;
        try{for(int i=0;i<WorkPerFlush&&pending.TryDequeue(out var work);i++)try{work();}catch(Exception e){Console.Error.WriteLine("Script command: "+e.Message);}}
        finally{try{city.ResolveDeaths();}finally{draining=false;}}
    }
    public bool Attach(object entity,string script)
    {
        var type=entity is Building?ScriptType.Building:ScriptType.AI;
        if(script!=""&&(!components.TryGetValue(script,out var c)||c.Type!=type||c.Failed))return false;
        return Queue(()=>
        {
            string name;
            switch(entity)
            {
                case Building b when city.Buildings.Contains(b):b.Behavior=script;name="onBuildingAttached";break;
                case Citizen v when city.Citizens.Contains(v):v.Behavior=script;name="onVillagerAttached";break;
                case Creature m when city.Enemies.Contains(m):m.Behavior=script;name="onMonsterAttached";break;
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
