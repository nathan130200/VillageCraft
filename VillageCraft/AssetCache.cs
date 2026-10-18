using System.IO.Compression;
namespace VillageCraft;

// Raw Minecraft files only; GPU textures, game content and saves stay outside this archive.
sealed class AssetCache(string path)
{
    readonly Dictionary<string,byte[]> files=new(StringComparer.OrdinalIgnoreCase);
    bool dirty;
    public int Count=>files.Count;
    public void Load()
    {
        if(!File.Exists(path))return;
        try
        {
            using var zip=ZipFile.OpenRead(path);
            var loaded=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach(var entry in zip.Entries)
            {
                if(entry.FullName.EndsWith('/'))continue;
                using var input=entry.Open();using var bytes=new MemoryStream();input.CopyTo(bytes);
                loaded.Add(entry.FullName,bytes.ToArray());
            }
            foreach(var item in loaded)files.Add(item.Key,item.Value);
            GameLog.Write(GameLogLevel.Info,$"Asset cache loaded: {files.Count} files.");
        }
        catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {GameLog.Write(GameLogLevel.Warning,"Asset cache unavailable; resolving external assets: "+e.Message);}
    }
    public byte[]? Read(string name,Func<byte[]?> resolve)
    {
        if(files.TryGetValue(name,out var bytes))return bytes;
        bytes=resolve();if(bytes!=null){files.Add(name,bytes);dirty=true;}return bytes;
    }
    public void Save()
    {
        if(!dirty)return;
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create))
                foreach(var item in files.OrderBy(f=>f.Key,StringComparer.Ordinal))
                {using var output=zip.CreateEntry(item.Key,CompressionLevel.Fastest).Open();output.Write(item.Value);}
            File.Move(temporary,path,true);dirty=false;
            GameLog.Write(GameLogLevel.Info,$"Asset cache saved: {files.Count} files.");
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException)
        {GameLog.Write(GameLogLevel.Warning,"Asset cache could not be saved: "+e.Message);}
        finally
        {
            try{File.Delete(temporary);}catch(Exception e)when(e is IOException or UnauthorizedAccessException)
            {GameLog.Write(GameLogLevel.Warning,"Asset cache temporary file cleanup failed: "+e.Message);}
        }
    }
}
