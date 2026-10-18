set_script_type(eST_Building);

// family_house: bound by Prefabs/family_house.toml. Edit this component independently.
// Costs, inputs, outputs and statistics remain in TOML.
/** @param {ScriptBuilding} building @param {ScriptVillager} worker */
function onBuildingProduce(building, worker) {
    return false;
}
