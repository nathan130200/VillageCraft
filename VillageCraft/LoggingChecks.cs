namespace VillageCraft;
static class LoggingChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("LOGGING: "+message);}
    public static void Run()
    {
        string token="logging-"+Guid.NewGuid().ToString("N"),folder=Path.Combine(Path.GetTempPath(),token);Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder,"logging_example.js"),$$"""
                function onInit() {
                    world.log("{{token}} info");
                    world.warn("{{token}} warning");
                    world.error("{{token}} error");
                    world.log(world.translate("ui.options"));
                    world.log(world.format("format.resource_stock", ["Milk", 2.5]));
                }
                function onUpdate(dt) { throw new Error("{{token}} critical script exception"); }
                """);
            Scripts.Load(folder);using var city=City.New(generateTerrain:false);city.Update(.01f);
            Check(city.Scripting.Errors.Count==1,"script exceptions are tracked");
            var api=new ScriptGame(city,"long_log");api.log(token+new string('x',300));
            GameLog.Write(GameLogLevel.Critical,token+" critical");
            using var stream=new FileStream(GameLog.FilePath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);using var reader=new StreamReader(stream);string contents=reader.ReadToEnd();
            Check(contents.Contains(token+" info")&&contents.Contains(token+" warning")&&contents.Contains(token+" error"),"script messages always reach file at every severity");
            Check(contents.Contains("Script [logging_example]")&&contents.Contains(token+" critical script exception")&&contents.Contains(token+new string('x',300)),"script identity, exception and long messages are retained");
            Check(contents.Contains("[Critical]")&&contents.Contains(token+" critical"),"critical errors are flushed to the run log");
        }
        finally{Catalog.Load();Directory.Delete(folder,true);}
        Console.WriteLine("PASS: always-on script logs, severity, script identity, exceptions and file flushing.");
    }
}
