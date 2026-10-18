using System.IO.Compression;
namespace VillageCraft;

static class AssetCacheChecks
{
    public static void Run()
    {
        string folder=Path.Combine(Path.GetTempPath(),"villagecraft-cache-"+Guid.NewGuid().ToString("N"));
        string path=Path.Combine(folder,"cache.zip");
        static void Check(bool ok,string message){if(!ok)throw new Exception("FAILED: "+message);}
        try
        {
            int reads=0;var first=new AssetCache(path);first.Load();
            byte[] source=[1,2,3,4];
            Check(first.Read("minecraft/textures/entity/piglin/piglin.png",()=>{reads++;return source;})!.SequenceEqual(source),"external bytes returned");
            first.Read("minecraft/textures/entity/piglin/piglin.png",()=>throw new Exception("duplicate source read"));
            first.Read("missing.png",()=>null);first.Save();
            using(var zip=ZipFile.OpenRead(path))Check(zip.Entries.Count==1&&zip.Entries[0].FullName=="minecraft/textures/entity/piglin/piglin.png","only resolved, requested files are cached");
            var second=new AssetCache(path);second.Load();
            Check(second.Read("minecraft/textures/entity/piglin/piglin.png",()=>throw new Exception("source must not be needed"))!.SequenceEqual(source),"restart works without source assets");
            second.Read("minecraft/textures/item/emerald.png",()=>[5,6]);second.Save();
            var third=new AssetCache(path);third.Load();Check(third.Count==2&&reads==1,"new assets extend existing cache");
            File.WriteAllText(path,"invalid zip");var broken=new AssetCache(path);broken.Load();
            Check(broken.Count==0,"corrupt archive falls back cleanly");broken.Read("replacement.png",()=>source);broken.Save();
            using(var repaired=ZipFile.OpenRead(path))Check(repaired.Entries.Count==1,"corrupt cache rebuilt atomically");
            Check(Directory.GetFiles(folder,"*.tmp").Length==0,"temporary archive cleaned up");
            Console.WriteLine("PASS: used-only asset cache, deduplication, source-free restart, additions and corrupt archive recovery.");
        }
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }
}
