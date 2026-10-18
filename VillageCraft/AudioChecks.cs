using System.Numerics;
namespace VillageCraft;

static class AudioChecks
{
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAILED: "+text);}
    public static void Run()
    {
        var settings=Catalog.Data.Audio;settings.Validate();
        Check(settings.Cues.Any(c=>c.Trigger==AudioTrigger.Settlement&&c.Files.Contains("minecraft/sounds/random/levelup.ogg")),"vanilla level-up settlement sound");
        Check(settings.Cues.Any(c=>c.Trigger==AudioTrigger.BossWave&&c.Files.Contains("minecraft/sounds/item/totem/use_totem.ogg")),"vanilla totem raid sound");
        foreach(string path in settings.Cues.SelectMany(c=>c.Files).Distinct())
        {
            var bytes=Assets.Read(path);Check(bytes is {Length:>4}&&bytes.AsSpan(0,4).SequenceEqual("OggS"u8),"indexed Minecraft OGG resolves: "+path);
        }
        static int Storm(AudioSettings options,int speed)
        {
            var gate=new AudioGate(options);int played=0;
            for(int frame=0;frame<600;frame++)
                for(int i=0;i<speed*30;i++)
                    if(gate.TryAcquire(options.Cues[i%options.Cues.Count],frame/60d))played++;
            return played;
        }
        int normal=Storm(settings,1),fast=Storm(settings,16);
        Check(normal<=settings.MaxStartsPerSecond*10&&fast<=settings.MaxStartsPerSecond*10,"real-time sound starts remain bounded under event storms at 1x and 16x");
        var exactCue=settings.Cues.First(c=>c.Trigger==AudioTrigger.Settlement);var cooldown=new AudioGate(settings);
        Check(cooldown.TryAcquire(exactCue,0)&&!cooldown.TryAcquire(exactCue,.01)&&!cooldown.TryAcquire(exactCue,double.NaN)&&cooldown.TryAcquire(exactCue,exactCue.Cooldown),"real-time cooldown and invalid-clock guard");
        var reserved=new AudioGate(settings);
        for(int i=0;i<settings.MaxStartsPerSecond-settings.ReservedAlertStarts;i++)
            Check(reserved.TryAcquire(new SoundCue{Id="burst_"+i,Cooldown=.1,Priority=55},i*.13),"ordinary rate slot");
        Check(!reserved.TryAcquire(new SoundCue{Id="overflow",Cooldown=.1,Priority=55},.8),"ordinary effects cannot consume reserved alert starts");
        Check(reserved.TryAcquire(exactCue,.81),"settlement keeps a reserved rate slot during a sound storm");
        using var city=City.New(generateTerrain:false);
        Check(GameAudio.SurfaceAt(city,settings,new(1,1))==AudioSurface.Grass,"grass terrain material");
        city.Terrain.Water[1+city.Width]=1;
        Check(GameAudio.SurfaceAt(city,settings,new(1,1))==AudioSurface.Water,"water material");city.Terrain.Water[1+city.Width]=0;
        city.Terrain.Elevation[1+city.Width]=3;
        Check(GameAudio.SurfaceAt(city,settings,new(1,1))==AudioSurface.Stone,"mountain rock material");city.Terrain.Elevation[1+city.Width]=0;
        foreach(var road in settings.RoadSurfaces.Concat(settings.BuildingSurfaces))
        {
            var building=new Building{Id=city.NextId++,Prefab=road.Key,X=2,Z=2,Health=100};city.Buildings.Add(building);city.InvalidateTopology();
            Check(GameAudio.SurfaceAt(city,settings,new(2,2))==road.Value,"surface of road "+road.Key);city.Buildings.Remove(building);city.InvalidateTopology();
        }
        Console.WriteLine($"PASS: Minecraft OGG index/cache resolution, settlement/raid cues, real-time cooldowns, 1x/16x storm limits ({normal}/{fast} starts in 10 s), material footsteps.");
    }
}
