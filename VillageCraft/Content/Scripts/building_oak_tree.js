set_script_type(eST_Building);

// oak_tree: bound by Prefabs/oak_tree.toml. Edit this component independently.
// Costs, inputs, outputs and statistics remain in TOML.
/** @param {ScriptBuilding} building @param {ScriptVillager} worker */
function onBuildingProduce(building, worker) {
    return false;
}

/** @param {ScriptBuilding} building */
function onBuildingDay(building) {
    if (building.growthDays > 0) building.grow();
}
