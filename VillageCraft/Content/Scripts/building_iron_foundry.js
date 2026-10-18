set_script_type(eST_Building);

// iron_foundry: bound by Prefabs/iron_foundry.toml. Edit this component independently.
// Costs, inputs, outputs and statistics remain in TOML.
/** @param {ScriptBuilding} building @param {ScriptVillager} worker */
function onBuildingProduce(building, worker) {
    return building.rewardWork(worker);
}

/** @param {ScriptBuilding} building */
function onBuildingDay(building) {
    if (!building.worked || world.day % 3 !== 0) return;
    building.serviceCredit += building.efficiency;
    building.createGuardian();
}
