set_script_type(eST_Building);

// well: bound by Prefabs/well.toml. Edit this component independently.
// Costs, inputs, outputs and statistics remain in TOML.
/** @param {ScriptBuilding} building @param {ScriptVillager} worker */
function onBuildingProduce(building, worker) {
    return false;
}

/** @param {ScriptBuilding} building */
function onBuildingDay(building) {
    building.repair(2);
}
