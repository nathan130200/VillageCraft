using System.Text.RegularExpressions;
namespace VillageCraft;
static class LocalizationChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("LOCALIZATION: "+message);}
    public static void Run()
    {
        string source=Path.Combine(AppContext.BaseDirectory,"Content","Locales"),folder=Path.Combine(Path.GetTempPath(),"villagecraft-locales-"+Guid.NewGuid());Directory.CreateDirectory(folder);string previous=L.Current;
        try
        {
            foreach(string file in Directory.GetFiles(source,"*.toml"))
            {
                File.Copy(file,Path.Combine(folder,Path.GetFileName(file)));
                bool inText=false;string prior=string.Empty;
                foreach(string line in File.ReadLines(file))
                {
                    if(line=="[Text]"){inText=true;continue;}
                    if(inText&&line.TrimStart().StartsWith('"'))Check(prior.TrimStart().StartsWith('#'),"each translation has an immediately preceding usage description");
                    if(!string.IsNullOrWhiteSpace(line))prior=line;
                }
            }
            Check(L.Languages.SelectMany(l=>l.Text.Keys).All(k=>Regex.IsMatch(k,@"^[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+$")),"catalogs use stable IDs instead of English sentence keys");
            File.WriteAllText(Path.Combine(folder,"es-ES.toml"),"""
                Id = "es-ES"
                Name = "Español"
                [Text]
                # Options button in the game menu.
                "ui.options" = "Opciones"
                # Resource storage: {0} is resource name; {1} is quantity (reordered here).
                "format.resource_stock" = "{1:0.##} unidades de {0}"
                """
            );
            L.Load(folder);L.Set("es-ES");
            Check(L.T("ui.options")=="Opciones"&&L.F("format.resource_stock","Milk",2.5f)=="2,5 unidades de Milk","new locale supports stable IDs, reordering and culture-specific numbers");
            Check(L.T("people.reassign_professions")=="Reassign professions","missing translations fall back to English");
            Check(L.T("mod.untranslated")=="mod.untranslated","unknown IDs remain visible for authoring");
            L.Set("pt-BR");Check(L.Ui("Options")=="Opções"&&L.Category("Roads")=="Caminhos"&&L.Task(TaskId.RepairingBuilding)=="Reparando construção","dynamic UI/category/task IDs resolve in Portuguese");
            File.WriteAllText(Path.Combine(folder,"es-ES.toml"),"""
                Id = "es-ES"
                Name = "Español"
                [Text]
                # Intentionally drops argument 0; validation must reject it.
                "format.resource_stock" = "{1}"
                """);
            bool rejected=false;try{L.Load(folder);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"a translation cannot silently drop a numbered argument");

        }
        finally{L.Load(source);L.Set(previous);Directory.Delete(folder,true);}
        Console.WriteLine("PASS: documented localization IDs, partial third language, fallbacks and reordered/culture-aware placeholders.");
    }
}
