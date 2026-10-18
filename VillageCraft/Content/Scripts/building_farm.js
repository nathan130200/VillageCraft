set_script_type(eST_Building);

// farm: bound by Prefabs/farm.toml. Edit this component independently.
// Costs, inputs, outputs and statistics remain in TOML.
/** @param {ScriptBuilding} building @param {ScriptVillager} worker */
function onBuildingProduce(building, worker) {
    return building.produce(worker);
}
