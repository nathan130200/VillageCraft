namespace VillageCraft;

static class ScriptChecks
{
    static void Check(bool condition,string message){if(!condition)throw new Exception("SCRIPT CHECK: "+message);}
    static City World()
    {
        var c=City.New(32,32);c.Rules.NightMonsters=false;
        foreach(ResourceType id in Catalog.Resources)c.Stock[id]+=10000-Catalog.Data.Game.InitialStock.GetValueOrDefault(id);return c;
    }
    public static void Run()
    {
        string folder=Path.Combine(Path.GetTempPath(),"villagecraft-events-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder,"mod.js"),"""
                // No registration: regular mod.
                let updates = 0;
                function onInit() {
                    world.addResource("Gold", 1);
                    if (world.getBuildings("cottage").length === 4 &&
                        world.getVillagers(v => v.profession === "farmer").length > 0)
                        world.addResource("Stone", 1);
                    world.queueSpawnBuilding("well", 9, 15, 0, "scenario well");
                    world.queueSpawnMonster("blaze", 6, 12, "scenario boss");
                }
                function onMonsterSpawned(m) {
                    if (m.hasTag("scenario") && world.getMonsters("tag:boss").length === 1 &&
                        world.getMonsters(x => x.id === m.id).length === 1 &&
                        world.getMonsters(m.id).length === 1)
                        world.addResource("Gold", 2);
                }
                function onBuildingPlaced(b) {
                    if (world.gerBuildings("scenario").length === 1 &&
                        world.getBuildings("prefab:well").length === 1)
                        world.addResource("Gold", 3);
                }
                function onBuildingDamaged(b, amount, source) {
                    world.addResource("Diamonds", amount);
                    world.queueDelete(b);
                }
                function onBuildingRemoved(b, reason) {
                    if (b.hasTag("scenario")) world.addResource("Tools", 1);
                }
                function onMonsterRemoved(m, reason) {
                    if (reason === "deleted") world.addResource("Iron", 1);
                }
                function onUpdate(dt) {
                    if (++updates === 1) world.addResource("Wool", 5);
                }
                function onShutdown() { world.addResource("Gold", 7); }
                """);
            File.WriteAllText(Path.Combine(folder,"ai.js"),"""
                set_script_type(eST_AI, "blaze");
                set_script_type(eST_Mod); // The first registration wins.
                function onInit() { world.addResource("Eggs", 1); }
                function onUpdate(dt) { world.addResource("Eggs", 1); }
                function onMonsterSpawned(m) { m.tags += " attached"; }
                function onMonsterUpdate(ctx, dt) {
                    ctx.self.tags = "scenario boss override";
                    ctx.moveTo(ctx.x + 1, ctx.z);
                }
                """);
            File.WriteAllText(Path.Combine(folder,"building.js"),"""
                set_script_type(eST_Building, "well");
                function onInit() { world.addResource("Eggs", 1); }
                function onUpdate(dt) { world.addResource("Eggs", 1); }
                function onBuildingDamaged(b, amount) { world.addResource("Iron", 9); }
                """);
            File.WriteAllText(Path.Combine(folder,"villager.js"),"""
                set_script_type(eST_AI, "farmer");
                function onVillagerUpdate(v, dt) { v.tags = "custom_ai"; v.moveTo(v.x + 1, v.z, dt); }
                """);
            Scripts.Load(folder);
            using var city=World();var runtime=city.Scripting;
            Check(city.Enemies.Count==0&&runtime.PendingCount>0,"onInit queues mutations");
            runtime.Flush();
            Check(city.Enemies.Count==1&&city.At(9,15)?.Tags=="scenario well","queued building and monster spawns");
            Check(city.Stock[ResourceType.Gold]==10006&&city.Stock[ResourceType.Stone]==10001-Catalog.Get("well").Cost[ResourceType.Stone],"callbacks and key/predicate queries");
            var building=city.At(9,15)!;var monster=city.Enemies[0];float initialX=monster.X;
            city.Update(.1f);
            Check(monster.X>initialX&&monster.Tags.Contains("override"),"registered AI overrides data behavior");
            Check(city.Citizens.Any(v=>v.Tags=="custom_ai"),"villager behavior override");
            Check(city.Stock[ResourceType.Eggs]==10004,"onInit and onUpdate run for AI and building scripts");
            Check(city.Stock[ResourceType.Wool]==10005,"mod onUpdate runs once per world update");
            city.DamageBuilding(building,4,monster);runtime.Flush();
            Check(city.Stock[ResourceType.Diamonds]==10004&&city.Stock[ResourceType.Iron]==10009&&!city.Buildings.Contains(building),"damage callback routing and queued deletion");
            var api=new ScriptWorld(city,runtime,new Jint.Engine());
            api.queueDeleteMonster(monster.Id);runtime.Flush();
            Check(city.Enemies.Count==0&&city.Stock[ResourceType.Iron]==10010,"monster deletion event");
            using var second=World();var other=second.Scripting;other.Flush();second.Update(.1f);
            Check(second.Stock[ResourceType.Wool]==10005,"per-world engines isolate script globals");
            float shutdownGold=city.Stock[ResourceType.Gold];city.Dispose();city.Dispose();
            Check(city.Stock[ResourceType.Gold]==shutdownGold+7&&!runtime.Queue(()=>{}),"onShutdown exactly once, closed queue");
            Check(runtime.Errors.Count==0&&other.Errors.Count==0,"event scripts execute without faults");
            // Attach behaviors to an individual instance; persisted by the binary save checks.
            Check(other.Attach(second.Enemies[0],"ai"),"instance behavior attachment accepted");other.Flush();
            Check(second.Enemies[0].Behavior=="ai","instance behavior applied at a safe point");
            Check(!other.Attach(second.Citizens[0],BuiltinScripts.Building),"wrong behavior type rejected");
            int accepted=0;for(int i=0;i<2100;i++)if(other.Queue(()=>{}))accepted++;
            Check(accepted==2048,"bounded mutation queue");other.Flush();Check(other.PendingCount==1792,"bounded queue drain");
        }
        finally{Catalog.Load();Directory.Delete(folder,true);}
    }
}
