set_script_type(eST_Building);

// cave: bound by Prefabs/cave.toml. Edit this component independently.
// Costs, inputs, outputs and statistics remain in TOML.
/** @param {ScriptBuilding} building @param {ScriptVillager} worker */
function onBuildingProduce(building, worker) {
    return building.produce(worker);
}
