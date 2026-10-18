namespace VillageCraft;
static class SavePreviewChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("SAVE PREVIEW: "+message);}
    public static void Run()
    {
        string path=Path.Combine(Path.GetTempPath(),"villagecraft-preview-"+Guid.NewGuid()+".vcs");
        try
        {
            using var city=City.New(generateTerrain:false);city.Day=12;city.PlaySeconds=3723.5;city.Stock[ResourceType.Emeralds]=123.5f;
            city.Save(path);var summary=BinarySave.Summary(path);
            Check(summary.Day==12&&summary.PlaySeconds==3723.5&&summary.Emeralds==123.5f&&summary.Preview.Length==0,"summary reads without a renderer or script engine");
            using var loaded=City.Load(path);Check(loaded.PlaySeconds==city.PlaySeconds,"real playtime survives loading");
            // A v13 file has the same records, without the trailing empty priority and cat maps.
            var legacy=File.ReadAllBytes(path)[..^8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(legacy.AsSpan(4),13);
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(legacy.AsSpan(6),legacy.Length-42);
            System.Security.Cryptography.SHA256.HashData(legacy.AsSpan(42)).CopyTo(legacy.AsSpan(10,32));
            string oldPath=path+".old";
            try{File.WriteAllBytes(oldPath,legacy);using var older=City.Load(oldPath);Check(older.PlaySeconds==city.PlaySeconds&&older.ProfessionPriorities.Count==0,"v13 saves still load with default profession priorities");}
            finally{File.Delete(oldPath);}
            var bytes=File.ReadAllBytes(path);bytes[^1]^=1;File.WriteAllBytes(path,bytes);
            bool rejected=false;try{BinarySave.Summary(path);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"browser rejects checksum damage instead of displaying trusted statistics");
            city.SavePreviewPng=new byte[SavePreview.MaxBytes+1];rejected=false;
            try{city.Save(path);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"oversized preview rejected");
        }
        finally{File.Delete(path);}
        Console.WriteLine("PASS: save summaries, real playtime, headless saves, checksums and preview size bounds.");
    }
}
