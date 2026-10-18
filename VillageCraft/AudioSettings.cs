namespace VillageCraft;

public enum AudioTrigger { Purr, Settlement, BossWave, Construction, Demolition, BuildingDamage, Work, Idle, Step, Hurt, Death, HouseOpen, HouseClose, DayAmbient, NightAmbient, Water, Harvest, Explosion, Footstep, Attack, Fuse }
public enum AudioCategory { Effects, Creatures, Ambience, Alerts, Footsteps }
public enum AudioSurface { Any, Grass, Stone, Wood, Gravel, Sand, Snow, Water, SoulSand }
public sealed class SoundCue
{
    public string Id {get;set;}=string.Empty;
    public AudioTrigger Trigger {get;set;}
    public AudioSurface Surface {get;set;}
    public AudioCategory Category {get;set;}=AudioCategory.Effects;
    public string Entity {get;set;}=string.Empty;
    public string Job {get;set;}=string.Empty;
    public string Prefab {get;set;}=string.Empty;
    public string[] Files {get;set;}=[];
    public float Volume {get;set;}=.5f;
    public double Cooldown {get;set;}=2;
    public bool Spatial {get;set;}=true;
    public int Priority {get;set;}=20;
}
public sealed class AudioSettings
{
    public int MaxVoices {get;set;}=12;
    public int MaxStartsPerSecond {get;set;}=8;
    public int ReservedAlertStarts {get;set;}=2;
    public int QueueLimit {get;set;}=48;
    public float HearingRadius {get;set;}=22;
    public Dictionary<string,AudioSurface> BuildingSurfaces {get;set;}=[];
    public Dictionary<string,AudioSurface> RoadSurfaces {get;set;}=[];
    public double FootstepSpacing {get;set;}=.2;
    public double FootstepInterval {get;set;}=.45;
    public double MinimumSpacing {get;set;}=.12;
    public double CreatureSpacing {get;set;}=.7;
    public double AmbienceSpacing {get;set;}=5;
    public double CreatureInterval {get;set;}=3;
    public double AmbienceInterval {get;set;}=16;
    public double WaterInterval {get;set;}=6;
    public List<SoundCue> Cues {get;set;}=[];
    public void Validate()
    {
        if(MaxVoices is <1 or >32||MaxStartsPerSecond is <2 or >24||ReservedAlertStarts<0||ReservedAlertStarts>=MaxStartsPerSecond||QueueLimit is <1 or >256||!float.IsFinite(HearingRadius)||HearingRadius is <1 or >100)
            throw new InvalidDataException("Invalid audio voice, rate or distance limits.");
        if(new[]{FootstepSpacing,FootstepInterval,MinimumSpacing,CreatureSpacing,AmbienceSpacing,CreatureInterval,AmbienceInterval,WaterInterval}.Any(v=>!double.IsFinite(v)||v<.01||v>600))throw new InvalidDataException("Invalid real-time audio intervals.");
        if(RoadSurfaces.Concat(BuildingSurfaces).Any(p=>string.IsNullOrWhiteSpace(p.Key)||!Enum.IsDefined(p.Value)||p.Value==AudioSurface.Any))throw new InvalidDataException("Invalid road audio surface mapping.");
        var ids=new HashSet<string>(StringComparer.Ordinal);
        foreach(var cue in Cues)
        {
            if(string.IsNullOrWhiteSpace(cue.Id)||!ids.Add(cue.Id)||!Enum.IsDefined(cue.Trigger)||!Enum.IsDefined(cue.Category)||!Enum.IsDefined(cue.Surface)||cue.Files.Length==0||cue.Files.Any(f=>!f.StartsWith("minecraft/sounds/",StringComparison.Ordinal)||!f.EndsWith(".ogg",StringComparison.Ordinal)||f.Contains("..")||f.Contains('\\'))||!float.IsFinite(cue.Volume)||cue.Volume is <0 or >1||!double.IsFinite(cue.Cooldown)||cue.Cooldown is <.1 or >600||cue.Priority is <0 or >100)
                throw new InvalidDataException("Invalid Minecraft audio cue: "+cue.Id);
        }
    }
}

// The caller supplies a monotonic real-time clock, never simulation time or speed-scaled dt.
sealed class AudioGate(AudioSettings settings)
{
    readonly Queue<double> starts=[];
    readonly Dictionary<string,double> nextCue=new(StringComparer.Ordinal);
    readonly double[] nextCategory=new double[Enum.GetValues<AudioCategory>().Length];
    double nextOrdinary;
    const int ImpactPriority=40,CriticalPriority=70;
    public bool TryAcquire(SoundCue cue,double now)
    {
        if(!double.IsFinite(now)||now<0)return false;
        while(starts.TryPeek(out double oldest)&&now-oldest>=1)starts.Dequeue();
        bool alert=cue.Category==AudioCategory.Alerts;
        if(starts.Count>=(alert?settings.MaxStartsPerSecond:settings.MaxStartsPerSecond-settings.ReservedAlertStarts)||now<nextCue.GetValueOrDefault(cue.Id)||cue.Priority<CriticalPriority&&now<nextCategory[(int)cue.Category]||!alert&&cue.Priority<ImpactPriority&&now<nextOrdinary)return false;
        starts.Enqueue(now);nextCue[cue.Id]=now+cue.Cooldown;
        nextCategory[(int)cue.Category]=now+(cue.Category switch{AudioCategory.Creatures=>settings.CreatureSpacing,AudioCategory.Ambience=>settings.AmbienceSpacing,AudioCategory.Footsteps=>settings.FootstepSpacing,_=>settings.MinimumSpacing});
        if(!alert)nextOrdinary=now+settings.MinimumSpacing;
        return true;
    }
}
