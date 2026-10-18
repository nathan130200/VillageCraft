using System.Numerics;
using Tomlyn;
namespace VillageCraft;
static class Checks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAILED: "+message);}
    static City Rich(int size=32){var c=City.New(size,size,generateTerrain:false);c.Rules.BirthChance=0;foreach(ResourceType id in Catalog.Resources)c.Stock[id]=10000;c.ReconcileStock();
        var warehouse=c.Buildings.First(b=>b.Plan.Warehouse);
        foreach(var v in c.Citizens){var pos=v.Position;v.Position=new(warehouse.X,warehouse.Z);c.Give(v,ResourceType.Food,2);v.Position=pos;}
        return c;}
    static void Advance(City c,float seconds){for(int i=0;i<(int)(seconds*10);i++)c.Update(.1f);}
    static City Worker(string profession,string prefab="farm")
    {
        var c=Rich();c.Citizens.Clear();c.Citizens.Add(new(){Id=c.NextId++,Home=c.Buildings.First(b=>b.Plan.Homes>0).Id,Profession=profession,X=9,Z=15});
        Check(c.Place(prefab,9,15),"worker fixture placement");
        var worker=c.Citizens[0];var warehouse=c.Buildings.First(b=>b.Plan.Warehouse);worker.Position=new(warehouse.X,warehouse.Z);c.Give(worker,ResourceType.Food,2);worker.Position=new(9,15);
        var workplace=c.At(9,15)!;workplace.Priority=3;c.Assign();
        foreach(var item in workplace.Plan.Input){float amount=Math.Max(10,item.Value*3);c.Spend(new(){[item.Key]=amount});c.Receive(workplace,item.Key,amount);}
        return c;
    }
    public static void Demo(City c)
    {
        foreach(ResourceType id in Catalog.Resources)c.Stock[id]=3000;
        int dx=c.AnchorX-16,dz=c.AnchorZ-16;
        string[] ids=["farm","lumber","quarry","forge","well","market","sheep_pen","pig_pen","chicken_coop","barracks","archery","mage_hall","iron_foundry","snow_shrine","park","family_house","watchtower","cave"];
        for(int i=0;i<ids.Length;i++)
        {
            int x=i<9?8+i:8+i-9,z=i<9?15:17,rotation=i<9?0:2;
            if(c.At(x+dx,z+dz)!=null){x=17;z=7+i%8*2;rotation=1;}c.Place(ids[i],x+dx,z+dz,rotation);
        }
        foreach(var b in c.Buildings.Where(b=>b.Plan.Homes>0))while(c.Citizens.Count(v=>v.Home==b.Id)<b.Plan.Homes)c.Citizens.Add(new(){Id=c.NextId++,Home=b.Id,X=b.X,Z=b.Z});
        c.Assign();Advance(c,36);
        foreach(string type in new[]{"iron_golem","snow_golem"})c.Defenders.Add(new(){Type=type,X=dx+10+(type=="iron_golem"?0:2),Z=dz+16,Health=Catalog.Data.Units[type].Health});
        c.Day=89;c.DayTime=11.5f;c.Message="Developed village demo: cave expeditions and equipment upgrades are available.";
    }
    public static void Run()
    {
        Check(Catalog.Data.Prefabs.Count>=27&&Catalog.Data.Professions.Count>=11,"split TOML content");
        foreach(string plant in new[]{"wild_flower","wild_grass"})
            Check(Catalog.Data.Models[plant].Count==1&&Catalog.Data.Models[plant][0].Shape=="cross","plants use crossed faces without cuboid edges");
        foreach(var (model,color) in new[]{("market","#C6A04E"),("market","#A24C35"),("market","#5C9048"),("pen","#A88840"),("pen","#D7C46A"),("coop","#A88840"),("coop","#D7C46A"),("quarry","#5E6263")})
        {
            var material=Catalog.Data.Models[model].First(p=>p.Color==color);
            Check(material.Texture!=string.Empty&&Assets.Read(material.Texture) is {Length:>8},model+" detail has a resolvable texture");
        }
        var chicken=Catalog.Data.Models["chicken"];var skin=Assets.Read("entity/chicken.png")!;
        int skinWidth=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(skin.AsSpan(16,4)),skinHeight=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(skin.AsSpan(20,4));
        Check(chicken.Count==8&&chicken.Select(p=>string.Join(',',p.UvOrigin!)).Distinct().Count()==6,"chicken has distinct anatomical atlas regions");
        Check(chicken.All(p=>p.UvOrigin![0]+2*(p.UvSize![0]+p.UvSize[2])<=skinWidth&&p.UvOrigin[1]+p.UvSize[1]+p.UvSize[2]<=skinHeight),"chicken UVs remain inside the actual skin");
        Check(Catalog.Data.Models["lumber"].Where(p=>p.Color is "#397144" or "#4F8F42").All(p=>p.Texture=="block/oak_leaves.png"),"lumber foliage is textured");
        Check(City.New().Width==32&&City.New().Height==32,"default 32 map");
        var large=City.New(256,256,generateTerrain:false);Check(large.Width==256&&large.Height==256&&large.AnchorX==128,"explicit 256 map");
        Check(large.Land(255,255)&&!large.Land(256,255)&&large.At(128,128)?.Plan.Path==true,"map bounds and centered square");
        var timer=System.Diagnostics.Stopwatch.StartNew();Check(large.Route(new(0,0),new(255,255),false).Count==510,"A* across a large flat map");timer.Stop();
        var rectangle=City.New(96,160);Check(rectangle.Land(95,159)&&!rectangle.Land(96,159),"rectangular map");
        bool badMap=false;try{City.New(4,4096);}catch(ArgumentOutOfRangeException){badMap=true;}Check(badMap,"map size limits");
        var c=Rich();float cash=c.Stock[ResourceType.Emeralds];Check(!c.Place("farm",32,10)&&c.Stock[ResourceType.Emeralds]==cash,"out-of-bounds rejection without charge");
        Check(!c.Place("farm",9,15,2)&&c.Place("farm",9,15),"door alignment");Check(!c.Place("well",9,15),"overlap rejection");
        Check(c.Place("well",10,17,2)&&c.Place("well",15,9,3)&&c.Place("well",17,9,1),"all entrance rotations");
        var road=c.At(12,16)!;Check(c.Place("cobble_road",12,16)&&c.At(12,16)==road,"road resurfacing");
        Check(c.TravelSpeed(new(12,16),"Stonecutting")>1.5f&&c.TravelSpeed(new(12,16),"Farming")<1,"profession road buffs");
        var specialist=Worker("farmer");var citizen=Worker("citizen");float specialistFood=specialist.Stock[ResourceType.Food],citizenFood=citizen.Stock[ResourceType.Food];
        Advance(specialist,4.1f);Advance(citizen,4.1f);
        Check(specialist.Stock[ResourceType.Food]-specialistFood==8&&citizen.Stock[ResourceType.Food]-citizenFood==4,"citizen produces exactly half of specialist output: "+(specialist.Stock[ResourceType.Food]-specialistFood)+" / "+(citizen.Stock[ResourceType.Food]-citizenFood)+" "+specialist.Citizens[0].Task+" "+specialist.Citizens[0].Work);
        Check(citizen.Citizens[0].Profession=="citizen"&&citizen.Efficiency(citizen.Citizens[0])==.5f,"citizen remains generic while helping");
        var moving=Worker("farmer");var walker=moving.Citizens[0];walker.Position=new(12,16);var before=walker.Position;moving.Update(.1f);
        Check(walker.Position!=before&&Math.Abs(walker.Facing-City.Heading(walker.Position-before))<.0001f,"villager faces actual movement");
        Check(Math.Abs(City.Heading(-Vector2.UnitX)-MathF.PI/2)<.001f&&City.Heading(Vector2.UnitY)==0,"heading convention");
        var untrained=Worker("citizen");Check(untrained.Train(untrained.Citizens[0],"farmer")&&untrained.Efficiency(untrained.Citizens[0])==1,"profession training");
        var growth=Rich();growth.Citizens.Clear();var home=growth.Buildings.First(b=>b.Plan.Homes>0);growth.Citizens.Add(new(){Id=growth.NextId++,Home=home.Id,X=home.X,Z=home.Z});growth.Citizens[0].Inventory[ResourceType.Food]=2;growth.Buildings.First(b=>b.Plan.Warehouse).Enabled=false;growth.Place("well",9,15);growth.Tick();
        Check(growth.Growth>1,"idle citizens support population growth");
        var miner=Worker("miner","cave");var explorer=Worker("explorer","cave");float mined=miner.Stock[ResourceType.Diamonds],explored=explorer.Stock[ResourceType.Diamonds];
        Advance(miner,4.1f);Advance(explorer,4.1f);
        Check(miner.Stock[ResourceType.Gold]>10000&&miner.Stock[ResourceType.AncientDebris]>10000&&explorer.Stock[ResourceType.Diamonds]-explored>miner.Stock[ResourceType.Diamonds]-mined,"cave expeditions and profession bonuses");
        var equipment=Worker("smith","forge");Check(equipment.Upgrade("iron_armor")&&equipment.ArmorReduction==.15f,"armor upgrade");
        Check(!equipment.Upgrade("netherite_armor")&&equipment.Upgrade("iron_tools")&&equipment.ToolEfficiency>1,"ordered tools upgrades");
        float input=equipment.Stock[ResourceType.Wood],tools=equipment.Stock[ResourceType.Tools];equipment.Stock[ResourceType.Iron]=0;Advance(equipment,3);
        Check(equipment.Stock[ResourceType.Wood]==input&&equipment.Stock[ResourceType.Tools]==tools,"atomic recipe consumption");
        var husbandry=Worker("keeper","sheep_pen");Advance(husbandry,2.1f);husbandry.Day=2;husbandry.Tick();Check(husbandry.Animals.Count==3&&husbandry.Stock[ResourceType.Wool]>10000,"animal reproduction");
        var guardians=Worker("smith","iron_foundry");Advance(guardians,2.1f);guardians.Day=2;guardians.Tick();Check(guardians.Defenders.Count==1,"golem creation");
        var helper=Worker("citizen","iron_foundry");Advance(helper,4.1f);helper.Day=2;helper.Tick();Check(helper.Defenders.Count==0,"citizen guardian crafting is half rate");
        helper.DayTime=helper.Rules.DayLength*.3f;helper.Citizens[0].Position=new(9,15);Advance(helper,4.1f);helper.Day=5;helper.Tick();Check(helper.Defenders.Count==1,"citizen guardian credit carries forward");
        var armored=Worker("citizen");var bare=Worker("citizen");armored.ArmorLevel=1;
        foreach(var target in new[]{armored,bare}){target.Enemies.Add(new(){Type="zombie",X=9,Z=15.5f,Health=1000});target.Update(.1f);}
        Check(armored.Citizens[0].Health>bare.Citizens[0].Health,"armor reduces actual incoming damage");
        foreach(var language in L.Languages)foreach(var entry in language.Text)
        {
            try{System.Text.CompositeFormat.Parse(entry.Value);}catch(FormatException){throw new Exception("Invalid localization format: "+language.Id+" / "+entry.Key);}
        }
        Check(L.Current=="pt-BR"&&L.T("ui.options")=="Op\u00e7\u00f5es"&&L.Name(Catalog.Get("farm"))=="Planta\u00e7\u00e3o de trigo","Brazilian Portuguese is default");
        string farmerId="farmer";L.Set("en-US");Check(L.T("ui.options")=="Options"&&L.Name(Catalog.Data.Professions[farmerId])=="Farmer","English language switch");
        Check(L.T("missing-key")=="missing-key","missing translation fallback");L.Set("pt-BR");
        var identity=Rich();Check(identity.Citizens.All(v=>!string.IsNullOrWhiteSpace(v.FirstName)&&!string.IsNullOrWhiteSpace(v.Surname)),"random full names assigned");
        var carried=identity.Citizens[0];var supply=identity.Buildings.First(b=>b.Plan.Warehouse);carried.Position=new(supply.X,supply.Z);string permanent=carried.FullName;L.Set("en-US");Check(carried.FullName==permanent,"names independent of locale");L.Set("pt-BR");
        float villageGold=identity.Stock[ResourceType.Gold];Check(identity.Give(carried,ResourceType.Gold,2)&&carried.Inventory[ResourceType.Gold]==2&&identity.Stock[ResourceType.Gold]==villageGold-2,"inventory transfer conserves stock");
        Check(!identity.Give(carried,ResourceType.Gold,float.NaN)&&!identity.Give(carried,(ResourceType)255,1),"inventory rejects invalid transfers");
        var inspectUi=new GameUi();inspectUi.InspectAt(identity,carried.Position);Check(inspectUi.InspectedCitizen>=0&&inspectUi.Inspected==-1,"villager inspection takes priority over home tile");
        var toolsUi=new GameUi();Check(toolsUi.InspectMode&&!toolsUi.IsBuilding&&toolsUi.Selected==string.Empty,"empty inspect tool is default");
        toolsUi.SelectPrefab("farm");Check(toolsUi.IsBuilding,"selecting prefab activates build tool");
        toolsUi.InspectAt(identity,carried.Position);toolsUi.PinCurrent();
        toolsUi.InspectAt(identity,new(30,30));Check(!toolsUi.IsBuilding&&toolsUi.InspectedCitizen==-1&&toolsUi.PinnedCount==1,"world click closes transient panels and preserves pins");
        var inspectMonster=identity.SpawnMonster("zombie",25,25)!;toolsUi.InspectAt(identity,inspectMonster.Position);
        Check(toolsUi.InspectedMonster==inspectMonster.Id,"default tool inspects monsters");toolsUi.ResetPanels();
        Check(toolsUi.PinnedCount==0&&toolsUi.InspectedMonster==-1,"world replacement clears panel references");
        Check(GameUi.ClockFrame(0)==32&&GameUi.ClockFrame(.25f)==48&&GameUi.ClockFrame(.5f)==0&&GameUi.ClockFrame(.75f)==16&&GameUi.ClockFrame(.999f)==31,"clock follows simulation phase");
        for(int i=0;i<64;i++)Check(Assets.Read($"item/clock_{i:00}.png") is {Length:>0},"clock frame asset");
        using(var minuteClock=Rich())
        {
            minuteClock.Rules.NightMonsters=false;
            minuteClock.DayTime=minuteClock.Rules.DayLength*59/1440f;
            minuteClock.Update(minuteClock.Rules.DayLength/1440f*1.05f);
            Check(minuteClock.Hour==1&&minuteClock.Minute==0&&minuteClock.ClockTime=="01:00","minutes roll over into hours");
            minuteClock.DayTime=minuteClock.Rules.DayLength-.01f;int beforeDay=minuteClock.Day;minuteClock.Update(.02f);
            Check(minuteClock.Day==beforeDay+1&&minuteClock.ClockTime=="00:00","midnight rolls into the next day");
        }
        using(var pickWorld=Worker("smith","forge"))
        {
            var worker=pickWorld.Citizens[0];var visual=Scene.VillagerPosition(pickWorld,worker);
            var ray=new Raylib_cs.Ray{Position=new(visual.X*Scene.Scale,8,visual.Y*Scene.Scale),Direction=-Vector3.UnitY};
            toolsUi.InspectRay(pickWorld,ray,visual);
            Check(toolsUi.InspectedCitizen==worker.Id,"picking uses the villager rendered position outside workplace");
        }
        var hunt=Rich();hunt.Rules.NightMonsters=false;hunt.Rules.DayLength=600;
        hunt.Citizens.Clear();var hunter=new Creature{Type="zombie",X=5,Z=5,Health=55};hunt.Enemies.Add(hunter);
        var initial=hunter.Position;for(int frame=0;frame<1200;frame++)hunt.Update(1f/60);
        Check(Vector2.Distance(initial,hunter.Position)>5,"monster moves over many real-time frames");
        var chase=Worker("citizen");chase.Rules.NightMonsters=false;chase.Rules.DayLength=600;
        var prey=chase.Citizens[0];var pursuer=new Creature{Type="zombie",X=prey.X-5,Z=prey.Z,Health=55};chase.Enemies.Add(pursuer);
        Advance(chase,10);Check(prey.Health<50,"monster hunts and attacks a distant villager");
        var siege=Rich();siege.Citizens.Clear();siege.Rules.NightMonsters=false;
        var siegeVictim=siege.Buildings.Where(b=>!b.Plan.Path).OrderBy(b=>b.X).First();
        siege.Enemies.Add(new(){Type="zombie",X=siegeVictim.X-3,Z=siegeVictim.Z,Health=55});
        Advance(siege,8);Check(siegeVictim.Health<siegeVictim.Plan.Health,"monster approaches and damages a building");
        var clock=Rich();Advance(clock,12);Check(clock.Day==1&&clock.DayPhase>.3f&&clock.DayPhase<.33f,"ten-minute default day");
        var night=Rich();night.Day=6;night.DayTime=night.Rules.DayLength*.75f-.01f;night.Update(.02f);
        Check(night.IsNight&&night.Enemies.Count==3&&night.Enemies.All(e=>e.NightSpawn),"one nighttime encounter");
        night.Update(.01f);Check(night.Enemies.Count==3,"no repeated spawn");
        night.DayTime=night.Rules.DayLength-.01f;night.Update(.02f);Check(night.IsNight&&night.Enemies.Count>0,"night continues after midnight");night.DayTime=night.Rules.DayLength*.25f;night.Update(.01f);Check(!night.IsNight&&night.Enemies.Count>0,"night monsters remain after dawn for sunlight mechanics");
        using(var scriptCity=Rich())
        {
            var preyScript=scriptCity.Citizens[0];
            var scriptMob=scriptCity.SpawnMonster("creeper",preyScript.X,preyScript.Z)!;scriptMob.BrainTime=.8f;
            scriptCity.Scripting.UpdateMonster(scriptMob,new MobScriptContext(scriptCity,scriptMob,.2f),.2f);
            Check(scriptMob.Health<=0&&scriptMob.BrainTime>=.99f,"direct JavaScript creeper binding");
            var archerScript=scriptCity.SpawnMonster("skeleton",preyScript.X,preyScript.Z)!;float previousHealth=preyScript.Health;
            scriptCity.Scripting.UpdateMonster(archerScript,new MobScriptContext(scriptCity,archerScript,.2f),.2f);
            Check(preyScript.Health<previousHealth,"direct JavaScript ranged binding");
        }
        var blast=Rich();blast.Citizens.Clear();var victimBlast=blast.Buildings.First(b=>!b.Plan.Path);
        float intact=victimBlast.Health;blast.Enemies.Add(new(){Type="creeper",X=victimBlast.X,Z=victimBlast.Z,Health=35,BrainTime=1});blast.Update(.1f);
        Check(victimBlast.Health<intact&&blast.Enemies.Count==0,"explosion damages buildings");
        var hookCity=Rich();Check(hookCity.Place("well",9,15),"script building fixture");var hookWell=hookCity.At(9,15)!;
        hookWell.Health=100;hookCity.Tick();Check(hookWell.Health==102,"custom building hook");
        var noNight=Rich();noNight.Rules.NightMonsters=false;noNight.DayTime=noNight.Rules.DayLength*.8f;noNight.Update(.01f);Check(noNight.Enemies.Count==0,"disable nighttime spawns");
        var raid=Rich();raid.Rules.RaidInterval=5;raid.Rules.MaxRaiders=4;raid.Day=4;raid.Tick();Check(raid.Enemies.Count==4&&raid.Waves==1&&raid.Enemies.Any(e=>e.Type=="nether_warlord"),"raid rules and cap");
        raid.SpawnWave();Check(raid.Enemies.Count==4,"concurrent hostile cap");raid.Rules.RaidsEnabled=false;raid.Day=9;raid.Tick();Check(raid.Waves==2,"disable raids");
        var defense=Worker("warrior","barracks");defense.Enemies.Add(new(){Type="zombie",X=9,Z=15.4f,Health=20});Advance(defense,2);Check(defense.Kills==1,"warrior combat");
        foreach(var (profession,building) in new[]{("warrior","barracks"),("archer","archery"),("mage","mage_hall")})
        {
            var patrol=Worker(profession,building);patrol.Rules.NightMonsters=false;patrol.DayTime=patrol.Rules.DayLength*.75f;
            var guard=patrol.Citizens[0];guard.HasWorked=true;var patrolStart=guard.Position;
            Advance(patrol,2);Check(guard.Task==TaskId.NightPatrol&&guard.Position!=patrolStart,profession+" night patrol");
            patrol.Enemies.Add(new(){Id=patrol.NextId++,Type="zombie",X=guard.X+.1f,Z=guard.Z,Health=1});
            Advance(patrol,.1f);Check(patrol.Kills==1,profession+" engages enemies during night patrol");
        }
        var destruction=Rich();destruction.Citizens.Clear();var victim=destruction.At(12,15)!;victim.Health=1;destruction.Enemies.Add(new(){Type="zombie",X=12,Z=14.6f,Health=55});destruction.Update(.1f);Check(!destruction.Buildings.Contains(victim),"building destruction");
        foreach(string icon in Assets.Icons.Values)Check(Assets.Read(icon) is {Length:>8},"resource icon "+icon);
        foreach(string invalid in new[]{"../escape","..\\escape","CON","options",string.Empty,"trailing."}){bool rejected=false;try{SaveStore.PathFor(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"save filename boundary");}
        var ui=new GameUi();ui.SetSpeed(3);ui.Escape();Check(ui.MenuOpen&&ui.Speed==4&&!ui.ExitRequested,"menu and speed state");
        ui.SetSpeed(4);Check(ui.Speed==16&&!ui.Paused,"fourth speed is four times third");
        var scaleOptions=new GraphicsOptions();Check(scaleOptions.UiScale==1,"default UI scale");scaleOptions.UiScale=float.NaN;scaleOptions.Validate();Check(scaleOptions.UiScale==1,"invalid UI scale fallback");
        string save=Path.Combine(Path.GetTempPath(),"villagecraft-"+Guid.NewGuid()+".vcs");
        try
        {
            var saved=City.StartVillage(256,128);foreach(ResourceType id in Catalog.Resources)saved.Stock[id]=3000;saved.Citizens[0].Facing=1.234f;saved.ArmorLevel=1;saved.Growth=1.5f;saved.Rules.RaidInterval=37;
            saved.Citizens[0].FirstName="Ana";saved.Citizens[0].Surname="Silva";saved.Citizens[0].Inventory[ResourceType.Diamonds]=3;
            saved.Citizens[0].Tags="founder worker";saved.Buildings[0].Tags="plaza";
            saved.LastNight=saved.Day;saved.Rules.NightCount=5;saved.Enemies.Add(new(){Type="creeper",X=10,Z=10,Health=35,NightSpawn=true,BrainTime=.45f});
            saved.Enemies[0].Tags="boss";saved.Enemies[0].Behavior="creeper";saved.Save(save);byte[] bytes=File.ReadAllBytes(save);Check(bytes.AsSpan(0,4).SequenceEqual("VCTY"u8),"save is native binary");
            var loaded=City.Load(save);Check(loaded.Terrain.Elevation.SequenceEqual(saved.Terrain.Elevation)&&loaded.Terrain.Water.SequenceEqual(saved.Terrain.Water)&&loaded.TutorialEnabled==saved.TutorialEnabled&&loaded.PeaceDays==5,"terrain and tutorial round trip");Check(loaded.Enemies[0].Id>0&&loaded.Enemies[0].Tags=="boss"&&loaded.Enemies[0].Behavior=="creeper"&&loaded.Citizens[0].Tags=="founder worker"&&loaded.Buildings[0].Tags=="plaza","entity IDs, tags and attached behaviors persist");Check(loaded.Citizens[0].FullName=="Ana Silva"&&loaded.Citizens[0].Inventory[ResourceType.Diamonds]==3,"citizen identity and inventory persist");Check(loaded.LastNight==saved.Day&&loaded.Rules.NightCount==5&&loaded.Enemies[0].NightSpawn&&loaded.Enemies[0].BrainTime==.45f,"night and script state round trip");Check(loaded.Width==256&&loaded.Height==128&&loaded.Citizens[0].Facing==1.234f&&loaded.Growth==1.5f&&loaded.ArmorLevel==1&&loaded.Rules.RaidInterval==37&&loaded.Citizens[0].Profession==saved.Citizens[0].Profession,"binary save round trip");
            var unsupported=(byte[])bytes.Clone();unsupported[4]=9;File.WriteAllBytes(save,unsupported);bool rejected=false;try{City.Load(save);}catch(InvalidDataException){rejected=true;}Check(rejected,"older beta formats are rejected clearly");
            bytes[^1]^=1;File.WriteAllBytes(save,bytes);bool corrupt=false;try{City.Load(save);}catch(InvalidDataException){corrupt=true;}Check(corrupt,"binary checksum rejects corruption");
            File.WriteAllBytes(save,bytes[..12]);corrupt=false;try{City.Load(save);}catch(InvalidDataException){corrupt=true;}Check(corrupt,"truncated save rejected");
        }
        finally{File.Delete(save);}
        string name="selftest-"+Guid.NewGuid().ToString("N"),copy=name+"-copy";
        try
        {
            var named=City.New(64,96);named.Save(SaveStore.PathFor(name));named.Save(SaveStore.PathFor(copy));
            Check(SaveStore.List().Contains(name)&&SaveStore.List().Contains(copy)&&City.Load(SaveStore.LoadPathFor(copy)).Height==96,"independent named binary saves");
        }
        finally{File.Delete(SaveStore.PathFor(name));File.Delete(SaveStore.PathFor(copy));}
        var displayPreferences=TomlSerializer.Deserialize<GraphicsOptions>(TomlFile.Serialize(new GraphicsOptions{DisplayMode=2,DisplayWidth=1920,DisplayHeight=1080}));
        Check(displayPreferences?.DisplayMode==2&&displayPreferences.DisplayWidth==1920&&displayPreferences.DisplayHeight==1080,"display preferences persist");
        var preferences=TomlSerializer.Deserialize<GraphicsOptions>(TomlFile.Serialize(new GraphicsOptions{Quality=1,Fps=120,Language="en-US"}));Check(preferences?.Quality==1&&preferences.Fps==120&&preferences.Language=="en-US","TOML graphics and language preferences");
        string content=Path.Combine(Path.GetTempPath(),"villagecraft-content-"+Guid.NewGuid());
        try
        {
            string source=Path.Combine(AppContext.BaseDirectory,"Content");foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){string dest=Path.Combine(content,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(dest)!);File.Copy(file,dest);}
            string custom=Path.Combine(content,"Prefabs","custom.toml");File.WriteAllText(custom,TomlFile.Serialize(new Plan{Id="custom_house",Name="TOML house",Category="Housing",Model="cottage",Homes=9,Cost=new(){[ResourceType.Emeralds]=1}}));
            File.Copy(Path.Combine(source,"Templates","profession.toml.example"),Path.Combine(content,"Professions","herbalist.toml"));
            Catalog.Load(content);var modded=Rich();
            Check(Catalog.Data.Professions.ContainsKey("herbalist")&&L.Name(Catalog.Data.Professions["herbalist"])=="Herbalist","new profession auto-loads with name fallback");
            var trainee=modded.Citizens[0];Check(modded.Train(trainee,"herbalist")&&modded.Efficiency(trainee)==1.1f,"new profession trainable without code");
            var herbalistVillage=City.StartVillage(32,32);foreach(ResourceType id in Catalog.Resources)herbalistVillage.Stock[id]=1000;
            var customSpecialist=herbalistVillage.Citizens[0];herbalistVillage.Train(customSpecialist,"herbalist");
            Check(customSpecialist.Work>=0&&herbalistVillage.Buildings.First(b=>b.Id==customSpecialist.Work).Plan.Job=="Farming","custom profession matches existing workplace activity");Check(modded.Place("custom_house",9,15)&&modded.Capacity==25,"new TOML prefab without code changes");
            File.WriteAllText(custom,"Id = [ broken");Check(Catalog.Get("custom_house").Homes==9,"catalog is cached after startup");
            bool rejected=false;try{Catalog.Load(content);}catch(InvalidDataException e){rejected=e.Message.Contains("custom.toml");}Check(rejected,"TOML diagnostics include source filename");
        }
        finally{Catalog.Load();if(Directory.Exists(content))Directory.Delete(content,true);}
        string scriptFolder=Path.Combine(Path.GetTempPath(),"villagecraft-js-"+Guid.NewGuid());Directory.CreateDirectory(scriptFolder);
        try
        {
            File.WriteAllText(Path.Combine(scriptFolder,"loop.js"),"function onUpdate(dt) { while(true) {} }");
            File.WriteAllText(Path.Combine(scriptFolder,"binding.js"),"set_script_type(eST_Building); function onBuildingWork(building, worker) { let person = world.villager(worker.id); person.heal(4); world.addResource('Gold', 2); person.give('Gold', 1); building.repair(3); }");
            File.WriteAllText(Path.Combine(scriptFolder,"customhunter.js"),"set_script_type(eST_AI, 'zombie'); function onMonsterUpdate(ctx, dt) { const p = world.getVillagers()[0]; const d = (p.x-ctx.x)**2 + (p.z-ctx.z)**2; if(d <= ctx.range**2) ctx.attackVillager(p.id); else ctx.moveTo(p.x,p.z); }");
            Scripts.Load(scriptFolder);
            var customHunt=Rich();var chosenPerson=customHunt.Citizens[0];customHunt.Citizens.RemoveAll(v=>v!=chosenPerson);customHunt.Rules.NightMonsters=false;
            var customMob=new Creature{Type="zombie",X=chosenPerson.X-2,Z=chosenPerson.Z,Health=55};customHunt.Enemies.Add(customMob);
            Catalog.Data.Units["zombie"].Behavior="customhunter";Advance(customHunt,5);
            Check(chosenPerson.Health<50&&customMob.X>chosenPerson.X-2,"JS selects and pursues its own live target");
            var bound=Rich();var boundPerson=bound.Citizens[0];boundPerson.Health=30;var boundBuilding=bound.Buildings.First(b=>!b.Plan.Path);boundBuilding.Health=50;float gold=bound.Stock[ResourceType.Gold];
            var boundStore=bound.Buildings.First(b=>b.Plan.Warehouse);boundPerson.Position=new(boundStore.X,boundStore.Z);
            boundBuilding.Behavior="binding";bound.Scripting.BuildingWork(boundBuilding,boundPerson);bound.Scripting.Flush();
            Check(boundPerson.Health==34&&boundBuilding.Health==53&&bound.Stock[ResourceType.Gold]==gold+1&&boundPerson.Inventory[ResourceType.Gold]==1,"live JS game methods mutate real simulation");
            var limit=System.Diagnostics.Stopwatch.StartNew();
            bound.Scripting.Tick(.1f);Check(bound.Scripting.Errors.Any(e=>e.StartsWith("Script loop:"))&&limit.ElapsedMilliseconds<2000,"runaway scripts contained");
        }
        finally{Directory.Delete(scriptFolder,true);Catalog.Load();}
        using(var queuedWorld=Rich())
        {
            var component=queuedWorld.Scripting;var target=queuedWorld.Buildings.First(b=>!b.Plan.Path);
            queuedWorld.DamageBuilding(target,10000);component.Flush();
            Check(!queuedWorld.Buildings.Contains(target),"queued lethal damage resolves before saving");City.Validate(queuedWorld);
        }
        OutcomeChecks.Run();
        Beta105Checks.Run();
        TerrainChecks.Run();
        ScriptChecks.Run();
        AiChecks.Run();
        LogisticsChecks.Run();
        PopulationChecks.Run();
        VillageImprovementsChecks.Run();
        ProfilerChecks.Run();
        AssetCacheChecks.Run();
        LocalizationChecks.Run();
        NightChecks.Run();
        LoggingChecks.Run();
        Assets.Dispose();Console.WriteLine($"PASS: pt-BR/en-US, direct JS bindings, villager names/inventory/inspection, beta v11 saves, seeded terrain, forestry and tutorial and lifecycle/events/queries/queues, split TOML/cache, 256 maps/A* ({timer.ElapsedMilliseconds} ms), facing, citizen 50%, growth, training, cave loot, equipment, simulation and binary save integrity.");
    }
}
