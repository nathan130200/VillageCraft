using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
namespace VillageCraft;

sealed class GameAudio:IDisposable
{
    readonly AudioSettings settings;
    readonly AudioGate gate;
    readonly Dictionary<string,Sound> clips=new(StringComparer.Ordinal);
    readonly Dictionary<SoundCue,Sound[]> variants=[];
    readonly Dictionary<AudioTrigger,SoundCue[]> cues;
    readonly List<Voice> voices=[];
    readonly List<Request> pending=[];
    readonly HashSet<(string,int)> queued=[];
    readonly Dictionary<int,Movement> movement=[];
    readonly Dictionary<int,TaskId> tasks=[];
    readonly long origin=Stopwatch.GetTimestamp();
    City? city;
    Vector2 listener,right=Vector2.UnitX;
    double nextCreature,nextAmbience,nextWater,nextMovement,nextHouse;
    bool ready,disposed;
    sealed class Voice {public Sound Sound;public required SoundCue Cue;public Vector2 Position;public float Gain;public int Source;}
    readonly record struct Request(SoundCue Cue,Vector2 Position,int Source);
    readonly record struct Movement(Vector2 Position,float Distance,double Next);
    readonly long[] playsByTrigger=new long[Enum.GetValues<AudioTrigger>().Length];
    public long PlayedCount(AudioTrigger trigger)=>playsByTrigger[(int)trigger];
    public int ActiveVoices=>voices.Count;
    public int LoadedSounds=>clips.Count;
    public long Played {get;private set;}
    public long Dropped {get;private set;}
    double Now=>Stopwatch.GetElapsedTime(origin).TotalSeconds;
    public GameAudio(AudioSettings settings)
    {this.settings=settings;gate=new(settings);cues=settings.Cues.GroupBy(c=>c.Trigger).ToDictionary(g=>g.Key,g=>g.ToArray());}
    public void Initialize()
    {
        if(ready||disposed)return;
        using var profile=Profiler.Measure(ProfileArea.Audio);
        Raylib.InitAudioDevice();ready=Raylib.IsAudioDeviceReady();
        if(!ready){GameLog.Write(GameLogLevel.Warning,"Audio device unavailable; continuing silently.");return;}
        foreach(string file in settings.Cues.SelectMany(c=>c.Files).Distinct(StringComparer.Ordinal))
        {
            byte[]? bytes;
            try{bytes=Assets.Read(file);}
            catch(Exception e)when(e is IOException or UnauthorizedAccessException)
            {GameLog.Write(GameLogLevel.Warning,"Minecraft sound source unavailable; continuing with loaded clips: "+e.Message);break;}
            if(bytes==null){GameLog.Write(GameLogLevel.Warning,"Missing Minecraft sound: "+file);continue;}
            var wave=Raylib.LoadWaveFromMemory(".ogg",bytes);
            try
            {
                if(!Raylib.IsWaveValid(wave)){GameLog.Write(GameLogLevel.Warning,"Cannot decode Minecraft OGG: "+file);continue;}
                var sound=Raylib.LoadSoundFromWave(wave);
                if(Raylib.IsSoundValid(sound))clips.Add(file,sound);
                else GameLog.Write(GameLogLevel.Warning,"Cannot load Minecraft sound: "+file);
            }
            finally{Raylib.UnloadWave(wave);}
        }
        foreach(var cue in settings.Cues)variants[cue]=cue.Files.Where(clips.ContainsKey).Select(f=>clips[f]).ToArray();
        Assets.FlushCache();GameLog.Write(GameLogLevel.Info,$"Audio: {clips.Count} Minecraft OGG clips decoded once; {settings.MaxVoices} voice limit.");
    }
    public void Bind(City next)
    {
        if(ReferenceEquals(city,next))return;
        if(city!=null)city.Changed-=OnEvent;
        StopVoices();pending.Clear();queued.Clear();movement.Clear();tasks.Clear();
        city=next;city.Changed+=OnEvent;
        foreach(var v in city.Citizens)tasks[v.Id]=v.Task;
        double now=Now;nextCreature=now+1;nextAmbience=now+8;nextWater=now+2;
    }
    SoundCue? Find(AudioTrigger trigger,string entity="",string job="",string prefab="",AudioSurface surface=AudioSurface.Any)
    {
        if(!cues.TryGetValue(trigger,out var choices))return null;
        foreach(var cue in choices)
            if((string.IsNullOrEmpty(cue.Entity)||cue.Entity==entity)&&(string.IsNullOrEmpty(cue.Job)||cue.Job==job)&&(string.IsNullOrEmpty(cue.Prefab)||cue.Prefab==prefab)&&(cue.Surface==AudioSurface.Any||cue.Surface==surface))return cue;
        return null;
    }
    void Enqueue(SoundCue? cue,Vector2 position,int source=0)
    {
        if(!ready||cue==null||GraphicsOptions.Current.MasterVolume<=0||CategoryVolume(cue.Category)<=0)return;
        if(cue.Spatial&&Vector2.DistanceSquared(position,listener)>=settings.HearingRadius*settings.HearingRadius)return;
        if(queued.Contains((cue.Id,source)))return;
        if(pending.Count>=settings.QueueLimit)
        {
            int worst=0;for(int i=1;i<pending.Count;i++)if(pending[i].Cue.Priority<pending[worst].Cue.Priority)worst=i;
            if(pending[worst].Cue.Priority>=cue.Priority){Dropped++;return;}
            queued.Remove((pending[worst].Cue.Id,pending[worst].Source));pending.RemoveAt(worst);Dropped++;
        }
        queued.Add((cue.Id,source));pending.Add(new(cue,position,source));
    }
    void OnEvent(WorldEvent evt)
    {
        using var profile=Profiler.Measure(ProfileArea.Audio);
        if(!ready)return;
        if(evt.Name=="onGoodsConverted"&&evt.Amount>0){Enqueue(Find(AudioTrigger.Settlement),listener);return;}
        if(evt.Name=="onBossWaveStarted"){Enqueue(Find(AudioTrigger.BossWave),listener);return;}
        if(evt.Name=="onExplosion"&&evt.Subject is Creature blast){Enqueue(Find(AudioTrigger.Explosion),blast.Position,blast.Id);return;}
        if(evt.Subject is Building b)
        {
            AudioTrigger? trigger=evt.Name switch{"onBuildingPlaced"=>AudioTrigger.Construction,"onBuildingRemoved"=>AudioTrigger.Demolition,"onBuildingDamaged"=>AudioTrigger.BuildingDamage,"onBuildingWork"=>AudioTrigger.Work,"onTreeHarvested"=>AudioTrigger.Harvest,_=>null};
            if(trigger is {} kind)Enqueue(Find(kind,job:b.Plan.Job,prefab:b.Prefab),new(b.X,b.Z),b.Id);
        }
        else if(evt.Subject is Citizen v)
        {
            AudioTrigger? trigger=evt.Name switch{"onVillagerAttack"=>AudioTrigger.Attack,"onVillagerDamaged"=>AudioTrigger.Hurt,"onVillagerRemoved" when evt.Reason=="killed"=>AudioTrigger.Death,_=>null};
            if(trigger is {} kind)Enqueue(Find(kind,kind==AudioTrigger.Attack?v.Profession:"villager"),v.Position,v.Id);
        }
        else if(evt.Subject is Creature m)
        {
            if(city!.IsNight&&(city.Animals.Contains(m)||evt.Name.StartsWith("onAnimal",StringComparison.Ordinal)))return;
            AudioTrigger? trigger=evt.Name switch{"onMonsterAttack" or "onGuardianAttack"=>AudioTrigger.Attack,"onMonsterPrimed"=>AudioTrigger.Fuse,"onMonsterDamaged" or "onGuardianDamaged" or "onAnimalDamaged"=>AudioTrigger.Hurt,"onMonsterRemoved" or "onGuardianRemoved" or "onAnimalRemoved" when evt.Reason=="killed"=>AudioTrigger.Death,_=>null};
            if(trigger is {} kind)Enqueue(Find(kind,m.Type),m.Position,m.Id);
        }
    }
    public static AudioSurface SurfaceAt(City city,AudioSettings settings,Vector2 position)
    {
        int x=(int)MathF.Round(position.X),z=(int)MathF.Round(position.Y);
        var b=city.At(x,z);
        if(b!=null&&settings.BuildingSurfaces.TryGetValue(b.Prefab,out var floor))return floor;
        if(b?.Plan.Path==true)return settings.RoadSurfaces.GetValueOrDefault(b.Prefab,AudioSurface.Stone);
        if(city.IsWater(x,z))return AudioSurface.Water;
        return city.Elevation(x,z)>=3?AudioSurface.Stone:AudioSurface.Grass;
    }
    void Moved(int id,Vector2 position,string type,double now)
    {
        if(!movement.TryGetValue(id,out var last)){movement[id]=new(position,0,now);return;}
        float delta=Vector2.Distance(last.Position,position);
        float distance=last.Distance+delta;double next=last.Next;
        if(distance>=.4f&&now>=next)
        {
            Enqueue(Find(AudioTrigger.Footstep,surface:SurfaceAt(city!,settings,position)),position,id);
            if(type is "iron_golem" or "snow_golem")Enqueue(Find(AudioTrigger.Step,type),position,id);
            distance=0;next=now+settings.FootstepInterval;
        }
        movement[id]=new(position,distance,next);
    }
    void SampleMovement(double now)
    {
        foreach(var v in city!.Citizens)if(v.Health>0)Moved(v.Id,v.Position,"villager",now);
        foreach(var m in city.Enemies.Concat(city.Defenders).Concat(city.IsNight?Enumerable.Empty<Creature>():city.Animals))
            if(m.Health>0&&m.Type!="blaze")Moved(m.Id,m.Position,m.Type,now);
        if(city.IsNight)foreach(var animal in city.Animals)movement.Remove(animal.Id);
        if(movement.Count>city.Citizens.Count+city.Enemies.Count+city.Defenders.Count+city.Animals.Count+32)
        {
            var ids=city.Citizens.Select(v=>v.Id).Concat(city.Enemies.Concat(city.Defenders).Concat(city.Animals).Select(m=>m.Id)).ToHashSet();
            foreach(int id in movement.Keys.Where(id=>!ids.Contains(id)).ToArray())movement.Remove(id);
        }
    }
    void SampleHomes()
    {
        foreach(var v in city!.Citizens)
        {
            if(tasks.TryGetValue(v.Id,out var old)&&old!=v.Task)
            {
                var home=city.Buildings.FirstOrDefault(b=>b.Id==v.Home);
                if(home!=null)
                {
                    if(v.Task==TaskId.Resting)Enqueue(Find(AudioTrigger.HouseClose),new(home.X,home.Z),home.Id);
                    else if(old==TaskId.Resting&&Vector2.DistanceSquared(v.Position,new(home.X,home.Z))<1)Enqueue(Find(AudioTrigger.HouseOpen),new(home.X,home.Z),home.Id);
                }
            }
            tasks[v.Id]=v.Task;
        }
        if(tasks.Count>city.Citizens.Count+32){var living=city.Citizens.Select(v=>v.Id).ToHashSet();foreach(int id in tasks.Keys.Where(id=>!living.Contains(id)).ToArray())tasks.Remove(id);}
    }
    void SampleCreature()
    {
        var nearby=city!.Enemies.Concat(city.IsNight?Enumerable.Empty<Creature>():city.Animals).Where(m=>m.Health>0&&Vector2.DistanceSquared(m.Position,listener)<settings.HearingRadius*settings.HearingRadius).ToArray();
        bool villager=nearby.Length==0||Random.Shared.Next(3)==0;
        if(villager)
        {
            var people=city.Citizens.Where(v=>v.Health>0&&v.Task!=TaskId.Resting&&Vector2.DistanceSquared(v.Position,listener)<settings.HearingRadius*settings.HearingRadius).ToArray();
            if(people.Length>0){var v=people[Random.Shared.Next(people.Length)];Enqueue(Find(AudioTrigger.Idle,"villager"),v.Position,v.Id);}
        }
        else{var m=nearby[Random.Shared.Next(nearby.Length)];Enqueue(Find(AudioTrigger.Idle,m.Type),m.Position,m.Id);}
    }
    void SampleAmbience()
    {
        var source=city!.Buildings.Where(b=>b.Enabled&&(city.IsNight?b.Prefab=="cave":b.Plan.Natural&&b.Plan.Harvest.Count>0)).OrderBy(b=>Vector2.DistanceSquared(new(b.X,b.Z),listener)).FirstOrDefault();
        if(source!=null)Enqueue(Find(city.IsNight?AudioTrigger.NightAmbient:AudioTrigger.DayAmbient,prefab:source.Prefab),new(source.X,source.Z),source.Id);
    }
    static float CategoryVolume(AudioCategory category)=>category switch
    {AudioCategory.Alerts=>GraphicsOptions.Current.AlertsVolume,AudioCategory.Creatures=>GraphicsOptions.Current.CreaturesVolume,AudioCategory.Ambience=>GraphicsOptions.Current.AmbienceVolume,_=>GraphicsOptions.Current.EffectsVolume};
    public void Update(City current,Vector2 focus,Vector2 cameraRight,float dt,bool suspended)
    {
        using var profile=Profiler.Measure(ProfileArea.Audio);
        Bind(current);listener=focus;right=cameraRight;
        if(!ready)return;
        double now=Now;bool quiet=suspended||current.GameOver||GraphicsOptions.Current.MasterVolume<=0;
        for(int i=voices.Count-1;i>=0;i--)
        {
            var voice=voices[i];
            if(current.IsNight&&current.Animals.Any(a=>a.Id==voice.Source)){Raylib.StopSound(voice.Sound);Raylib.UnloadSoundAlias(voice.Sound);voices.RemoveAt(i);continue;}
            float attenuation=voice.Cue.Spatial?MathF.Pow(Math.Clamp(1-Vector2.Distance(listener,voice.Position)/settings.HearingRadius,0,1),2):1;
            float target=quiet?0:voice.Cue.Volume*GraphicsOptions.Current.MasterVolume*CategoryVolume(voice.Cue.Category)*attenuation;
            voice.Gain+=(target-voice.Gain)*Math.Min(1,Math.Max(0,dt)*12);
            Raylib.SetSoundVolume(voice.Sound,voice.Gain);
            Raylib.SetSoundPan(voice.Sound,voice.Cue.Spatial?Math.Clamp(.5f+Vector2.Dot(voice.Position-listener,right)/settings.HearingRadius*.45f,.05f,.95f):.5f);
            if(!Raylib.IsSoundPlaying(voice.Sound)||quiet&&voice.Gain<.001f){Raylib.StopSound(voice.Sound);Raylib.UnloadSoundAlias(voice.Sound);voices.RemoveAt(i);}
        }
        if(!quiet)
        {
            if(now>=nextMovement){nextMovement=now+.08;SampleMovement(now);}
            if(now>=nextHouse){nextHouse=now+.35;SampleHomes();}
            if(now>=nextCreature){nextCreature=now+settings.CreatureInterval*(.8+Random.Shared.NextDouble()*.4);SampleCreature();}
            if(now>=nextAmbience){nextAmbience=now+settings.AmbienceInterval*(.85+Random.Shared.NextDouble()*.3);SampleAmbience();}
            if(now>=nextWater)
            {
                nextWater=now+settings.WaterInterval*(.85+Random.Shared.NextDouble()*.3);
                var water=city!.Buildings.Where(b=>b.Enabled&&b.Plan.Water>0).OrderBy(b=>Vector2.DistanceSquared(listener,new(b.X,b.Z))).FirstOrDefault();
                if(water!=null)Enqueue(Find(AudioTrigger.Water,prefab:water.Prefab),new(water.X,water.Z),water.Id);
            }
            pending.Sort(static(a,b)=>b.Cue.Priority.CompareTo(a.Cue.Priority));
            foreach(var request in pending)Play(request,now);
        }
        pending.Clear();queued.Clear();
    }
    void Play(Request request,double now)
    {
        if(city!.IsNight&&city.Animals.Any(a=>a.Id==request.Source))return;
        var cue=request.Cue;
        if(CategoryVolume(cue.Category)<=0)return;
        float attenuation=cue.Spatial?MathF.Pow(Math.Clamp(1-Vector2.Distance(listener,request.Position)/settings.HearingRadius,0,1),2):1;
        if(attenuation<.001f)return;
        if(!variants.TryGetValue(cue,out var available)||available.Length==0)return;
        var original=available[Random.Shared.Next(available.Length)];
        if(voices.Count>=settings.MaxVoices)
        {
            int worst=0;for(int i=1;i<voices.Count;i++)if(voices[i].Cue.Priority<voices[worst].Cue.Priority)worst=i;
            if(voices[worst].Cue.Priority>=cue.Priority){Dropped++;return;}
            if(!gate.TryAcquire(cue,now)){Dropped++;return;}
            Raylib.StopSound(voices[worst].Sound);Raylib.UnloadSoundAlias(voices[worst].Sound);voices.RemoveAt(worst);
        }
        else if(!gate.TryAcquire(cue,now)){Dropped++;return;}
        var sound=Raylib.LoadSoundAlias(original);if(!Raylib.IsSoundValid(sound))return;
        float gain=cue.Volume*GraphicsOptions.Current.MasterVolume*CategoryVolume(cue.Category)*attenuation;
        Raylib.SetSoundVolume(sound,gain);Raylib.SetSoundPitch(sound,1); // Simulation speed never changes audio pitch.
        Raylib.SetSoundPan(sound,cue.Spatial?Math.Clamp(.5f+Vector2.Dot(request.Position-listener,right)/settings.HearingRadius*.45f,.05f,.95f):.5f);
        Raylib.PlaySound(sound);voices.Add(new(){Sound=sound,Cue=cue,Position=request.Position,Gain=gain,Source=request.Source});Played++;playsByTrigger[(int)cue.Trigger]++;
    }
    void StopVoices(){foreach(var voice in voices){Raylib.StopSound(voice.Sound);Raylib.UnloadSoundAlias(voice.Sound);}voices.Clear();}
    public void Dispose()
    {
        if(disposed)return;disposed=true;if(city!=null)city.Changed-=OnEvent;StopVoices();
        foreach(var clip in clips.Values)Raylib.UnloadSound(clip);clips.Clear();
        if(ready)Raylib.CloseAudioDevice();ready=false;
    }
}
