set_script_type(eST_Mod);
// Settlement dispatches onDawn only once per save day, at 06:00 (not midnight or 18:00).
function onDawn() {
    if(Math.random()<world.catSettings.dawnSpawnChance)world.queueSpawnCat();
}
