set_script_type(eST_Building);

/** @param {ScriptBuilding} building */
function onBuildingDay(building) {
    building.repair(2);
}
