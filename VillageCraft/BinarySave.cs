using System.Security.Cryptography;
using System.Text;
namespace VillageCraft;

// Explicit, versioned little-endian records. No runtime type activation or BinaryFormatter.
public static class BinarySave
{
    public const int MaxBytes=64*1024*1024;
    static readonly UTF8Encoding Utf8=new(false,true);
    static void Text(BinaryWriter w,string text){byte[] bytes=Utf8.GetBytes(text);if(bytes.Length>65536)throw new InvalidDataException("Save string is too long.");w.Write(bytes.Length);w.Write(bytes);}
    static string Text(BinaryReader r){int n=Count(r,65536);var bytes=r.ReadBytes(n);if(bytes.Length!=n)throw new EndOfStreamException();return Utf8.GetString(bytes);}
    static int Count(BinaryReader r,int max){int n=r.ReadInt32();if(n<0||n>max)throw new InvalidDataException("Invalid binary collection length.");return n;}
    static void Map(BinaryWriter w,Dictionary<ResourceType,float> map){w.Write(map.Count);foreach(var e in map.OrderBy(e=>e.Key)){w.Write((byte)e.Key);w.Write(e.Value);}}
    static void Map(BinaryWriter w,Dictionary<string,float> map){w.Write(map.Count);foreach(var e in map.OrderBy(e=>e.Key)){Text(w,e.Key);w.Write(e.Value);}}
    static ResourceType Resource(BinaryReader r,bool optional=false){var id=(ResourceType)r.ReadByte();if(!Enum.IsDefined(id)||!optional&&id==ResourceType.None)throw new InvalidDataException("Invalid resource ID.");return id;}
    static Dictionary<string,float> Effects(BinaryReader r)
    {int n=Count(r,16);var result=new Dictionary<string,float>();for(int i=0;i<n;i++)if(!result.TryAdd(Text(r),r.ReadSingle()))throw new InvalidDataException("Duplicate effect.");return result;}
    static Dictionary<ResourceType,float> Map(BinaryReader r)
    {int n=Count(r,64);var result=new Dictionary<ResourceType,float>();for(int i=0;i<n;i++)if(!result.TryAdd(Resource(r),r.ReadSingle()))throw new InvalidDataException("Duplicate resource.");return result;}
    static void Rules(BinaryWriter w,VillageRules r)
    {w.Write(r.RaidsEnabled);w.Write(r.RaidInterval);w.Write(r.WaveBase);w.Write(r.WaveGrowth);w.Write(r.MaxRaiders);w.Write(r.MaxPopulation);w.Write(r.AnimalsPerPen);w.Write(r.GolemsPerWorkshop);w.Write(r.DayLength);w.Write(r.ProductionCycle);}
    static VillageRules Rules(BinaryReader r)=>new(){RaidsEnabled=r.ReadBoolean(),RaidInterval=r.ReadInt32(),WaveBase=r.ReadInt32(),WaveGrowth=r.ReadInt32(),MaxRaiders=r.ReadInt32(),MaxPopulation=r.ReadInt32(),AnimalsPerPen=r.ReadInt32(),GolemsPerWorkshop=r.ReadInt32(),DayLength=r.ReadSingle(),ProductionCycle=r.ReadSingle()};
    static void Creatures(BinaryWriter w,List<Creature> list)
    {w.Write(list.Count);foreach(var c in list){Text(w,c.Type);w.Write(c.Home);w.Write(c.X);w.Write(c.Z);w.Write(c.Health);w.Write(c.Cooldown);w.Write(c.Age);w.Write(c.Facing);}}
    static List<Creature> Creatures(BinaryReader r,int max)
    {int n=Count(r,max);var list=new List<Creature>(n);for(int i=0;i<n;i++)list.Add(new(){Type=Text(r),Home=r.ReadInt32(),X=r.ReadSingle(),Z=r.ReadSingle(),Health=r.ReadSingle(),Cooldown=r.ReadSingle(),Age=r.ReadSingle(),Facing=r.ReadSingle()});return list;}
    public static void Write(string path,City c)
    {
        using var profile=Profiler.Measure(ProfileArea.Save);
        c.EnsureEntityIds();c.Terrain.Validate(c.Width,c.Height);
        using var payload=new MemoryStream();
        using(var w=new BinaryWriter(payload,Utf8,true))
        {
            w.Write(c.Width);w.Write(c.Height);w.Write(c.Day);w.Write(c.DayTime);w.Write(c.Tax);w.Write(c.Happiness);w.Write(c.Balance);w.Write(c.NextId);w.Write(c.Waves);w.Write(c.Kills);w.Write(c.ArmorLevel);w.Write(c.ToolLevel);w.Write(c.Growth);Text(w,c.Message);Rules(w,c.Rules);Map(w,c.Stock);
            w.Write(c.Buildings.Count);foreach(var b in c.Buildings){w.Write(b.Id);Text(w,b.Prefab);w.Write(b.X);w.Write(b.Z);w.Write(b.Rotation);w.Write(b.Health);w.Write(b.Priority);w.Write(b.Enabled);w.Write(b.Worked);w.Write(b.WorkEffort);w.Write(b.Attendance);w.Write(b.ServiceCredit);}
            w.Write(c.Citizens.Count);foreach(var v in c.Citizens){w.Write(v.Id);w.Write(v.Home);w.Write(v.Work);w.Write(v.X);w.Write(v.Z);w.Write(v.Health);Text(w,Tasks.Label(v.Task));w.Write(v.Cooldown);w.Write(v.WorkTime);w.Write(v.HasWorked);Text(w,v.Profession);w.Write(v.Facing);}
            Creatures(w,c.Animals);Creatures(w,c.Defenders);Creatures(w,c.Enemies);w.Write(c.Events.Count);foreach(var e in c.Events)Text(w,e);
            w.Write(c.LastNight);w.Write(c.Rules.NightMonsters);w.Write(c.Rules.NightCount);
            foreach(var e in c.Enemies){w.Write(e.NightSpawn);w.Write(e.BrainTime);}
            foreach(var v in c.Citizens){CitizenNames.Ensure(v);Text(w,v.FirstName);Text(w,v.Surname);Map(w,v.Inventory);}
            foreach(var b in c.Buildings){Text(w,b.Tags);Text(w,b.Behavior);}
            foreach(var v in c.Citizens){Text(w,v.Tags);Text(w,v.Behavior);}
            foreach(var m in c.Animals.Concat(c.Defenders).Concat(c.Enemies)){w.Write(m.Id);Text(w,m.Tags);Text(w,m.Behavior);}
            w.Write(c.Terrain.Seed);w.Write(c.Terrain.Elevation.Length);
            for(int i=0;i<c.Terrain.Elevation.Length;i++){w.Write((sbyte)c.Terrain.Elevation[i]);w.Write(c.Terrain.Water[i]);w.Write(c.Terrain.Grass[i]);}
            foreach(var b in c.Buildings)w.Write(b.GrowthDays);
            w.Write(c.PeaceDays);w.Write(c.TutorialEnabled);w.Write(c.TutorialStep);w.Write(c.TutorialInspected);w.Write(c.TutorialRoadBuilt);w.Write(c.TutorialTreesHarvested);
            foreach(var b in c.Buildings){Map(w,b.Storage);w.Write((byte)b.RequestedResource);}
            foreach(var v in c.Citizens){Map(w,v.Cargo);Map(w,v.Effects);w.Write(v.PickupSource);w.Write(v.DeliveryTarget);w.Write((byte)v.DeliveryResource);}
            w.Write(c.StorageInitialized);w.Write(c.LastSettlementDay);w.Write(c.EnchantedArmor);
            foreach(var m in c.Enemies){w.Write(m.LeatherHelmet);w.Write(m.BossWave);}
            w.Write(c.GameOver);var stats=c.Statistics;
            w.Write(stats.RecordedSinceDay);w.Write(stats.SimulatedSeconds);w.Write(stats.PeakPopulation);w.Write(stats.Arrivals);w.Write(stats.VillagersLost);w.Write(stats.BuildingsPlaced);w.Write(stats.BuildingsDestroyed);w.Write(stats.Deliveries);w.Write(stats.SunDeaths);w.Write(stats.EmeraldsFromGoods);Map(w,stats.Production);
            w.Write(c.Rules.BirthChance);w.Write(stats.Births);w.Write(c.Babies.Count);foreach(var baby in c.Babies){w.Write(baby.Home);w.Write(baby.DueDay);}
            w.Write(c.Rules.NightSpawnIntervalMinutes);w.Write(c.LastNightSpawnMinute);


        }
        if(payload.Length>MaxBytes)throw new InvalidDataException("Village exceeds save size limit.");
        byte[] bytes=payload.ToArray();string full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using(var file=File.Create(full+".tmp"))using(var w=new BinaryWriter(file,Utf8))
        {w.Write("VCTY"u8);w.Write((ushort)11);w.Write(bytes.Length);w.Write(SHA256.HashData(bytes));w.Write(bytes);file.Flush(true);}
        File.Move(full+".tmp",full,true);
    }
    public static City Read(string path)
    {
        using var profile=Profiler.Measure(ProfileArea.Load);
        try
        {
            using var file=File.OpenRead(path);if(file.Length>MaxBytes+42)throw new InvalidDataException("Save exceeds size limit.");
            using var header=new BinaryReader(file,Utf8,true);
            if(!header.ReadBytes(4).AsSpan().SequenceEqual("VCTY"u8))throw new InvalidDataException("Unsupported binary save format.");
            ushort format=header.ReadUInt16();if(format!=11)throw new InvalidDataException("This beta requires save format 11. Older beta saves are not supported.");
            int length=Count(header,MaxBytes);byte[] hash=header.ReadBytes(32);if(file.Length-file.Position!=length)throw new InvalidDataException("Truncated save or unexpected trailing data.");
            byte[] bytes=header.ReadBytes(length);if(!CryptographicOperations.FixedTimeEquals(hash,SHA256.HashData(bytes)))throw new InvalidDataException("Save checksum failed; the file is damaged.");
            using var payload=new MemoryStream(bytes,false);using var r=new BinaryReader(payload,Utf8);
            var c=new City{PeaceDays=0,Version=3,Width=r.ReadInt32(),Height=r.ReadInt32(),Day=r.ReadInt32(),DayTime=r.ReadSingle(),Tax=r.ReadSingle(),Happiness=r.ReadSingle(),Balance=r.ReadSingle(),NextId=r.ReadInt32(),Waves=r.ReadInt32(),Kills=r.ReadInt32(),ArmorLevel=r.ReadInt32(),ToolLevel=r.ReadInt32(),Growth=r.ReadSingle(),Message=Text(r),Rules=Rules(r),Stock=Map(r)};
            int n=Count(r,512*512);for(int i=0;i<n;i++)c.Buildings.Add(new(){Id=r.ReadInt32(),Prefab=Text(r),X=r.ReadInt32(),Z=r.ReadInt32(),Rotation=r.ReadInt32(),Health=r.ReadSingle(),Priority=r.ReadInt32(),Enabled=r.ReadBoolean(),Worked=r.ReadBoolean(),WorkEffort=r.ReadSingle(),Attendance=r.ReadSingle(),ServiceCredit=r.ReadSingle()});
            n=Count(r,4096);for(int i=0;i<n;i++)c.Citizens.Add(new(){Id=r.ReadInt32(),Home=r.ReadInt32(),Work=r.ReadInt32(),X=r.ReadSingle(),Z=r.ReadSingle(),Health=r.ReadSingle(),Task=Tasks.Parse(Text(r)),Cooldown=r.ReadSingle(),WorkTime=r.ReadSingle(),HasWorked=r.ReadBoolean(),Profession=Text(r),Facing=r.ReadSingle()});
            c.Animals=Creatures(r,32768);c.Defenders=Creatures(r,8192);c.Enemies=Creatures(r,4096);n=Count(r,512);for(int i=0;i<n;i++)c.Events.Add(Text(r));
            c.LastNight=r.ReadInt32();c.Rules.NightMonsters=r.ReadBoolean();c.Rules.NightCount=r.ReadInt32();
            foreach(var e in c.Enemies){e.NightSpawn=r.ReadBoolean();e.BrainTime=r.ReadSingle();}
            foreach(var v in c.Citizens){v.FirstName=Text(r);v.Surname=Text(r);v.Inventory=Map(r);}
            foreach(var b in c.Buildings){b.Tags=Text(r);b.Behavior=Text(r);}
            foreach(var v in c.Citizens){v.Tags=Text(r);v.Behavior=Text(r);}
            foreach(var m in c.Animals.Concat(c.Defenders).Concat(c.Enemies)){m.Id=r.ReadInt32();m.Tags=Text(r);m.Behavior=Text(r);}
            int seed=r.ReadInt32(),count=Count(r,512*512);if(count!=c.Width*c.Height)throw new InvalidDataException("Invalid terrain size.");
            c.Terrain=TerrainData.Flat(c.Width,c.Height);c.Terrain.Seed=seed;
            for(int i=0;i<count;i++){c.Terrain.Elevation[i]=r.ReadSByte();c.Terrain.Water[i]=r.ReadByte();c.Terrain.Grass[i]=r.ReadByte();}
            foreach(var b in c.Buildings)b.GrowthDays=r.ReadInt32();
            c.PeaceDays=r.ReadInt32();c.TutorialEnabled=r.ReadBoolean();c.TutorialStep=r.ReadInt32();c.TutorialInspected=r.ReadBoolean();c.TutorialRoadBuilt=r.ReadBoolean();c.TutorialTreesHarvested=r.ReadInt32();
            foreach(var b in c.Buildings){b.Storage=Map(r);b.RequestedResource=Resource(r,true);}
            foreach(var v in c.Citizens){v.Cargo=Map(r);v.Effects=Effects(r);v.PickupSource=r.ReadInt32();v.DeliveryTarget=r.ReadInt32();v.DeliveryResource=Resource(r,true);}
            c.StorageInitialized=r.ReadBoolean();c.LastSettlementDay=r.ReadInt32();c.EnchantedArmor=r.ReadBoolean();
            foreach(var m in c.Enemies){m.LeatherHelmet=r.ReadBoolean();m.BossWave=r.ReadBoolean();}
            c.GameOver=r.ReadBoolean();c.Statistics=new(){RecordedSinceDay=r.ReadInt32(),SimulatedSeconds=r.ReadDouble(),PeakPopulation=r.ReadInt32(),Arrivals=r.ReadInt32(),VillagersLost=r.ReadInt32(),BuildingsPlaced=r.ReadInt32(),BuildingsDestroyed=r.ReadInt32(),Deliveries=r.ReadInt32(),SunDeaths=r.ReadInt32(),EmeraldsFromGoods=r.ReadSingle(),Production=Map(r)};
            c.Rules.BirthChance=r.ReadSingle();c.Statistics.Births=r.ReadInt32();n=Count(r,4096);for(int i=0;i<n;i++)c.Babies.Add(new(){Home=r.ReadInt32(),DueDay=r.ReadInt32()});
            c.Rules.NightSpawnIntervalMinutes=r.ReadInt32();c.LastNightSpawnMinute=r.ReadInt64();
            if(payload.Position!=payload.Length)throw new InvalidDataException("Unexpected binary records.");return c;
        }
        catch(Exception e)when(e is EndOfStreamException or DecoderFallbackException){throw new InvalidDataException("Truncated or invalid binary save.",e);}
    }
}
